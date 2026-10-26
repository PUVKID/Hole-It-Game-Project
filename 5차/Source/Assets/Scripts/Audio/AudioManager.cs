using UnityEngine;
public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource[] sfxSources;
    [SerializeField] private AudioSource moveLoopSource,musicSource;
    [SerializeField] private AudioClip moveLoop,attract,swallow,growth,missionComplete,levelClear,timeWarning,fail,button,booster,levelStart;
    [SerializeField,Range(0,1)] private float sfxVolume=0.8f;
    [SerializeField,Range(0,1)] private float moveVolume=0.18f;
    private float master=0.85f,userSfx=0.8f,userMusic=0.3f;
    private int nextSource;private float lastSwallowTime,swallowPitch=1;
    public static AudioManager Instance{get;private set;}
    private void Awake(){Instance=this;if(moveLoopSource!=null){moveLoopSource.clip=moveLoop;moveLoopSource.loop=true;moveLoopSource.volume=0;}RefreshVolumes();}
    private void OnDestroy(){if(Instance==this)Instance=null;}
    private static float Read(string key,float d){float v=PlayerPrefs.GetFloat("HoleIt.Audio."+key,d);return NumberGuard.Finite(v)?Mathf.Clamp01(v):d;}
    public void RefreshVolumes(){master=PlayerPrefs.GetInt("HoleIt.Audio.Mute",0)==1?0:Read("Master",0.85f);userSfx=Read("SFX",0.8f);userMusic=Read("Music",0.3f);if(sfxSources!=null)foreach(var s in sfxSources)if(s!=null)s.volume=master*userSfx*sfxVolume;if(musicSource!=null)musicSource.volume=master*userMusic;if(moveLoopSource!=null&&master==0)moveLoopSource.volume=0;}
    public void SetMusic(AudioClip clip){if(musicSource==null||musicSource.clip==clip)return;musicSource.Stop();musicSource.clip=clip;musicSource.loop=true;musicSource.volume=master*userMusic;if(clip!=null)musicSource.Play();}
    public void StopMovement(){if(moveLoopSource!=null){moveLoopSource.volume=0;moveLoopSource.Pause();}}
    public void SetMoveIntensity(float speed){if(moveLoopSource==null||moveLoop==null)return;float v=master*userSfx*moveVolume*Mathf.Clamp01(speed);moveLoopSource.volume=Mathf.MoveTowards(moveLoopSource.volume,v,Time.unscaledDeltaTime*2);if(moveLoopSource.volume>0.005f&&!moveLoopSource.isPlaying)moveLoopSource.Play();else if(moveLoopSource.volume<=0.005f&&moveLoopSource.isPlaying)moveLoopSource.Pause();}
    public void PlayAttract(){Play(attract,0.25f,1);}
    public void PlaySwallow(){float now=Time.unscaledTime;swallowPitch=now-lastSwallowTime<0.6f?Mathf.Min(1.5f,swallowPitch+0.04f):1;lastSwallowTime=now;Play(swallow,0.7f,swallowPitch);}
    public void PlayGrowth(){Play(growth,0.85f,1);}
    public void PlayMissionComplete(){Play(missionComplete,0.85f,1);}
    public void PlayLevelClear(){Play(levelClear,0.85f,1);}
    public void PlayTimeWarning(){Play(timeWarning,0.55f,1);}
    public void PlayFail(){Play(fail,0.75f,1);}
    public void PlayButton(){Play(button,0.55f,1);}
    public void PlayBooster(){Play(booster,0.7f,1);}
    public void PlayLevelStart(){Play(levelStart,0.7f,1);}
    private void Play(AudioClip clip,float volume,float pitch){if(clip==null||sfxSources==null||sfxSources.Length==0)return;AudioSource src=sfxSources[nextSource];nextSource=(nextSource+1)%sfxSources.Length;if(src==null)return;src.pitch=pitch;src.PlayOneShot(clip,volume);}
}
