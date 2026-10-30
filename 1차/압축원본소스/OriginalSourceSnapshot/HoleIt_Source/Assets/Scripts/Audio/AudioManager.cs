using UnityEngine;

/// <summary>
/// 효과음 재생. AudioClip과 AudioSource는 모두 Inspector에서 지정한다 (코드 하드코딩/런타임 생성 없음).
/// 클립이 비어 있으면 해당 효과음은 조용히 건너뛴다.
/// </summary>
public class AudioManager : MonoBehaviour
{
    [Header("Sources (미리 배치된 AudioSource들)")]
    [SerializeField] private AudioSource[] sfxSources;
    [SerializeField] private AudioSource moveLoopSource;

    [Header("Clips")]
    [SerializeField] private AudioClip moveLoop;
    [SerializeField] private AudioClip attract;
    [SerializeField] private AudioClip swallow;
    [SerializeField] private AudioClip growth;
    [SerializeField] private AudioClip missionComplete;
    [SerializeField] private AudioClip levelClear;
    [SerializeField] private AudioClip timeWarning;
    [SerializeField] private AudioClip fail;
    [SerializeField] private AudioClip button;

    [Header("Volume")]
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.8f;
    [Range(0f, 1f)] [SerializeField] private float moveVolume = 0.25f;

    public static AudioManager Instance { get; private set; }

    private int nextSource;
    private float lastSwallowTime;
    private float swallowPitch = 1f;

    private void Awake()
    {
        Instance = this;
        if (moveLoopSource != null && moveLoop != null)
        {
            moveLoopSource.clip = moveLoop;
            moveLoopSource.loop = true;
            moveLoopSource.volume = 0f;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>이동 속도(0~1)에 맞춰 이동 루프 볼륨 조절.</summary>
    public void SetMoveIntensity(float speed01)
    {
        if (moveLoopSource == null || moveLoop == null) return;
        float target = speed01 > 0.05f ? moveVolume * Mathf.Clamp01(speed01) : 0f;
        moveLoopSource.volume = Mathf.MoveTowards(moveLoopSource.volume, target, Time.unscaledDeltaTime * 2f);
        if (moveLoopSource.volume > 0.01f && !moveLoopSource.isPlaying) moveLoopSource.Play();
        else if (moveLoopSource.volume <= 0.01f && moveLoopSource.isPlaying) moveLoopSource.Pause();
    }

    public void PlayAttract() { Play(attract, 0.5f, 1f); }

    /// <summary>연속으로 먹을수록 피치가 약간 올라가는 쾌감 연출.</summary>
    public void PlaySwallow()
    {
        float now = Time.unscaledTime;
        if (now - lastSwallowTime < 0.6f) swallowPitch = Mathf.Min(1.5f, swallowPitch + 0.04f);
        else swallowPitch = 1f;
        lastSwallowTime = now;
        Play(swallow, 0.7f, swallowPitch);
    }

    public void PlayGrowth() { Play(growth, 0.9f, 1f); }
    public void PlayMissionComplete() { Play(missionComplete, 1f, 1f); }
    public void PlayLevelClear() { Play(levelClear, 1f, 1f); }
    public void PlayTimeWarning() { Play(timeWarning, 0.8f, 1f); }
    public void PlayFail() { Play(fail, 1f, 1f); }
    public void PlayButton() { Play(button, 0.6f, 1f); }

    private void Play(AudioClip clip, float volume, float pitch)
    {
        if (clip == null || sfxSources == null || sfxSources.Length == 0) return;
        AudioSource src = sfxSources[nextSource];
        nextSource = (nextSource + 1) % sfxSources.Length;
        src.pitch = pitch;
        src.PlayOneShot(clip, volume * sfxVolume);
    }
}
