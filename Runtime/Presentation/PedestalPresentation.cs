using System.Collections;
using UnityEngine;

namespace CrystalCollector
{
    /// <summary>
    /// Presentation-only response for a pedestal after its child crystal is accepted by Visual Scripting.
    /// It never owns score or collection state.
    /// </summary>
    public sealed class PedestalPresentation : MonoBehaviour
    {
        [SerializeField] private Renderer[] accentRenderers;
        [SerializeField] private Light[] lights;
        private Coroutine routine;

        public void Configure(Renderer[] renderers, Light[] pedestalLights)
        {
            accentRenderers = renderers;
            lights = pedestalLights;
        }

        public void PlayDeactivation()
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(DeactivateRoutine());
        }

        private IEnumerator DeactivateRoutine()
        {
            var start = 1f;
            var elapsed = 0f;
            while (elapsed < 0.36f)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / 0.36f);
                var brightness = Mathf.Lerp(start, 0.18f, t);
                if (lights != null) foreach (var light in lights) if (light != null) light.intensity = brightness * 2.5f;
                yield return null;
            }

            if (accentRenderers != null)
            {
                foreach (var renderer in accentRenderers)
                {
                    if (renderer == null || renderer.sharedMaterial == null) continue;
                    var material = renderer.material;
                    if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", Color.black);
                }
            }
        }
    }
}
