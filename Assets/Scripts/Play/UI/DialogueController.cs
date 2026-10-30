using UnityEngine;
using DG.Tweening;


public class DialogueController : MonoBehaviour
{
    [Header("Avatar Settings")]
    [SerializeField] private RectTransform teacherAvatarRect;
    [SerializeField] private RectTransform monitorAvatarRect;

    [Header("Teacher Dialogue Boxes References")]
    [SerializeField] private RectTransform teacherDialogueBox1;
    [SerializeField] private RectTransform teacherDialogueBox2;
    [SerializeField] private RectTransform teacherButtomBox1;
    [SerializeField] private RectTransform teacherButtomBox2;

    [Header("Monitor Dialogue Boxes References")]
    [SerializeField] private RectTransform monitorDialogueBox1;
    [SerializeField] private RectTransform monitorDialogueBox2;
    [SerializeField] private RectTransform monitorButtomBox1;
    [SerializeField] private RectTransform monitorButtomBox2;

    [Header(" Battle Test Manager Reference")]
    [SerializeField] private BattleTestManager battleTestManager;

    public bool IsDialogueActive { get; private set; } = false;
    public bool IsMonitorAI => currentAIType == BattleTestManager.AIType.Monitor;

    private BattleTestManager.AIType currentAIType;

    public void SetAIType(BattleTestManager.AIType aiType)
    {
        currentAIType = aiType;
    }

    private RectTransform activeAvatarRect;
    private Vector2 avatarOriginalPos;

    private RectTransform activeAIBox;
    private Vector2 activeAIOriginalPos;

    private RectTransform activePlayerBox;
    private Vector2 activePlayerOriginalPos;

    private bool hasTriggeredMonitorDialogue1 = false;
    private bool hasTriggeredMonitorDialogue2 = false;

    private bool hasTriggeredTeacherDialogue1 = false;
    private bool hasTriggeredTeacherDialogue2 = false;
    private int playerWinStreak;

    private int activeDialogueIndex = 1;

    private float slideDuration = 0.5f;
    private float avatarOffset = 300f;
    private float aiDialogueOffset = 1200f;
    private float playerDialogueOffset = 1000f;

    private System.Action currentDialogueCompleteCallback;
    private System.Action delayedDialogueCompleteCallback;

    private void Awake()
    {
        if (monitorAvatarRect != null) avatarOriginalPos = monitorAvatarRect.anchoredPosition;
        if (teacherAvatarRect != null) avatarOriginalPos = teacherAvatarRect.anchoredPosition;

        ResetAllToOffscreen();
    }

    public void CheckAIDeckCondition(int currentDeckCount, System.Action onFinished = null)
    {
        if (IsDialogueActive)
        {
            return;
        }

        bool dialogueTriggered = false;

        switch (currentAIType)
        {
            case BattleTestManager.AIType.Teacher:
                if (currentDeckCount < 5 && !hasTriggeredTeacherDialogue1)
                {
                    dialogueTriggered = TriggerAndTrackDialogue(
                        1,
                        ref hasTriggeredTeacherDialogue1,
                        teacherAvatarRect,
                        teacherDialogueBox1,
                        teacherButtomBox1,
                        onFinished);
                }
                break;

            case BattleTestManager.AIType.Monitor:
                if (currentDeckCount < 12 && !hasTriggeredMonitorDialogue1)
                {
                    dialogueTriggered = TriggerAndTrackDialogue(1, ref hasTriggeredMonitorDialogue1, monitorAvatarRect, monitorDialogueBox1, monitorButtomBox1, onFinished);
                }
                break;

            case BattleTestManager.AIType.Child:
            case BattleTestManager.AIType.Student:
                break;
        }

        if (!dialogueTriggered)
        {
            onFinished?.Invoke();
        }
    }

    public bool CheckPlayerWinStreak(bool playerWon, System.Action onFinished = null)
    {
        if (currentAIType != BattleTestManager.AIType.Teacher)
        {
            return false;
        }

        if (!playerWon)
        {
            playerWinStreak = 0;
            return false;
        }

        playerWinStreak++;
        if (playerWinStreak < 2 || hasTriggeredTeacherDialogue2 || IsDialogueActive)
        {
            return false;
        }

        hasTriggeredTeacherDialogue2 = true;
        activeDialogueIndex = 2;
        TriggerDialogueWithBlock(teacherAvatarRect, teacherDialogueBox2, teacherButtomBox2, onFinished);
        return true;
    }

