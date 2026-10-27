using UnityEngine;
using UnityEngine.Serialization;

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
    ReshuffleOpponentDeck,
    StealCard
}

[CreateAssetMenu(fileName = "New Card", menuName = "Cards/Card")]
public class CardData : ScriptableObject
{
    // Must be unique for every card, saves and the collection use it
    public string cardName;

    // The card's picture. Old cards saved as "cardFrontSprite" load into this slot too.
    [FormerlySerializedAs("cardFrontSprite")]
    public Sprite cardArt;

    public CardCategory category;

    [Header("Rarity (match the stars drawn on the art)")]
    [Range(1, 3)] public int stars = 1;

    [Header("Optional sound when this card is revealed or played (meant for some 3-star cards)")]
    public AudioClip signatureSfx;

    // Set by the code that builds Artifact cards, not by hand
    [HideInInspector] public bool isArtifact;
    [HideInInspector] public ArtifactEffect artifactEffect;

    // Keeps the battle scene code working (it uses this name for the picture)
    public Sprite cardFrontSprite
    {
        get => cardArt;
        set => cardArt = value;
    }

#if UNITY_EDITOR
    // If a card has no Card Name, use the asset's file name so it's never empty
    void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(cardName) && !string.IsNullOrEmpty(name))
        {
            cardName = name;
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif
}