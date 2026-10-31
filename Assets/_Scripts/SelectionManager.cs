using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(1)]
public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance { get; private set; }

    [SerializeField] private Camera Cam;
    [SerializeField] private LayerMask SelectableLayers;
    [SerializeField] private LayerMask GroundLayers;
    [SerializeField] private RectTransform SelectionBox;
    [SerializeField] private GameObject SelectionInfo;
    [SerializeField] private TextMeshProUGUI SelectedUnitsTmp, GoldCostTmp, HalfSelected;
    [SerializeField] private GameEvent OnSelect;

    public readonly HashSet<Node> SelectedNodes = new();
    private readonly Dictionary<Transform, Node> _nodes = new();
    private Vector2 _selectionBoxOrigin;
    private bool _isDragging;
    private EventSystem _eventSystem;
    private CursorManager _cursorManager;
    private GoldManager _goldManager;
    private ulong _networkId;
    private int _selectedUnitsCount;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void OnNodeSpawn(Component sender, object data)
    {
        _nodes.Add((Transform)sender, (Node)data);
    }

    private void Start()
    {
        _eventSystem = EventSystem.current;
        _cursorManager = CursorManager.Instance;
        _goldManager = GoldManager.Instance;
        _networkId = NetworkManager.Singleton.LocalClientId;
    }

    private void FixedUpdate()
    {
        DrawDeploymentLine();
        UpdateSelectionInfo();
        if (!_isDragging) return;
        HandleBoxVisuals();
    }

    private void UpdateSelectionInfo()
    {
        if (SelectedNodes.Count > 0)
        {
            SelectionInfo.transform.position = Input.mousePosition;
            var goldCost = SelectedNodes.Count * 2;
            GoldCostTmp.color = goldCost > _goldManager.GetGold(_networkId)
                ? Color.red
                : Color.white;
            SelectedUnitsTmp.color = _selectedUnitsCount < 1 ? Color.red : Color.white;
            HalfSelected.color = _selectedUnitsCount < 2 ? Color.red : Color.white;
            
            GoldCostTmp.text = goldCost.ToString();
            _selectedUnitsCount = 0;
            foreach (var node in SelectedNodes)
            {
                _selectedUnitsCount += node.CurrentUnits.Value;
            }
            SelectedUnitsTmp.text = _selectedUnitsCount.ToString();
            var half = Mathf.FloorToInt(_selectedUnitsCount * 0.5f);
            HalfSelected.text = half.ToString();
            
            if (!SelectionInfo.activeSelf)
            {
                SelectionInfo.SetActive(true);
            }
        }
        else if (SelectionInfo.activeSelf)
        {
            SelectionInfo.SetActive(false);
        }
    }

    public void OnLeftClick(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (_eventSystem.IsPointerOverGameObject()) return;
            OnLeftMouseDown();
        }

        if (context.canceled && _isDragging)
        {
            OnLeftMouseUp(Keyboard.current.leftCtrlKey.isPressed);
        }
    }
    
    private void OnLeftMouseDown()
    {
        _selectionBoxOrigin = Input.mousePosition;
        _isDragging = true;
    }

    private void OnLeftMouseUp(bool ctrl)
    {
        Node clickedNode = null;

        if (Physics.Raycast(Cam.ScreenPointToRay(Input.mousePosition), out var hit, Mathf.Infinity, SelectableLayers))
        {
            clickedNode = hit.collider.GetComponent<Node>();
        }
        else if (!ctrl)
        {
            DeSelectEverything();
        }

        if (!HandleBoxSelection(ctrl) && clickedNode != null)
        {
            if (ctrl)
            {
                if (SelectedNodes.Contains(clickedNode))
                {
                    DeSelect(clickedNode);
                }
                else
                {
                    Select(clickedNode);
                }
            }
            else
            {
                DeSelectEverything();
                Select(clickedNode);
            }
        }

        var firstSelectedNode = SelectedNodes.FirstOrDefault();
        if (!ctrl && firstSelectedNode != null)
        {
            SoundFXManager.Instance.PlaySfx(firstSelectedNode.RunTimeSettings.SelectionSfx,
                firstSelectedNode.transform.position);
        }

        _isDragging = false;
    }

    private bool _linesDrawn;
    
    private void DrawDeploymentLine()
    {
        if (_linesDrawn && SelectedNodes.Count < 1)
        {
            DisableLines();
        }
        if (SelectedNodes.Count < 1) return;

        if (Physics.Raycast(Cam.ScreenPointToRay(Input.mousePosition), out var hit, Mathf.Infinity, SelectableLayers))
        {
            foreach (var node in SelectedNodes)
            {
                if (node.transform == hit.transform) continue;
                var nodePos = node.transform.position;
                var hitPos = hit.transform.position;
                node.UpdateDeploymentLr(true);
                var dir = (nodePos - hitPos).normalized;
                var targetPos = hitPos + (dir * 3);
                targetPos += (Vector3.up * 1.4f);
                var points = GenerateCurvedLine(nodePos + (Vector3.up * 4.5f), targetPos, 5, 10);
                node.UpdateDeploymentLr(points);
            }
            _linesDrawn = true;
        }
        else
        {
            DisableLines();
        }
    }

    private void DisableLines()
    {
        foreach (var node in SelectedNodes)
        {
            node.UpdateDeploymentLr(false);
        }
        _linesDrawn = false;
    }

    private static Vector3[] GenerateCurvedLine(Vector3 start, Vector3 end, float curveHeight = 2f, int resolution = 20)
    {
        Vector3[] points = new Vector3[resolution + 1];

        for (int i = 0; i <= resolution; i++)
        {
            float t = i / (float)resolution; // Normalized value between 0 and 1

            // Linear interpolation between start and end
            Vector3 point = Vector3.Lerp(start, end, t);

            // Apply parabolic curve
            float height = Mathf.Sin(t * Mathf.PI) * curveHeight; // Makes it rise in the middle
            point.y += height;

            points[i] = point;
        }

        return points;
    }

    private bool HandleBoxSelection(bool ctrl)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            SelectionBox,
            _selectionBoxOrigin,
            null,
            out Vector2 localOrigin
        );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            SelectionBox,
            Input.mousePosition,
            null,
            out Vector2 localEnd
        );

        var selectionRect = new Rect(
            Mathf.Min(localOrigin.x, localEnd.x),
            Mathf.Min(localOrigin.y, localEnd.y),
            Mathf.Abs(localEnd.x - localOrigin.x),
            Mathf.Abs(localEnd.y - localOrigin.y)
        );

        if (!ctrl) DeSelectEverything();

        var selectedAny = false;
        foreach (var nodeT in _nodes)
        {
            Vector2 screenPoint = Cam.WorldToScreenPoint(nodeT.Key.position);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                SelectionBox,
                screenPoint,
                null,
                out Vector2 localPoint
            );

            if (selectionRect.Contains(localPoint))
            {
                Select(nodeT.Value);
                selectedAny = true;
            }

            SelectionBox.sizeDelta = Vector2.zero;
        }

        return selectedAny;
    }

    private void OnSelectionUpdate()
    {
        var cursorMode = SelectedNodes.Count > 0 ? CursorType.NodeSelected : CursorType.Default;
        _cursorManager.SetCursor(cursorMode);
        OnSelect.Raise();
    }

    private void Select(Node node)
    {
        if (!node.IsNodeOwner()) return;

        node.OnSelect();
        SelectedNodes.Add(node);
        OnSelectionUpdate();
    }

    public void DeSelect(Node node)
    {
        node.OnDeSelect();
        SelectedNodes.Remove(node);
        OnSelectionUpdate();
    }

    public void DeSelectEverything()
    {
        DisableLines();
        foreach (var selected in SelectedNodes)
        {
            selected.OnDeSelect();
        }

        SelectedNodes.Clear();
        OnSelectionUpdate();
    }

    private void HandleBoxVisuals()
    {
        var pointA = _selectionBoxOrigin;
        var pointB = Input.mousePosition;

        var minX = Mathf.Min(pointA.x, pointB.x);
        var minY = Mathf.Min(pointA.y, pointB.y);
        var width = Mathf.Abs(pointB.x - pointA.x);
        var height = Mathf.Abs(pointB.y - pointA.y);

        SelectionBox.position = new Vector2(minX + width / 2, minY + height / 2);

        SelectionBox.sizeDelta = new Vector2(width, height);
    }
}