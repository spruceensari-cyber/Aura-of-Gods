using System.Collections;
using UnityEngine;

public enum AOGAudioCue
{
    UIConfirm,
    UIBack,
    AbilityCast,
    AbilityImpact,
    BasicAttack,
    ChampionHit,
    ChampionDeath,
    TowerShot,
    ObjectiveSpawn,
    ObjectiveSlain,
    MatchStart,
    Victory,
    Defeat
}

/// <summary>
/// Central audio bus and cue router.
/// Uses assignable production clips when present and procedural fallback tones/noise otherwise.
/// </summary>
public class AOGAudioDirectorRuntime : MonoBehaviour
{
    public static AOGAudioDirectorRuntime Instance { get; private set; }

    [Header("Music")]
    public AudioClip menuMusic;
    public AudioClip matchMusic;
    public AudioClip bossMusic;

    [Header("SFX")]
    public AudioClip uiConfirm;
    public AudioClip uiBack;
    public AudioClip abilityCast;
    public AudioClip abilityImpact;
    public AudioClip basicAttack;
    public AudioClip championHit;
    public AudioClip championDeath;
    public AudioClip towerShot;
    public AudioClip objectiveSpawn;
    public AudioClip objectiveSlain;
    public AudioClip matchStart;
    public AudioClip victory;
    public AudioClip defeat;

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private AudioSource uiSource;
    private AudioSource voiceSource;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<AOGAudioDirectorRuntime>() != null)
            return;

        GameObject obj = new GameObject("AOG_Audio_Director_Runtime");
        obj.AddComponent<AOGAudioDirectorRuntime>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        musicSource = CreateSource("Music", true, 0.55f);
        sfxSource = CreateSource("SFX", false, 0.88f);
        uiSource = CreateSource("UI", false, 0.72f);
        voiceSource = CreateSource("Voice", false, 0.92f);
    }

    public void PlayMatchMusic()
    {
        PlayMusic(matchMusic, CreateDroneFallback());
    }

    public void PlayBossMusic()
    {
        PlayMusic(bossMusic, CreatePulseFallback());
    }

    public void StopMusic(float fadeSeconds = 0.4f)
    {
        StartCoroutine(FadeOutMusic(fadeSeconds));
    }

    public void PlayCue(AOGAudioCue cue, Vector3? worldPosition = null)
    {
        AudioClip clip = GetAssignedClip(cue) ?? CreateFallback(cue);
        if (clip == null)
            return;

        AudioSource source = cue == AOGAudioCue.UIConfirm || cue == AOGAudioCue.UIBack ? uiSource : sfxSource;
        source.pitch = GetPitch(cue);
        source.spatialBlend = worldPosition.HasValue ? 0.65f : 0f;
        if (worldPosition.HasValue)
            source.transform.position = worldPosition.Value;
        source.PlayOneShot(clip);
    }

    public void PlayVoice(AudioClip clip)
    {
        if (clip == null)
            return;
        voiceSource.Stop();
        voiceSource.clip = clip;
        voiceSource.Play();
    }

    private void PlayMusic(AudioClip preferred, AudioClip fallback)
    {
        AudioClip clip = preferred ?? fallback;
        if (clip == null)
            return;

        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    private AudioSource CreateSource(string label, bool loop, float volume)
    {
        GameObject child = new GameObject(label + "_Bus");
        child.transform.SetParent(transform, false);
        AudioSource source = child.AddComponent<AudioSource>();
        source.loop = loop;
        source.playOnAwake = false;
        source.volume = volume;
        return source;
    }

    private AudioClip GetAssignedClip(AOGAudioCue cue)
    {
        return cue switch
        {
            AOGAudioCue.UIConfirm => uiConfirm,
            AOGAudioCue.UIBack => uiBack,
            AOGAudioCue.AbilityCast => abilityCast,
            AOGAudioCue.AbilityImpact => abilityImpact,
            AOGAudioCue.BasicAttack => basicAttack,
            AOGAudioCue.ChampionHit => championHit,
            AOGAudioCue.ChampionDeath => championDeath,
            AOGAudioCue.TowerShot => towerShot,
            AOGAudioCue.ObjectiveSpawn => objectiveSpawn,
            AOGAudioCue.ObjectiveSlain => objectiveSlain,
            AOGAudioCue.MatchStart => matchStart,
            AOGAudioCue.Victory => victory,
            AOGAudioCue.Defeat => defeat,
            _ => null
        };
    }

    private AudioClip CreateFallback(AOGAudioCue cue)
    {
        return cue switch
        {
            AOGAudioCue.UIConfirm => CreateTone("UIConfirm", 760f, 0.07f, 0.24f),
            AOGAudioCue.UIBack => CreateTone("UIBack", 310f, 0.09f, 0.22f),
            AOGAudioCue.AbilityCast => CreateSweep("AbilityCast", 220f, 840f, 0.18f, 0.22f),
            AOGAudioCue.AbilityImpact => CreateImpact("AbilityImpact", 0.16f, 0.32f),
            AOGAudioCue.BasicAttack => CreateTone("BasicAttack", 170f, 0.08f, 0.20f),
            AOGAudioCue.ChampionHit => CreateImpact("ChampionHit", 0.10f, 0.28f),
            AOGAudioCue.ChampionDeath => CreateSweep("ChampionDeath", 220f, 70f, 0.52f, 0.32f),
            AOGAudioCue.TowerShot => CreateSweep("TowerShot", 120f, 520f, 0.22f, 0.30f),
            AOGAudioCue.ObjectiveSpawn => CreateSweep("ObjectiveSpawn", 90f, 300f, 0.90f, 0.34f),
            AOGAudioCue.ObjectiveSlain => CreateImpact("ObjectiveSlain", 0.55f, 0.36f),
            AOGAudioCue.MatchStart => CreateSweep("MatchStart", 180f, 920f, 0.75f, 0.36f),
            AOGAudioCue.Victory => CreateSweep("Victory", 260f, 1040f, 1.2f, 0.38f),
            AOGAudioCue.Defeat => CreateSweep("Defeat", 240f, 65f, 1.15f, 0.34f),
            _ => null
        };
    }

    private float GetPitch(AOGAudioCue cue)
    {
        if (cue == AOGAudioCue.BasicAttack || cue == AOGAudioCue.ChampionHit)
            return Random.Range(0.94f, 1.06f);
        return 1f;
    }

    private AudioClip CreateTone(string name, float frequency, float duration, float amplitude)
    {
        int sampleRate = 44100;
        int samples = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float env = 1f - i / (float)samples;
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * amplitude * env;
        }
        AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip CreateSweep(string name, float startFrequency, float endFrequency, float duration, float amplitude)
    {
        int sampleRate = 44100;
        int samples = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
        float[] data = new float[samples];
        float phase = 0f;
        for (int i = 0; i < samples; i++)
        {
            float x = i / (float)samples;
            float freq = Mathf.Lerp(startFrequency, endFrequency, x);
            phase += 2f * Mathf.PI * freq / sampleRate;
            float env = Mathf.Sin(Mathf.PI * x);
            data[i] = Mathf.Sin(phase) * amplitude * env;
        }
        AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip CreateImpact(string name, float duration, float amplitude)
    {
        int sampleRate = 44100;
        int samples = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float x = i / (float)samples;
            float env = Mathf.Exp(-8f * x);
            float low = Mathf.Sin(2f * Mathf.PI * 95f * i / sampleRate);
            float noise = Random.Range(-1f, 1f) * 0.55f;
            data[i] = (low + noise) * amplitude * env;
        }
        AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip CreateDroneFallback()
    {
        int sampleRate = 44100;
        int samples = sampleRate * 8;
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float a = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.08f;
            float b = Mathf.Sin(2f * Mathf.PI * 82.5f * t) * 0.05f;
            float shimmer = Mathf.Sin(2f * Mathf.PI * 220f * t) * (0.015f + 0.008f * Mathf.Sin(t * 0.8f));
            data[i] = a + b + shimmer;
        }
        AudioClip clip = AudioClip.Create("AOG_Procedural_Match_Drone", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip CreatePulseFallback()
    {
        int sampleRate = 44100;
        int samples = sampleRate * 8;
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float pulse = Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * 1.5f * t));
            data[i] = Mathf.Sin(2f * Mathf.PI * 72f * t) * 0.12f * pulse;
        }
        AudioClip clip = AudioClip.Create("AOG_Procedural_Boss_Pulse", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private IEnumerator FadeOutMusic(float duration)
    {
        if (musicSource == null || !musicSource.isPlaying)
            yield break;

        float start = musicSource.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(start, 0f, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        musicSource.Stop();
        musicSource.volume = start;
    }
}
