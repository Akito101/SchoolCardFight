using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelRewards", menuName = "Game/Level Rewards")]
public class LevelRewards : ScriptableObject
{
    [System.Serializable]
    public class Reward
    {
        public Sprite stamp;
        public int coins = 50;
        public int coinsOnLose = 0;

        [Header("Deck rules")]
        public int deckSize = 10;

        [Header("First clear only")]
        public ArtifactEffect artifact = ArtifactEffect.None;

        [Header("Enemy")]
        public ArtifactEffect enemyArtifact = ArtifactEffect.None;
    }

    public List<Reward> levels = new List<Reward>();

    public Reward Get(int index)
    {
        return index >= 0 && index < levels.Count ? levels[index] : null;
    }

    // ONE place that decides how many cards a level needs.
    // Reads Deck Size from the asset; if that is 0 or the asset is missing,
    // uses the fallback (the Min Cards on the level button). Never below 1.
    public static int GetDeckSize(int levelIndex, int fallback)
    {
        LevelRewards rewards = Resources.Load<LevelRewards>("LevelRewards");
        Reward reward = rewards != null ? rewards.Get(levelIndex) : null;

        int needed = reward != null ? reward.deckSize : 0;
        if (needed <= 0) needed = fallback;

        return Mathf.Max(1, needed);
    }
}