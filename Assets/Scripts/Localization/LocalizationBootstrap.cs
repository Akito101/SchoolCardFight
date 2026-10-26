using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LocalizationBootstrap
{
    private class Entry
    {
        public string english;
        public string lastApplied;
    }

    private static readonly Dictionary<TMP_Text, Entry> tracked = new Dictionary<TMP_Text, Entry>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Localizer.OnLanguageChanged -= ApplyAll;
        Localizer.OnLanguageChanged += ApplyAll;

        ScanScene();   // the very first scene
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ScanScene();
    }

    private static void ScanScene()
    {
        tracked.Clear();

        TMP_Text[] texts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (TMP_Text t in texts)
        {
            string english = t.text;
            if (!Localizer.HasEntry(english)) continue;   // only texts that have a row in the CSV
            tracked[t] = new Entry { english = english, lastApplied = english };
        }

        ApplyAll();
    }

    private static void ApplyAll()
    {
        List<TMP_Text> remove = new List<TMP_Text>();

        foreach (KeyValuePair<TMP_Text, Entry> pair in tracked)
        {
            TMP_Text t = pair.Key;
            Entry e = pair.Value;

            if (t == null) { remove.Add(t); continue; }                 // the object was destroyed
            if (t.text != e.lastApplied) { remove.Add(t); continue; }   // a script changed it, so the script owns it now

            string translated = Localizer.T(e.english);
            t.text = translated;
            e.lastApplied = translated;
        }

        foreach (TMP_Text t in remove) tracked.Remove(t);
    }
}