using System;
using UnityEngine;

namespace IMXR.BalloonShooter
{
    /// <summary>
    /// Manages match countdown timer (default 60 seconds).
    /// Triggers game-over when time expires.
    /// </summary>
    public class GameTimer : MonoBehaviour
    {
        public static GameTimer Instance { get; private set; }

        [Header("Timer Settings")]
        [SerializeField] private float matchDuration = 60.0f;

        private float timeRemaining = 60.0f;
        private bool isTimerRunning = false;

        public float TimeRemaining => Mathf.Max(0f, timeRemaining);
        public bool IsRunning => isTimerRunning;
        public float Duration => matchDuration;

        public event Action<float> OnTimerTick;
        public event Action OnTimerCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            timeRemaining = matchDuration;
        }

        public void StartTimer(float customDuration = -1)
        {
            timeRemaining = (customDuration > 0) ? customDuration : matchDuration;
            isTimerRunning = true;
            OnTimerTick?.Invoke(timeRemaining);
        }

        public void StopTimer()
        {
            isTimerRunning = false;
        }

        public void ResetTimer()
        {
            isTimerRunning = false;
            timeRemaining = matchDuration;
            OnTimerTick?.Invoke(timeRemaining);
        }

        private void Update()
        {
            if (!isTimerRunning) return;

            timeRemaining -= Time.deltaTime;
            OnTimerTick?.Invoke(timeRemaining);

            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                isTimerRunning = false;
                OnTimerCompleted?.Invoke();

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.GameOver();
                }
            }
        }

        public string GetFormattedTime()
        {
            int seconds = Mathf.CeilToInt(TimeRemaining);
            int mins = seconds / 60;
            int secs = seconds % 60;
            return $"{mins:00}:{secs:00}";
        }
    }
}
