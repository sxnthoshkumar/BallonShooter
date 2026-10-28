using UnityEngine;

namespace IMXR.BalloonShooter
{
    /// <summary>
    /// Spawns particle effects for balloon pop explosions, laser sparks, and miss puffs.
    /// Safely configures ParticleSystem modules while stopped to avoid Unity 6 runtime duration errors.
    /// </summary>
    public class ParticleManager : MonoBehaviour
    {
        public static ParticleManager Instance { get; private set; }

        [Header("Optional Prefab Overrides")]
        [SerializeField] private GameObject customExplosionPrefab;
        [SerializeField] private GameObject customSparksPrefab;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void SpawnBalloonPopExplosion(Vector3 position, Color balloonColor)
        {
            if (customExplosionPrefab != null)
            {
                GameObject p = Instantiate(customExplosionPrefab, position, Quaternion.identity);
                Destroy(p, 2f);
                return;
            }

            // Create procedural particle burst
            GameObject pObj = new GameObject("ProceduralPopParticles");
            pObj.transform.position = position;

            ParticleSystem ps = pObj.AddComponent<ParticleSystem>();
            
            // Critical for Unity 6: Stop system completely before modifying main module properties
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystemRenderer psRenderer = pObj.GetComponent<ParticleSystemRenderer>();
            psRenderer.material = new Material(Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default"));

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.2f;
            main.startLifetime = 0.6f;
            main.startSpeed = 7f;
            main.startSize = 0.18f;
            main.startColor = balloonColor;
            main.gravityModifier = 0.8f;
            main.stopAction = ParticleSystemStopAction.None;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 35) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.35f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(balloonColor, 0f), 
                    new GradientColorKey(Color.white, 0.2f), 
                    new GradientColorKey(balloonColor, 1f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(1f, 0f), 
                    new GradientAlphaKey(1f, 0.7f), 
                    new GradientAlphaKey(0f, 1f) 
                }
            );
            colorOverLifetime.color = grad;

            ps.Play();
            Destroy(pObj, 1.0f);
        }

        public void SpawnMissSparks(Vector3 position, Vector3 normal)
        {
            if (customSparksPrefab != null)
            {
                GameObject p = Instantiate(customSparksPrefab, position, Quaternion.LookRotation(normal));
                Destroy(p, 1.5f);
                return;
            }

            GameObject pObj = new GameObject("ProceduralSparks");
            pObj.transform.position = position;
            pObj.transform.rotation = Quaternion.LookRotation(normal);

            ParticleSystem ps = pObj.AddComponent<ParticleSystem>();

            // Critical for Unity 6: Stop system completely before modifying main module properties
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystemRenderer psRenderer = pObj.GetComponent<ParticleSystemRenderer>();
            psRenderer.material = new Material(Shader.Find("Sprites/Default"));

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.15f;
            main.startLifetime = 0.25f;
            main.startSpeed = 4f;
            main.startSize = 0.06f;
            main.startColor = new Color(1f, 0.9f, 0.3f);
            main.stopAction = ParticleSystemStopAction.None;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 12) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;

            ps.Play();
            Destroy(pObj, 0.5f);
        }
    }
}
