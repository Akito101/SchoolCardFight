using UnityEngine;
using System.Collections;
using DG.Tweening;
using TMPro;

public enum AIType
{
    Child,
    Student,
    Monitor,
    Teacher
}
public class DialogueController : MonoBehaviour
{
    [Header("AI Configuration")]
    [SerializeField] private AIType currentAIType = AIType.Monitor;

    [Header("Avatar Settings")]
    [SerializeField] private RectTransform avatarRect;

    [Header("Monitor Dialogue Box References")]
    [SerializeField] private RectTransform monitorDialogueBox1;
    [SerializeField] private RectTransform monitorDialogueBox2;

    [Header("Teacher Dialogue Boxes References")]
    [SerializeField] private RectTransform teacherDialogueBox1;
    [SerializeField] private RectTransform teacherDialogueBox2;

    private Vector2 avatarOriginalPos;
    private RectTransform activeDialogueBox;
    private Vector2 activeDialogueOriginalPos;
    private bool hasTriggeredMonitorDialogue1 = false;
    private bool hasTriggeredMonitorDialogue2 = false;

    private float slideDuration = 0.45f;
    private float offscreenRightOffset = 800f;

    private void Awake()
    {
        if (avatarRect != null)
        {
            avatarOriginalPos = avatarRect.anchoredPosition;
            avatarRect.anchoredPosition = new Vector2(avatarOriginalPos.x + offscreenRightOffset, avatarOriginalPos.y);
            avatarRect.gameObject.SetActive(false);
        }

        // Hide all pre-rendered dialogue boxes on start
        HideAllDialogueBoxes();
    }

    public void CheckAIDeckCondition(int currentDeckCount)
    {
        switch (currentAIType)
        {
            case AIType.Monitor:
                // First condition: Deck count drops below 14
                if (currentDeckCount < 14 && !hasTriggeredMonitorDialogue1)
                {
                    hasTriggeredMonitorDialogue1 = true;
                    ShowDialogueSequence(monitorDialogueBox1);
                }
                // Second condition example: Deck count drops below 7
                else if (currentDeckCount < 7 && !hasTriggeredMonitorDialogue2)
                {
                    hasTriggeredMonitorDialogue2 = true;
                    ShowDialogueSequence(monitorDialogueBox2);
                }
                break;

            case AIType.Teacher:
                // Teacher logic can be hooked up here similarly
                break;

            case AIType.Child:
            case AIType.Student:
                // Currently have no dialogue UI assigned
                break;
        }
    }

    public void ShowDialogueSequence(RectTransform targetDialogueBox)
    {
        if (targetDialogueBox == null) return;

        // Hide any active dialogue box before starting a new sequence
        HideAllDialogueBoxes();

        activeDialogueBox = targetDialogueBox;
        activeDialogueOriginalPos = activeDialogueBox.anchoredPosition;

        // Position dialogue box offscreen initially
        activeDialogueBox.anchoredPosition = new Vector2(activeDialogueOriginalPos.x + offscreenRightOffset, activeDialogueOriginalPos.y);
        activeDialogueBox.gameObject.SetActive(true);

        Sequence seq = DOTween.Sequence();

        // 1. Slide Avatar from right to left (original position)
        if (avatarRect != null)
        {
            avatarRect.gameObject.SetActive(true);
            seq.Append(avatarRect.DOAnchorPos(avatarOriginalPos, slideDuration).SetEase(Ease.OutBack));
        }

        // 2. Slide Dialogue Box from right to left (original position) 0.2s after Avatar starts
        seq.Insert(0.2f, activeDialogueBox.DOAnchorPos(activeDialogueOriginalPos, slideDuration).SetEase(Ease.OutQuad));
    }

    public void HideDialogue(System.Action onComplete = null)
    {
        Sequence seq = DOTween.Sequence();

        if (activeDialogueBox != null)
        {
            seq.Join(activeDialogueBox.DOAnchorPos(new Vector2(activeDialogueOriginalPos.x + offscreenRightOffset, activeDialogueOriginalPos.y), 0.3f).SetEase(Ease.InQuad));
        }

        if (avatarRect != null)
        {
            seq.Insert(0.1f, avatarRect.DOAnchorPos(new Vector2(avatarOriginalPos.x + offscreenRightOffset, avatarOriginalPos.y), 0.3f).SetEase(Ease.InBack));
        }

        seq.OnComplete(() =>
        {
            HideAllDialogueBoxes();
            if (avatarRect != null) avatarRect.gameObject.SetActive(false);
            onComplete?.Invoke();
        });
    }

    private void HideAllDialogueBoxes()
    {
        if (monitorDialogueBox1 != null) monitorDialogueBox1.gameObject.SetActive(false);
        if (monitorDialogueBox2 != null) monitorDialogueBox2.gameObject.SetActive(false);
        if (teacherDialogueBox1 != null) teacherDialogueBox1.gameObject.SetActive(false);
        if (teacherDialogueBox2 != null) teacherDialogueBox2.gameObject.SetActive(false);
    }

    public void SetAIType(AIType newAIType)
    {
        currentAIType = newAIType;
        hasTriggeredMonitorDialogue1 = false;
        hasTriggeredMonitorDialogue2 = false;
    }
}
