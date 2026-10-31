using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelNode : MonoBehaviour
{
    public string enemyName;

    [Tooltip("Backup only. The real deck size is Deck Size on the LevelRewards asset. This number is used if that one is 0 or missing.")]
    public int minCards = 10;

    public Button button;
    public TextMeshProUGUI label;
    public GameObject lockIcon;     // optional
    public GameObject clearedMark;  // optional
}