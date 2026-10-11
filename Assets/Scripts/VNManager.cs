using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

[System.Serializable]
public class DialogueLine
{
    public string speakerName;
    [TextArea(2, 5)] public string text;
    public Sprite background;      // leave empty to keep the previous background
    public Sprite character;       // leave empty to keep the previous character
    public bool hideCharacter;     // tick for narration lines with no character
}

public class VNManager : MonoBehaviour
{
    [Header("Story")]
    public List<DialogueLine> lines = new List<DialogueLine>();
    public string nextScene = "TestGamePlay"; // will become the pack-opening scene later

    [Header("UI References")]
    public Image backgroundImage;
    public Image characterImage;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;

    public float charsPerSecond = 40f;

    private int index = -1;
    private bool isTyping = false;
    private Coroutine typingRoutine;

    void Start()
    {
        characterImage.gameObject.SetActive(false); // hidden until a line gives it a sprite
        ShowNextLine();
    }

    // Hook this to TapArea's OnClick
    public void OnScreenTapped()
    {
        if (isTyping) FinishTyping();   // first tap completes the line instantly
        else ShowNextLine();            // second tap moves on
    }

    // Hook this to SkipButton's OnClick
    public void OnSkipPressed()
    {
        SceneManager.LoadScene(nextScene);
    }

    private void ShowNextLine()
    {
        index++;
        if (index >= lines.Count)
        {
            SceneManager.LoadScene(nextScene);
            return;
        }

        DialogueLine line = lines[index];

        if (line.background != null) backgroundImage.sprite = line.background;

        if (line.hideCharacter)
        {
            characterImage.gameObject.SetActive(false);
        }
        else if (line.character != null)
        {
            characterImage.sprite = line.character;
            characterImage.gameObject.SetActive(true);
        }

        speakerText.text = line.speakerName;
        typingRoutine = StartCoroutine(TypeText(line.text));
    }

    private IEnumerator TypeText(string fullText)
    {
        isTyping = true;
        dialogueText.text = fullText;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();
        int total = dialogueText.textInfo.characterCount;

        float shown = 0f;
        while (dialogueText.maxVisibleCharacters < total)
        {
            shown += Time.deltaTime * charsPerSecond;
            dialogueText.maxVisibleCharacters = Mathf.Min(total, (int)shown);
            yield return null;
        }
        isTyping = false;
    }

    private void FinishTyping()
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        dialogueText.maxVisibleCharacters = 99999;
        isTyping = false;
    }
}