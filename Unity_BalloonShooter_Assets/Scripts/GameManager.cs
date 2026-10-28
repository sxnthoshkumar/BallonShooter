using System;
using UnityEngine;

namespace IMXR.BalloonShooter
{
    public enum GameState
    {
        MainMenu,
        Calibration,
        Playing,
        GameOver
    }

    /// <summary>
    /// Master GameManager singleton coordinating game state flow,
    /// match lifecycle, score resets, spawner control, and UI transitions.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("State")]
        [SerializeField] private GameState currentState = GameState.Playing;

        [Header("Auto Start Configuration")]
        [SerializeField] private bool autoStartAtLaunch = true;

        public GameState CurrentState => currentState;
        public bool IsPlaying => currentState == GameState.Playing;

        public event Action<GameState> OnGameStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (autoStartAtLaunch)
            {
                StartGame();
            }
            else
            {
                SetState(GameState.Calibration);
                if (CalibrationManager.Instance != null)
                {
                    CalibrationManager.Instance.ShowCalibration();
                }
            }
        }

        public void StartGame()
        {
            SetState(GameState.Playing);

            // Reset score and stats
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ResetScore();
            }

            // Hide Game Over Modal
            if (UIManager.Instance != null)
            {
                UIManager.Instance.HideGameOverModal();
            }

            // Start balloon spawning
            if (BalloonSpawner.Instance != null)
            {
                BalloonSpawner.Instance.StartSpawning();
            }

            // Start match timer
            if (GameTimer.Instance != null)
            {
                GameTimer.Instance.StartTimer();
            }
        }

        public void GameOver()
        {
            SetState(GameState.GameOver);

            // Stop spawning balloons
            if (BalloonSpawner.Instance != null)
            {
                BalloonSpawner.Instance.StopSpawning();
            }

            // Stop timer
            if (GameTimer.Instance != null)
            {
                GameTimer.Instance.StopTimer();
            }

            // Play game over audio fanfare
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayGameOverSound();
            }

            // Display Game Over statistics UI
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowGameOverModal();
            }
        }

        public void RestartGame()
        {
            StartGame();
        }

        private void SetState(GameState newState)
        {
            currentState = newState;
            OnGameStateChanged?.Invoke(newState);
        }
    }
}
