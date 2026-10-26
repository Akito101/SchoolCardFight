using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LanguageToggleButton : MonoBehaviour
{
    public Button button;
    public TextMeshProUGUI label;   // optional

    void Awake()
    {
        button.onClick.AddListener(Next);
    }

    void OnEnable()
    {
        Refresh();
    }

    private void Next()
    {
        IReadOnlyList<string> languages = Localizer.Languages;
        int index = 0;
        for (int i = 0; i < languages.Count; i++)
        {
            if (languages[i] == Localizer.Language) index = i;
        }

        Localizer.Language = languages[(index + 1) % languages.Count];
        Refresh();
    }

    private void Refresh()
    {
        if (label != null) label.text = Localizer.T("Language: {0}", Localizer.Language);
    }
}