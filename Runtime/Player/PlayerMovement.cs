using UnityEngine;
using UnityEngine.InputSystem;

namespace CrystalCollector
{
    /// <summary>
    /// Reads keyboard input and applies smoothed Rigidbody movement.
    /// Collection is intentionally not part of this component.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 6.5f;
        [SerializeField] private float acceleration = 18f;
        [SerializeField] private float turnSpeed = 14f;

        private Rigidbody body;
        private Vector2 input;
        private Vector3 currentVelocity;
        private bool inputEnabled = true;

        public Vector3 CurrentVelocity => currentVelocity;
        public Vector2 CurrentInput => input;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.constraints = RigidbodyConstraints.FreezeRotation;
        }

        private void Update()
        {
            input = inputEnabled ? ReadKeyboardInput() : Vector2.zero;
        }

        private void FixedUpdate()
        {
            var desiredVelocity = new Vector3(input.x, 0f, input.y) * moveSpeed;
            currentVelocity = Vector3.MoveTowards(currentVelocity, desiredVelocity, acceleration * Time.fixedDeltaTime);
            body.MovePosition(body.position + currentVelocity * Time.fixedDeltaTime);

            var horizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
            if (horizontalVelocity.sqrMagnitude > 0.01f)
            {
                var targetRotation = Quaternion.LookRotation(horizontalVelocity.normalized, Vector3.up);
                body.MoveRotation(Quaternion.Slerp(body.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
            }
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            if (!enabled)
            {
                input = Vector2.zero;
            }
        }

        public static Vector2 NormalizeInput(Vector2 rawInput)
        {
            return rawInput.sqrMagnitude > 1f ? rawInput.normalized : rawInput;
        }

        private static Vector2 ReadKeyboardInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            var rawInput = Vector2.zero;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) rawInput.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) rawInput.x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) rawInput.y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) rawInput.y += 1f;
            return NormalizeInput(rawInput);
        }
    }
}
