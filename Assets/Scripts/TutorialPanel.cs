using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class TutorialPanel : MonoBehaviour
{
    [Header("Pages (in order)")]
    public Sprite[] pages;

    [Header("UI")]
    public Image pageImage;
    public Button backButton;
    public Button nextButton;
    public TextMeshProUGUI nextButtonText;    // optional
    public TextMeshProUGUI pageText;          // optional, shows "2 / 5"
    public Button closeButton;                // optional X button

    [Header("Options")]
    public string nextLabel = "Next";
    public string lastPageLabel = "Play";     // the text of the button on the last page
    public bool pauseGameWhileOpen = false;   // tick this in the battle scene
    public bool allowCloseAnytime = true;     // shows the X button

    private int index;
    private System.Action onFinished;
    private bool pausedByMe;
    private float savedTimeScale = 1f;
    private bool wired;

    void Awake()
    {
        Wire();
    }

    void OnDisable()
    {
        ReleasePause();   // never leave the game frozen, even if the scene changes while this is open
    }

    private void Wire()
    {
        if (wired) return;
        wired = true;

        backButton.onClick.AddListener(() => { index--; ShowPage(true); });
        nextButton.onClick.AddListener(OnNext);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    // Use this one for a normal button's OnClick (the battle "?" button)
    public void Open()
    {
        Open(null);
    }

    // finished = what to do when the player presses the button on the last page
    public void Open(System.Action finished)
    {
        gameObject.SetActive(true);   // also runs Awake the first time
        transform.SetAsLastSibling(); // draws on top of cards spawned after the scene started
        Wire();

        onFinished = finished;
        index = 0;

        if (pauseGameWhileOpen && !pausedByMe)
        {
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            pausedByMe = true;
        }

        ShowPage(false);
    }

    public void Close()
    {
        ReleasePause();
        gameObject.SetActive(false);
    }

    private void OnNext()
    {
        if (pages != null && index >= pages.Length - 1)
        {
            System.Action callback = onFinished;
            onFinished = null;
            Close();
            callback?.Invoke();
            return;
        }

        index++;
        ShowPage(true);
    }

    private void ShowPage(bool animate)
    {
        if (pages == null || pages.Length == 0) return;
        index = Mathf.Clamp(index, 0, pages.Length - 1);

        pageImage.sprite = pages[index];
        pageImage.DOKill();
        if (animate)
        {
            pageImage.color = new Color(1f, 1f, 1f, 0f);
            pageImage.DOFade(1f, 0.2f).SetUpdate(true);   // works even while the game is paused
        }
        else
        {
            pageImage.color = Color.white;
        }

        bool last = index == pages.Length - 1;
        backButton.interactable = index > 0;
        if (nextButtonText != null) nextButtonText.text = last ? lastPageLabel : nextLabel;
        if (pageText != null) pageText.text = $"{index + 1} / {pages.Length}";
        if (closeButton != null) closeButton.gameObject.SetActive(allowCloseAnytime);
    }

    private void ReleasePause()
    {
        if (!pausedByMe) return;
        Time.timeScale = savedTimeScale;
        pausedByMe = false;
    }
}