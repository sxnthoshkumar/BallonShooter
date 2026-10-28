"""
Configuration settings for Webcam Hand Tracking and Unity UDP Bridge.
Configured for: Thumb Aiming + Index-to-Thumb Touch Trigger Firing.
"""

# ==========================================
# NETWORK SETTINGS (Python -> Unity UDP)
# ==========================================
UDP_IP = "127.0.0.1"        # Localhost
UDP_PORT = 5065             # Port matching Unity HandTrackingReceiver
SEND_FREQUENCY_LIMIT = 0    # 0 for unlimited (send every processed frame, ~30-60 FPS)

# ==========================================
# WEBCAM & OPENCV SETTINGS
# ==========================================
CAMERA_INDEX = 0            # Default laptop webcam (use 1 if you have an external USB webcam)
FRAME_WIDTH = 640           # 640x480 gives optimal tracking speed & low CPU latency
FRAME_HEIGHT = 480
TARGET_FPS = 60
MIRROR_WEBCAM = True        # Horizontal flip so left/right movement matches natural hand motion

# ==========================================
# MEDIAPIPE HAND TRACKING SETTINGS
# ==========================================
MAX_NUM_HANDS = 1           # Focus on single dominant hand for responsive gameplay
MIN_DETECTION_CONFIDENCE = 0.65
MIN_TRACKING_CONFIDENCE = 0.60
MODEL_COMPLEXITY = 1        # 0 = Light, 1 = Full (Accurate)

# ==========================================
# THUMB AIMING & COORDINATE SMOOTHING
# ==========================================
AIM_LANDMARK_INDEX = 4      # Landmark 4 = THUMB_TIP (User aims with thumb)
SMOOTHING_ALPHA = 0.35      # EMA smoothing for steady aim

# Screen boundary clamping and margin expanders (helps reaching screen edges easily)
SCREEN_MARGIN_X = 0.08      # 8% inner deadzone/margin so moving hand reaches screen edges easily
SCREEN_MARGIN_Y = 0.08

INVERT_X = False            # Invert X axis if needed
INVERT_Y = True             # Invert Y because OpenCV Y=0 is TOP, while Unity Screen Y=0 is BOTTOM

# ==========================================
# INDEX-TO-THUMB TOUCH TRIGGER SETTINGS
# ==========================================
# Normalized distance between Index Tip (8) and Thumb Tip (4) to fire
TOUCH_FIRE_THRESHOLD = 0.25     # When index tip touches down on thumb tip -> FIRE!
TOUCH_RELEASE_THRESHOLD = 0.38  # When index lifts back up -> ARMED / READY

# Shooting debounce cooldown in seconds
SHOOT_COOLDOWN_SEC = 0.22       # Fast 220 ms debounce between shots

# ==========================================
# DEBUG & DISPLAY
# ==========================================
SHOW_DEBUG_WINDOW = True
DRAW_LANDMARKS = True
DRAW_TOUCH_CONNECTOR = True     # Draws visual connector line between Thumb Tip and Index Tip
DRAW_HUD_INFO = True
WINDOW_TITLE = "IMXR - Thumb Aim + Index Tap Trigger"
