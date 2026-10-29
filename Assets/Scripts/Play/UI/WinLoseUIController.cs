using UnityEngine;

public class WinLoseUIController : MonoBehaviour
{
    private const int AITypeCount = 4;

    [Header("AI-specific result UI")]
    [SerializeField] private GameObject[] winUI = new GameObject[4];
    [SerializeField] private GameObject[] loseUI = new GameObject[4];

    private void Awake()
    {
        winUI = EnsureSlotCount(winUI);
        loseUI = EnsureSlotCount(loseUI);
        CreatePlaceholders(winUI, "Win");
        CreatePlaceholders(loseUI, "Lose");
        HideAll();
    }

    public void ShowResult(BattleTestManager.AIType aiType, bool playerWon)
    {
        HideAll();

        GameObject[] ui = playerWon ? winUI : loseUI;
        int index = Mathf.Clamp((int)aiType, 0, ui.Length - 1);
        ui[index].SetActive(true);
    }

    private GameObject[] EnsureSlotCount(GameObject[] ui)
    {
        GameObject[] slots = new GameObject[AITypeCount];
        if (ui != null)
        {
            for (int i = 0; i < Mathf.Min(ui.Length, slots.Length); i++)
            {
                slots[i] = ui[i];
            }
        }

        return slots;
    }

    private void CreatePlaceholders(GameObject[] ui, string result)
    {
        for (int i = 0; i < ui.Length; i++)
        {
            if (ui[i] != null) continue;

            GameObject placeholder = new GameObject(
                $"{result}{(BattleTestManager.AIType)i}UI");
            placeholder.transform.SetParent(transform, false);
            ui[i] = placeholder;
        }
    }

    private void HideAll()
    {
        Hide(winUI);
        Hide(loseUI);
    }

    private void Hide(GameObject[] ui)
    {
        foreach (GameObject item in ui)
        {
            if (item != null) item.SetActive(false);
        }
    }
}
