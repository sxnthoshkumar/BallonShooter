"""
Thumb-Aim + Index Touch Trigger Gesture Detector.
Mechanics:
- Thumb Tip (Landmark 4) controls the steady 3D Aim Reticle.
- Index Finger (Landmark 8) touches the Thumb Tip to FIRE!
- Releasing the index finger upwards re-arms the trigger instantly.
"""

import time
import math
import numpy as np
import config

class GestureDetector:
    def __init__(self):
        self.last_shot_time = 0.0
        self.is_armed = True            # True when index finger is raised and ready to tap
        self.gesture_state = "NO_HAND"  # NO_HAND, AIMING, TOUCH_FIRE, COOLDOWN, RELEASE_TO_REARM
        self.smoothed_dist = 0.5

    def _euclidean_dist(self, p1, p2):
        """Calculate 3D Euclidean distance between two MediaPipe landmarks."""
        return math.sqrt((p1[0] - p2[0])**2 + (p1[1] - p2[1])**2 + (p1[2] - p2[2])**2)

    def detect_gesture(self, landmarks_list):
        """
        Evaluates hand landmarks for Thumb Aim + Index Touch firing.
        Returns:
            shoot (bool): True if index touched thumb this frame
            gesture_state (str): "AIMING", "TOUCH_FIRE", "COOLDOWN", "NO_HAND"
            is_aiming (bool): True if hand is detected and aiming
            debug_info (dict): Diagnostic metrics for visual HUD
        """
        current_time = time.time()
        time_since_last_shot = current_time - self.last_shot_time
        in_cooldown = time_since_last_shot < config.SHOOT_COOLDOWN_SEC
        cooldown_remaining = max(0.0, config.SHOOT_COOLDOWN_SEC - time_since_last_shot)

        if not landmarks_list or len(landmarks_list) < 21:
            self.gesture_state = "NO_HAND"
            self.is_armed = True
            debug_info = {
                "touch_distance": 0.0,
                "is_armed": True,
                "cooldown_remaining": 0.0,
                "is_aiming": False,
                "thumb_pos": (0.5, 0.5),
                "index_pos": (0.5, 0.5)
            }
            return False, self.gesture_state, False, debug_info

        # Key Landmarks
        wrist = landmarks_list[0]
        thumb_tip = landmarks_list[4]      # Aim pointer
        index_tip = landmarks_list[8]      # Touch hammer
        middle_mcp = landmarks_list[9]

        # 1. Palm Scale Normalization (Wrist to Middle MCP)
        palm_scale = self._euclidean_dist(wrist, middle_mcp)
        if palm_scale < 1e-4:
            palm_scale = 0.2

        # 2. Normalized Touch Distance (Index Tip to Thumb Tip)
        raw_dist = self._euclidean_dist(thumb_tip, index_tip) / palm_scale

        # Smooth distance to eliminate sensor flutter
        alpha = 0.50
        self.smoothed_dist = alpha * raw_dist + (1.0 - alpha) * self.smoothed_dist

        # 3. Hysteresis Re-arming Logic (Index lifts back up)
        if self.smoothed_dist >= config.TOUCH_RELEASE_THRESHOLD:
            self.is_armed = True

        shoot_event = False

        # 4. State & Firing Evaluation
        if in_cooldown:
            self.gesture_state = "COOLDOWN"
        elif self.smoothed_dist <= config.TOUCH_FIRE_THRESHOLD and self.is_armed:
            # INDEX TOUCHED THUMB -> FIRE!
            shoot_event = True
            self.is_armed = False
            self.last_shot_time = current_time
            self.gesture_state = "TOUCH_FIRE"
        elif not self.is_armed:
            self.gesture_state = "RELEASE_TO_REARM"
        else:
            self.gesture_state = "AIMING"

        debug_info = {
            "touch_distance": round(self.smoothed_dist, 3),
            "is_armed": self.is_armed,
            "cooldown_remaining": round(cooldown_remaining, 2),
            "is_aiming": True,
            "thumb_pos": (thumb_tip[0], thumb_tip[1]),
            "index_pos": (index_tip[0], index_tip[1]),
            "raw_dist": round(raw_dist, 3)
        }

        return shoot_event, self.gesture_state, True, debug_info
