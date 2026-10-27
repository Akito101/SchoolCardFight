using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelRewards", menuName = "Game/Level Rewards")]
public class LevelRewards : ScriptableObject
{
    [System.Serializable]
    public class Reward
    {
        public Sprite stamp;                     // optional, only used if you assign a Stamp Image
        public int coins = 50;                   // paid on every win
        public int coinsOnLose = 0;

        [Header("Deck rules")]
        public int deckSize = 10;   // exact number of cards the player fights with. 0 = no limit

        [Header("First clear only")]
        public ArtifactEffect artifact = ArtifactEffect.None;   // None = no artifact for this level

        [Header("Enemy")]
        public List<CardData> enemyDeck = new List<CardData>();
        public ArtifactEffect enemyArtifact = ArtifactEffect.None;
    }

    public List<Reward> levels = new List<Reward>();   // index 0 = level 1 (the tutorial)

    public Reward Get(int index)
    {
        return index >= 0 && index < levels.Count ? levels[index] : null;
    }
}