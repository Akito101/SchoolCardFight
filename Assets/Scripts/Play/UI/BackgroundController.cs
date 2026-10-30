using UnityEngine;

public class BackgroundController : MonoBehaviour
{
    [Header("AI Background GameObjects")]
    [SerializeField] private GameObject teacherBackgroundObj;
    [SerializeField] private GameObject monitorBackgroundObj;
    [SerializeField] private GameObject studentBackgroundObj;
    [SerializeField] private GameObject childBackgroundObj;



    public void SetBackgroundForAI(BattleTestManager.AIType aiType)
    {
        // 1: Hide all background objects first
        HideAllBackgrounds();

        // 2: Enable only the background corresponding to the current AI type
        switch (aiType)
        {
            case BattleTestManager.AIType.Teacher:
                if (teacherBackgroundObj != null) teacherBackgroundObj.SetActive(true);
                break;

            case BattleTestManager.AIType.Monitor:
                if (monitorBackgroundObj != null) monitorBackgroundObj.SetActive(true);
                break;

            case BattleTestManager.AIType.Student:
                if (studentBackgroundObj != null) studentBackgroundObj.SetActive(true);
                break;

            case BattleTestManager.AIType.Child:
                if (childBackgroundObj != null) childBackgroundObj.SetActive(true);
                break;

            default:
                if (monitorBackgroundObj != null) monitorBackgroundObj.SetActive(true);
                break;
        }
    }

    private void HideAllBackgrounds()
    {
        if (teacherBackgroundObj != null) teacherBackgroundObj.SetActive(false);
        if (monitorBackgroundObj != null) monitorBackgroundObj.SetActive(false);
        if (studentBackgroundObj != null) studentBackgroundObj.SetActive(false);
        if (childBackgroundObj != null) childBackgroundObj.SetActive(false);
    }
}
