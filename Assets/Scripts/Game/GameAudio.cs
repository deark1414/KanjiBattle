using UnityEngine;

public enum GameSound
{
    Click,
    Attack,
    Skill,
    Heal,
    Hit,
    Win,
    Lose
}

public sealed class GameAudio : MonoBehaviour
{
    private static GameAudio instance;
    private AudioSource sfxSource;
    private AudioSource bgmSource;
    private readonly System.Collections.Generic.Dictionary<GameSound, AudioClip> clips = new();

    public static GameAudio Instance
    {
        get
        {
            if (instance == null)
            {
                var obj = new GameObject("GameAudio");
                DontDestroyOnLoad(obj);
                instance = obj.AddComponent<GameAudio>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.volume = 0.45f;

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.volume = 0.12f;
    }

    public void EnsureBgm()
    {
        if (bgmSource == null) Awake();
        if (bgmSource.isPlaying) return;
        bgmSource.clip = CreateToneClip("BGM", 0.08f, 20f, 196f, 247f, 294f, 247f);
        bgmSource.Play();
    }

    public void Play(GameSound sound)
    {
        if (sfxSource == null) Awake();
        if (!clips.TryGetValue(sound, out var clip) || clip == null)
        {
            clip = CreateClipFor(sound);
            clips[sound] = clip;
        }
        sfxSource.PlayOneShot(clip);
    }

    private static AudioClip CreateClipFor(GameSound sound)
    {
        return sound switch
        {
            GameSound.Click => CreateToneClip("Click", 0.25f, 0.08f, 440f, 660f),
            GameSound.Attack => CreateToneClip("Attack", 0.35f, 0.12f, 220f, 180f),
            GameSound.Skill => CreateToneClip("Skill", 0.40f, 0.18f, 440f, 660f, 880f),
            GameSound.Heal => CreateToneClip("Heal", 0.35f, 0.20f, 523f, 659f, 784f),
            GameSound.Hit => CreateToneClip("Hit", 0.30f, 0.10f, 160f, 110f),
            GameSound.Win => CreateToneClip("Win", 0.45f, 0.45f, 392f, 523f, 659f, 784f),
            GameSound.Lose => CreateToneClip("Lose", 0.45f, 0.45f, 294f, 247f, 196f),
            _ => CreateToneClip("Tone", 0.25f, 0.10f, 440f)
        };
    }

    private static AudioClip CreateToneClip(string name, float volume, float duration, params float[] notes)
    {
        const int sampleRate = 22050;
        int totalSamples = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
        var data = new float[totalSamples];
        int noteCount = Mathf.Max(1, notes.Length);
        int noteSamples = Mathf.Max(1, totalSamples / noteCount);

        for (int i = 0; i < totalSamples; i++)
        {
            int noteIndex = Mathf.Min(noteCount - 1, i / noteSamples);
            float freq = notes[noteIndex];
            float t = i / (float)sampleRate;
            float envelope = Mathf.Clamp01(1f - (i / (float)totalSamples));
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * volume * envelope;
        }

        var clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
