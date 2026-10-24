using UnityEngine;

namespace CrystalCollector
{
    /// <summary>
    /// Visual-only player bob and movement lean.
    /// </summary>
    public sealed class PlayerPresentation : MonoBehaviour
    {
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private Transform bodyVisual;
        [SerializeField] private Transform coreVisual;

        private Vector3 bodyBasePosition;
        private Quaternion bodyBaseRotation;

        public void Configure(PlayerMovement playerMovement, Transform body, Transform core)
        {
            movement = playerMovement;
            bodyVisual = body;
            coreVisual = core;
        }

        private void Awake()
        {
            if (movement == null) movement = GetComponent<PlayerMovement>();
            if (bodyVisual != null)
            {
                bodyBasePosition = bodyVisual.localPosition;
                bodyBaseRotation = bodyVisual.localRotation;
            }
        }

        private void Update()
        {
            if (bodyVisual == null || movement == null) return;

            var speed = Mathf.Clamp01(movement.CurrentVelocity.magnitude / 6.5f);
            var bob = Mathf.Sin(Time.unscaledTime * 6.5f) * 0.045f * speed;
            bodyVisual.localPosition = bodyBasePosition + Vector3.up * bob;

            var localVelocity = transform.InverseTransformDirection(movement.CurrentVelocity);
            var lean = Quaternion.Euler(localVelocity.z * 2.5f, 0f, -localVelocity.x * 2.5f);
            bodyVisual.localRotation = Quaternion.Slerp(bodyVisual.localRotation, bodyBaseRotation * lean, 10f * Time.unscaledDeltaTime);

            if (coreVisual != null)
            {
                coreVisual.Rotate(0f, 80f * Time.unscaledDeltaTime, 0f, Space.Self);
            }
        }
    }
}
