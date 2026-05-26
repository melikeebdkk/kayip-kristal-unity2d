using System.Collections.Generic;
using UnityEngine;

public class Switch : MonoBehaviour
{
    public Door[] Doors;
    public string UserTag = "";
    public bool IsSticky;
    [Header("Audio")]
    public AudioClip TurnOnSoundFX;
    public AudioClip TurnOffSoundFX;
    [Header("Animator")]
    public bool UseAnimator = true;
    public string AnimBoolParamName = "On";

    [Header("Debug")]
    [SerializeField] private bool _isOn;
    private Animator _animator;
    private BoxCollider2D _boxCollider2D;
    private Color _gizmosColor;
    private readonly HashSet<Collider2D> _activeUsers = new();

    public bool IsOn
    {
        get => _isOn;
        set
        {
            if (_isOn == value) return;
            _isOn = value;
            PlaySoundFX(_isOn ? TurnOnSoundFX : TurnOffSoundFX);
            if (UseAnimator && _animator) _animator.SetBool(AnimBoolParamName, _isOn);
            foreach (Door door in Doors) if (door) door.IsOpen = _isOn;
        }
    }

    private void Awake() => _animator = GetComponent<Animator>();

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (!IsAllowedUser(collider)) return;
        _activeUsers.Add(collider);
        IsOn = true;
    }

    private void OnTriggerExit2D(Collider2D collider)
    {
        if (!IsAllowedUser(collider)) return;
        _activeUsers.Remove(collider);
        if (IsSticky) return;
        IsOn = _activeUsers.Count > 0;
    }

    private void OnValidate() => IsOn = _isOn;

    private void OnDisable()
    {
        _activeUsers.Clear();
        if (!IsSticky) IsOn = false;
    }

    private bool IsAllowedUser(Collider2D collider) =>
        string.IsNullOrEmpty(UserTag) || collider.CompareTag(UserTag);

    private static void PlaySoundFX(AudioClip clip)
    {
        if (clip != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySoundFX(clip);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = _gizmosColor == default ?
            (TryGetComponent(out SpriteRenderer spriteRenderer) ? spriteRenderer.color : Color.gray) : _gizmosColor;
        foreach (Door door in Doors) if (door) Gizmos.DrawLine(transform.position, door.transform.position);
        if (_boxCollider2D || (!_boxCollider2D && TryGetComponent<BoxCollider2D>(out _boxCollider2D)))
            Gizmos.DrawWireCube(_boxCollider2D.bounds.center, _boxCollider2D.bounds.size);
    }
}
