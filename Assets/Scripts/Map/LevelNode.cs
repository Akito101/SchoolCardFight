using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelNode : MonoBehaviour
{
    public string enemyName;
    public int minCards = 10;

    public Button button;
    public TextMeshProUGUI label;
    public GameObject lockIcon;     // optional
    public GameObject clearedMark;  // optional
}