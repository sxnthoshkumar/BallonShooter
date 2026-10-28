using System.Collections;
using UnityEngine;

namespace IMXR.BalloonShooter
{
    /// <summary>
    /// Executes raycast shooting mechanics when a gesture or trigger is received.
    /// Draws visual 3D laser tracers, triggers balloon pop logic, particles, and audio.
    /// </summary>
    public class ShootingController : MonoBehaviour
    {
        public static ShootingController Instance { get; private set; }

        [Header("Laser Visuals")]
        [SerializeField] private LineRenderer laserLineRenderer;
        [SerializeField] private float laserDuration = 0.08f;
        [SerializeField] private Color laserColor = new Color(0.2f, 1f, 1f, 1f);
        [SerializeField] private float laserWidth = 0.04f;

        [Header("Screen Shake")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float shakeMagnitude = 0.06f;
        [SerializeField] private float shakeDuration = 0.12f;

        [Header("Laser Origin Offset (Relative to Camera)")]
        [SerializeField] private Vector3 gunOffset = new Vector3(0.35f, -0.3f, 0.6f); // Lower-right virtual gun position

        private Vector3 originalCameraLocalPos;
        private Coroutine shakeCoroutine;
        private Coroutine laserCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            if (cameraTransform != null)
            {
                originalCameraLocalPos = cameraTransform.localPosition;
            }

            EnsureLaserRenderer();
        }

        private void Start()
        {
            if (HandTrackingReceiver.Instance != null)
            {
                HandTrackingReceiver.Instance.OnShootTriggered += HandleShootTrigger;
            }
        }

        private void OnDestroy()
        {
            if (HandTrackingReceiver.Instance != null)
            {
                HandTrackingReceiver.Instance.OnShootTriggered -= HandleShootTrigger;
            }
        }

        private void EnsureLaserRenderer()
        {
            if (laserLineRenderer == null)
            {
                GameObject laserObj = new GameObject("LaserTracerRenderer");
                laserObj.transform.SetParent(transform);
                laserLineRenderer = laserObj.AddComponent<LineRenderer>();
                laserLineRenderer.startWidth = laserWidth;
                laserLineRenderer.endWidth = laserWidth * 0.5f;
                laserLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                laserLineRenderer.startColor = laserColor;
                laserLineRenderer.endColor = Color.white;
                laserLineRenderer.positionCount = 2;
                laserLineRenderer.enabled = false;
            }
        }

        public void HandleShootTrigger()
        {
            // Only allow shooting during active gameplay
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                return;
            }

            ExecuteShot();
        }

        public void ExecuteShot()
        {
            if (AimController.Instance == null) return;

            AimController.Instance.FlashFiringVisual();

            Ray aimRay = AimController.Instance.CurrentAimRay;
            Vector3 origin = cameraTransform != null
                ? cameraTransform.TransformPoint(gunOffset)
                : aimRay.origin;

            Vector3 hitPoint;
            bool hitBalloon = false;

            if (AimController.Instance.HasHit)
            {
                RaycastHit hit = AimController.Instance.CurrentHit;
                hitPoint = hit.point;

                Balloon balloon = hit.collider.GetComponentInParent<Balloon>();
                if (balloon != null && !balloon.IsPopped)
                {
                    hitBalloon = true;
                    balloon.Pop(hitPoint);

                    if (ScoreManager.Instance != null)
                    {
                        ScoreManager.Instance.RegisterHit(balloon.ScoreValue, hitPoint);
                    }

                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayPopSound(balloon.PitchModifier);
                    }
                }
                else
                {
                    // Hit non-balloon collider (e.g. wall/floor)
                    if (ParticleManager.Instance != null)
                    {
                        ParticleManager.Instance.SpawnMissSparks(hitPoint, hit.normal);
                    }
                }
            }
            else
            {
                // Missed into distance
                hitPoint = aimRay.origin + aimRay.direction * 25f;
            }

            if (!hitBalloon)
            {
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.RegisterMiss();
                }

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayMissSound();
                }
            }

            // Fire sound
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayLaserShootSound();
            }

            // Draw laser tracer
            if (laserCoroutine != null) StopCoroutine(laserCoroutine);
            laserCoroutine = StartCoroutine(AnimateLaser(origin, hitPoint));

            // Camera shake
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(AnimateScreenShake());
        }

        private IEnumerator AnimateLaser(Vector3 start, Vector3 end)
        {
            if (laserLineRenderer == null) yield break;

            laserLineRenderer.SetPosition(0, start);
            laserLineRenderer.SetPosition(1, end);
            laserLineRenderer.enabled = true;

            yield return new WaitForSeconds(laserDuration);

            laserLineRenderer.enabled = false;
        }

        private IEnumerator AnimateScreenShake()
        {
            if (cameraTransform == null) yield break;

            float elapsed = 0f;
            while (elapsed < shakeDuration)
            {
                float percent = 1f - (elapsed / shakeDuration);
                Vector3 randomOffset = Random.insideUnitSphere * shakeMagnitude * percent;
                cameraTransform.localPosition = originalCameraLocalPos + randomOffset;

                elapsed += Time.deltaTime;
                yield return null;
            }

            cameraTransform.localPosition = originalCameraLocalPos;
        }
    }
}
