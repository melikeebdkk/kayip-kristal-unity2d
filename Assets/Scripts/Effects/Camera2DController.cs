using UnityEngine;

public class Camera2DController : MonoBehaviour
{
    public Transform TargetTransform;
    public bool FollowTarget = true;
    public Vector2 Offset;
    [Header("Easing")]
    public bool Easing = true;
    public Easings.Methods EasingMethod = 0;
    [Min(0f)] public float EasingSeconds = 3f;

    private Camera _camera;
    public Vector3 TargetPosition => TargetTransform 
        ? new Vector3(TargetTransform.position.x, TargetTransform.position.y, transform.position.z) + (Vector3)Offset
        : transform.position;
    public bool TargetPositionIsDifferent => TargetTransform && Vector3.Distance(TargetPosition, transform.position) > 0.01f;

    private void LateUpdate()
    {
        if (FollowTarget && TargetTransform && TargetPositionIsDifferent)
        {
            if (Easing) FollowTargetWithEasing();
            else FollowTargetDirectly();
        }
    }

    private void FollowTargetWithEasing()
    {
        float smoothing = EasingSeconds <= 0f ? 1f : Mathf.Clamp01(Time.deltaTime / EasingSeconds);
        transform.position = Vector3.Lerp(transform.position, TargetPosition, Easings.Perform(smoothing, EasingMethod));
    }

    private void FollowTargetDirectly()
    {
        transform.position = TargetPosition;
    }

    [ContextMenu("Reset Following")]
    private void ResetFollowing()
    {
        FollowTarget = false;
        transform.position = new Vector3(0f, 0f, transform.position.z);
    }

    private void OnDrawGizmos()
    {
        if (TargetTransform)
        {
            Gizmos.DrawLine(transform.position, TargetPosition);
            Gizmos.DrawLine(TargetPosition, TargetTransform.position);
            Gizmos.color = Color.blue;
            Gizmos.DrawCube(TargetPosition, 0.3f * Vector3.one);
            if (!_camera) _camera = GetComponent<Camera>();
            Gizmos.DrawWireCube(TargetPosition, new Vector3(2f * _camera.orthographicSize * _camera.aspect, 2f * _camera.orthographicSize));
        }
    }
}
