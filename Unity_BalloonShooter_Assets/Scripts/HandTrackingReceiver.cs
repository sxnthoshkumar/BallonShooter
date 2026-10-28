using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace IMXR.BalloonShooter
{
    [Serializable]
    public class TrackingPacket
    {
        public bool handDetected;
        public float indexX;
        public float indexY;
        public bool shoot;
        public string gestureState;
        public bool isFingerGun;
        public float confidence;
        public double timestamp;
        public int seq;
    }

    /// <summary>
    /// Background UDP receiver thread that listens for Python hand tracking JSON packets.
    /// Thread-safely transfers latest tracking coordinates and shoot events to Unity's main thread.
    /// </summary>
    public class HandTrackingReceiver : MonoBehaviour
    {
        public static HandTrackingReceiver Instance { get; private set; }

        [Header("Network Configuration")]
        [Tooltip("UDP port to listen on. Must match config.py in Python.")]
        [SerializeField] private int port = 5065;

        [Header("Live Tracking State (Read Only)")]
        [SerializeField] private bool isConnected = false;
        [SerializeField] private bool handDetected = false;
        [SerializeField] private Vector2 normalizedAim = new Vector2(0.5f, 0.5f);
        [SerializeField] private string gestureState = "IDLE";
        [SerializeField] private bool isFingerGun = false;
        [SerializeField] private float confidence = 0f;

        [Header("Connection Timeout")]
        [Tooltip("Seconds without UDP packets before marking as disconnected.")]
        [SerializeField] private float disconnectTimeout = 1.2f;

        [Header("Debug Logging")]
        [SerializeField] private bool enableDebugLogging = true;

        // Thread and socket management
        private UdpClient udpClient;
        private Thread receiveThread;
        private volatile bool isRunning = false;

        // Thread-safe lock and staging buffers
        private readonly object lockObject = new object();
        private string pendingJsonData = null;
        private bool shootTriggerPending = false;
        private int totalPacketsReceived = 0;
        private float lastPacketReceivedTime = 0f;
        private float lastLogTime = 0f;

        // Public properties & Events
        public bool IsConnected => isConnected;
        public bool IsHandDetected => handDetected && isConnected;
        public Vector2 NormalizedAim => normalizedAim;
        public string GestureState => gestureState;
        public bool IsFingerGun => isFingerGun;
        public float Confidence => confidence;

        public event Action<Vector2> OnAimUpdated;
        public event Action OnShootTriggered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            StartReceiver();
        }

        public void StartReceiver()
        {
            if (isRunning) return;

            try
            {
                isRunning = true;
                receiveThread = new Thread(ReceiveDataLoop)
                {
                    IsBackground = true,
                    Name = "UDP_HandTrackingReceiver"
                };
                receiveThread.Start();
                Debug.Log($"[HandTrackingReceiver] Listening for Python tracking packets on UDP port {port}...");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HandTrackingReceiver] Failed to start UDP receiver: {ex.Message}");
            }
        }

        private void ReceiveDataLoop()
        {
            try
            {
                udpClient = new UdpClient();
                udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));

                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

                while (isRunning)
                {
                    try
                    {
                        byte[] data = udpClient.Receive(ref remoteEndPoint);
                        if (data != null && data.Length > 0)
                        {
                            string json = Encoding.UTF8.GetString(data);
                            lock (lockObject)
                            {
                                pendingJsonData = json;
                                totalPacketsReceived++;
                            }
                        }
                    }
                    catch (SocketException)
                    {
                        if (!isRunning) break;
                    }
                    catch
                    {
                        if (!isRunning) break;
                    }
                }
            }
            catch (Exception ex)
            {
                if (isRunning)
                {
                    // Will be logged if critical
                }
            }
        }

        private void Update()
        {
            bool hasNewPacket = false;
            string jsonToProcess = null;
            bool triggerShootThisFrame = false;

            lock (lockObject)
            {
                if (pendingJsonData != null)
                {
                    jsonToProcess = pendingJsonData;
                    pendingJsonData = null;
                    hasNewPacket = true;
                    lastPacketReceivedTime = Time.realtimeSinceStartup;
                }

                if (shootTriggerPending)
                {
                    triggerShootThisFrame = true;
                    shootTriggerPending = false;
                }

                // Check for connection timeout safely on Main Thread
                isConnected = (Time.realtimeSinceStartup - lastPacketReceivedTime) < disconnectTimeout && lastPacketReceivedTime > 0f;
            }

            if (hasNewPacket && !string.IsNullOrEmpty(jsonToProcess))
            {
                try
                {
                    TrackingPacket packet = JsonUtility.FromJson<TrackingPacket>(jsonToProcess);
                    if (packet != null)
                    {
                        handDetected = packet.handDetected;
                        normalizedAim = new Vector2(
                            Mathf.Clamp01(packet.indexX),
                            Mathf.Clamp01(packet.indexY)
                        );
                        gestureState = packet.gestureState;
                        isFingerGun = packet.isFingerGun;
                        confidence = packet.confidence;

                        if (packet.shoot)
                        {
                            triggerShootThisFrame = true;
                        }

                        OnAimUpdated?.Invoke(normalizedAim);

                        // Debug Logging
                        if (enableDebugLogging && (Time.realtimeSinceStartup - lastLogTime > 0.3f || packet.shoot))
                        {
                            lastLogTime = Time.realtimeSinceStartup;
                            Debug.Log($"[HandTrackingReceiver] UDP RECEIVED: indexX={packet.indexX:F3}, indexY={packet.indexY:F3}, handDetected={packet.handDetected}, shoot={packet.shoot}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[HandTrackingReceiver] JSON parse error: {ex.Message}");
                }
            }
            else if (!isConnected)
            {
                handDetected = false;
                gestureState = "DISCONNECTED";
            }

            // Keyboard/Mouse fallback for easy testing in Editor
            if (InputFallback.IsShootTriggerPressed())
            {
                triggerShootThisFrame = true;
            }

            if (triggerShootThisFrame)
            {
                OnShootTriggered?.Invoke();
            }
        }

        private void OnApplicationQuit()
        {
            StopReceiver();
        }

        private void OnDestroy()
        {
            StopReceiver();
        }

        public void StopReceiver()
        {
            isRunning = false;

            try
            {
                udpClient?.Close();
                udpClient = null;
            }
            catch { }

            try
            {
                if (receiveThread != null && receiveThread.IsAlive)
                {
                    receiveThread.Join(150);
                    receiveThread = null;
                }
            }
            catch { }

            Debug.Log("[HandTrackingReceiver] UDP receiver stopped.");
        }
    }
}
