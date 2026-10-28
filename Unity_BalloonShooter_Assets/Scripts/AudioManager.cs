using UnityEngine;

namespace IMXR.BalloonShooter
{
    /// <summary>
    /// Audio Manager featuring a built-in Procedural Audio Synthesizer.
    /// Generates crisp balloon pops, sci-fi laser blasts, combo chimes, and UI sounds
    /// mathematically at runtime without requiring external .wav/.mp3 audio assets!
    /// Also supports optional custom AudioClip overrides.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource laserSource;

        [Header("Optional Custom Audio Clips")]
        [SerializeField] private AudioClip customPopClip;
        [SerializeField] private AudioClip customLaserClip;
        [SerializeField] private AudioClip customMissClip;
        [SerializeField] private AudioClip customComboClip;
        [SerializeField] private AudioClip customGameOverClip;

        // Procedurally synthesized fallback clips
        private AudioClip synthPopClip;
        private AudioClip synthLaserClip;
        private AudioClip synthMissClip;
        private AudioClip synthComboClip;
        private AudioClip synthGameOverClip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureAudioSources();
            GenerateProceduralAudioClips();
        }

        private void EnsureAudioSources()
        {
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.spatialBlend = 0f; // 2D audio
            }

            if (laserSource == null)
            {
                laserSource = gameObject.AddComponent<AudioSource>();
                laserSource.playOnAwake = false;
                laserSource.spatialBlend = 0f;
                laserSource.volume = 0.6f;
            }
        }

        private void GenerateProceduralAudioClips()
        {
            synthPopClip = CreateSynthesizedPopClip();
            synthLaserClip = CreateSynthesizedLaserClip();
            synthMissClip = CreateSynthesizedMissClip();
            synthComboClip = CreateSynthesizedComboClip();
            synthGameOverClip = CreateSynthesizedGameOverClip();
        }

        public void PlayPopSound(float pitchMultiplier = 1.0f)
        {
            AudioClip clip = (customPopClip != null) ? customPopClip : synthPopClip;
            if (clip != null && sfxSource != null)
            {
                sfxSource.pitch = Random.Range(0.95f, 1.05f) * pitchMultiplier;
                sfxSource.PlayOneShot(clip, 0.85f);
            }
        }

        public void PlayLaserShootSound()
        {
            AudioClip clip = (customLaserClip != null) ? customLaserClip : synthLaserClip;
            if (clip != null && laserSource != null)
            {
                laserSource.pitch = Random.Range(0.95f, 1.05f);
                laserSource.PlayOneShot(clip, 0.65f);
            }
        }

        public void PlayMissSound()
        {
            AudioClip clip = (customMissClip != null) ? customMissClip : synthMissClip;
            if (clip != null && sfxSource != null)
            {
                sfxSource.pitch = 1.0f;
                sfxSource.PlayOneShot(clip, 0.4f);
            }
        }

        public void PlayComboSound(int comboCount)
        {
            AudioClip clip = (customComboClip != null) ? customComboClip : synthComboClip;
            if (clip != null && sfxSource != null)
            {
                sfxSource.pitch = Mathf.Clamp(1.0f + (comboCount * 0.08f), 1.0f, 2.0f);
                sfxSource.PlayOneShot(clip, 0.75f);
            }
        }

        public void PlayGameOverSound()
        {
            AudioClip clip = (customGameOverClip != null) ? customGameOverClip : synthGameOverClip;
            if (clip != null && sfxSource != null)
            {
                sfxSource.pitch = 1.0f;
                sfxSource.PlayOneShot(clip, 0.9f);
            }
        }

        // =========================================================================
        // PROCEDURAL SYNTHESIS MATHEMATICS
        // =========================================================================

        private AudioClip CreateSynthesizedPopClip()
        {
            int sampleRate = 44100;
            float duration = 0.12f;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                // Frequency drops rapidly from 600Hz down to 80Hz + burst of noise
                float freq = Mathf.Lerp(650f, 60f, progress * progress);
                float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
                float noise = (Random.value * 2f - 1f) * Mathf.Exp(-progress * 25f);
                float envelope = Mathf.Exp(-progress * 18f);

                samples[i] = (sine * 0.6f + noise * 0.4f) * envelope;
            }

            AudioClip clip = AudioClip.Create("Synth_Pop", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateSynthesizedLaserClip()
        {
            int sampleRate = 44100;
            float duration = 0.14f;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                // Rapid downward frequency sweep from 1400Hz to 250Hz
                float freq = Mathf.Lerp(1400f, 250f, progress);
                float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
                float envelope = Mathf.Exp(-progress * 12f);

                samples[i] = sine * envelope * 0.7f;
            }

            AudioClip clip = AudioClip.Create("Synth_Laser", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateSynthesizedMissClip()
        {
            int sampleRate = 44100;
            float duration = 0.1f;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                float freq = Mathf.Lerp(120f, 40f, progress);
                float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
                float envelope = Mathf.Exp(-progress * 20f);

                samples[i] = sine * envelope * 0.5f;
            }

            AudioClip clip = AudioClip.Create("Synth_Miss", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateSynthesizedComboClip()
        {
            int sampleRate = 44100;
            float duration = 0.22f;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                // Harmonic bell chord (880Hz + 1320Hz + 1760Hz)
                float sine1 = Mathf.Sin(2f * Mathf.PI * 880f * t);
                float sine2 = Mathf.Sin(2f * Mathf.PI * 1320f * t);
                float sine3 = Mathf.Sin(2f * Mathf.PI * 1760f * t);
                float envelope = Mathf.Exp(-progress * 9f);

                samples[i] = (sine1 * 0.5f + sine2 * 0.3f + sine3 * 0.2f) * envelope * 0.7f;
            }

            AudioClip clip = AudioClip.Create("Synth_Combo", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateSynthesizedGameOverClip()
        {
            int sampleRate = 44100;
            float duration = 0.6f;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                // 3 rising arpeggio tones
                float freq = (progress < 0.33f) ? 440f : ((progress < 0.66f) ? 554.37f : 659.25f);
                float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
                float envelope = Mathf.Sin(progress * Mathf.PI);

                samples[i] = sine * envelope * 0.6f;
            }

            AudioClip clip = AudioClip.Create("Synth_GameOver", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
