using System.Collections;
using System.Linq;
using Unity.Multiplayer.Playmode;
using UnityEngine;
using UnityEngine.UI;

public class TEMP_FastJoin : MonoBehaviour
{
    [SerializeField] private Button[] HostButtons;
    [SerializeField] private Button[] ClientButtons;

    private void Start()
    {
        StartCoroutine(nameof(RunButtons));
    }

    private IEnumerator RunButtons()
    {
        var tags = CurrentPlayer.ReadOnlyTags();

        if (tags.Contains("P1"))
        {
            foreach (var button in HostButtons)
            {
                button.onClick.Invoke();
                yield return new WaitForSeconds(0.25f);
            }
            
            yield return new WaitForSeconds(2f);
        }
        else
        {
            yield return new WaitForSeconds(5f);
            foreach (var button in ClientButtons)
            {
                button.onClick.Invoke();
            }
        }
    }
}