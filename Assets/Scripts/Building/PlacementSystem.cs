using System;
using Jiangshi.Economy;
using Jiangshi.Grid;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace Jiangshi.Building
{
    public sealed class PlacementSystem : MonoBehaviour
    {
        private const float MaxPlacementDistance = 500f;

        [SerializeField] private Camera worldCamera;
        [SerializeField] private GridManager gridManager;
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private BuildingManager buildingManager;
        [SerializeField] private BuildingData selectedBuilding;
        [SerializeField] private BuildingData[] buildingOptions;
        [SerializeField] private LayerMask placementMask = -1;
        [SerializeField] private bool enableKeyboardSelection = true;
        [SerializeField] private Color validPreviewColor = new Color(0.15f, 0.95f, 0.35f, 0.45f);
        [SerializeField] private Color invalidPreviewColor = new Color(1f, 0.18f, 0.12f, 0.45f);

        private GameObject previewInstance;
        private BuildingData previewBuilding;
        private Renderer[] previewRenderers = new Renderer[0];
        private Material validPreviewMaterial;
        private Material invalidPreviewMaterial;
        private bool previewMaterialInitialized;
        private bool lastPreviewCanPlace;
        private bool placementRotated;

        public BuildingData SelectedBuilding => selectedBuilding;
        public BuildingData[] BuildingOptions => buildingOptions;
        public event Action<BuildingData> SelectedBuildingChanged;

        private void Awake()
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            validPreviewMaterial = CreatePreviewMaterial("Valid Placement Preview", validPreviewColor);
            invalidPreviewMaterial = CreatePreviewMaterial("Invalid Placement Preview", invalidPreviewColor);
        }

        private void OnDestroy()
        {
            DestroyPreview();
            DestroyPreviewMaterials();
        }

        private void Update()
        {
            HandleSelectionInput();
            HandleRotationInput();
            UpdatePreview();

            if (selectedBuilding == null || worldCamera == null)
            {
                return;
            }

            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                SelectBuilding(null);
                return;
            }

            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                TryPlaceAtMouse();
            }
        }

        public void SelectBuilding(BuildingData buildingData)
        {
            if (buildingData != null && !CanSelectBuilding(buildingData))
            {
                buildingData = null;
            }

            if (selectedBuilding == buildingData)
            {
                return;
            }

            selectedBuilding = buildingData;
            placementRotated = false;
            DestroyPreview();
            SelectedBuildingChanged?.Invoke(selectedBuilding);
        }

        public void SelectBuildingOption(int index)
        {
            if (buildingOptions == null || index < 0 || index >= buildingOptions.Length)
            {
                return;
            }

            SelectBuilding(buildingOptions[index]);
        }

        public bool CanSelectBuilding(BuildingData buildingData)
        {
            return buildingData != null
                && buildingData.prefab != null
                && !IsUniqueBuildingAlreadyPresent(buildingData);
        }

        public bool TryPlaceAtMouse()
        {
            if (selectedBuilding == null || gridManager == null)
            {
                return false;
            }

            if (!TryGetMouseGridPosition(out var gridPosition))
            {
                return false;
            }

            return TryPlace(selectedBuilding, gridPosition);
        }

        public bool TryPlace(BuildingData buildingData, Vector3 worldPosition)
        {
            if (gridManager == null)
            {
                return false;
            }

            return TryPlace(buildingData, gridManager.WorldToGrid(worldPosition));
        }

        public bool TryPlace(BuildingData buildingData, GridPosition gridPosition)
        {
            if (buildingData == null || buildingData.prefab == null || gridManager == null || resourceManager == null)
            {
                return false;
            }

            if (IsUniqueBuildingAlreadyPresent(buildingData))
            {
                return false;
            }

            var occupiedSize = GetPlacementSize(buildingData);
            if (!CanPlace(buildingData, gridPosition, occupiedSize))
            {
                return false;
            }

            if (!resourceManager.TrySpend(buildingData.buildCost))
            {
                return false;
            }

            var position = gridManager.GridToWorld(gridPosition, occupiedSize);
            var instance = Instantiate(buildingData.prefab, position, GetPlacementRotation(buildingData));
            SnapObjectBottomToY(instance, position.y);

            var building = instance.GetComponent<Building>();
            if (building != null)
            {
                building.Initialize(buildingData, gridPosition, occupiedSize);
                buildingManager?.Register(building);
            }

            gridManager.SetOccupied(gridPosition, occupiedSize, true, !buildingData.blocksMovement);
            return true;
        }

        public bool CanPlace(BuildingData buildingData, GridPosition gridPosition)
        {
            return CanPlace(buildingData, gridPosition, GetPlacementSize(buildingData));
        }

        private bool CanPlace(BuildingData buildingData, GridPosition gridPosition, Vector2Int occupiedSize)
        {
            return buildingData != null
                && buildingData.prefab != null
                && gridManager != null
                && resourceManager != null
                && !IsUniqueBuildingAlreadyPresent(buildingData)
                && gridManager.CanOccupy(gridPosition, occupiedSize)
                && resourceManager.CanAfford(buildingData.buildCost)
                && CanAffordPower(buildingData)
                && CanAffordPopulation(buildingData);
        }

        private bool IsUniqueBuildingAlreadyPresent(BuildingData buildingData)
        {
            if (buildingData == null || !buildingData.triggersDefeatOnDestroyed)
            {
                return false;
            }

            if (buildingManager != null)
            {
                foreach (var building in buildingManager.Buildings)
                {
                    if (building != null && building.Data == buildingData)
                    {
                        return true;
                    }
                }
            }

            foreach (var building in FindObjectsOfType<Building>())
            {
                if (building != null && building.Data == buildingData)
                {
                    return true;
                }
            }

            return false;
        }

        private bool CanAffordPower(BuildingData buildingData)
        {
            return buildingData.powerCost <= 0 || resourceManager.Get(ResourceType.Power) >= buildingData.powerCost;
        }

        private bool CanAffordPopulation(BuildingData buildingData)
        {
            return buildingData.populationCost <= 0
                || resourceManager.Get(ResourceType.Population) >= buildingData.populationCost;
        }

        private void HandleSelectionInput()
        {
            if (!enableKeyboardSelection || buildingOptions == null)
            {
                return;
            }

            var optionCount = Mathf.Min(buildingOptions.Length, 9);
            for (var i = 0; i < optionCount; i++)
            {
                var alphaKey = (KeyCode)((int)KeyCode.Alpha1 + i);
                var keypadKey = (KeyCode)((int)KeyCode.Keypad1 + i);
                if (Input.GetKeyDown(alphaKey) || Input.GetKeyDown(keypadKey))
                {
                    SelectBuildingOption(i);
                    return;
                }
            }

            if (buildingOptions.Length > 9 && (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0)))
            {
                SelectBuildingOption(9);
            }
        }

        private void HandleRotationInput()
        {
            if (selectedBuilding == null || !CanRotate(selectedBuilding))
            {
                placementRotated = false;
                return;
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                placementRotated = !placementRotated;
            }
        }

        private void UpdatePreview()
        {
            if (selectedBuilding == null || selectedBuilding.prefab == null || gridManager == null || worldCamera == null)
            {
                SetPreviewActive(false);
                return;
            }

            EnsurePreview();

            if (previewInstance == null || !TryGetMouseGridPosition(out var gridPosition))
            {
                SetPreviewActive(false);
                return;
            }

            var occupiedSize = GetPlacementSize(selectedBuilding);
            var position = gridManager.GridToWorld(gridPosition, occupiedSize);
            previewInstance.transform.SetPositionAndRotation(position, GetPlacementRotation(selectedBuilding));
            SnapObjectBottomToY(previewInstance, position.y);
            SetPreviewActive(true);
            ApplyPreviewMaterial(CanPlace(selectedBuilding, gridPosition, occupiedSize));
        }

        private Vector2Int GetPlacementSize(BuildingData buildingData)
        {
            if (buildingData == null)
            {
                return Vector2Int.one;
            }

            var size = buildingData.size;
            return placementRotated && CanRotate(buildingData) ? new Vector2Int(size.y, size.x) : size;
        }

        private Quaternion GetPlacementRotation(BuildingData buildingData)
        {
            var baseRotation = buildingData != null && buildingData.prefab != null
                ? buildingData.prefab.transform.rotation
                : Quaternion.identity;

            return placementRotated && CanRotate(buildingData)
                ? Quaternion.Euler(0f, 90f, 0f) * baseRotation
                : baseRotation;
        }

        private static bool CanRotate(BuildingData buildingData)
        {
            return buildingData != null
                && buildingData.canRotatePlacement
                && buildingData.size.x != buildingData.size.y;
        }

        private void EnsurePreview()
        {
            if (previewInstance != null && previewBuilding == selectedBuilding)
            {
                return;
            }

            DestroyPreview();

            previewBuilding = selectedBuilding;
            previewInstance = Instantiate(selectedBuilding.prefab);
            previewInstance.name = $"{selectedBuilding.displayName} Preview";
            previewInstance.SetActive(false);

            foreach (var collider in previewInstance.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            foreach (var collider in previewInstance.GetComponentsInChildren<Collider2D>(true))
            {
                collider.enabled = false;
            }

            foreach (var behaviour in previewInstance.GetComponentsInChildren<MonoBehaviour>(true))
            {
                behaviour.enabled = false;
            }

            previewRenderers = previewInstance.GetComponentsInChildren<Renderer>(true);
            previewMaterialInitialized = false;
            ApplyPreviewMaterial(false);
        }

        private void SetPreviewActive(bool active)
        {
            if (previewInstance != null && previewInstance.activeSelf != active)
            {
                previewInstance.SetActive(active);
            }
        }

        private void DestroyPreview()
        {
            if (previewInstance == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(previewInstance);
            }
            else
            {
                DestroyImmediate(previewInstance);
            }

            previewInstance = null;
            previewBuilding = null;
            previewRenderers = new Renderer[0];
            previewMaterialInitialized = false;
        }

        private bool TryGetMouseGridPosition(out GridPosition gridPosition)
        {
            gridPosition = default;

            if (!TryGetPlacementPoint(out var worldPosition))
            {
                return false;
            }

            gridPosition = gridManager.WorldToGrid(worldPosition);
            return true;
        }

        private bool TryGetPlacementPoint(out Vector3 worldPosition)
        {
            worldPosition = default;

            if (worldCamera == null)
            {
                return false;
            }

            var ray = worldCamera.ScreenPointToRay(Input.mousePosition);
            var hits = Physics.RaycastAll(ray, MaxPlacementDistance, placementMask, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0)
            {
                return false;
            }

            var bestHit = hits[0];
            for (var i = 1; i < hits.Length; i++)
            {
                if (hits[i].distance > bestHit.distance)
                {
                    bestHit = hits[i];
                }
            }

            worldPosition = bestHit.point;
            return true;
        }

        private void ApplyPreviewMaterial(bool canPlace)
        {
            if (previewRenderers == null || previewRenderers.Length == 0)
            {
                return;
            }

            if (previewMaterialInitialized && lastPreviewCanPlace == canPlace)
            {
                return;
            }

            var color = canPlace ? validPreviewColor : invalidPreviewColor;
            var material = canPlace ? validPreviewMaterial : invalidPreviewMaterial;
            foreach (var previewRenderer in previewRenderers)
            {
                if (previewRenderer is SpriteRenderer sr)
                {
                    sr.color = color;
                }
                else
                {
                    var materials = previewRenderer.sharedMaterials;
                    for (var i = 0; i < materials.Length; i++)
                    {
                        materials[i] = material;
                    }

                    previewRenderer.sharedMaterials = materials;
                    previewRenderer.shadowCastingMode = ShadowCastingMode.Off;
                    previewRenderer.receiveShadows = false;
                }
            }

            lastPreviewCanPlace = canPlace;
            previewMaterialInitialized = true;
        }

        private Material CreatePreviewMaterial(string materialName, Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                name = materialName,
                color = color
            };

            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
            return material;
        }

        private void DestroyPreviewMaterials()
        {
            DestroyMaterial(validPreviewMaterial);
            DestroyMaterial(invalidPreviewMaterial);
        }

        private void DestroyMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(material);
            }
            else
            {
                DestroyImmediate(material);
            }
        }

        private static void SnapObjectBottomToY(GameObject instance, float targetY)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            var position = instance.transform.position;
            position.y += targetY - bounds.min.y;
            instance.transform.position = position;
        }
    }
}
