using UnityEngine;
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
        // Reset all to darkened first
        SetGameObjectBrightness(artifact1, false);
        SetGameObjectBrightness(artifact2, false);
        SetGameObjectBrightness(artifact3, false);

        // Highlight only the active one based on the player's chosen artifact
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
        if (spellObj != null)
        {
            Image img = spellObj.GetComponent<Image>();
            if (img != null)
            {
                img.color = isActive ? normalColor : darkenedColor;
            }
        }
    }
}
