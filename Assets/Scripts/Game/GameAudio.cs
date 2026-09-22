using System.Collections.Generic;
using UnityEngine;

public enum GameBgm { Top, Battle, Boss }

public enum GameSound
{
    Click, Attack, Skill, Heal, Hit, Win, Lose, Slash, HammerStun,
    Spear, Gun, Arrow, Stone, Fireball, WoodPush, TrapSet, Charge,
    BirdRetreat, ClawDouble, Defense, ShieldReflect, ArmorReduce, WallCounter,
    DragonBreath, DragonRoar
}

public sealed class GameAudio : MonoBehaviour
{
    private static GameAudio instance;
    private AudioSource sfxSource;
    private AudioSource bgmSource;
    private readonly Dictionary<GameSound, AudioClip> clips = new();
    private readonly Dictionary<GameBgm, AudioClip> bgmClips = new();
    private bool bgmRequested;

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
        DontDestroyOnLoad(gameObject);
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.volume = 0.45f;
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.volume = 0.12f;
    }

    public void EnsureBgm() => PlayBgm(GameBgm.Battle, true);

    public void PlayBgm(GameBgm bgm) => PlayBgm(bgm, true);

    private void PlayBgm(GameBgm bgm, bool restartIfDifferent)
    {
        EnsureSources();
        bgmRequested = true;
        var clip = GetBgmClip(bgm);
        if (clip == null) return;
        if (!restartIfDifferent && bgmSource.isPlaying) return;
        if (bgmSource.isPlaying && bgmSource.clip == clip) return;
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    private void Update()
    {
        // WebGL may reject the first Play call until the user interacts with the page.
        // Retry the already-selected clip on a later frame after that gesture unlocks audio.
        if (bgmRequested && bgmSource != null && bgmSource.clip != null && !bgmSource.isPlaying)
        {
            bgmSource.Play();
        }
    }

    public void Play(GameSound sound)
    {
        EnsureSources();
        if (!clips.TryGetValue(sound, out var clip) || clip == null)
        {
            clip = LoadSoundClip(sound) ?? CreateClipFor(sound);
            clips[sound] = clip;
        }
        if (clip != null) sfxSource.PlayOneShot(clip);
    }

    public void PlaySkillSound(SkillType skillType)
    {
        GameSound sound = skillType switch
        {
            SkillType.Slash => GameSound.Slash,
            SkillType.StunBlow => GameSound.HammerStun,
            SkillType.Arrow => GameSound.Arrow,
            SkillType.Gun => GameSound.Gun,
            SkillType.Spear => GameSound.Spear,
            SkillType.Stone => GameSound.Stone,
            SkillType.Fireball => GameSound.Fireball,
            SkillType.WoodPush => GameSound.WoodPush,
            SkillType.WaterHeal or SkillType.Heal => GameSound.Heal,
            SkillType.Soil => GameSound.TrapSet,
            SkillType.HorseCharge => GameSound.Charge,
            SkillType.BirdRetreat => GameSound.BirdRetreat,
            SkillType.TigerTwinClaw => GameSound.ClawDouble,
            SkillType.Shield => GameSound.ShieldReflect,
            SkillType.Armor => GameSound.ArmorReduce,
            SkillType.Wall => GameSound.WallCounter,
            SkillType.Dragon => GameSound.DragonBreath,
            _ => GameSound.Skill
        };
        Play(sound);
    }

    private AudioClip GetBgmClip(GameBgm bgm)
    {
        if (bgmClips.TryGetValue(bgm, out var cached)) return cached;
        string fileName = bgm switch
        {
            GameBgm.Top => "bgm_top_loop",
            GameBgm.Boss => "bgm_boss_loop",
            _ => "battle_bgm_loop_01"
        };
        cached = Resources.Load<AudioClip>("Audio/BGM/" + fileName);
        bgmClips[bgm] = cached;
        return cached;
    }

    private static AudioClip LoadSoundClip(GameSound sound)
    {
        string fileName = sound switch
        {
            GameSound.Click => "sfx_ui_click",
            GameSound.Attack => "sfx_normal_attack",
            GameSound.Skill => "sfx_charge",
            GameSound.Heal => "sfx_heal",
            GameSound.Hit => "sfx_hit",
            GameSound.Win => "sfx_victory",
            GameSound.Lose => "sfx_defeat",
            GameSound.Slash => "sfx_slash",
            GameSound.HammerStun => "sfx_hammer_stun",
            GameSound.Spear => "sfx_spear",
            GameSound.Gun => "sfx_gun",
            GameSound.Arrow => "sfx_arrow",
            GameSound.Stone => "sfx_stone",
            GameSound.Fireball => "sfx_fireball",
            GameSound.WoodPush => "sfx_wood_push",
            GameSound.TrapSet => "sfx_trap_set",
            GameSound.Charge => "sfx_charge",
            GameSound.BirdRetreat => "sfx_bird_retreat",
            GameSound.ClawDouble => "sfx_claw_double",
            GameSound.Defense => "sfx_defense",
            GameSound.ShieldReflect => "sfx_shield_reflect",
            GameSound.ArmorReduce => "sfx_armor_reduce",
            GameSound.WallCounter => "sfx_wall_counter",
            GameSound.DragonBreath => "sfx_dragon_breath",
            GameSound.DragonRoar => "sfx_dragon_roar",
            _ => null
        };
        return string.IsNullOrEmpty(fileName) ? null : Resources.Load<AudioClip>("Audio/SE/" + fileName);
    }

    private void EnsureSources()
    {
        if (sfxSource != null && bgmSource != null) return;
        Awake();
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
            float t = i / (float)sampleRate;
            float envelope = Mathf.Clamp01(1f - (i / (float)totalSamples));
            data[i] = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * t) * volume * envelope;
        }
        var clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
