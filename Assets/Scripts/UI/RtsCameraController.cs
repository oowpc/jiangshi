using UnityEngine;

namespace Jiangshi.UI
{
    public sealed class RtsCameraController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 22f;
        [SerializeField] private float zoomSpeed = 4f;
        [SerializeField] private float minOrthographicSize = 8f;
        [SerializeField] private float maxOrthographicSize = 28f;
        [SerializeField] private Vector2 xBounds = new Vector2(-8f, 136f);
        [SerializeField] private Vector2 zBounds = new Vector2(-32f, 136f);
        [SerializeField] private float edgeScrollMargin = 0.03f;

        private Camera controlledCamera;

        private void Awake()
        {
            controlledCamera = GetComponent<Camera>();
        }

        private void Update()
        {
            Move();
            Zoom();
        }

        private void Move()
        {
            var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

            var mousePos = Input.mousePosition;
            var mouseX = mousePos.x / Screen.width;
            var mouseY = mousePos.y / Screen.height;

            if (mouseX <= edgeScrollMargin) input.x = -1f;
            if (mouseX >= 1f - edgeScrollMargin) input.x = 1f;
            if (mouseY <= edgeScrollMargin) input.z = -1f;
            if (mouseY >= 1f - edgeScrollMargin) input.z = 1f;

            if (input.sqrMagnitude <= 0f)
            {
                return;
            }

            input.Normalize();
            var right = transform.right;
            var forward = Vector3.Cross(right, Vector3.up).normalized;
            var movement = (right * input.x + forward * input.z) * (moveSpeed * Time.unscaledDeltaTime);
            var nextPosition = transform.position + movement;
            nextPosition.x = Mathf.Clamp(nextPosition.x, xBounds.x, xBounds.y);
            nextPosition.z = Mathf.Clamp(nextPosition.z, zBounds.x, zBounds.y);
            transform.position = nextPosition;
        }

        private void Zoom()
        {
            if (controlledCamera == null || !controlledCamera.orthographic)
            {
                return;
            }

            var scroll = Input.mouseScrollDelta.y;
            if (Mathf.Approximately(scroll, 0f))
            {
                return;
            }

            controlledCamera.orthographicSize = Mathf.Clamp(
                controlledCamera.orthographicSize - scroll * zoomSpeed,
                minOrthographicSize,
                maxOrthographicSize);
        }
    }
}
