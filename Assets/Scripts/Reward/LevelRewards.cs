using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelRewards", menuName = "Game/Level Rewards")]
public class LevelRewards : ScriptableObject
{
    [System.Serializable]
    public class Reward
    {
        public string enemyName = "Enemy";
        public Sprite stamp;                     // the enemy's seal on the win panel (optional)
        public int coins = 50;                   // paid on every win
        public int coinsOnLose = 0;

        [Header("First clear only")]
        public ArtifactEffect artifact = ArtifactEffect.None;   // None = no artifact for this level
    }

    public List<Reward> levels = new List<Reward>();   // index 0 = level 1 (the tutorial)

    public Reward Get(int index)
    {
        return index >= 0 && index < levels.Count ? levels[index] : null;
    }
}