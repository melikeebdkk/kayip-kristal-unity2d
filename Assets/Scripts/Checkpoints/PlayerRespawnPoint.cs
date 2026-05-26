using UnityEngine;

public static class PlayerRespawnPoint
{
    private static Vector3? _current;

    public static void Set(Vector3 position) => _current = position;

    public static Vector3 Resolve(Vector3 fallbackPosition) => _current ?? fallbackPosition;

    public static void Clear() => _current = null;
}
