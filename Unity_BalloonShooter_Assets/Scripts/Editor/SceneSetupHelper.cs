#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace IMXR.BalloonShooter.Editor
{
    /// <summary>
    /// Automated Unity Editor Scene Builder.
    /// Creates the complete 3D immersive arena, lighting, First-Person camera,
    /// Cyberpunk HUD Canvas, Crosshair, Balloon Spawner, and all C# managers with one click!
    /// </summary>
    public class SceneSetupHelper : EditorWindow
    {
        [MenuItem("Tools/IMXR Balloon Shooter/Build Complete 3D Game Scene", false, 0)]
        public static void BuildCompleteGameScene()
        {
            if (EditorUtility.DisplayDialog("Build 3D Balloon Shooter Scene",
                "This will generate the complete 3D environment, Lighting, First-Person Camera, Cyber HUD Canvas, Crosshair, and all Game Managers in your active scene.\n\nProceed?",
                "Yes, Build Scene", "Cancel"))
            {
                ExecuteSceneBuild();
            }
        }

        private static void ExecuteSceneBuild()
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();

            // 1. Setup Camera
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
                camObj.tag = "MainCamera";
            }
            mainCam.transform.position = new Vector3(0f, 1.6f, 0f);
            mainCam.transform.rotation = Quaternion.identity;
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.05f, 0.07f, 0.12f, 1f); // Sleek dark navy
            mainCam.fieldOfView = 60f;

            // 2. Setup Lighting
#if UNITY_2023_1_OR_NEWER
            Light dirLight = Object.FindFirstObjectByType<Light>();
#else
            Light dirLight = FindObjectOfType<Light>();
#endif
            if (dirLight == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                dirLight = lightObj.AddComponent<Light>();
                dirLight.type = LightType.Directional;
            }
            dirLight.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            dirLight.color = new Color(0.9f, 0.95f, 1f);
            dirLight.intensity = 1.1f;

            // Add subtle neon accent light
            GameObject accentLightObj = GameObject.Find("NeonAccentLight");
            if (accentLightObj == null)
            {
                accentLightObj = new GameObject("NeonAccentLight");
                Light pLight = accentLightObj.AddComponent<Light>();
                pLight.type = LightType.Point;
                pLight.color = new Color(0f, 0.8f, 1f);
                pLight.intensity = 2f;
                pLight.range = 25f;
                accentLightObj.transform.position = new Vector3(0f, 4f, 6f);
            }

            // 3. Build 3D Arena Environment (Floor, Walls, Target Pedestals)
            GameObject envRoot = GameObject.Find("Environment_Arena");
            if (envRoot == null)
            {
                envRoot = new GameObject("Environment_Arena");

                // Floor
                GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "Floor";
                floor.transform.SetParent(envRoot.transform);
                floor.transform.position = new Vector3(0f, 0f, 8f);
                floor.transform.localScale = new Vector3(2.5f, 1f, 2.5f);
                Material floorMat = new Material(Shader.Find("Standard"));
                floorMat.color = new Color(0.1f, 0.12f, 0.16f);
                floorMat.SetFloat("_Glossiness", 0.4f);
                floor.GetComponent<MeshRenderer>().material = floorMat;

                // Back Wall
                GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                backWall.name = "BackWall";
                backWall.transform.SetParent(envRoot.transform);
                backWall.transform.position = new Vector3(0f, 3f, 14f);
                backWall.transform.localScale = new Vector3(25f, 8f, 1f);
                Material wallMat = new Material(Shader.Find("Standard"));
                wallMat.color = new Color(0.07f, 0.08f, 0.11f);
                backWall.GetComponent<MeshRenderer>().material = wallMat;

                // Decorative Cyber Pillars
                CreatePillar(envRoot.transform, new Vector3(-5.5f, 2.5f, 11f));
                CreatePillar(envRoot.transform, new Vector3(5.5f, 2.5f, 11f));
            }

            // 4. Ensure EventSystem for UI
#if UNITY_2023_1_OR_NEWER
            EventSystem existingEventSystem = Object.FindFirstObjectByType<EventSystem>();
#else
            EventSystem existingEventSystem = FindObjectOfType<EventSystem>();
