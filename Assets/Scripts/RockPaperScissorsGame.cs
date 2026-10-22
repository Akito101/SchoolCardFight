using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

public enum Hand { Rock, Paper, Scissors }

public class RockPaperScissorsGame : MonoBehaviour
{
    [Header("Rules")]
    public int roundsToWin = 2;        // 2 = best of 3
    public int winReward = 100;
    public int loseReward = 0;
    public string mapScene = "Map";

    [Header("Timing")]
    public float resultShowTime = 2.2f;   // how long the result stays on screen after the reveal

    [Header("Hand movement")]
    public float restX = 600f;    // where the hands wait, mostly off-screen at the sides
    public float clashX = 230f;   // how far from the center the hands meet
    public bool mirrorOpponentHand = true;   // flips the right hand so it faces left

    [Header("Hand art, in this order: Rock, Paper, Scissors")]
    public Sprite[] playerHandSprites;        // the left-hand pictures
    public Sprite[] opponentHandSprites;      // the right-hand pictures
    public bool showFistWhilePumping = true;  // shows the Rock picture while hands slide in and pump

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI resultText;

    public RectTransform playerHandRoot;
    public Image playerHandImage;
    public TextMeshProUGUI playerHandText;

    public RectTransform opponentHandRoot;
    public Image opponentHandImage;
    public TextMeshProUGUI opponentHandText;

    public Button rockButton;
    public Button paperButton;
    public Button scissorsButton;
    public Button leaveButton;

    public GameObject endPanel;
    public TextMeshProUGUI endText;
    public Button playAgainButton;
    public Button backToMapButton;

    private static readonly Color WinColor = new Color(0.35f, 0.9f, 0.45f);
    private static readonly Color LoseColor = new Color(0.95f, 0.35f, 0.35f);
    private static readonly Color TieColor = new Color(1f, 0.85f, 0.3f);

    private int playerScore;
    private int opponentScore;
    private Color defaultResultColor;
    private bool roundRunning;
    private float playerBaseY;
    private float opponentBaseY;

    void Awake()
    {
        rockButton.onClick.AddListener(() => PlayRound(Hand.Rock));
        paperButton.onClick.AddListener(() => PlayRound(Hand.Paper));
        scissorsButton.onClick.AddListener(() => PlayRound(Hand.Scissors));
        defaultResultColor = resultText.color;

        leaveButton.onClick.AddListener(GoToMap);
        backToMapButton.onClick.AddListener(GoToMap);
        playAgainButton.onClick.AddListener(StartMatch);

        // remember the height you placed the hands at in the scene
        playerBaseY = playerHandRoot.anchoredPosition.y;
        opponentBaseY = opponentHandRoot.anchoredPosition.y;

        if (mirrorOpponentHand)
            opponentHandImage.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
    }

    void Start()
    {
        StartMatch();
    }

    private void StartMatch()
    {
        playerScore = 0;
        opponentScore = 0;
        roundRunning = false;

        endPanel.SetActive(false);
        SetButtons(true);
        ResetForNextRound();
        UpdateScore();
    }

    // hands back at the sides, question marks, ready for a new pick
    private void ResetForNextRound()
    {
        playerHandRoot.DOKill();
        opponentHandRoot.DOKill();

        playerHandRoot.anchoredPosition = new Vector2(-restX, playerBaseY);
        opponentHandRoot.anchoredPosition = new Vector2(restX, opponentBaseY);
        playerHandRoot.localScale = Vector3.one;
        opponentHandRoot.localScale = Vector3.one;

        ShowHand(playerHandImage, playerHandText, -1);
        ShowHand(opponentHandImage, opponentHandText, -1);

        resultText.color = defaultResultColor;
        resultText.text = "Pick your hand!";
    }

    private void PlayRound(Hand mine)
    {
        if (roundRunning) return;
        StartCoroutine(RoundRoutine(mine));
    }

