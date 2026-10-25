using UnityEngine;

public class ArtifactController : MonoBehaviour
{
    private bool playerHasProtection = false;
    private bool aiHasProtection = false;

    public void ActivateProtection(BoardCardOwner owner)
    {
        if (owner == BoardCardOwner.Player)
        {
            playerHasProtection = true;
            Debug.Log("🛡️ PLAYER Protection Artifact activated!");
        }
        else if (owner == BoardCardOwner.AI)
        {
            aiHasProtection = true;
            Debug.Log("🛡️ AI Protection Artifact activated!");
        }
    }

    public bool ConsumeProtection(BoardCardOwner defender)
    {
        if (defender == BoardCardOwner.Player && playerHasProtection)
        {
            playerHasProtection = false;
            Debug.Log("🛡️ Player's Protection triggered! AI match attempt was blocked!");
            return true;
        }

        if (defender == BoardCardOwner.AI && aiHasProtection)
        {
            aiHasProtection = false;
            Debug.Log("🛡️ AI's Protection triggered! Player match attempt was blocked!");
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
