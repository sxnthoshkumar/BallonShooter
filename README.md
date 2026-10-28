# 🎈 IMXR: 3D Webcam Hand-Tracking Balloon Shooter

An immersive, VR-like 3D computer vision game where your **real hand becomes the game controller** using only a **standard laptop webcam, MediaPipe hand tracking, and Unity 3D**.

No VR headset, controllers, or external sensors required!

---

## 🌟 System Architecture & Interaction Pipeline

```
 [ LAPTOP WEBCAM ]
         │ (OpenCV 640x480 @ 60 FPS)
         ▼
 [ MEDIAPIPE HAND TRACKING ]
         │ (21 3D Landmarks Extraction)
         ▼
 [ INDEX FINGERTIP TRACKING & EMA SMOOTHING ]
         │ (Normalized Coordinates X, Y: 0.0 → 1.0)
         ▼
 [ FINGER GUN GESTURE DETECTOR ]
         │ (Index Extended + Thumb Cocked / Trigger Drop Action)
         ▼
 [ UDP JSON SOCKET STREAMER ] (localhost:5065)
         │
 ════════╪════════════════════════════════════════════════════════════════════
         ▼
 [ UNITY UDP RECEIVER THREAD ]
         │
         ▼
 [ AIM CONTROLLER (Screen Mapping & Viewport Interpolation) ]
         │
         ▼
 [ 3D FIRST-PERSON CAMERA RAYCAST ]
         │
    ┌────┴──────────────────────────┐
    ▼                               ▼
[ BALLOON HIT ]                 [ MISS ]
  ├─ Pop Burst Animation          ├─ Sparks & Miss Sound
  ├─ Confetti Particles           └─ Reset Combo Streak
  ├─ Procedural Pop Audio
  ├─ Score (+10/+20/+50) & Combo
  └─ Respawn New Balloon
```

---

## 📁 Project Structure

```
c:\Users\santhosh kumar\Project Files\IA3\IMXR\
├── python/
│   ├── config.py             # Tracking tuning, UDP ports, thresholds, webcam config
│   ├── hand_tracker.py       # MediaPipe 21-landmark tracking & Exponential Moving Average filter
│   ├── gesture_detector.py   # Finger gun recognition & thumb trigger mechanics with cooldown
│   ├── unity_sender.py       # High-speed non-blocking UDP JSON transmitter
│   ├── main.py               # Main Python CV app with Cyberpunk OpenCV HUD overlay
│   ├── test_sender.py        # Automated test streamer (test Unity without a camera)
│   └── requirements.txt      # Python dependencies (opencv-python, mediapipe, numpy)
│
└── Unity_BalloonShooter_Assets/
    └── Scripts/
        ├── HandTrackingReceiver.cs   # Background UDP listener & thread-safe state queue
        ├── AimController.cs          # Coordinate normalization & crosshair reticle controller
        ├── ShootingController.cs     # 3D Raycasting, Laser tracer line renderer & screen shake
        ├── Balloon.cs                # 3D floating balloon, type variants, colors & pop animation
        ├── BalloonSpawner.cs         # Frustum bounding box spawner & population manager
        ├── ScoreManager.cs           # Score, shots fired, accuracy % & combo multiplier
        ├── GameTimer.cs              # 60-second match countdown & state trigger
        ├── UIManager.cs              # Top HUD, CV tracking bar, popups & game over recap modal
        ├── AudioManager.cs           # Built-in Procedural Audio Synthesizer (Zero asset dependencies!)
        ├── ParticleManager.cs        # Colorful balloon burst confetti & spark particles
        ├── CalibrationManager.cs     # Interactive alignment & sensitivity calibrator
        ├── GameManager.cs            # Master singleton game loop & restart controller
        └── Editor/
            └── SceneSetupHelper.cs   # 1-Click Unity Editor scene generator!
```

---

## 🚀 Quick Setup & Installation

### Step 1: Python Setup (Computer Vision Engine)

1. Open your terminal / command prompt in the project root:
   ```bash
   cd "c:\Users\santhosh kumar\Project Files\IA3\IMXR"
   ```

2. Create and activate a Python virtual environment:
   ```powershell
   python -m venv venv
   .\venv\Scripts\activate
   ```

3. Install the required computer vision libraries:
   ```powershell
   pip install -r python/requirements.txt
   ```

4. Test run the hand tracker:
   ```powershell
   python python/main.py
   ```
   *You should see your webcam window open with a live cyber HUD, hand landmark skeleton, and aiming crosshair!*
   *(Press `q` to close).*

---

### Step 2: Unity Setup (3D Game Engine)

