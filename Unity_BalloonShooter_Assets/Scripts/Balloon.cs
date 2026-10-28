using System.Collections;
using UnityEngine;

namespace IMXR.BalloonShooter
{
    public enum BalloonType
    {
        StandardRed,
        SpeedyBlue,
        FloatyGreen,
        GoldenBonus
    }

    /// <summary>
    /// Represents an individual 3D Balloon in the scene.
    /// Manages floating animation, color styling, score value, and popping destruction.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class Balloon : MonoBehaviour
    {
        [Header("Balloon Attributes")]
        [SerializeField] private BalloonType balloonType = BalloonType.StandardRed;
        [SerializeField] private int scoreValue = 10;
        [SerializeField] private float pitchModifier = 1.0f;
        [SerializeField] private Color balloonColor = new Color(1f, 0.2f, 0.3f, 1f);

        [Header("Floating Animation")]
        [SerializeField] private float floatSpeed = 1.5f;
        [SerializeField] private float floatAmplitude = 0.25f;
        [SerializeField] private float swaySpeed = 0.8f;
        [SerializeField] private float swayAmplitude = 0.15f;
        [SerializeField] private float rotationSpeed = 15f;

        [Header("Visual Mesh Reference")]
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Transform visualRoot;

        private Vector3 spawnPosition;
        private float randomSeed;
        private bool isPopped = false;
        private SphereCollider sphereCollider;

        public bool IsPopped => isPopped;
        public int ScoreValue => scoreValue;
        public float PitchModifier => pitchModifier;
        public BalloonType Type => balloonType;
        public Color BalloonColor => balloonColor;

        private void Awake()
        {
            sphereCollider = GetComponent<SphereCollider>();
            if (visualRoot == null) visualRoot = transform;
            if (meshRenderer == null) meshRenderer = GetComponentInChildren<MeshRenderer>();
            randomSeed = Random.Range(0f, 100f);
        }

        private void Start()
        {
            spawnPosition = transform.position;
        }

        public void Initialize(BalloonType type, Vector3 position)
        {
            balloonType = type;
            transform.position = position;
            spawnPosition = position;
            isPopped = false;
            if (sphereCollider != null) sphereCollider.enabled = true;
            transform.localScale = Vector3.one;

            ConfigureTypeAttributes();
        }

        private void ConfigureTypeAttributes()
        {
            switch (balloonType)
            {
                case BalloonType.StandardRed:
                    scoreValue = 10;
                    pitchModifier = 1.0f;
                    balloonColor = new Color(0.95f, 0.15f, 0.2f, 1.0f);
                    floatSpeed = 1.5f;
                    break;
                case BalloonType.SpeedyBlue:
                    scoreValue = 20;
                    pitchModifier = 1.3f;
                    balloonColor = new Color(0.15f, 0.6f, 1.0f, 1.0f);
                    floatSpeed = 2.4f;
                    break;
                case BalloonType.FloatyGreen:
                    scoreValue = 15;
                    pitchModifier = 1.15f;
                    balloonColor = new Color(0.15f, 0.9f, 0.4f, 1.0f);
                    floatSpeed = 1.0f;
                    break;
                case BalloonType.GoldenBonus:
                    scoreValue = 50;
                    pitchModifier = 1.6f;
                    balloonColor = new Color(1.0f, 0.85f, 0.1f, 1.0f);
                    floatSpeed = 3.0f;
                    break;
            }

            ApplyMaterialColor();
        }

        public void ApplyMaterialColor()
        {
            if (meshRenderer != null)
            {
                Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");
                Material mat = (shader != null) ? new Material(shader) : new Material(Shader.Find("Sprites/Default"));
                mat.color = balloonColor;
                if (balloonType == BalloonType.GoldenBonus)
                {
                    if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.8f);
                    if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.9f);
                    if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.9f);
                    mat.EnableKeyword("_EMISSION");
                    if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", balloonColor * 0.4f);
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", balloonColor);
                }
                else
                {
                    if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.6f);
                    if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.6f);
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", balloonColor);
                }
                meshRenderer.material = mat;
            }
        }

        private void Update()
        {
            if (isPopped) return;

            // Gentle procedural floating & swaying
            float time = Time.time + randomSeed;
            float offsetY = Mathf.Sin(time * floatSpeed) * floatAmplitude;
            float offsetX = Mathf.Cos(time * swaySpeed) * swayAmplitude;

            transform.position = spawnPosition + new Vector3(offsetX, offsetY, 0f);
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

        public void Pop(Vector3 hitPoint)
        {
            if (isPopped) return;
            isPopped = true;

            if (sphereCollider != null) sphereCollider.enabled = false;

            // Spawn particle explosion
            if (ParticleManager.Instance != null)
            {
                ParticleManager.Instance.SpawnBalloonPopExplosion(transform.position, balloonColor);
            }

            // Animate rapid pop scale down and destroy
            StartCoroutine(AnimatePopAndDestroy());
        }

        private IEnumerator AnimatePopAndDestroy()
        {
            float duration = 0.08f;
            float elapsed = 0f;
            Vector3 originalScale = transform.localScale;

            // Quick expand then vanish
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(originalScale, originalScale * 1.35f, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Notify spawner
            if (BalloonSpawner.Instance != null)
            {
                BalloonSpawner.Instance.OnBalloonPopped(this);
            }

            Destroy(gameObject);
        }
    }
}
