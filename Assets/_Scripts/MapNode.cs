using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MapNode : NetworkBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, IScrollHandler
{
    [SerializeField] private TextMeshProUGUI NodeCountText;
    [SerializeField] private Image OwnerColor, ShipImage;
    public bool IsWater;

    public NetworkVariable<int> BuildingNodesCount = 
        new NetworkVariable<int>(writePerm: NetworkVariableWritePermission.Server);

    public NetworkVariable<PlayerColor> Color =
        new NetworkVariable<PlayerColor>(writePerm: NetworkVariableWritePermission.Server);

    public NetworkVariable<int>
        MinimumSize = new NetworkVariable<int>(writePerm: NetworkVariableWritePermission.Server);

    private bool _isDragging;
    private bool _isHovering;
    
    public override void OnNetworkSpawn()
    {
        transform.SetParent(GameObject.Find("MapEditor").transform, false);

        if (!IsServer)
        {
            enabled = false;
        }
        
        BuildingNodesCount.OnValueChanged += UpdateNodesUI;
        Color.OnValueChanged += UpdateColor;
        MinimumSize.OnValueChanged += UpdateMinimumSize;
        
        if (!IsServer)
        {
            InitData();
        }

        if (IsServer)
        {
            MinimumSize.Value = Math.Max(MinimumSize.Value, 4);
            BuildingNodesCount.Value = IsWater ? 1 : 2;
        }
    }

    public void InitData()
    {
        UpdateMinimumSize(-10, MinimumSize.Value);
        UpdateNodesUI(-10, BuildingNodesCount.Value);
        
        if (IsWater) return;
        UpdateColor(PlayerColor.None, Color.Value);
    }

    private void UpdateMinimumSize(int previousValue, int newValue)
    {
        var normalizedValue = Mathf.InverseLerp(10, 15, newValue);
        var scaledValue = Mathf.Lerp(4, 7.5f, normalizedValue);

        transform.localScale = new Vector2(scaledValue, scaledValue);
    }
    
    public void OnPlayerColorChanged(Component sender, object data)
    {
        var colors = (Tuple<PlayerColor, PlayerColor>)data;
        if (colors.Item1 == Color.Value)
        {
            Color.Value = colors.Item2;
        }
    }

    private void OnDisable()
    {
        BuildingNodesCount.OnValueChanged -= UpdateNodesUI;
        Color.OnValueChanged -= UpdateColor;
        MinimumSize.OnValueChanged -= UpdateMinimumSize;
    }

    private void UpdateColor(PlayerColor previousvalue, PlayerColor newvalue)
    {
        var clr = ColorManager.Instance.Colors[newvalue].UIColor;
        OwnerColor.color = clr;
        NodeCountText.color = clr;
    }

    private void UpdateNodesUI(int previousvalue, int newvalue)
    {
        if (IsWater)
        {
            BuildingNodesCount.Value = newvalue >= 2 ? 2 : 1;
            ShipImage.enabled = newvalue >= 2;
        }
        else
        {
            NodeCountText.text = newvalue.ToString();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (Keyboard.current.altKey.isPressed)
        {
            _isDragging = true;
        }
    }

    private int _playerIndex;
    public void OnPointerUp(PointerEventData eventData)
    {
        if (_isDragging)
        {
            _isDragging = false;
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if(Keyboard.current.shiftKey.isPressed && !IsWater)
            {
                Color.Value = PlayerColor.None;
            }
            else
            {
                BuildingNodesCount.Value = Mathf.Clamp(BuildingNodesCount.Value - 1, 1, 4);
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            if(Keyboard.current.shiftKey.isPressed && !IsWater)
            {
                Color.Value = PlayerManager.Instance.Players[_playerIndex].Color.Value;
                _playerIndex = _playerIndex == PlayerManager.Instance.Players.Count - 1 ? 0 : _playerIndex + 1;
            }
            else
            {
                BuildingNodesCount.Value = Mathf.Clamp(BuildingNodesCount.Value + 1, 1, 4);
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
    }

    private void Update()
    {
        if (_isHovering && Keyboard.current.deleteKey.wasPressedThisFrame)
        {
            Destroy(gameObject);
        }
        
        if (_isDragging)
        {
            Vector3 mousePosition = Mouse.current.position.value;

            transform.position = mousePosition;
            var clampedX = Mathf.Clamp(transform.localPosition.x, -556.2995f, +556.2995f);
            var clampedY = Mathf.Clamp(transform.localPosition.y, -400, 400);
            transform.localPosition = new Vector2(clampedX, clampedY);
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        var value = eventData.scrollDelta.y > 0 ? 2 : -2;
        MinimumSize.Value = Mathf.Clamp(MinimumSize.Value + value, 10, 15);
    }

}