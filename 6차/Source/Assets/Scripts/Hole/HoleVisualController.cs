using UnityEngine;

/// <summary>
/// Hole 비주얼. Stencil 마스크 디스크(바닥을 지움) + 내부 Pit 원통(깊이감) + 림 링.
/// 모든 Mesh/Material은 Editor 빌더가 만든 에셋을 Inspector로 참조한다 (런타임 생성 없음).
/// </summary>
public class HoleVisualController : MonoBehaviour
{
    [Tooltip("Stencil 마스크 디스크 (XZ 지름 = 반지름*2)")]
    [SerializeField] private Transform maskDisc;
    [Tooltip("구멍 내부 어두운 원통 (바닥이 사라진 느낌)")]
    [SerializeField] private Transform pit;
    [Tooltip("가장자리 림 링")]
    [SerializeField] private Transform rim;
    [SerializeField] private float pitDepth = 6f;
    [SerializeField] private float rimWidth = 0.18f;

    public void SetRadius(float radius, float punch)
    {
        float r = radius * punch;
        float d = r * 2f;
        if (maskDisc != null) maskDisc.localScale = new Vector3(d, 1f, d);
        if (pit != null)
        {
            pit.localScale = new Vector3(d, pitDepth * 0.5f, d);
            pit.localPosition = new Vector3(0f, -pitDepth * 0.5f, 0f);
        }
        if (rim != null)
        {
            float rd = d + rimWidth * 2f;
            rim.localScale = new Vector3(rd, 1f, rd);
        }
    }
}
