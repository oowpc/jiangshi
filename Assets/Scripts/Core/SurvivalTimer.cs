using UnityEngine;

namespace Jiangshi.Core
{
    public sealed class SurvivalTimer : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private float durationSeconds = 600f;

        private float elapsedSeconds;

        public float DurationSeconds => durationSeconds;
        public float ElapsedSeconds => elapsedSeconds;
        public float RemainingSeconds => Mathf.Max(0f, durationSeconds - elapsedSeconds);
        public bool IsComplete => elapsedSeconds >= durationSeconds;

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }
        }

        private void Update()
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }

            if (gameManager == null || gameManager.State != GameState.Playing || IsComplete)
            {
                return;
            }

            elapsedSeconds += Time.deltaTime;
            if (IsComplete)
            {
                gameManager.Win();
            }
        }
    }
}
