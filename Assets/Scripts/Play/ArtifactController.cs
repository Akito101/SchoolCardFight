using UnityEngine;

public class ArtifactController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private ArtifactUIController artifactUIController;

    private bool playerHasProtection = false;
    private bool aiHasProtection = false;

    public void ActivateProtection(BoardCardOwner owner)
    {
        if (owner == BoardCardOwner.Player)
        {
            playerHasProtection = true;
        }
        else if (owner == BoardCardOwner.AI)
        {
            aiHasProtection = true;
        }
    }

    public bool ConsumeProtection(BoardCardOwner defender, System.Action onNotificationComplete = null)
    {
        if (defender == BoardCardOwner.Player && playerHasProtection)
        {
            playerHasProtection = false;

            if (artifactUIController != null)
            {
                artifactUIController.ShowArtifactNotification(ArtifactEffect.Protection, onNotificationComplete);
            }

            return true;
        }

        if (defender == BoardCardOwner.AI && aiHasProtection)
        {
            aiHasProtection = false;

            if (artifactUIController != null)
            {
                artifactUIController.ShowArtifactNotification(ArtifactEffect.Protection, onNotificationComplete);
            }
            return true;
        }

        return false;
    }

    public void ResetProtectionOnMatchWinner(BoardCardOwner winner)
    {
        if (winner == BoardCardOwner.Player && playerHasProtection)
        {
            playerHasProtection = false;
            Debug.Log("🛡️ Player won a match! Player's active Protection has been reset.");
        }
        else if (winner == BoardCardOwner.AI && aiHasProtection)
        {
            aiHasProtection = false;
            Debug.Log("🛡️ AI won a match! AI's active Protection has been reset.");
        }
    }

    public void ResetAllProtection()
    {
        playerHasProtection = false;
        aiHasProtection = false;
    }
}
