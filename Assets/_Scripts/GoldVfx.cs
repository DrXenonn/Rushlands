using TMPro;
using UnityEngine;

public class GoldVfx : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI Txt;
    [SerializeField] private GameObject Territory;
    [SerializeField] private float Ttl = 3;

    private void Awake()
    {
        Destroy(gameObject, Ttl);
    }

    public void Init(byte amount, bool territoryBonus)
    {
        Territory.SetActive(territoryBonus);
        Txt.text = "+" + amount;
    }
}