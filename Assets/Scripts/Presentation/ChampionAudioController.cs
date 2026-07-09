using System;
using UnityEngine;
using UnityEngine.Audio;

[Serializable]
public class ChampionAudioBank
{
    public AudioClip[] clips;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.5f, 1.5f)] public float minPitch = 0.96f;
    [Range(0.5f, 1.5f)] public float maxPitch = 1.04f;

    [NonSerialized] public int lastIndex = -1;
}

public class ChampionAudioController : MonoBehaviour
{
    [Header("Mixer Routing")]
    public AudioMixerGroup sfxMixerGroup;
    public AudioMixerGroup voiceMixerGroup;

    [Header("Movement")]
    public ChampionAudioBank footsteps = new ChampionAudioBank();

    [Header("Combat")]
    public ChampionAudioBank attackWhooshes = new ChampionAudioBank();
    public ChampionAudioBank attackImpacts = new ChampionAudioBank();
    public ChampionAudioBank hitReactions = new ChampionAudioBank();
    public ChampionAudioBank death = new ChampionAudioBank();
    public ChampionAudioBank recall = new ChampionAudioBank();

    [Header("Abilities")]
    public ChampionAudioBank qCast = new ChampionAudioBank();
    public ChampionAudioBank wCast = new ChampionAudioBank();
    public ChampionAudioBank eCast = new ChampionAudioBank();
    public ChampionAudioBank rCast = new ChampionAudioBank();
    public ChampionAudioBank qImpact = new ChampionAudioBank();
    public ChampionAudioBank wImpact = new ChampionAudioBank();
    public ChampionAudioBank eImpact = new ChampionAudioBank();
    public ChampionAudioBank rImpact = new ChampionAudioBank();

    [Header("Voice")]
    public ChampionAudioBank voiceLines = new ChampionAudioBank();
    [Min(0f)] public float voiceCooldown = 2.5f;

    private AudioSource sfxSource;
    private AudioSource voiceSource;
    private float nextVoiceTime;
    private bool proceduralFallbackConfigured;

    private void Awake()
    {
        sfxSource = EnsureSource("Champion_SFX", sfxMixerGroup, 0.9f);
        voiceSource = EnsureSource("Champion_Voice", voiceMixerGroup, 0.75f);
    }

    public void ConfigureProceduralFallback(ChampionArchetype archetype)
    {
        if (proceduralFallbackConfigured)
            return;

        proceduralFallbackConfigured = true;

        float baseFrequency;
        float weight;
        switch (archetype)
        {
            case ChampionArchetype.ArcaneCaster:
                baseFrequency = 620f;
                weight = 0.45f;
                break;
            case ChampionArchetype.Guardian:
                baseFrequency = 150f;
                weight = 1.25f;
                break;
            default:
                baseFrequency = 360f;
                weight = 0.75f;
                break;
        }

        AssignIfEmpty(footsteps, CreateVariants("step", 3, i => CreateImpactClip("step_" + i, 0.10f, 75f + i * 7f, 0.34f * weight, i + 11)));
        AssignIfEmpty(attackWhooshes, CreateVariants("whoosh", 3, i => CreateWhooshClip("whoosh_" + i, 0.18f, baseFrequency * (0.8f + i * 0.08f), i + 31)));
        AssignIfEmpty(attackImpacts, CreateVariants("impact", 3, i => CreateImpactClip("impact_" + i, 0.16f, baseFrequency * 0.42f + i * 12f, 0.72f * weight, i + 51)));
        AssignIfEmpty(hitReactions, CreateVariants("hit", 2, i => CreateImpactClip("hit_" + i, 0.12f, baseFrequency * 0.3f, 0.48f * weight, i + 71)));
        AssignIfEmpty(death, new[] { CreateSweepClip("death_fall", 0.65f, baseFrequency * 0.8f, baseFrequency * 0.18f, 0.65f) });
        AssignIfEmpty(recall, new[] { CreateSweepClip("recall_rise", 0.85f, baseFrequency * 0.7f, baseFrequency * 1.9f, 0.38f) });

        AssignAbilityBank(qCast, "q_cast", baseFrequency * 1.00f, baseFrequency * 1.55f, 0.28f);
        AssignAbilityBank(wCast, "w_cast", baseFrequency * 0.78f, baseFrequency * 1.35f, 0.34f);
        AssignAbilityBank(eCast, "e_cast", baseFrequency * 1.15f, baseFrequency * 0.72f, 0.36f);
        AssignAbilityBank(rCast, "r_cast", baseFrequency * 0.55f, baseFrequency * 2.25f, 0.62f);

        AssignIfEmpty(qImpact, new[] { CreateImpactClip("q_impact", 0.22f, baseFrequency * 0.52f, 0.7f * weight, 101) });
        AssignIfEmpty(wImpact, new[] { CreateImpactClip("w_impact", 0.25f, baseFrequency * 0.44f, 0.78f * weight, 111) });
        AssignIfEmpty(eImpact, new[] { CreateImpactClip("e_impact", 0.28f, baseFrequency * 0.36f, 0.88f * weight, 121) });
        AssignIfEmpty(rImpact, new[] { CreateImpactClip("r_impact", 0.46f, baseFrequency * 0.24f, 1f * weight, 131) });
    }

    private void AssignAbilityBank(ChampionAudioBank bank, string name, float startFrequency, float endFrequency, float length)
    {
        AssignIfEmpty(bank, new[] { CreateSweepClip(name, length, startFrequency, endFrequency, 0.42f) });
    }

    private static void AssignIfEmpty(ChampionAudioBank bank, AudioClip[] generated)
    {
        if (bank != null && (bank.clips == null || bank.clips.Length == 0))
            bank.clips = generated;
    }

