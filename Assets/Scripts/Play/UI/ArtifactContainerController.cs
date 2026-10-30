using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class ArtifactContainerController : MonoBehaviour
{
    [Header("Artifact Icon References")]
    [SerializeField] private GameObject artifact1;   // Protection
    [SerializeField] private GameObject artifact2;   // Reshuffle
    [SerializeField] private GameObject artifact3;   // Steal

    private readonly Color lockedColor = Color.black;                     // not owned
    private readonly Color dimColor = new Color(0.45f, 0.45f, 0.45f, 1f); // owned, not chosen
    private readonly Color brightColor = Color.white;                     // chosen

    public void UpdateSpellUI()
    {
        UpdateSpellUI(PlayerArtifactChoice.HasChoice
            ? PlayerArtifactChoice.Effect
            : ArtifactEffect.None);
    }

    public void UpdateSpellUI(ArtifactEffect activeArtifact)
    {
        // Tutorial (level 0) owns nothing, so all three stay black
        List<ArtifactEffect> owned = PlayerProgress.LevelSelected == 0
            ? new List<ArtifactEffect>()
            : GetOwnedArtifacts();

        ApplySlot(artifact1, ArtifactEffect.Protection, owned, activeArtifact);
        ApplySlot(artifact2, ArtifactEffect.ReshuffleOpponentDeck, owned, activeArtifact);
        ApplySlot(artifact3, ArtifactEffect.StealCard, owned, activeArtifact);
    }

    // Same logic as MapManager.GetOwnedArtifacts()
    private List<ArtifactEffect> GetOwnedArtifacts()
    {
        List<ArtifactEffect> owned = new List<ArtifactEffect>();

        LevelRewards rewards = Resources.Load<LevelRewards>("LevelRewards");
        if (rewards == null) return owned;

        int cleared = PlayerProgress.ClearedLevels;
        for (int i = 0; i < cleared; i++)
        {
            LevelRewards.Reward r = rewards.Get(i);
            if (r == null || r.artifact == ArtifactEffect.None) continue;
            if (!owned.Contains(r.artifact)) owned.Add(r.artifact);
        }
        return owned;
    }

    private void ApplySlot(GameObject slot, ArtifactEffect effect, List<ArtifactEffect> owned, ArtifactEffect active)
    {
        if (slot == null) return;
        slot.SetActive(true);

        Image img = slot.GetComponent<Image>();
        if (img == null) return;

        bool isOwned = owned.Contains(effect);
        img.color = !isOwned ? lockedColor : (active == effect ? brightColor : dimColor);
    }
}