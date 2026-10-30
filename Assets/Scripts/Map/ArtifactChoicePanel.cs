using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ArtifactChoicePanel : MonoBehaviour
{
    public Button protectionButton;
    public Button reshuffleButton;
    public Button stealButton;
    public Button closeButton;

    private Action<ArtifactEffect> onChosen;
    private bool chosen;

    void Awake()
    {
        protectionButton.onClick.AddListener(() => Choose(ArtifactEffect.Protection));
        reshuffleButton.onClick.AddListener(() => Choose(ArtifactEffect.ReshuffleOpponentDeck));
        stealButton.onClick.AddListener(() => Choose(ArtifactEffect.StealCard));

        if (closeButton != null)
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));
    }

    // MapManager calls this with the artifacts the player owns
    public void Show(List<ArtifactEffect> available, Action<ArtifactEffect> callback)
    {
        onChosen = callback;
        chosen = false;

        // only show the buttons for artifacts the player has earned
        protectionButton.gameObject.SetActive(available.Contains(ArtifactEffect.Protection));
        reshuffleButton.gameObject.SetActive(available.Contains(ArtifactEffect.ReshuffleOpponentDeck));
        stealButton.gameObject.SetActive(available.Contains(ArtifactEffect.StealCard));

        gameObject.SetActive(true);
    }

    private void Choose(ArtifactEffect effect)
    {
        if (chosen) return;   // ignores a double tap
        chosen = true;
        onChosen?.Invoke(effect);
    }
}