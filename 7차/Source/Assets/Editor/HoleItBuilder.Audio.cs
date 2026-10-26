using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>빌더 - 외부 에셋 없이 쓸 수 있는 간단한 효과음(WAV)을 에디터에서 합성한다. 나중에 Inspector에서 교체 가능.</summary>
public static partial class HoleItBuilder
{
    private class AudioClips
    {
        public AudioClip moveLoop, attract, swallow, growth, mission, clear, warning, fail, button;
    }

    private const int SampleRate = 22050;

    private static AudioClips CreateAudio()
    {
        AudioClips c = new AudioClips();

        // 이동 루프: 1초 동안 정수 주기라 이음새가 없다
        c.moveLoop = Wav("sfx_move", 1f, t => 0.18f * Mathf.Sin(2f * Mathf.PI * 90f * t) + 0.08f * Mathf.Sin(2f * Mathf.PI * 135f * t));

        // 끌려오는 소리: 위로 올라가는 스윕
        c.attract = Wav("sfx_attract", 0.2f, t => Env(t, 0.2f) * 0.5f * Mathf.Sin(2f * Mathf.PI * (300f * t + 750f * t * t)));

        // 삼키는 소리: 짧은 팝
        c.swallow = Wav("sfx_swallow", 0.14f, t => Env(t, 0.14f) * 0.7f * Mathf.Sin(2f * Mathf.PI * (520f * t - 1400f * t * t)));

        // 성장: 도미솔 아르페지오
        c.growth = Wav("sfx_growth", 0.34f, t => Notes(t, 0.08f, 523.25f, 659.25f, 783.99f, 1046.5f) * 0.55f);

        // 미션 완료: 두 음
        c.mission = Wav("sfx_mission", 0.3f, t => Notes(t, 0.12f, 659.25f, 880f, 1318.5f) * 0.6f);

        // 레벨 클리어: 길게 올라가는 팡파레
        c.clear = Wav("sfx_clear", 0.9f, t => Notes(t, 0.14f, 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f, 1568f) * 0.6f);

        // 시간 경고: 짧은 비프
        c.warning = Wav("sfx_warning", 0.12f, t => Env(t, 0.12f) * 0.5f * Mathf.Sin(2f * Mathf.PI * 880f * t));

        // 실패: 내려가는 소리
        c.fail = Wav("sfx_fail", 0.6f, t => Env(t, 0.6f) * 0.55f * Mathf.Sin(2f * Mathf.PI * (420f * t - 330f * t * t)));

        // 버튼: 짧은 클릭
        c.button = Wav("sfx_button", 0.06f, t => Env(t, 0.06f) * 0.4f * Mathf.Sin(2f * Mathf.PI * 1000f * t));
        return c;
    }

    /// <summary>빠른 어택 + 지수 감쇠 엔벨로프.</summary>
    private static float Env(float t, float dur)
    {
        return Mathf.Min(1f, t / 0.004f) * Mathf.Exp(-3.2f * t / dur);
    }

    /// <summary>음 높이 목록을 step 간격으로 순서대로 재생 (각 음은 자체 감쇠).</summary>
    private static float Notes(float t, float step, params float[] freqs)
    {
        int i = Mathf.Min(freqs.Length - 1, (int)(t / step));
        float local = t - i * step;
        float e = Mathf.Min(1f, local / 0.004f) * Mathf.Exp(-5f * local);
        return e * Mathf.Sin(2f * Mathf.PI * freqs[i] * t);
    }

    private static AudioClip Wav(string name, float duration, System.Func<float, float> sample)
    {
        string path = AudioDir + "/" + name + ".wav";
        int n = Mathf.CeilToInt(duration * SampleRate);
        byte[] bytes = new byte[44 + n * 2];

        WriteAscii(bytes, 0, "RIFF");
        WriteInt(bytes, 4, 36 + n * 2);
        WriteAscii(bytes, 8, "WAVE");
        WriteAscii(bytes, 12, "fmt ");
        WriteInt(bytes, 16, 16);
        WriteShort(bytes, 20, 1);            // PCM
        WriteShort(bytes, 22, 1);            // mono
        WriteInt(bytes, 24, SampleRate);
        WriteInt(bytes, 28, SampleRate * 2);
        WriteShort(bytes, 32, 2);
        WriteShort(bytes, 34, 16);
        WriteAscii(bytes, 36, "data");
        WriteInt(bytes, 40, n * 2);

        for (int i = 0; i < n; i++)
        {
            float v = Mathf.Clamp(sample((float)i / SampleRate), -1f, 1f);
            WriteShort(bytes, 44 + i * 2, (short)(v * 32000f));
        }

        File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), path), bytes);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    private static void WriteAscii(byte[] b, int o, string s)
    {
        for (int i = 0; i < s.Length; i++) b[o + i] = (byte)s[i];
    }

    private static void WriteInt(byte[] b, int o, int v)
    {
        b[o] = (byte)(v & 0xff);
        b[o + 1] = (byte)((v >> 8) & 0xff);
        b[o + 2] = (byte)((v >> 16) & 0xff);
        b[o + 3] = (byte)((v >> 24) & 0xff);
    }

    private static void WriteShort(byte[] b, int o, short v)
    {
        b[o] = (byte)(v & 0xff);
        b[o + 1] = (byte)((v >> 8) & 0xff);
    }
}
