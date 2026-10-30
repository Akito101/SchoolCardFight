using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class ArtifactContainerController : MonoBehaviour
{
    [Header("Artifact Icon References")]
    [SerializeField] private GameObject artifact1;
    [SerializeField] private GameObject artifact2;
    [SerializeField] private GameObject artifact3;

    private Color normalColor = Color.white;
    private Color darkenedColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    public void UpdateSpellUI(ArtifactEffect activeArtifact)
    {
        // 1. Fetch owned artifacts from PlayerProgress storage
        List<string> ownedArtifacts = PlayerProgress.GetOwnedArtifacts();

        // 2. Check ownership for each specific artifact slot 
        // (Note: Protection is treated as owned by default if the save list is completely empty for new players)
        bool ownsProtection = ownedArtifacts.Contains(ArtifactEffect.Protection.ToString()) || ownedArtifacts.Count == 0;
        bool ownsReshuffle = ownedArtifacts.Contains(ArtifactEffect.ReshuffleOpponentDeck.ToString());
        bool ownsSteal = ownedArtifacts.Contains(ArtifactEffect.StealCard.ToString());

        // 3. Enable or disable UI objects based on ownership
        if (artifact1 != null) artifact1.SetActive(ownsProtection);
        if (artifact2 != null) artifact2.SetActive(ownsReshuffle);
        if (artifact3 != null) artifact3.SetActive(ownsSteal);

        // 4. Reset all active ones to darkened first
        SetGameObjectBrightness(artifact1, false);
        SetGameObjectBrightness(artifact2, false);
        SetGameObjectBrightness(artifact3, false);

        // 5. Highlight only the currently active battle artifact
        switch (activeArtifact)
        {
            case ArtifactEffect.Protection:
                SetGameObjectBrightness(artifact1, true);
                break;
            case ArtifactEffect.ReshuffleOpponentDeck:
                SetGameObjectBrightness(artifact2, true);
                break;
            case ArtifactEffect.StealCard:
                SetGameObjectBrightness(artifact3, true);
                break;
            case ArtifactEffect.None:
                break;
        }
    }

    private void SetGameObjectBrightness(GameObject spellObj, bool isActive)
    {
        if (spellObj != null && spellObj.activeSelf)
        {
            Image img = spellObj.GetComponent<Image>();
            if (img != null)
            {
                img.color = isActive ? normalColor : darkenedColor;
            }
        }
    }
}
