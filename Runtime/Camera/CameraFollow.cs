using UnityEngine;

namespace CrystalCollector
{
    /// <summary>
    /// Lightweight elevated camera follow used instead of Cinemachine.
    /// </summary>
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 12f, -12f);
        [SerializeField] private float positionSmoothTime = 0.22f;
        [SerializeField] private float lookAheadDistance = 1.4f;

        private Vector3 positionVelocity;
        private PlayerMovement movement;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            movement = target == null ? null : target.GetComponent<PlayerMovement>();
        }

        private void Start()
        {
            if (target == null)
            {
                var player = FindFirstObjectByType<PlayerMovement>();
                if (player != null) SetTarget(player.transform);
            }

            if (target != null)
            {
                transform.position = target.position + followOffset;
                transform.LookAt(target.position + Vector3.up * 0.65f);
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            var lookAhead = movement == null ? Vector3.zero : movement.CurrentVelocity * (lookAheadDistance / 6.5f);
            var desiredPosition = target.position + followOffset + new Vector3(lookAhead.x, 0f, lookAhead.z);
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, positionSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);

            var lookTarget = target.position + Vector3.up * 0.6f + new Vector3(lookAhead.x * 0.35f, 0f, lookAhead.z * 0.35f);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookTarget - transform.position, Vector3.up), 8f * Time.unscaledDeltaTime);
        }
    }
}
