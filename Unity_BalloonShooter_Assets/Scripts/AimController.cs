using UnityEngine;
using UnityEngine.UI;

namespace IMXR.BalloonShooter
{
    /// <summary>
    /// Controls the aiming reticle, coordinate mapping from hand tracking,
    /// smoothing/filtering, and projects 3D raycasts into the virtual world.
    /// </summary>
    public class AimController : MonoBehaviour
    {
        public static AimController Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private RectTransform crosshairRect;
        [SerializeField] private Image crosshairImage;

        [Header("Aim Mapping & Tuning")]
        [Range(0.01f, 0.5f)]
        [Tooltip("Smooth time for aim dampening. Lower = sharper, Higher = smoother.")]
        [SerializeField] private float smoothTime = 0.04f;

        [SerializeField] private float sensitivityX = 1.0f;
        [SerializeField] private float sensitivityY = 1.0f;
        [SerializeField] private bool invertX = false;
        [SerializeField] private bool invertY = false;

        [Header("Crosshair Aesthetics")]
        [SerializeField] private Color normalColor = new Color(0f, 0.9f, 1f, 0.9f);       // Cyan
        [SerializeField] private Color lockOnColor = new Color(1f, 0.2f, 0.2f, 1.0f);     // Neon Red
        [SerializeField] private Color firingColor = new Color(1f, 1f, 1f, 1.0f);        // White Flash
        [SerializeField] private Color lostHandColor = new Color(1f, 0.5f, 0f, 0.4f);     // Faded Orange

        [Header("Balloon Detection Layer")]
        [SerializeField] private LayerMask balloonLayerMask = ~0;
        [SerializeField] private float maxRayDistance = 30f;

        [Header("Debug Logging")]
        [SerializeField] private bool enableDebugLogging = true;

        // Runtime state
        private Vector2 currentScreenPos;
        private Vector2 targetScreenPos;
        private Vector2 smoothVelocity;
        private bool isTargetingBalloon = false;
        private Ray currentAimRay;
        private RaycastHit currentHit;
        private bool hasHit = false;
        private float lastLogTime = 0f;

        public Vector2 CurrentScreenPos => currentScreenPos;
        public Ray CurrentAimRay => currentAimRay;
        public bool HasHit => hasHit;
        public RaycastHit CurrentHit => currentHit;
        public bool IsTargetingBalloon => isTargetingBalloon;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            // Auto-bind crosshair if not wired in Inspector
            if (crosshairRect == null)
            {
                GameObject crosshairObj = GameObject.Find("Crosshair");
                if (crosshairObj != null)
                {
                    crosshairRect = crosshairObj.GetComponent<RectTransform>();
                    if (crosshairImage == null) crosshairImage = crosshairObj.GetComponent<Image>();
                }
            }
        }

        private void Start()
        {
            currentScreenPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            targetScreenPos = currentScreenPos;
        }

        private void Update()
        {
            UpdateAimCoordinates();
            UpdateAimRaycast();
            UpdateCrosshairVisuals();
        }

        private void UpdateAimCoordinates()
        {
            bool usingHand = false;

            if (HandTrackingReceiver.Instance != null && HandTrackingReceiver.Instance.IsHandDetected)
            {
                usingHand = true;
                Vector2 norm = HandTrackingReceiver.Instance.NormalizedAim;

                // Center-relative scaling with sensitivity
                float centeredX = (norm.x - 0.5f) * sensitivityX + 0.5f;
                float centeredY = (norm.y - 0.5f) * sensitivityY + 0.5f;

                if (invertX) centeredX = 1.0f - centeredX;
                if (invertY) centeredY = 1.0f - centeredY;

                centeredX = Mathf.Clamp01(centeredX);
                centeredY = Mathf.Clamp01(centeredY);

                targetScreenPos = new Vector2(centeredX * Screen.width, centeredY * Screen.height);
            }
            else
            {
                // If using mouse fallback in editor when no hand is present
                if (InputFallback.TryGetMouseAim(out Vector2 mousePos))
                {
                    targetScreenPos = mousePos;
                }
                else
                {
                    // Gently return target towards screen center
                    targetScreenPos = Vector2.Lerp(targetScreenPos, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), Time.deltaTime * 2f);
                }
            }

            // Smooth interpolation
            currentScreenPos = Vector2.SmoothDamp(currentScreenPos, targetScreenPos, ref smoothVelocity, smoothTime);

            if (crosshairRect != null)
            {
                crosshairRect.position = currentScreenPos;
            }

            // Debug Logging (throttled)
            if (enableDebugLogging && (Time.realtimeSinceStartup - lastLogTime > 0.3f))
            {
                lastLogTime = Time.realtimeSinceStartup;
                if (usingHand)
                {
                    Debug.Log($"[AimController] AIM UPDATED (Hand): screenX={currentScreenPos.x:F1}, screenY={currentScreenPos.y:F1}");
                }
            }
        }

        private void UpdateAimRaycast()
        {
            if (mainCamera == null) return;

            currentAimRay = mainCamera.ScreenPointToRay(currentScreenPos);
            hasHit = Physics.Raycast(currentAimRay, out currentHit, maxRayDistance, balloonLayerMask);

            if (hasHit && currentHit.collider != null)
            {
                Balloon balloon = currentHit.collider.GetComponentInParent<Balloon>();
                isTargetingBalloon = (balloon != null && !balloon.IsPopped);
            }
            else
            {
                isTargetingBalloon = false;
            }
        }

        private void UpdateCrosshairVisuals()
        {
            if (crosshairImage == null) return;

            bool handDetected = HandTrackingReceiver.Instance != null && HandTrackingReceiver.Instance.IsHandDetected;

            if (!handDetected)
            {
                crosshairImage.color = lostHandColor;
                crosshairRect.localScale = Vector3.Lerp(crosshairRect.localScale, Vector3.one * 0.8f, Time.deltaTime * 10f);
            }
            else if (isTargetingBalloon)
            {
                crosshairImage.color = lockOnColor;
                crosshairRect.localScale = Vector3.Lerp(crosshairRect.localScale, Vector3.one * 1.25f, Time.deltaTime * 15f);
            }
            else
            {
                crosshairImage.color = normalColor;
                crosshairRect.localScale = Vector3.Lerp(crosshairRect.localScale, Vector3.one, Time.deltaTime * 10f);
            }
        }

        public void FlashFiringVisual()
        {
            if (crosshairImage != null)
            {
                crosshairImage.color = firingColor;
            }
            if (crosshairRect != null)
            {
                crosshairRect.localScale = Vector3.one * 1.5f;
            }
        }

        public void SetTuning(float sensX, float sensY, float smooth, bool invX, bool invY)
        {
            sensitivityX = sensX;
            sensitivityY = sensY;
            smoothTime = smooth;
            invertX = invX;
            invertY = invY;
        }
    }
}
