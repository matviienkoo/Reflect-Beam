using UnityEngine;
using TMPro;
using MirraGames.SDK;
using MirraGames.SDK.Common;

public sealed class MirraLocalizedTMP : MonoBehaviour
{
    [SerializeField] private string russian;
    [SerializeField] private string english;

    private void Start()
    {
        var label = GetComponent<TMP_Text>();
        if (MirraSDK.Language.Current == LanguageType.English)
        {
            label.text = english;
        }
        else
        {
            label.text = russian;
        }
    }
}
