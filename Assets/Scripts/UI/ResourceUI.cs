using Jiangshi.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace Jiangshi.UI
{
    public sealed class ResourceUI : MonoBehaviour
    {
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private ResourceType resourceType;
        [SerializeField] private Text valueText;

        private void Start()
        {
            if (resourceManager == null)
            {
                return;
            }

            resourceManager.ResourceChanged += OnResourceChanged;
            Refresh();
        }

        private void OnDestroy()
        {
            if (resourceManager != null)
            {
                resourceManager.ResourceChanged -= OnResourceChanged;
            }
        }

        private void OnResourceChanged(ResourceType changedType, int value)
        {
            if (changedType == resourceType)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            if (valueText != null)
            {
                valueText.text = resourceManager.Get(resourceType).ToString();
            }
        }
    }
}

