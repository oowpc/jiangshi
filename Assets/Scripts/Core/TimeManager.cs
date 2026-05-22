using UnityEngine;

namespace Jiangshi.Core
{
    public sealed class TimeManager : MonoBehaviour
    {
        [SerializeField] private float normalScale = 1f;
        [SerializeField] private float fastScale = 2f;

        public bool IsPaused { get; private set; }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : normalScale;
        }

        public void SetFastForward(bool enabled)
        {
            if (IsPaused)
            {
                return;
            }

            Time.timeScale = enabled ? fastScale : normalScale;
        }
    }
}

