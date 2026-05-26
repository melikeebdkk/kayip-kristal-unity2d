using UnityEngine;

public class CrystalPulseEffect : MonoBehaviour
{
    public float BobHeight = 0.18f;
    public float BobSpeed = 2.5f;
    public float RotateSpeed = 80f;
    public float PulseAmount = 0.12f;

    private Vector3 _startPosition;
    private Vector3 _startScale;

    private void Awake()
    {
        _startPosition = transform.position;
        _startScale = transform.localScale;
    }

    private void Update()
    {
        float wave = Mathf.Sin(Time.time * BobSpeed);
        transform.position = _startPosition + Vector3.up * (wave * BobHeight);
        transform.Rotate(Vector3.forward, RotateSpeed * Time.deltaTime);
        transform.localScale = _startScale * (1f + Mathf.Abs(wave) * PulseAmount);
    }
}
