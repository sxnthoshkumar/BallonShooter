using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace IMXR.BalloonShooter
{
    /// <summary>
    /// Futuristic Cyberpunk UI Manager for the 3D Balloon Shooter.
    /// Manages Top HUD, Bottom Computer Vision Status bar, Floating Score popups,
    /// and Game-Over summary modal with complete statistics.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Top HUD Elements")]
        [SerializeField] private Text scoreText;
        [SerializeField] private Text timerText;
        [SerializeField] private Text accuracyText;
        [SerializeField] private Text comboText;

        [Header("Bottom Hand Tracking Status Bar")]
        [SerializeField] private Text cameraStatusText;
        [SerializeField] private Text handStatusText;
        [SerializeField] private Text gestureStatusText;
        [SerializeField] private Image statusIndicatorLight;

        [Header("Game Over Screen")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Text finalScoreText;
        [SerializeField] private Text finalPoppedText;
        [SerializeField] private Text finalShotsText;
        [SerializeField] private Text finalAccuracyText;
        [SerializeField] private Text finalBestComboText;
        [SerializeField] private Button playAgainButton;

        [Header("Floating Text Prefab / Parent")]
        [SerializeField] private RectTransform floatingTextContainer;
        [SerializeField] private GameObject floatingTextPrefab;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (playAgainButton != null)
            {
                playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            }
        }

        private void Start()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            UpdateTopHUD();
            UpdateTrackingStatus();
        }

        private void Update()
        {
            UpdateTopHUD();
            UpdateTrackingStatus();
        }

        public void UpdateTopHUD()
        {
            if (ScoreManager.Instance != null)
            {
                if (scoreText != null)
                {
                    scoreText.text = $"SCORE: {ScoreManager.Instance.CurrentScore}";
                }

                if (accuracyText != null)
                {
                    accuracyText.text = $"ACCURACY: {ScoreManager.Instance.AccuracyPercentage:F0}%";
                }

                if (comboText != null)
                {
                    if (ScoreManager.Instance.CurrentCombo >= 2)
                    {
                        comboText.gameObject.SetActive(true);
                        comboText.text = $"COMBO x{ScoreManager.Instance.CurrentMultiplier:F1} ({ScoreManager.Instance.CurrentCombo})";
                        comboText.color = Color.Lerp(Color.yellow, Color.red, (ScoreManager.Instance.CurrentCombo - 2) / 8f);
                    }
                    else
                    {
                        comboText.gameObject.SetActive(false);
                    }
                }
            }

            if (GameTimer.Instance != null && timerText != null)
            {
                timerText.text = $"TIME: {GameTimer.Instance.GetFormattedTime()}";

                if (GameTimer.Instance.TimeRemaining <= 10f)
                {
                    timerText.color = (Mathf.PingPong(Time.time * 4f, 1f) > 0.5f) ? Color.red : Color.white;
                }
                else
                {
                    timerText.color = Color.white;
                }
            }
        }

        public void UpdateTrackingStatus()
        {
            if (HandTrackingReceiver.Instance == null) return;

            bool isConnected = HandTrackingReceiver.Instance.IsConnected;
            bool handDetected = HandTrackingReceiver.Instance.IsHandDetected;
            string gesture = HandTrackingReceiver.Instance.GestureState;

            // Camera status
            if (cameraStatusText != null)
            {
                cameraStatusText.text = isConnected ? "CAMERA: CONNECTED" : "CAMERA: DISCONNECTED";
                cameraStatusText.color = isConnected ? new Color(0.2f, 1f, 0.4f) : new Color(1f, 0.3f, 0.2f);
            }

            // Hand status
            if (handStatusText != null)
            {
                if (!isConnected)
                {
                    handStatusText.text = "HAND: WAITING FOR PYTHON...";
                    handStatusText.color = new Color(0.7f, 0.7f, 0.7f);
                }
                else if (handDetected)
                {
                    handStatusText.text = "HAND: DETECTED";
                    handStatusText.color = new Color(0.1f, 1f, 0.5f);
                }
                else
                {
                    handStatusText.text = "HAND: NOT DETECTED";
                    handStatusText.color = new Color(1f, 0.6f, 0.1f);
                }
            }

            // Gesture status
            if (gestureStatusText != null)
            {
                gestureStatusText.text = $"GESTURE: {gesture}";
                switch (gesture)
                {
                    case "READY_TO_SHOOT":
                        gestureStatusText.color = new Color(0.1f, 1f, 0.3f); // Green
                        break;
                    case "FIRED":
                        gestureStatusText.color = new Color(1f, 0.2f, 0.2f); // Red
                        break;
                    case "AIMING":
                        gestureStatusText.color = new Color(0.2f, 0.8f, 1f); // Cyan
                        break;
                    case "COOLDOWN":
                        gestureStatusText.color = new Color(1f, 0.85f, 0.1f); // Yellow
                        break;
                    default:
                        gestureStatusText.color = new Color(0.6f, 0.6f, 0.6f);
                        break;
                }
            }

            // Indicator dot
            if (statusIndicatorLight != null)
            {
                statusIndicatorLight.color = (isConnected && handDetected)
                    ? new Color(0.1f, 1f, 0.4f, 1f)
                    : (isConnected ? new Color(1f, 0.6f, 0f, 1f) : new Color(1f, 0.2f, 0.2f, 1f));
            }
        }

        public void ShowFloatingScore(int points, float multiplier, Vector3 worldPosition)
        {
            if (floatingTextContainer == null && Camera.main != null) return;

            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
            if (screenPos.z < 0) return; // Behind camera

            GameObject popup = new GameObject("FloatingScorePopup");
            popup.transform.SetParent(floatingTextContainer != null ? floatingTextContainer : transform, false);
            popup.transform.position = screenPos;

            Text text = popup.AddComponent<Text>();
            text.font = (scoreText != null && scoreText.font != null) ? scoreText.font : (Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf"));
            text.fontSize = (multiplier > 1.0f) ? 32 : 26;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = (multiplier > 1.0f) ? $"+{points} (x{multiplier:F1})" : $"+{points}";
            text.color = (multiplier > 1.0f) ? Color.yellow : new Color(0.2f, 1f, 0.5f);

            // Add outline
            Outline outline = popup.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, -2);

            StartCoroutine(AnimateFloatingText(popup, text));
        }

        private IEnumerator AnimateFloatingText(GameObject popup, Text text)
        {
            float duration = 0.8f;
            float elapsed = 0f;
            Vector3 startPos = popup.transform.position;
            Color initialColor = text.color;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                popup.transform.position = startPos + new Vector3(0, t * 60f, 0);
                text.color = new Color(initialColor.r, initialColor.g, initialColor.b, 1f - t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(popup);
        }

        public void ShowGameOverModal()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }

            if (ScoreManager.Instance != null)
            {
                if (finalScoreText != null) finalScoreText.text = $"FINAL SCORE: {ScoreManager.Instance.CurrentScore}";
                if (finalPoppedText != null) finalPoppedText.text = $"Balloons Popped: {ScoreManager.Instance.BalloonsPopped}";
                if (finalShotsText != null) finalShotsText.text = $"Shots Fired: {ScoreManager.Instance.TotalShotsFired}";
                if (finalAccuracyText != null) finalAccuracyText.text = $"Accuracy: {ScoreManager.Instance.AccuracyPercentage:F1}%";
                if (finalBestComboText != null) finalBestComboText.text = $"Best Combo: {ScoreManager.Instance.BestCombo}x";
            }
        }

        public void HideGameOverModal()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
        }

        public void OnPlayAgainClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
        }
    }
}
