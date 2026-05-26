using UnityEngine;

public static class PlayerLocator
{
    public const string MainPlayerName = "Player";

    public static GameObject FindMainPlayer()
    {
        GameObject namedPlayer = GameObject.Find(MainPlayerName);
        if (IsUsablePlayer(namedPlayer))
            return namedPlayer;

        GameObject[] taggedPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject taggedPlayer in taggedPlayers)
        {
            if (taggedPlayer.name == MainPlayerName && IsUsablePlayer(taggedPlayer))
                return taggedPlayer;
        }

        foreach (GameObject taggedPlayer in taggedPlayers)
        {
            if (IsUsablePlayer(taggedPlayer))
                return taggedPlayer;
        }

        return null;
    }

    public static Actor FindMainActor()
    {
        GameObject player = FindMainPlayer();
        return player != null ? player.GetComponent<Actor>() : null;
    }

    public static bool IsMainPlayer(GameObject candidate)
    {
        if (!IsUsablePlayer(candidate))
            return false;

        return FindMainPlayer() == candidate;
    }

    private static bool IsUsablePlayer(GameObject candidate)
    {
        return candidate != null
            && candidate.activeInHierarchy
            && candidate.CompareTag("Player")
            && candidate.GetComponent<Platformer2DController>() != null;
    }
}
