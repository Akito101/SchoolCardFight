using UnityEngine;
using UnityEngine.UI;

public class SpellUIController : MonoBehaviour
{
    [Header("Spell UI References (Image Components)")]
    [SerializeField] private GameObject spell1;
    [SerializeField] private GameObject spell2;
    [SerializeField] private GameObject spell3;

    private Color normalColor = Color.white;
    private Color darkenedColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    public void UpdateSpellUI(ArtifactEffect activeArtifact)
    {
        // Reset all to darkened first
        SetGameObjectBrightness(spell1, false);
        SetGameObjectBrightness(spell2, false);
        SetGameObjectBrightness(spell3, false);

        // Highlight only the active one based on the player's chosen artifact
        switch (activeArtifact)
        {
            case ArtifactEffect.Protection:
                SetGameObjectBrightness(spell1, true);
                break;
            case ArtifactEffect.ReshuffleEnemyCard:
                SetGameObjectBrightness(spell2, true);
                break;
            case ArtifactEffect.StealCard:
                SetGameObjectBrightness(spell3, true);
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