    private bool TriggerAndTrackDialogue(int dialogueIndex, ref bool triggerFlag, RectTransform avatar, RectTransform aiBox, RectTransform playerBox, System.Action onFinished)
    {
        triggerFlag = true;
        activeDialogueIndex = dialogueIndex;

        TriggerDialogueWithBlock(avatar, aiBox, playerBox, () =>
        {
            onFinished?.Invoke();
        });

        return true;
    }

    public void TriggerMonitorDialogue2()
    {
        if (IsDialogueActive || hasTriggeredMonitorDialogue2) return;

        if (currentAIType != BattleTestManager.AIType.Monitor) return;

        hasTriggeredMonitorDialogue2 = true;
        activeDialogueIndex = 2;

        TriggerDialogueWithBlock(monitorAvatarRect, monitorDialogueBox2, monitorButtomBox2, () =>
        {
            Debug.Log("Monitor Dialogue 2 triggered via player first artifact completed.");
        });
    }

    public void TriggerDialogueWithBlock(RectTransform targetAvatar, RectTransform targetAIBox, RectTransform targetPlayerBox, System.Action onDialogueComplete)
    {
        IsDialogueActive = true;
        currentDialogueCompleteCallback = onDialogueComplete;
        ShowDialoguePair(targetAvatar, targetAIBox, targetPlayerBox);
    }

    public void ShowDialoguePair(RectTransform targetAvatar, RectTransform targetAIBox, RectTransform targetPlayerBox, System.Action onComplete = null)
    {
        if (targetAIBox == null || targetPlayerBox == null) return;

        ResetAllToOffscreen();

        // Initialize active references
        activeAvatarRect = targetAvatar;
        activeAIBox = targetAIBox;
        activeAIOriginalPos = activeAIBox.anchoredPosition;
        activePlayerBox = targetPlayerBox;
        activePlayerOriginalPos = activePlayerBox.anchoredPosition;

        // Position AI Dialogue Box offscreen right
        activeAIBox.anchoredPosition = new Vector2(activeAIOriginalPos.x + aiDialogueOffset, activeAIOriginalPos.y);
        activeAIBox.gameObject.SetActive(true);

        // Position AI Avatar offscreen right
        activeAvatarRect.anchoredPosition = new Vector2(avatarOriginalPos.x + avatarOffset, avatarOriginalPos.y);
        activeAvatarRect.gameObject.SetActive(true);

        // Position Unique Player Dialogue Box offscreen right
        activePlayerBox.anchoredPosition = new Vector2(activePlayerOriginalPos.x - playerDialogueOffset, activePlayerOriginalPos.y);
        activePlayerBox.gameObject.SetActive(true);

        Sequence seq = DOTween.Sequence();

        // Step 1: AI Dialogue Box slides in first from right to left
        seq.Append(activeAIBox.DOAnchorPos(activeAIOriginalPos, slideDuration).SetEase(Ease.OutQuad));

        // Step 2: AI Avatar slides in alongside
        seq.Insert(0.1f, activeAvatarRect.DOAnchorPos(avatarOriginalPos, slideDuration).SetEase(Ease.OutQuad));

        // Step 3: Pause for 0.1 seconds after AI completes entering
        seq.AppendInterval(0.1f);

        // Step 4: Unique Paired Player Dialogue Box slides in from left to right
        seq.Append(activePlayerBox.DOAnchorPos(activePlayerOriginalPos, slideDuration).SetEase(Ease.OutQuad));

        seq.OnComplete(() =>
        {
            onComplete?.Invoke();
        });
    }

    public void OnAgreeClicked() => HandlePlayerChoice("AGREE");
    public void OnDisagreeClicked() => HandlePlayerChoice("DISAGREE");

    public void HandlePlayerChoice(string choice)
    {
        bool agreed = choice == "AGREE";

        switch (currentAIType)
        {
            case BattleTestManager.AIType.Teacher:
                HandleTeacherChoice(agreed);
                break;

            case BattleTestManager.AIType.Monitor:
                HandleMonitorChoice(agreed);
                break;

            case BattleTestManager.AIType.Child:
            case BattleTestManager.AIType.Student:
                break;
        }

        OnPlayerDialogueClicked();
    }

    private void HandleTeacherChoice(bool agreed)
    {
        if (battleTestManager == null) return;

        if (activeDialogueIndex != 1 && activeDialogueIndex != 2)
        {
            return;
        }

        WaitForTeacherDialogueEffect();
        if (activeDialogueIndex == 1)
        {
            battleTestManager.HandleTeacherDialogue1Choice(agreed);
            return;
        }

        battleTestManager.HandleTeacherDialogue2Choice(agreed);
    }

