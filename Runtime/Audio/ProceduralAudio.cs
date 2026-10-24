using UnityEngine;

namespace CrystalCollector
{
    /// <summary>
    /// Generates small synthesized UI and gameplay tones without external audio assets.
    /// </summary>
    public sealed class ProceduralAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource source;

        private AudioClip clickClip;
        private AudioClip pickupClip;
        private AudioClip victoryClip;
        private AudioClip pauseClip;

        private void Awake()
        {
            if (source == null) source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            clickClip = CreateTone("CrystalClick", 620f, 0.08f, 0.18f);
            pickupClip = CreateTone("CrystalPickup", 760f, 0.2f, 0.22f);
            victoryClip = CreateChord("CrystalVictory", new[] { 523.25f, 659.25f, 783.99f }, 0.65f, 0.26f);
            pauseClip = CreateTone("CrystalPause", 310f, 0.12f, 0.16f);
        }

        public void PlayClick() => Play(clickClip);
        public void PlayPickup() => Play(pickupClip);
        public void PlayVictory() => Play(victoryClip);
        public void PlayPause() => Play(pauseClip);
        public void PlayResume() => Play(clickClip);

        private void Play(AudioClip clip)
        {
            if (clip != null && source != null) source.PlayOneShot(clip);
        }

        private static AudioClip CreateTone(string clipName, float frequency, float duration, float volume)
        {
            const int sampleRate = 44100;
            var samples = Mathf.Max(1, Mathf.RoundToInt(duration * sampleRate));
            var clip = AudioClip.Create(clipName, samples, 1, sampleRate, false);
            var data = new float[samples];
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)sampleRate;
                var envelope = 1f - i / (float)samples;
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * volume;
            }
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateChord(string clipName, float[] frequencies, float duration, float volume)
        {
            const int sampleRate = 44100;
            var samples = Mathf.Max(1, Mathf.RoundToInt(duration * sampleRate));
            var clip = AudioClip.Create(clipName, samples, 1, sampleRate, false);
            var data = new float[samples];
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)sampleRate;
                var envelope = 1f - i / (float)samples;
                var sample = 0f;
                foreach (var frequency in frequencies) sample += Mathf.Sin(2f * Mathf.PI * frequency * t);
                data[i] = sample / Mathf.Max(1, frequencies.Length) * envelope * volume;
            }
            clip.SetData(data, 0);
            return clip;
        }
    }
}
