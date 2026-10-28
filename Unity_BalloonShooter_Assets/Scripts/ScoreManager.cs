using System;
using UnityEngine;

namespace IMXR.BalloonShooter
{
    /// <summary>
    /// Tracks game score, shots fired, accuracy percentage, and combo streak multipliers.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [Header("Combo System")]
        [SerializeField] private float comboWindowSec = 2.5f;

        // Statistics
        private int currentScore = 0;
        private int balloonsPopped = 0;
        private int totalShotsFired = 0;
        private int currentCombo = 0;
        private int bestCombo = 0;
        private float lastHitTime = -10f;

        public int CurrentScore => currentScore;
        public int BalloonsPopped => balloonsPopped;
        public int TotalShotsFired => totalShotsFired;
        public int CurrentCombo => currentCombo;
        public int BestCombo => bestCombo;

        public float AccuracyPercentage
        {
            get
            {
                if (totalShotsFired <= 0) return 0f;
                return Mathf.Clamp((float)balloonsPopped / totalShotsFired * 100f, 0f, 100f);
            }
        }

        public float CurrentMultiplier
        {
            get
            {
                if (currentCombo >= 10) return 3.0f;
                if (currentCombo >= 6) return 2.5f;
                if (currentCombo >= 3) return 2.0f;
                if (currentCombo >= 1) return 1.5f;
                return 1.0f;
            }
        }

        public event Action<int, int> OnScoreChanged; // (newScore, addedPoints)
        public event Action<int, float> OnComboChanged; // (comboCount, multiplier)

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void ResetScore()
        {
            currentScore = 0;
            balloonsPopped = 0;
            totalShotsFired = 0;
            currentCombo = 0;
            bestCombo = 0;
            lastHitTime = -10f;

            OnScoreChanged?.Invoke(0, 0);
            OnComboChanged?.Invoke(0, 1.0f);
        }

        private void Update()
        {
            // Reset combo if player waited too long between hits
            if (currentCombo > 0 && (Time.time - lastHitTime) > comboWindowSec)
            {
                currentCombo = 0;
                OnComboChanged?.Invoke(0, 1.0f);
            }
        }

        public void RegisterHit(int basePoints, Vector3 hitWorldPos)
        {
            totalShotsFired++;
            balloonsPopped++;
            lastHitTime = Time.time;
            currentCombo++;

            if (currentCombo > bestCombo)
            {
                bestCombo = currentCombo;
            }

            int earnedPoints = Mathf.RoundToInt(basePoints * CurrentMultiplier);
            currentScore += earnedPoints;

            OnScoreChanged?.Invoke(currentScore, earnedPoints);
            OnComboChanged?.Invoke(currentCombo, CurrentMultiplier);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowFloatingScore(earnedPoints, CurrentMultiplier, hitWorldPos);
            }

            if (currentCombo >= 3 && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayComboSound(currentCombo);
            }
        }

        public void RegisterMiss()
        {
            totalShotsFired++;
            if (currentCombo > 0)
            {
                currentCombo = 0;
                OnComboChanged?.Invoke(0, 1.0f);
            }
        }
    }
}
