using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class VNManager : MonoBehaviour
{
    [Header("Pictures in order. Each tap shows the next one.")]
    public Sprite[] steps;
    public float fadeTime = 0.35f;

    [Header("Images (both stretched to fill the screen)")]
    public Image baseImage;       // shows the current picture
    public Image overlayImage;    // fades the next picture in on top

    [Header("Buttons")]
    public Button tapArea;
    public Button skipButton;

    [Header("Scene to load after the last picture")]
    public string nextScene = "PackOpening";

    private int index = 0;

    void Awake()
    {
        tapArea.onClick.AddListener(OnTap);
        skipButton.onClick.AddListener(GoToNextScene);
    }

    void Start()
    {
        if (steps == null || steps.Length == 0)
        {
            Debug.LogWarning("MangaReader: no pictures in Steps!");
            GoToNextScene();
            return;
        }

        baseImage.sprite = steps[0];
        baseImage.color = Color.white;

        overlayImage.sprite = null;
        overlayImage.color = new Color(1f, 1f, 1f, 0f);
    }

    private void OnTap()
    {
        overlayImage.DOComplete();   // if the last fade is still running, finish it first

        index++;
        if (index >= steps.Length)
        {
            GoToNextScene();
            return;
        }

        Sprite next = steps[index];
        overlayImage.sprite = next;
        overlayImage.color = new Color(1f, 1f, 1f, 0f);
        overlayImage.DOFade(1f, fadeTime).OnComplete(() =>
        {
            baseImage.sprite = next;
            overlayImage.color = new Color(1f, 1f, 1f, 0f);
        });
    }

    private void GoToNextScene()
    {
        SceneManager.LoadScene(nextScene);
    }
}