    private void HandleMonitorChoice(bool agreed)
    {
        if (battleTestManager == null) return;

        if (activeDialogueIndex == 1)
        {
            SuppressDialogueContinuation();
            HandleMonitorDialogue1Choice(agreed);
        }
        else if (activeDialogueIndex == 2)
        {
            HandleMonitorDialogue2Choice(agreed);
        }
    }

    private void HandleMonitorDialogue1Choice(bool agreed)
    {
        if (agreed)
        {
            battleTestManager.HandleMonitorDialogueAgree();
        }
        else
        {
            battleTestManager.HandleMonitorDialogueDisagree();
        }
    }


    private void HandleMonitorDialogue2Choice(bool agreed)
    {
        if (agreed)
        {
            battleTestManager.HandleMonitorDialogue2Agree();
        }
        else
        {
            battleTestManager.HandleMonitorDialogue2Disagree();
        }
    }

    public void WaitForTeacherDialogueEffect()
    {
        delayedDialogueCompleteCallback = currentDialogueCompleteCallback;
        currentDialogueCompleteCallback = null;
    }

    public void SuppressDialogueContinuation()
    {
        currentDialogueCompleteCallback = null;
    }

    public void CompleteTeacherDialogueEffect()
    {
        System.Action callback = delayedDialogueCompleteCallback;
        delayedDialogueCompleteCallback = null;
        callback?.Invoke();
    }

    public void OnPlayerDialogueClicked()
    {
        if (!IsDialogueActive) return;

        HideDialogue(() =>
        {
            IsDialogueActive = false;
            System.Action tempCallback = currentDialogueCompleteCallback;
            currentDialogueCompleteCallback = null;
            tempCallback?.Invoke();
        });
    }

    public void HideDialogue(System.Action onComplete = null)
    {
        Sequence seq = DOTween.Sequence();

        // 1. Unique Player dialogue slides out first
        if (activePlayerBox != null)
        {
            seq.Append(activePlayerBox.DOAnchorPos(new Vector2(activePlayerOriginalPos.x - playerDialogueOffset, activePlayerOriginalPos.y), 0.25f).SetEase(Ease.InQuad));
        }

        // 2. AI Avatar slides out
        if (activeAvatarRect != null)
        {
            seq.Insert(0.1f, activeAvatarRect.DOAnchorPos(new Vector2(avatarOriginalPos.x + avatarOffset, avatarOriginalPos.y), 0.25f).SetEase(Ease.InBack));
        }

        // 3. AI Dialogue Box slides out
        if (activeAIBox != null)
        {
            seq.Insert(0.15f, activeAIBox.DOAnchorPos(new Vector2(activeAIOriginalPos.x + aiDialogueOffset, activeAIOriginalPos.y), 0.25f).SetEase(Ease.InQuad));
        }

        seq.OnComplete(() =>
        {
            ResetAllToOffscreen();
            onComplete?.Invoke();
        });
    }

    private void ResetAllToOffscreen()
    {
        HideAllBoxes();

        if (monitorAvatarRect != null)
        {
            monitorAvatarRect.anchoredPosition = new Vector2(avatarOriginalPos.x + avatarOffset, avatarOriginalPos.y);
            monitorAvatarRect.gameObject.SetActive(false);
        }

        if (teacherAvatarRect != null)
        {
            teacherAvatarRect.anchoredPosition = new Vector2(avatarOriginalPos.x + avatarOffset, avatarOriginalPos.y);
            teacherAvatarRect.gameObject.SetActive(false);
        }
    }

    private void HideAllBoxes()
    {
        if (monitorDialogueBox1 != null) monitorDialogueBox1.gameObject.SetActive(false);
        if (monitorButtomBox1 != null) monitorButtomBox1.gameObject.SetActive(false);

        if (monitorDialogueBox2 != null) monitorDialogueBox2.gameObject.SetActive(false);
        if (monitorButtomBox2 != null) monitorButtomBox2.gameObject.SetActive(false);

        if (teacherDialogueBox1 != null) teacherDialogueBox1.gameObject.SetActive(false);
        if (teacherButtomBox1 != null) teacherButtomBox1.gameObject.SetActive(false);

        if (teacherDialogueBox2 != null) teacherDialogueBox2.gameObject.SetActive(false);
        if (teacherButtomBox2 != null) teacherButtomBox2.gameObject.SetActive(false);
    }
}
