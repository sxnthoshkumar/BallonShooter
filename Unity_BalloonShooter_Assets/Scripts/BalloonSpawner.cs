using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IMXR.BalloonShooter
{
    /// <summary>
    /// Spawns and manages 3D balloons inside a safe spatial volume in front of the camera.
    /// Handles respawning, minimum spacing checks, and weighted bonus balloon probabilities.
    /// </summary>
    public class BalloonSpawner : MonoBehaviour
    {
        public static BalloonSpawner Instance { get; private set; }

        [Header("Spawn Prefab Reference")]
        [SerializeField] private GameObject balloonPrefab;

        [Header("Active Population")]
        [SerializeField] private int maxActiveBalloons = 7;
        [SerializeField] private float respawnDelay = 0.5f;

        [Header("Spawn Volume (Relative to Spawner/Camera)")]
        [SerializeField] private float minX = -4.0f;
        [SerializeField] private float maxX = 4.0f;
        [SerializeField] private float minY = 0.6f;
        [SerializeField] private float maxY = 3.2f;
        [SerializeField] private float minZ = 5.5f;
        [SerializeField] private float maxZ = 11.5f;

        [Header("Spacing")]
        [SerializeField] private float minDistanceBetweenBalloons = 1.3f;

        private readonly List<Balloon> activeBalloons = new List<Balloon>();
        private bool isSpawningActive = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void StartSpawning()
        {
            isSpawningActive = true;
            ClearAllBalloons();
            StartCoroutine(InitialSpawnRoutine());
        }

        public void StopSpawning()
        {
            isSpawningActive = false;
            StopAllCoroutines();
        }

        public void ClearAllBalloons()
        {
            for (int i = activeBalloons.Count - 1; i >= 0; i--)
            {
                if (activeBalloons[i] != null)
                {
                    Destroy(activeBalloons[i].gameObject);
                }
            }
            activeBalloons.Clear();
        }

        private IEnumerator InitialSpawnRoutine()
        {
            while (activeBalloons.Count < maxActiveBalloons && isSpawningActive)
            {
                SpawnSingleBalloon();
                yield return new WaitForSeconds(0.12f);
            }
        }

        public void OnBalloonPopped(Balloon balloon)
        {
            activeBalloons.Remove(balloon);

            if (isSpawningActive)
            {
                StartCoroutine(DelayedRespawn());
            }
        }

        private IEnumerator DelayedRespawn()
        {
            yield return new WaitForSeconds(respawnDelay);

            if (isSpawningActive && activeBalloons.Count < maxActiveBalloons)
            {
                SpawnSingleBalloon();
            }
        }

        private void SpawnSingleBalloon()
        {
            Vector3 spawnPos = FindValidSpawnPosition();
            BalloonType chosenType = SelectRandomBalloonType();

            GameObject newBalloonObj;
            if (balloonPrefab != null)
            {
                newBalloonObj = Instantiate(balloonPrefab, spawnPos, Quaternion.identity, transform);
            }
            else
            {
                // Procedural sphere balloon if prefab is missing
                newBalloonObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                newBalloonObj.name = "Procedural_Balloon";
                newBalloonObj.transform.SetParent(transform);
                newBalloonObj.transform.position = spawnPos;
                newBalloonObj.AddComponent<Balloon>();
            }

            Balloon balloonComp = newBalloonObj.GetComponent<Balloon>();
            if (balloonComp == null)
            {
                balloonComp = newBalloonObj.AddComponent<Balloon>();
            }

            balloonComp.Initialize(chosenType, spawnPos);
            activeBalloons.Add(balloonComp);
        }

        private Vector3 FindValidSpawnPosition()
        {
            Vector3 candidatePos = Vector3.zero;
            int maxAttempts = 20;

            for (int i = 0; i < maxAttempts; i++)
            {
                float rx = Random.Range(minX, maxX);
                float ry = Random.Range(minY, maxY);
                float rz = Random.Range(minZ, maxZ);

                candidatePos = transform.position + new Vector3(rx, ry, rz);

                bool tooClose = false;
                foreach (var b in activeBalloons)
                {
                    if (b != null && Vector3.Distance(candidatePos, b.transform.position) < minDistanceBetweenBalloons)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    return candidatePos;
                }
            }

            return candidatePos;
        }

        private BalloonType SelectRandomBalloonType()
        {
            float rand = Random.value;
            if (rand < 0.55f)
            {
                return BalloonType.StandardRed;     // 55%
            }
            else if (rand < 0.78f)
            {
                return BalloonType.SpeedyBlue;      // 23%
            }
            else if (rand < 0.93f)
            {
                return BalloonType.FloatyGreen;     // 15%
            }
            else
            {
                return BalloonType.GoldenBonus;     // 7% Rare Golden
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Visualize spawn bounding box in Unity Editor scene view
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
            Vector3 center = transform.position + new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, (minZ + maxZ) * 0.5f);
            Vector3 size = new Vector3(maxX - minX, maxY - minY, maxZ - minZ);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
