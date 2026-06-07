using UnityEngine;
using Jiangshi.Grid;

namespace Jiangshi.UI
{
    public sealed class RtsCameraController : MonoBehaviour
    {
        [SerializeField] private GridManager gridManager;
        [SerializeField] private float moveSpeed = 22f;
        [SerializeField] private float zoomSpeed = 4f;
        [SerializeField] private float minOrthographicSize = 8f;
        [SerializeField] private float maxOrthographicSize = 28f;
        [SerializeField] private Vector2 xBounds = new Vector2(-8f, 136f);
        [SerializeField] private Vector2 zBounds = new Vector2(-32f, 136f);
        [SerializeField] private float mapOverscrollMargin = 8f;
        [SerializeField] private float groundY = 0f;
        [SerializeField] private bool clampVisibleAreaToMap = true;
        [SerializeField] private float edgeScrollMargin = 0.03f;

        private Camera controlledCamera;

        private void Awake()
        {
            controlledCamera = GetComponent<Camera>();
            if (gridManager == null)
            {
                gridManager = FindObjectOfType<GridManager>();
            }
        }

        private void Start()
        {
            ClampPosition();
        }

        private void Update()
        {
            Move();
            Zoom();
            ClampPosition();
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
            transform.position += movement;
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

        private void ClampPosition()
        {
            if (clampVisibleAreaToMap && TryClampVisibleGroundArea())
            {
                return;
            }

            var nextPosition = transform.position;
            nextPosition.x = Mathf.Clamp(nextPosition.x, xBounds.x, xBounds.y);
            nextPosition.z = Mathf.Clamp(nextPosition.z, zBounds.x, zBounds.y);
            transform.position = nextPosition;
        }

        private bool TryClampVisibleGroundArea()
        {
            if (controlledCamera == null || !controlledCamera.orthographic)
            {
                return false;
            }

            if (!TryGetAllowedBounds(out var allowedMinX, out var allowedMaxX, out var allowedMinZ, out var allowedMaxZ)
                || !TryGetVisibleGroundBounds(out var visibleMinX, out var visibleMaxX, out var visibleMinZ, out var visibleMaxZ))
            {
                return false;
            }

            var deltaX = GetClampDelta(visibleMinX, visibleMaxX, allowedMinX, allowedMaxX);
            var deltaZ = GetClampDelta(visibleMinZ, visibleMaxZ, allowedMinZ, allowedMaxZ);

            if (!Mathf.Approximately(deltaX, 0f) || !Mathf.Approximately(deltaZ, 0f))
            {
                transform.position += new Vector3(deltaX, 0f, deltaZ);
            }

            return true;
        }

        private bool TryGetAllowedBounds(out float minX, out float maxX, out float minZ, out float maxZ)
        {
            if (gridManager == null)
            {
                gridManager = FindObjectOfType<GridManager>();
            }

            if (gridManager == null || gridManager.Width <= 0 || gridManager.Height <= 0)
            {
                minX = Mathf.Min(xBounds.x, xBounds.y);
                maxX = Mathf.Max(xBounds.x, xBounds.y);
                minZ = Mathf.Min(zBounds.x, zBounds.y);
                maxZ = Mathf.Max(zBounds.x, zBounds.y);
                return true;
            }

            var firstCell = gridManager.GridToWorld(new GridPosition(0, 0));
            var lastCell = gridManager.GridToWorld(new GridPosition(gridManager.Width - 1, gridManager.Height - 1));
            var margin = Mathf.Max(0f, mapOverscrollMargin);
            var halfCell = gridManager.CellSize * 0.5f;

            minX = Mathf.Min(firstCell.x, lastCell.x) - halfCell - margin;
            maxX = Mathf.Max(firstCell.x, lastCell.x) + halfCell + margin;
            minZ = Mathf.Min(firstCell.z, lastCell.z) - halfCell - margin;
            maxZ = Mathf.Max(firstCell.z, lastCell.z) + halfCell + margin;
            return true;
        }

        private bool TryGetVisibleGroundBounds(out float minX, out float maxX, out float minZ, out float maxZ)
        {
            minX = minZ = float.PositiveInfinity;
            maxX = maxZ = float.NegativeInfinity;

            var groundPlane = new Plane(Vector3.up, new Vector3(0f, groundY, 0f));
            var viewCorners = new[]
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(0f, 1f, 0f),
                new Vector3(1f, 0f, 0f),
                new Vector3(1f, 1f, 0f)
            };

            foreach (var corner in viewCorners)
            {
                var ray = controlledCamera.ViewportPointToRay(corner);
                if (!groundPlane.Raycast(ray, out var enter))
                {
                    return false;
                }

                var point = ray.GetPoint(enter);
                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                minZ = Mathf.Min(minZ, point.z);
                maxZ = Mathf.Max(maxZ, point.z);
            }

            return true;
        }

        private static float GetClampDelta(float visibleMin, float visibleMax, float allowedMin, float allowedMax)
        {
            var visibleSize = visibleMax - visibleMin;
            var allowedSize = allowedMax - allowedMin;

            if (visibleSize >= allowedSize)
            {
                return (allowedMin + allowedMax - visibleMin - visibleMax) * 0.5f;
            }

            if (visibleMin < allowedMin)
            {
                return allowedMin - visibleMin;
            }

            if (visibleMax > allowedMax)
            {
                return allowedMax - visibleMax;
            }

            return 0f;
        }
    }
}
