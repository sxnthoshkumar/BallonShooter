using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace IMXR.BalloonShooter
{
    /// <summary>
    /// Calibration Manager assisting the player to align their hand in front of the webcam
    /// and test the aiming crosshair before beginning the match.
    /// </summary>
    public class CalibrationManager : MonoBehaviour
    {
        public static CalibrationManager Instance { get; private set; }

        [Header("UI Overlay")]
        [SerializeField] private GameObject calibrationPanel;
        [SerializeField] private Text calibrationStatusText;
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private Toggle invertYToggle;
        [SerializeField] private Button startMatchButton;

        private bool isCalibrating = false;

        public bool IsCalibrating => isCalibrating;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (startMatchButton != null)
            {
                startMatchButton.onClick.AddListener(CompleteCalibrationAndPlay);
            }

            if (sensitivitySlider != null)
            {
                sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            }

            if (invertYToggle != null)
            {
                invertYToggle.onValueChanged.AddListener(OnInvertYChanged);
            }
        }

        public void ShowCalibration()
        {
            isCalibrating = true;
            if (calibrationPanel != null)
            {
                calibrationPanel.SetActive(true);
            }
        }

        public void HideCalibration()
        {
            isCalibrating = false;
            if (calibrationPanel != null)
            {
                calibrationPanel.SetActive(false);
            }
        }

        private void Update()
        {
            if (!isCalibrating) return;

            if (calibrationStatusText != null && HandTrackingReceiver.Instance != null)
            {
                if (!HandTrackingReceiver.Instance.IsConnected)
                {
                    calibrationStatusText.text = "Waiting for Python CV script...\nRun 'python main.py' in your terminal.";
                    calibrationStatusText.color = new Color(1f, 0.4f, 0.2f);
                }
                else if (!HandTrackingReceiver.Instance.IsHandDetected)
                {
                    calibrationStatusText.text = "Place your hand clearly in front of the laptop webcam.\nRaise your Index Finger to aim!";
                    calibrationStatusText.color = new Color(1f, 0.8f, 0.2f);
                }
                else
                {
                    calibrationStatusText.text = "Hand Detected! Move your index finger to test crosshair.\nClick 'START GAME' or perform shooting gesture to start!";
                    calibrationStatusText.color = new Color(0.2f, 1f, 0.4f);
                }
            }
        }

        private void OnSensitivityChanged(float val)
        {
            if (AimController.Instance != null)
            {
                AimController.Instance.SetTuning(val, val, 0.04f, false, invertYToggle != null && invertYToggle.isOn);
            }
        }

        private void OnInvertYChanged(bool val)
        {
            if (AimController.Instance != null)
            {
                float sens = sensitivitySlider != null ? sensitivitySlider.value : 1.0f;
                AimController.Instance.SetTuning(sens, sens, 0.04f, false, val);
            }
        }

        public void CompleteCalibrationAndPlay()
        {
            HideCalibration();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame();
            }
        }
    }
}
