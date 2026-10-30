using UnityEngine;

/// <summary>
/// 탑다운 쿼터뷰 추적 카메라 (Orthographic). Hole 크기에 맞춰 Orthographic Size를 부드럽게 조정한다.
/// FOV 대신 Ortho Size를 쓰므로 원근 왜곡 없이 시야가 넓어진다.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private HoleController hole;
    [Tooltip("이동 방향으로 카메라를 살짝 앞서 보내는 정도(초)")]
    [SerializeField] private float lookAhead = 0.12f;
    [Tooltip("화면 중심을 위/아래로 보정 (상단 Mission UI 때문에 Hole을 약간 아래쪽에 두려면 +)")]
    [SerializeField] private float verticalOffset = 0.0f;

    private Camera cam;
    private Transform tr;
    private float zoomMultiplier = 1f;
    private Vector3 smoothPos;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        tr = transform;
        cam.orthographic = true;
    }

    public void ResetForLevel(LevelData level)
    {
        zoomMultiplier = level.cameraZoomMultiplier <= 0f ? 1f : level.cameraZoomMultiplier;
        GameSettings s = hole.Settings;
        cam.orthographicSize = hole.SizeTable.GetCameraSizeForGrowth(hole.Growth) * zoomMultiplier;
        smoothPos = ComputeFocus();
        Apply(smoothPos);
    }

    private Vector3 ComputeFocus()
    {
        Vector3 f = hole.Position;
        Vector2 v = hole.Velocity;
        f.x += v.x * lookAhead;
        f.z += v.y * lookAhead + verticalOffset;
        return f;
    }

    private void LateUpdate()
    {
        GameSettings s = hole.Settings;
        float dt = Time.unscaledDeltaTime;

        Vector3 target = ComputeFocus();
        smoothPos = Vector3.Lerp(smoothPos, target, 1f - Mathf.Exp(-s.cameraSmooth * dt));
        Apply(smoothPos);

        float targetSize = hole.SizeTable.GetCameraSizeForGrowth(hole.Growth) * zoomMultiplier;
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, 1f - Mathf.Exp(-s.cameraZoomSmooth * dt));
    }

    private void Apply(Vector3 focus)
    {
        GameSettings s = hole.Settings;
        Quaternion rot = Quaternion.Euler(s.cameraPitch, s.cameraYaw, 0f);
        tr.rotation = rot;
        tr.position = focus - rot * Vector3.forward * s.cameraDistance;
    }
}
