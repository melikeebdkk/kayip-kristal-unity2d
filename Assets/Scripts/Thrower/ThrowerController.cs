using UnityEngine;

[RequireComponent(typeof(Thrower))]
public class ThrowerController : MonoBehaviour
{
    [Header("Controls")]
    public KeyCode ThrowKey = KeyCode.F;

    private Thrower _thrower;

    private void Awake() => _thrower = GetComponent<Thrower>();

    private void Update()
    {
        if (Input.GetKeyDown(ThrowKey)) _thrower.ThrowProjectile();
    }
}
