using UnityEngine;

namespace CrystalCollector
{
    /// <summary>
    /// Visual-only procedural motion for generated world props.
    /// </summary>
    public sealed class WorldAnimator : MonoBehaviour
    {
        [SerializeField] private Vector3 rotationSpeed = new Vector3(0f, 35f, 0f);
        [SerializeField] private float floatAmplitude = 0.18f;
        [SerializeField] private float floatFrequency = 1.2f;

        private Vector3 baseLocalPosition;
        private float phase;

        private void Awake()
        {
            baseLocalPosition = transform.localPosition;
            phase = (GetInstanceID() & 1023) * 0.013f;
        }

        private void Update()
        {
            transform.Rotate(rotationSpeed * Time.unscaledDeltaTime, Space.Self);
            var offset = Mathf.Sin((Time.unscaledTime + phase) * floatFrequency) * floatAmplitude;
            transform.localPosition = baseLocalPosition + Vector3.up * offset;
        }
    }
}
