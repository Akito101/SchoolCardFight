using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class Localizer
{
    private const string PrefKey = "Language";
    private const string FileName = "Localization";   // Assets/Resources/Localization.csv

    public static event Action OnLanguageChanged;

    private static Dictionary<string, string[]> table;
    private static List<string> languages;
    private static string current;
    private static readonly HashSet<string> warned = new HashSet<string>();

    public static IReadOnlyList<string> Languages
    {
        get { EnsureLoaded(); return languages; }
    }

    public static string Language
    {
        get
        {
            EnsureLoaded();
            if (current == null)
            {
                string saved = PlayerPrefs.GetString(PrefKey, "English");
                current = languages.Contains(saved) ? saved : "English";
            }
            return current;
        }
        set
        {
            current = value;
            PlayerPrefs.SetString(PrefKey, value);
            PlayerPrefs.Save();
            OnLanguageChanged?.Invoke();
        }
    }

    public static bool HasEntry(string english)
    {
        if (string.IsNullOrWhiteSpace(english)) return false;
        EnsureLoaded();
        return table.ContainsKey(Normalize(english));
    }

    // Use this for any text: Localizer.T("Play")
    public static string T(string english)
    {
        if (string.IsNullOrEmpty(english)) return english;
        EnsureLoaded();

        int column = languages.IndexOf(Language);
        if (column <= 0) return english;   // English: show the original

        string key = Normalize(english);
        if (table.TryGetValue(key, out string[] row))
        {
            if (column < row.Length && !string.IsNullOrWhiteSpace(row[column])) return row[column];
        }
        else if (Debug.isDebugBuild && warned.Add(key))
        {
            Debug.LogWarning($"Localizer: no row in the CSV for \"{key}\"");
        }

        return english;   // fallback so nothing is ever blank
    }

    // Use this when the text has numbers: Localizer.T("Packs left: {0}", count)
    public static string T(string english, params object[] args)
    {
        string text = T(english);
        try
        {
            return string.Format(text, args);
        }
        catch (FormatException)
        {
            Debug.LogError($"Localizer: broken {{0}} placeholder in the translation of \"{english}\"");
            return string.Format(english, args);
        }
    }

    private static void EnsureLoaded()
    {
        if (table != null) return;

        table = new Dictionary<string, string[]>();
        languages = new List<string> { "English" };

        TextAsset file = Resources.Load<TextAsset>(FileName);
        if (file == null)
        {
            Debug.LogWarning("Localizer: Assets/Resources/Localization.csv not found, everything stays in English.");
            return;
        }

        string text = file.text;
        if (text.Length > 0 && text[0] == '\uFEFF') text = text.Substring(1);   // remove the BOM some programs add

        List<List<string>> rows = ParseCsv(text);
        if (rows.Count == 0) return;

        languages = new List<string>();
        foreach (string header in rows[0]) languages.Add(header.Trim());
        languages[0] = "English";

        for (int r = 1; r < rows.Count; r++)
        {
            List<string> row = rows[r];
            if (row.Count == 0 || string.IsNullOrWhiteSpace(row[0])) continue;

            string[] cells = new string[row.Count];
            for (int c = 0; c < row.Count; c++) cells[c] = NormalizeNewlines(row[c]);
            table[cells[0].Trim()] = cells;
        }
    }

    private static string NormalizeNewlines(string s)
    {
        return s.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    private static string Normalize(string s)
    {
        return NormalizeNewlines(s).Trim();
    }

    // Reads a CSV file, including quoted cells with commas and line breaks inside
    private static List<List<string>> ParseCsv(string text)
    {
        List<List<string>> rows = new List<List<string>>();
        List<string> row = new List<string>();
        StringBuilder cell = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                    else inQuotes = false;
                }
                else cell.Append(c);
            }
            else if (c == '"') inQuotes = true;
            else if (c == ',') { row.Add(cell.ToString()); cell.Length = 0; }
            else if (c == '\n' || c == '\r')
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                row.Add(cell.ToString());
                cell.Length = 0;
                rows.Add(row);
                row = new List<string>();
            }
            else cell.Append(c);
        }

        if (cell.Length > 0 || row.Count > 0)
        {
            row.Add(cell.ToString());
            rows.Add(row);
        }

        return rows;
    }
}