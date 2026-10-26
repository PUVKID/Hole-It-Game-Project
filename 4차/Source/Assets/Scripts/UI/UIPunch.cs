using UnityEngine;

/// <summary>짧은 Scale Punch. 코루틴/DOTween 없이 Update 기반(시간 정지 중에도 동작)이라 GC가 없다.</summary>
public class UIPunch : MonoBehaviour
{
    [SerializeField] private float amount = 0.25f;
    [SerializeField] private float duration = 0.28f;

    private float t = -1f;
    private Vector3 baseScale = Vector3.one;
    private bool cached;

    private void Cache()
    {
        if (cached) return;
        baseScale = transform.localScale;
        cached = true;
    }

    public void Play(float amountMultiplier = 1f)
    {
        Cache();
        t = 0f;
        amountScale = amountMultiplier;
        enabled = true;
    }

    private float amountScale = 1f;

    private void Awake()
    {
        Cache();
    }

    private void Update()
    {
        if (t < 0f) { enabled = false; return; }
        t += Time.unscaledDeltaTime;
        float k = Mathf.Clamp01(t / Mathf.Max(.01f,duration));
        float s = 1f + amount * amountScale * Mathf.Sin(k * Mathf.PI);
        transform.localScale = baseScale * s;
        if (k >= 1f)
        {
            transform.localScale = baseScale;
            t = -1f;
            enabled = false;
        }
    }
}