1. Open **Unity Hub** and create a new **3D (Core / Built-in Render Pipeline)** project (Unity 2021.3 LTS, 2022.3 LTS, or 6000+).
2. Copy the `Unity_BalloonShooter_Assets\Scripts\` folder into your Unity project's `Assets\` directory (so it is at `Assets/Scripts/`).
3. Return to Unity. Let the scripts compile.
4. In the top Unity menu bar, click:
   👉 **`Tools` → `IMXR Balloon Shooter` → `Build Complete 3D Game Scene`**
5. Click **"Yes, Build Scene"**.
   *That's it! The automated scene helper will construct the 3D room, lighting, camera, HUD canvas, crosshair, balloon spawner, particle systems, procedural sound synthesizers, and all managers automatically!*
6. Save your scene as `Assets/Scenes/MainGameScene.unity`.

---

## 🎮 How to Play

1. Start the Python CV script:
   ```powershell
   python python/main.py
   ```
2. In Unity, press the **▶ Play** button.
3. Position yourself ~50–80 cm in front of your webcam.
4. **Aiming**: Form a "Finger Gun" with your hand (Index finger extended forward, other 3 fingers curled). Move your index finger around to glide the crosshair across the screen.
5. **Shooting**: 
   - Cock your thumb up (Ready to fire, reticle turns green).
   - Flick your thumb down or squeeze inward (like pulling a trigger) to fire!
   - *Tip:* You can also tap your thumb against your index knuckle for a quick-trigger tap.
   - *Desktop fallback:* You can also press `Spacebar` or `Left Mouse Click` in Unity to fire anytime.
6. Pop red, blue, green, and rare golden balloons within the **60-second time limit**!

---

## 🎓 Technical & Viva Demonstration Guide

### 1. How Webcam Coordinates become Unity 3D Aiming Coordinates
1. **OpenCV Frame Capture**: Captured at $640 \times 480$ pixels. Horizontally flipped (`cv2.flip(frame, 1)`) so left/right movements act as a natural mirror.
2. **MediaPipe Landmark Extraction**: MediaPipe outputs Landmark #8 (`INDEX_FINGER_TIP`) in normalized coordinates $(x, y) \in [0.0, 1.0]$ where $(0,0)$ is top-left.
3. **Coordinate Inversion & Margin Expansion**:
   - Unity screen coordinates place $(0,0)$ at bottom-left, so $y_{unity} = 1.0 - y_{opencv}$.
   - An 8% boundary margin is applied so the player doesn't have to push their physical hand out of camera view to reach the screen edges:
     $$x_{mapped} = \text{clamp}\left(\frac{x - 0.08}{1.0 - 2(0.08)}, 0.0, 1.0\right)$$
4. **Exponential Moving Average (EMA) Jitter Reduction**:
   $$P_t = \alpha \cdot P_{raw} + (1 - \alpha) \cdot P_{t-1} \quad (\alpha = 0.35)$$
   This eliminates webcam noise while maintaining zero noticeable latency.
5. **Unity 3D Raycasting**:
   - `AimController` converts $(x, y)$ to Screen Space: `Vector2 screenPos = new Vector2(x * Screen.width, y * Screen.height)`.
   - `Camera.main.ScreenPointToRay(screenPos)` casts a 3D ray through the virtual viewport into the physics scene.

---

### 2. How the Finger Gun Gesture & Trigger Work
- **Index Extension Check**: The Euclidean distance from `Wrist (0)` to `IndexTip (8)` must exceed $1.3 \times$ `Wrist to IndexPIP (6)`.
- **Fingers Curl Check**: Distances from `Wrist` to `MiddleTip (12)`, `RingTip (16)`, and `PinkyTip (20)` are less than their PIP joints.
- **Trigger Pull Detection**:
  - *Cocked State*: Thumb tip is elevated above the thumb IP joint.
  - *Trigger State*: Thumb drops rapidly ($\Delta Y > 0.065$) or thumb tip moves towards the index knuckle (`dist < 0.055`).
- **Cooldown Debounce**: A $0.35\text{s}$ debounce timer prevents accidental multi-firing per trigger pull.

---

### 3. Built-in Procedural Audio Synthesizer (Zero Assets Needed!)
`AudioManager.cs` utilizes mathematical wave formulas at runtime (`AudioClip.Create`) to generate sound:
- **Balloon Pop**: Rapid pitch-decay sine wave (650Hz $\to$ 60Hz) blended with exponential white noise.
- **Laser Blaster**: High-speed downward frequency sweep (1400Hz $\to$ 250Hz).
- **Combo Bell**: Polyphonic harmonic chord (880Hz + 1320Hz + 1760Hz).
- **Game Over**: Ascending 3-tone arpeggio fanfare.

---

## 🔧 Troubleshooting

| Issue | Cause | Solution |
|---|---|---|
| Python says `Cannot open webcam` | Incorrect camera index | In `python/config.py`, change `CAMERA_INDEX = 0` to `1` or `2`. |
| Crosshair inverted vertically | OpenCV vs Unity Y-axis | In `config.py`, check `INVERT_Y = True`, or toggle Invert Y in Unity. |
| Unity not receiving packets | UDP port blocked | Ensure port `5065` is allowed on localhost, or restart Python script. |
| Hand tracking flickers | Dim lighting | Ensure your hand is well-lit and clearly distinct from the background. |
| Want to test Unity without camera | Testing mode | Run `python python/test_sender.py` to send automated aiming and shots! |
