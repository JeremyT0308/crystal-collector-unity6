using UnityEngine;

namespace CrystalCollector
{
    /// <summary>
    /// Subtle procedural menu camera drift that keeps the hero composition alive.
    /// </summary>
    public sealed class MenuCameraMotion : MonoBehaviour
    {
        [SerializeField] private Vector3 target = new Vector3(0f, 1.2f, 0.5f);
        [SerializeField] private float orbitRadius = 0.42f;
        [SerializeField] private float orbitSpeed = 0.12f;
        [SerializeField] private float verticalDrift = 0.08f;

        private Vector3 basePosition;

        private void Awake()
        {
            basePosition = transform.position;
        }

        private void LateUpdate()
        {
            var time = Time.unscaledTime * orbitSpeed;
            transform.position = basePosition + new Vector3(Mathf.Sin(time) * orbitRadius, Mathf.Sin(time * 0.7f) * verticalDrift, Mathf.Cos(time) * orbitRadius * 0.45f);
            var lookTarget = target + new Vector3(Mathf.Sin(time * 0.5f) * 0.12f, Mathf.Sin(time * 0.8f) * 0.06f, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookTarget - transform.position, Vector3.up), 4f * Time.unscaledDeltaTime);
        }
    }
}