#endif
            if (existingEventSystem == null)
            {
                GameObject eventObj = new GameObject("EventSystem");
                existingEventSystem = eventObj.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                eventObj.AddComponent<InputSystemUIInputModule>();
#else
                eventObj.AddComponent<StandaloneInputModule>();
#endif
            }
            else
            {
#if ENABLE_INPUT_SYSTEM
                // If existing EventSystem still has legacy StandaloneInputModule in Unity 6, upgrade it
                StandaloneInputModule legacyModule = existingEventSystem.GetComponent<StandaloneInputModule>();
                if (legacyModule != null)
                {
                    DestroyImmediate(legacyModule);
                }
                if (existingEventSystem.GetComponent<InputSystemUIInputModule>() == null)
                {
                    existingEventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                }
#endif
            }

            // 5. Build Cyberpunk UI Canvas
#if UNITY_2023_1_OR_NEWER
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
#else
            Canvas canvas = FindObjectOfType<Canvas>();
#endif
            GameObject canvasObj;
            if (canvas == null)
            {
                canvasObj = new GameObject("GameCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            else
            {
                canvasObj = canvas.gameObject;
            }

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            // --- Top HUD Bar ---
            GameObject topBar = GetOrCreateUIElement(canvasObj.transform, "TopHUDBar");
            RectTransform topBarRect = topBar.GetComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0f, 1f);
            topBarRect.anchorMax = new Vector2(1f, 1f);
            topBarRect.pivot = new Vector2(0.5f, 1f);
            topBarRect.anchoredPosition = new Vector2(0f, 0f);
            topBarRect.sizeDelta = new Vector2(0f, 80f);

            // Score Text (Top Left)
            Text scoreTxt = CreateUIText(topBar.transform, "ScoreText", "SCORE: 0", 36, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(400f, 60f), defaultFont, new Color(0f, 1f, 0.8f));

            // Timer Text (Top Center)
            Text timerTxt = CreateUIText(topBar.transform, "TimerText", "TIME: 01:00", 42, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 60f), defaultFont, Color.white);

            // Accuracy Text (Top Right)
            Text accuracyTxt = CreateUIText(topBar.transform, "AccuracyText", "ACCURACY: 100%", 32, TextAnchor.MiddleRight, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-40f, 0f), new Vector2(400f, 60f), defaultFont, new Color(1f, 0.85f, 0.2f));

            // Combo Streak Text (Below Timer)
            Text comboTxt = CreateUIText(topBar.transform, "ComboText", "COMBO x2.0!", 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -25f), new Vector2(300f, 40f), defaultFont, Color.yellow);
            comboTxt.gameObject.SetActive(false);

            // --- Bottom Hand Tracking Status Bar ---
            GameObject bottomBar = GetOrCreateUIElement(canvasObj.transform, "BottomTrackingBar");
            RectTransform bottomBarRect = bottomBar.GetComponent<RectTransform>();
            bottomBarRect.anchorMin = new Vector2(0.5f, 0f);
            bottomBarRect.anchorMax = new Vector2(0.5f, 0f);
            bottomBarRect.pivot = new Vector2(0.5f, 0f);
            bottomBarRect.anchoredPosition = new Vector2(0f, 20f);
            bottomBarRect.sizeDelta = new Vector2(750f, 50f);

            // Add sleek translucent dark panel background
            Image bottomBg = bottomBar.GetComponent<Image>() ?? bottomBar.AddComponent<Image>();
            bottomBg.color = new Color(0.04f, 0.05f, 0.08f, 0.75f);

            // Status Indicator Dot
            GameObject dotObj = GetOrCreateUIElement(bottomBar.transform, "StatusDot");
            RectTransform dotRect = dotObj.GetComponent<RectTransform>();
            dotRect.anchorMin = new Vector2(0f, 0.5f);
            dotRect.anchorMax = new Vector2(0f, 0.5f);
            dotRect.anchoredPosition = new Vector2(25f, 0f);
            dotRect.sizeDelta = new Vector2(14f, 14f);
            Image dotImg = dotObj.GetComponent<Image>() ?? dotObj.AddComponent<Image>();
            dotImg.color = Color.green;

            Text camStatusTxt = CreateUIText(bottomBar.transform, "CamStatusText", "CAMERA: CONNECTED", 18, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(45f, 0f), new Vector2(220f, 30f), defaultFont, new Color(0.2f, 1f, 0.4f));
            Text handStatusTxt = CreateUIText(bottomBar.transform, "HandStatusText", "HAND: DETECTED", 18, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220f, 30f), defaultFont, new Color(0.2f, 0.9f, 1f));
            Text gestureStatusTxt = CreateUIText(bottomBar.transform, "GestureStatusText", "GESTURE: READY", 18, TextAnchor.MiddleRight, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-25f, 0f), new Vector2(220f, 30f), defaultFont, Color.white);

            // --- Crosshair UI Element ---
            GameObject crosshairObj = GetOrCreateUIElement(canvasObj.transform, "Crosshair");
            RectTransform crosshairRect = crosshairObj.GetComponent<RectTransform>();
            crosshairRect.sizeDelta = new Vector2(44f, 44f);
            Image crosshairImg = crosshairObj.GetComponent<Image>() ?? crosshairObj.AddComponent<Image>();
            crosshairImg.color = new Color(0f, 0.9f, 1f, 0.9f);
            crosshairImg.raycastTarget = false;

            // --- Floating Score Container ---
            GameObject floatContainer = GetOrCreateUIElement(canvasObj.transform, "FloatingScoreContainer");
            RectTransform floatContainerRect = floatContainer.GetComponent<RectTransform>();
            floatContainerRect.anchorMin = Vector2.zero;
            floatContainerRect.anchorMax = Vector2.one;
            floatContainerRect.sizeDelta = Vector2.zero;

            // --- Game Over Modal ---
            GameObject gameOverModal = GetOrCreateUIElement(canvasObj.transform, "GameOverModal");
            RectTransform modalRect = gameOverModal.GetComponent<RectTransform>();
            modalRect.anchorMin = new Vector2(0.5f, 0.5f);
            modalRect.anchorMax = new Vector2(0.5f, 0.5f);
            modalRect.pivot = new Vector2(0.5f, 0.5f);
            modalRect.anchoredPosition = Vector2.zero;
            modalRect.sizeDelta = new Vector2(600f, 520f);

            Image modalBg = gameOverModal.GetComponent<Image>() ?? gameOverModal.AddComponent<Image>();
            modalBg.color = new Color(0.06f, 0.08f, 0.12f, 0.94f);

            CreateUIText(gameOverModal.transform, "Title", "MISSION COMPLETE", 40, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(500f, 60f), defaultFont, new Color(0f, 1f, 0.85f));
            Text finalScore = CreateUIText(gameOverModal.transform, "FinalScore", "FINAL SCORE: 0", 36, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(500f, 50f), defaultFont, Color.yellow);
            Text finalPopped = CreateUIText(gameOverModal.transform, "FinalPopped", "Balloons Popped: 0", 24, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(500f, 40f), defaultFont, Color.white);
            Text finalShots = CreateUIText(gameOverModal.transform, "FinalShots", "Shots Fired: 0", 24, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -225f), new Vector2(500f, 40f), defaultFont, Color.white);
            Text finalAccuracy = CreateUIText(gameOverModal.transform, "FinalAccuracy", "Accuracy: 0%", 24, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -270f), new Vector2(500f, 40f), defaultFont, new Color(0.3f, 1f, 0.6f));
            Text finalCombo = CreateUIText(gameOverModal.transform, "FinalCombo", "Best Combo: 0x", 24, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -315f), new Vector2(500f, 40f), defaultFont, new Color(1f, 0.7f, 0.2f));

            // Play Again Button
            GameObject btnObj = GetOrCreateUIElement(gameOverModal.transform, "PlayAgainButton");
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0f);
            btnRect.anchorMax = new Vector2(0.5f, 0f);
            btnRect.anchoredPosition = new Vector2(0f, 60f);
            btnRect.sizeDelta = new Vector2(260f, 60f);
            Image btnImg = btnObj.GetComponent<Image>() ?? btnObj.AddComponent<Image>();
            btnImg.color = new Color(0f, 0.75f, 0.95f);
            Button playAgainBtn = btnObj.GetComponent<Button>() ?? btnObj.AddComponent<Button>();

            CreateUIText(btnObj.transform, "BtnText", "PLAY AGAIN", 24, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 50f), defaultFont, Color.black);
            gameOverModal.SetActive(false);

            // 6. Setup Managers Root
            GameObject managersRoot = GameObject.Find("GameManagers");
            if (managersRoot == null)
            {
                managersRoot = new GameObject("GameManagers");
            }

            // Add C# Manager Components
            HandTrackingReceiver receiver = managersRoot.GetComponent<HandTrackingReceiver>() ?? managersRoot.AddComponent<HandTrackingReceiver>();
            AimController aimCtrl = managersRoot.GetComponent<AimController>() ?? managersRoot.AddComponent<AimController>();
            ShootingController shootCtrl = managersRoot.GetComponent<ShootingController>() ?? managersRoot.AddComponent<ShootingController>();
            BalloonSpawner spawner = managersRoot.GetComponent<BalloonSpawner>() ?? managersRoot.AddComponent<BalloonSpawner>();
            ScoreManager scoreMgr = managersRoot.GetComponent<ScoreManager>() ?? managersRoot.AddComponent<ScoreManager>();
            GameTimer gameTimer = managersRoot.GetComponent<GameTimer>() ?? managersRoot.AddComponent<GameTimer>();
            UIManager uiMgr = managersRoot.GetComponent<UIManager>() ?? managersRoot.AddComponent<UIManager>();
            AudioManager audioMgr = managersRoot.GetComponent<AudioManager>() ?? managersRoot.AddComponent<AudioManager>();
            ParticleManager particleMgr = managersRoot.GetComponent<ParticleManager>() ?? managersRoot.AddComponent<ParticleManager>();
            GameManager gameMgr = managersRoot.GetComponent<GameManager>() ?? managersRoot.AddComponent<GameManager>();

            // Wire SerializedFields in AimController & UIManager via Reflection / SerializedObject
            SerializedObject aimSO = new SerializedObject(aimCtrl);
            aimSO.FindProperty("mainCamera").objectReferenceValue = mainCam;
            aimSO.FindProperty("crosshairRect").objectReferenceValue = crosshairRect;
            aimSO.FindProperty("crosshairImage").objectReferenceValue = crosshairImg;
            aimSO.ApplyModifiedProperties();

            SerializedObject uiSO = new SerializedObject(uiMgr);
            uiSO.FindProperty("scoreText").objectReferenceValue = scoreTxt;
            uiSO.FindProperty("timerText").objectReferenceValue = timerTxt;
            uiSO.FindProperty("accuracyText").objectReferenceValue = accuracyTxt;
            uiSO.FindProperty("comboText").objectReferenceValue = comboTxt;
            uiSO.FindProperty("cameraStatusText").objectReferenceValue = camStatusTxt;
            uiSO.FindProperty("handStatusText").objectReferenceValue = handStatusTxt;
            uiSO.FindProperty("gestureStatusText").objectReferenceValue = gestureStatusTxt;
            uiSO.FindProperty("statusIndicatorLight").objectReferenceValue = dotImg;
            uiSO.FindProperty("gameOverPanel").objectReferenceValue = gameOverModal;
            uiSO.FindProperty("finalScoreText").objectReferenceValue = finalScore;
            uiSO.FindProperty("finalPoppedText").objectReferenceValue = finalPopped;
            uiSO.FindProperty("finalShotsText").objectReferenceValue = finalShots;
            uiSO.FindProperty("finalAccuracyText").objectReferenceValue = finalAccuracy;
            uiSO.FindProperty("finalBestComboText").objectReferenceValue = finalCombo;
            uiSO.FindProperty("playAgainButton").objectReferenceValue = playAgainBtn;
            uiSO.FindProperty("floatingTextContainer").objectReferenceValue = floatContainerRect;
            uiSO.ApplyModifiedProperties();

            Undo.CollapseUndoOperations(undoGroup);
            EditorUtility.SetDirty(managersRoot);
            EditorUtility.SetDirty(canvasObj);

            Debug.Log("[SceneSetupHelper] SUCCESS! 3D Balloon Shooter scene, HUD Canvas, Managers, and 3D Arena created and linked!");
            EditorUtility.DisplayDialog("Setup Complete!", "3D Balloon Shooter scene built successfully!\n\n1. Run 'python main.py' in your terminal.\n2. Hit Unity Play button to start playing!", "Awesome!");
        }

        private static GameObject GetOrCreateUIElement(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing.gameObject;

            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static Text CreateUIText(Transform parent, string name, string initialText, int fontSize, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta, Font font, Color color)
        {
            GameObject obj = GetOrCreateUIElement(parent, name);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            Text txt = obj.GetComponent<Text>() ?? obj.AddComponent<Text>();
            txt.font = font;
            txt.fontSize = fontSize;
            txt.text = initialText;
            txt.alignment = alignment;
            txt.color = color;
            txt.raycastTarget = false;

            Outline outline = obj.GetComponent<Outline>() ?? obj.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            return txt;
        }

        private static void CreatePillar(Transform parent, Vector3 position)
        {
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "CyberPillar";
            pillar.transform.SetParent(parent);
            pillar.transform.position = position;
            pillar.transform.localScale = new Vector3(0.5f, 3f, 0.5f);

            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.12f, 0.15f, 0.2f);
            mat.SetFloat("_Metallic", 0.7f);
            mat.SetFloat("_Glossiness", 0.8f);
            pillar.GetComponent<MeshRenderer>().material = mat;
        }
    }
}
#endif
