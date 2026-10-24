using System.Collections;
using UnityEngine;

namespace CrystalCollector
{
    /// <summary>
    /// Drives the central landmark's visual pulse and victory reaction.
    /// </summary>
    public sealed class NexusPresentation : MonoBehaviour
    {
        [SerializeField] private Transform[] energyRings;
        [SerializeField] private ParticleSystem[] particles;
        [SerializeField] private Light[] lights;
        private Coroutine pulseRoutine;

        public void Configure(Transform[] rings, ParticleSystem[] nexusParticles, Light[] nexusLights)
        {
            energyRings = rings;
            particles = nexusParticles;
            lights = nexusLights;
        }

        public void PlayVictoryReaction()
        {
            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            pulseRoutine = StartCoroutine(VictoryRoutine());
        }

        private IEnumerator VictoryRoutine()
        {
            if (particles != null) foreach (var particle in particles) if (particle != null) particle.Emit(48);
            var elapsed = 0f;
            while (elapsed < 1.4f)
            {
                elapsed += Time.unscaledDeltaTime;
                var pulse = 1f + Mathf.Sin(elapsed * 18f) * 0.12f * (1f - elapsed / 1.4f);
                if (energyRings != null) foreach (var ring in energyRings) if (ring != null) ring.localScale = Vector3.one * pulse;
                if (lights != null) foreach (var light in lights) if (light != null) light.intensity = 6f + Mathf.Sin(elapsed * 14f) * 2.5f;
                yield return null;
            }
        }
    }
}
