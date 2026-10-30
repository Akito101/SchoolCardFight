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
}