    private static AudioClip[] CreateVariants(string prefix, int count, Func<int, AudioClip> factory)
    {
        AudioClip[] result = new AudioClip[count];
        for (int i = 0; i < count; i++)
            result[i] = factory(i);
        return result;
    }

    private static AudioClip CreateWhooshClip(string name, float length, float toneFrequency, int seed)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.Max(1, Mathf.RoundToInt(length * sampleRate));
        float[] data = new float[sampleCount];
        System.Random rng = new System.Random(seed);
        float filteredNoise = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleCount;
            float envelope = Mathf.Sin(Mathf.PI * t) * Mathf.Pow(1f - t, 0.35f);
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            filteredNoise = Mathf.Lerp(filteredNoise, noise, 0.18f + t * 0.25f);
            float tone = Mathf.Sin(2f * Mathf.PI * toneFrequency * (0.35f + t * 0.9f) * i / sampleRate);
            data[i] = (filteredNoise * 0.58f + tone * 0.18f) * envelope * 0.55f;
        }

        return BuildClip(name, data, sampleRate);
    }

    private static AudioClip CreateImpactClip(string name, float length, float frequency, float weight, int seed)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.Max(1, Mathf.RoundToInt(length * sampleRate));
        float[] data = new float[sampleCount];
        System.Random rng = new System.Random(seed);
        float phase = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleCount;
            float envelope = Mathf.Exp(-8f * t);
            float currentFrequency = Mathf.Lerp(frequency * 1.35f, frequency * 0.72f, t);
            phase += 2f * Mathf.PI * currentFrequency / sampleRate;
            float transient = i < sampleRate * 0.018f ? (float)(rng.NextDouble() * 2.0 - 1.0) * (1f - i / (sampleRate * 0.018f)) : 0f;
            float body = Mathf.Sin(phase) + 0.35f * Mathf.Sin(phase * 0.5f);
            data[i] = Mathf.Clamp((body * 0.42f * weight + transient * 0.35f) * envelope, -0.95f, 0.95f);
        }

        return BuildClip(name, data, sampleRate);
    }

    private static AudioClip CreateSweepClip(string name, float length, float startFrequency, float endFrequency, float volume)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.Max(1, Mathf.RoundToInt(length * sampleRate));
        float[] data = new float[sampleCount];
        float phase = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleCount;
            float frequency = Mathf.Lerp(startFrequency, endFrequency, t * t * (3f - 2f * t));
            phase += 2f * Mathf.PI * frequency / sampleRate;
            float envelope = Mathf.Sin(Mathf.PI * t) * Mathf.Pow(1f - t, 0.25f);
            float harmonic = Mathf.Sin(phase) + 0.32f * Mathf.Sin(phase * 2.01f) + 0.14f * Mathf.Sin(phase * 3.97f);
            data[i] = harmonic * envelope * volume;
        }

        return BuildClip(name, data, sampleRate);
    }

    private static AudioClip BuildClip(string name, float[] data, int sampleRate)
    {
        AudioClip clip = AudioClip.Create("AOG_" + name, data.Length, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioSource EnsureSource(string childName, AudioMixerGroup mixerGroup, float spatialBlend)
    {
        Transform existing = transform.Find(childName);
        GameObject host = existing != null ? existing.gameObject : new GameObject(childName);

        if (existing == null)
            host.transform.SetParent(transform, false);

        AudioSource source = host.GetComponent<AudioSource>();
        if (source == null)
            source = host.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = spatialBlend;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = 2f;
        source.maxDistance = 32f;
        source.dopplerLevel = 0f;
        source.outputAudioMixerGroup = mixerGroup;
        return source;
    }

    private void Play(ChampionAudioBank bank, AudioSource source)
    {
        if (bank == null || bank.clips == null || bank.clips.Length == 0 || source == null)
            return;

        int index;
        if (bank.clips.Length == 1)
        {
            index = 0;
        }
        else
        {
            do
            {
                index = UnityEngine.Random.Range(0, bank.clips.Length);
            }
            while (index == bank.lastIndex);
        }

        AudioClip clip = bank.clips[index];
        if (clip == null)
            return;

        bank.lastIndex = index;
        source.pitch = UnityEngine.Random.Range(bank.minPitch, bank.maxPitch);
        source.PlayOneShot(clip, bank.volume);
    }

    public void PlayFootstep() => Play(footsteps, sfxSource);
    public void PlayAttackWhoosh() => Play(attackWhooshes, sfxSource);
    public void PlayAttackImpact() => Play(attackImpacts, sfxSource);
    public void PlayHitReaction() => Play(hitReactions, sfxSource);
    public void PlayDeath() => Play(death, sfxSource);
    public void PlayRecall() => Play(recall, sfxSource);

    public void PlayAbilityCast(int slot)
    {
        switch (slot)
        {
            case 0: Play(qCast, sfxSource); break;
            case 1: Play(wCast, sfxSource); break;
            case 2: Play(eCast, sfxSource); break;
            case 3: Play(rCast, sfxSource); break;
        }
    }

    public void PlayAbilityImpact(int slot)
    {
        switch (slot)
        {
            case 0: Play(qImpact, sfxSource); break;
            case 1: Play(wImpact, sfxSource); break;
            case 2: Play(eImpact, sfxSource); break;
            case 3: Play(rImpact, sfxSource); break;
        }
    }

    public void TryPlayVoiceLine()
    {
        if (Time.time < nextVoiceTime || voiceSource == null || voiceSource.isPlaying)
            return;

        nextVoiceTime = Time.time + voiceCooldown;
        Play(voiceLines, voiceSource);
    }
}
