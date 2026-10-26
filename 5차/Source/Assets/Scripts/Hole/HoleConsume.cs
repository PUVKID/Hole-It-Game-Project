using UnityEngine;

/// <summary>
/// 바닥 아래의 넓은 Consume Trigger. Falling 상태로 떨어진 오브젝트가 닿으면
/// 점수/미션/성장을 보고하고 풀로 반환한다 (EdibleObject.Consume).
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class HoleConsume : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != Layers.Falling) return;
        EdibleObject e = other.GetComponentInParent<EdibleObject>();
        if (e != null && e.State == SwallowState.Falling) e.Consume();
    }
}