    private IEnumerator RoundRoutine(Hand mine)
    {
        roundRunning = true;
        SetButtons(false);

        resultText.color = Color.white;
        resultText.text = "";

        // 1. both hands slide in from the sides
        playerHandRoot.DOAnchorPosX(-clashX, 0.5f).SetEase(Ease.OutBack);
        opponentHandRoot.DOAnchorPosX(clashX, 0.5f).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(0.6f);

        // 2. three pumps, like the real game
        string[] beats = { "Sone...", "Swan..." };
        Color[] beatColors = { Color.red, Color.cyan };
        for (int i = 0; i < beats.Length; i++)
        {
            resultText.text = beats[i];
            resultText.color = beatColors[i];
            playerHandRoot.DOAnchorPosY(playerBaseY + 70f, 0.15f).SetLoops(2, LoopType.Yoyo);
            opponentHandRoot.DOAnchorPosY(opponentBaseY + 70f, 0.15f).SetLoops(2, LoopType.Yoyo);
            yield return new WaitForSeconds(0.45f);
        }

        // 3. reveal both hands at the same moment
        resultText.text = "Si!";
        resultText.color = Color.yellow;
        Hand theirs = (Hand)Random.Range(0, 3);
        ShowHand(playerHandImage, playerHandText, (int)mine);
        ShowHand(opponentHandImage, opponentHandText, (int)theirs);

        int outcome = Compare(mine, theirs);
        Vector3 bigPunch = new Vector3(0.25f, 0.25f, 0f);

        if (outcome > 0)
        {
            playerScore++;
            resultText.text = "Si!\nYou win this round!";
            resultText.color = WinColor;
            playerHandRoot.DOPunchScale(bigPunch, 0.4f, 6, 0.5f);
            opponentHandImage.DOFade(0.4f, 0.3f);
        }
        else if (outcome < 0)
        {
            opponentScore++;
            resultText.text = "Si!\nYou lose this round!";
            resultText.color = LoseColor;
            opponentHandRoot.DOPunchScale(bigPunch, 0.4f, 6, 0.5f);
            playerHandImage.DOFade(0.4f, 0.3f);
        }
        else
        {
            resultText.text = "Si!\nTie! Go again.";
            resultText.color = TieColor;
            playerHandRoot.DOPunchScale(bigPunch * 0.5f, 0.3f, 6, 0.5f);
            opponentHandRoot.DOPunchScale(bigPunch * 0.5f, 0.3f, 6, 0.5f);
        }
        UpdateScore();

        // 4. let the player see who won
        yield return new WaitForSeconds(resultShowTime);

        // 5. hands slide back out
        playerHandRoot.DOAnchorPosX(-restX, 0.4f).SetEase(Ease.InBack);
        opponentHandRoot.DOAnchorPosX(restX, 0.4f).SetEase(Ease.InBack);
        yield return new WaitForSeconds(0.5f);

        if (playerScore >= roundsToWin || opponentScore >= roundsToWin)
        {
            EndMatch(playerScore > opponentScore);
        }
        else
        {
            ResetForNextRound();
            SetButtons(true);
        }

        roundRunning = false;
    }

    // 1 = you win, -1 = you lose, 0 = tie
    private int Compare(Hand mine, Hand theirs)
    {
        if (mine == theirs) return 0;
        return ((int)mine - (int)theirs + 3) % 3 == 1 ? 1 : -1;
    }

    private void EndMatch(bool won)
    {
        int reward = won ? winReward : loseReward;
        if (reward > 0) PlayerProgress.AddCoins(reward);

        if (won) endText.text = $"You won!\n+{reward} coins";
        else if (reward > 0) endText.text = $"You lost...\n+{reward} coins";
        else endText.text = "You lost...\nBetter luck next time!";

        endPanel.SetActive(true);
    }

    // index -1 = not revealed yet, otherwise 0 = Rock, 1 = Paper, 2 = Scissors
    private void ShowHand(Image image, TextMeshProUGUI label, int index)
    {
        Sprite[] sprites = (image == playerHandImage) ? playerHandSprites : opponentHandSprites;

        // before the reveal, show a fist (like the real game) if we have the art
        int shown = (index < 0 && showFistWhilePumping) ? 0 : index;
        bool hasSprite = shown >= 0 && sprites != null && shown < sprites.Length && sprites[shown] != null;

        if (hasSprite)
        {
            image.sprite = sprites[shown];
            image.color = Color.white;
            label.text = "";
        }
        else
        {
            image.sprite = null;
            image.color = new Color(0.8f, 0.8f, 0.95f);
            label.text = index < 0 ? "?" : ((Hand)index).ToString();
        }
    }

    private void UpdateScore()
    {
        scoreText.text = $"You {playerScore}  -  {opponentScore} Opponent";
    }

    private void SetButtons(bool on)
    {
        rockButton.interactable = on;
        paperButton.interactable = on;
        scissorsButton.interactable = on;
    }

    private void GoToMap()
    {
        SceneManager.LoadScene(mapScene);
    }
}