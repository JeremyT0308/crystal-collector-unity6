using System.Collections;
using UnityEngine;

namespace CrystalCollector
{
    /// <summary>
    /// Visual animation for a crystal. The Visual Scripting graph owns collection validity.
    /// </summary>
    public sealed class CollectiblePresentation : MonoBehaviour
    {
        [SerializeField] private float rotationSpeed = 70f;
        [SerializeField] private float floatAmplitude = 0.2f;
        [SerializeField] private float floatFrequency = 1.5f;

        private Vector3 basePosition;
        private Vector3 baseScale;
        private float phase;
        private bool collected;
        private ParticleSystem[] particles;

        private void Awake()
        {
            basePosition = transform.localPosition;
            baseScale = transform.localScale;
            phase = (GetInstanceID() & 2047) * 0.017f;
            particles = GetComponentsInChildren<ParticleSystem>(true);
        }

        private void Update()
        {
            if (collected) return;

            transform.Rotate(0f, rotationSpeed * Time.unscaledDeltaTime, 0f, Space.Self);
            var bob = Mathf.Sin((Time.unscaledTime + phase) * floatFrequency) * floatAmplitude;
            transform.localPosition = basePosition + Vector3.up * bob;
        }

        /// <summary>
        /// Called by the collection graph after it has accepted a Player trigger.
        /// </summary>
        public void PlayPickupFeedback()
        {
            if (collected) return;
            collected = true;
            StopAllCoroutines();
            if (particles != null)
            {
                foreach (var particle in particles)
                {
                    if (particle == null) continue;
                    particle.Emit(12);
                }
            }
            GetComponentInParent<PedestalPresentation>()?.PlayDeactivation();
            StartCoroutine(PickupRoutine());
        }

        private IEnumerator PickupRoutine()
        {
            var startPosition = transform.localPosition;
            var startScale = transform.localScale;
            var elapsed = 0f;
            while (elapsed < 0.42f)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / 0.42f);
                var eased = 1f - Mathf.Pow(1f - t, 3f);
                transform.localPosition = startPosition + Vector3.up * (eased * 1.25f);
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, eased);
                yield return null;
            }

            transform.localScale = baseScale;
            gameObject.SetActive(false);
        }
    }
}
