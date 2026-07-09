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

    private void Awake()
    {
        sfxSource = EnsureSource("Champion_SFX", sfxMixerGroup, 0.9f);
        voiceSource = EnsureSource("Champion_Voice", voiceMixerGroup, 0.75f);
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
