using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
[System.Serializable]
public struct UpgradeButton
{
    public TextMeshProUGUI Name;
    public Button Button;
    public Image Icon;
    public TextMeshProUGUI Price;
}

public class NodeUIManager : MonoBehaviour
{
    [SerializeField] private GameObject[] UiElements;
    [SerializeField] private TextMeshProUGUI Name, TrainingSpeed, Capacity, Defence, MineSpeed, Damage, AttackSpeed, Range;
    [SerializeField] private Image StatMenuIcon;
    [SerializeField] private SelectionManager SelectionManager;
    [SerializeField] private UpgradeButton[] UpgradeButtons;
    [SerializeField] private UpgradeButton DemolishButton;
    [SerializeField] private Color OriginalTextColor, NoGoldTextColor;
    [SerializeField] private Sprite DemolishIcon;
    [SerializeField] private Sprite UpgradeIcon;
    [SerializeField] private float NoGoldIconOpacity;
    private bool _canUpgrade = true;
    public static NodeUIManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    
    public void OnSelectNodeEvent(Component sender, object data)
    {
        if (SelectionManager.SelectedNodes.Count == 1)
        {
            var node = SelectionManager.SelectedNodes.First();
            UpdateStats(node);
            UpdateBuildMenu(node);
            ShowUI(true);
        }
        else
        {
            ShowUI(false);
        }
    }

    public void OnGoldChanged(Component sender, object data)
    {
        UpdateBuildMenu(SelectionManager.SelectedNodes.Count == 1 ? SelectionManager.SelectedNodes.First() : null);
    }

    private void ShowUI(bool state)
    {
        if (!_canUpgrade)
        {
            state = false;
        }
        
        foreach (var obj in UiElements)
        {
            obj.SetActive(state);
        }
    }

    public void UpdateStats(Node node)
    {
        if (node == null) return;
        var settings = node.RunTimeSettings;
        Name.text = settings.Name;

        StatMenuIcon.sprite = settings.Icon;
        SetTMP(TrainingSpeed, settings.UnitsPerSecond, "/s");
        SetTMP(Capacity, settings.MaxUnits);
        SetTMP(Defence, settings.Armor);
        SetTMP(MineSpeed, settings.GoldPerSecond, "/s");
        SetTMP(Damage, settings.BaseDamage);
        SetTMP(AttackSpeed, settings.AttackPerSecond, "/s");
        SetTMP(Range, settings.Range, "m");
    }
    
    private void UpdateBuildMenu(Node node)
    {
        if (node is null || !_canUpgrade) return;
        
        foreach (var button in UpgradeButtons)
        {
            button.Button.gameObject.SetActive(false);
        }

        var playerID = NetworkManager.Singleton.LocalClientId;
        var currentGold = GoldManager.Instance.GetGold(playerID);
        
        for (var i = 0; i < node.RunTimeSettings.UpgradeOptions.Length; i++)
        {
            var btn = UpgradeButtons[i];
            btn.Button.gameObject.SetActive(true);
            
            var upgradeOptions = node.RunTimeSettings.UpgradeOptions[i];

            var hasGold = currentGold >= upgradeOptions.Price;
            btn.Button.interactable = hasGold;
            
            btn.Price.faceColor = hasGold ? OriginalTextColor : NoGoldTextColor;
            
            btn.Name.text = upgradeOptions.Name;
            btn.Price.text = upgradeOptions.Price.ToString();
            
            btn.Button.image.sprite = node.RunTimeSettings.UpgradeOptions.Length == 1 ? UpgradeIcon : upgradeOptions.Icon;
            
            btn.Button.onClick.RemoveAllListeners();
            btn.Button.onClick.AddListener(() => OnRequestBuilding(node, upgradeOptions));
            
        }
        
        var demolishNode = node.RunTimeSettings.DemolishedNode;
        if (demolishNode != null)
        {
            var hasGold = currentGold >= demolishNode.Price;
            
            DemolishButton.Price.faceColor = hasGold ? OriginalTextColor : NoGoldTextColor;
            DemolishButton.Button.interactable = hasGold;

            DemolishButton.Name.text = demolishNode.Name;
            DemolishButton.Price.text = demolishNode.Price.ToString();
            DemolishButton.Button.image.sprite = DemolishIcon;
            DemolishButton.Button.onClick.RemoveAllListeners();
            DemolishButton.Button.onClick.AddListener(() => OnRequestBuilding(node, demolishNode));
            DemolishButton.Button.gameObject.SetActive(true);
        }
        else
        {
            DemolishButton.Button.gameObject.SetActive(false);
        }
    }

    private void OnRequestBuilding(Node node , NodeSettings nodeSettings)
    {
        _canUpgrade = false;
        ShowUI(false);
        NodeUpgradeManager.Instance.NodeUpgradeReqServerRpc(node.NetworkObjectId, nodeSettings.ID, NetworkManager.Singleton.LocalClientId);
    }

    public void OnFinishUpgrade()
    {
        _canUpgrade = true;
        var node = SelectionManager.SelectedNodes.Count == 1 ? SelectionManager.SelectedNodes.First() : null;
        UpdateBuildMenu(node);
        UpdateStats(node);
        ShowUI(true);
    }

    private static void SetTMP(TMP_Text tmp, float value, string suffix = "")
    {
        var isZero = Mathf.Approximately(value, 0f); 
        tmp.gameObject.SetActive(!isZero);
        if (!isZero)
            tmp.text = $"{value:0.##}{suffix}";
    }
}