using UnityEngine;

public enum CardCategory
{
    Spirits,
    Legends,
    Supernatural,
    Beasts,
    Humans
}

public enum ArtifactEffect
{
    None,
    Protection,
    ReshuffleEnemyCard,
    StealCard
}

[CreateAssetMenu(fileName = "New Card", menuName = "Cards/Card")]
public class CardData : ScriptableObject
{
    public string cardName;
    public Sprite cardFrontSprite;
    public CardCategory category;

    [Header("Only fill these in if this card is an Artifact")]
    public bool isArtifact;
    public ArtifactEffect artifactEffect;
}