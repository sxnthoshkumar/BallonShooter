"""
HandTracker Module using MediaPipe Hands and OpenCV.
Handles video frame processing, 21-landmark extraction, coordinate normalization,
and Exponential Moving Average (EMA) smoothing for jitter-free aiming.
"""

import cv2
import numpy as np
import mediapipe as mp
import config

class HandTracker:
    def __init__(self):
        self.mp_hands = mp.solutions.hands
        self.hands = self.mp_hands.Hands(
            static_image_mode=False,
            max_num_hands=config.MAX_NUM_HANDS,
            model_complexity=config.MODEL_COMPLEXITY,
            min_detection_confidence=config.MIN_DETECTION_CONFIDENCE,
            min_tracking_confidence=config.MIN_TRACKING_CONFIDENCE
        )
        self.mp_draw = mp.solutions.drawing_utils
        self.mp_draw_styles = mp.solutions.drawing_styles

        # Exponential Moving Average (EMA) smoothed coordinates
        self.smoothed_x = 0.5
        self.smoothed_y = 0.5
        self.has_initial_coords = False

    def process_frame(self, frame):
        """
        Process a single OpenCV BGR frame.
        Returns:
            processed_frame: RGB frame with optional drawings
            landmarks_list: List of (x, y, z) 3D normalized landmarks or None
            raw_landmarks: MediaPipe LandmarkList object or None
        """
        if config.MIRROR_WEBCAM:
            frame = cv2.flip(frame, 1)

        h, w, _ = frame.shape
        rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        
        # Performance optimization: Mark image as not writeable before inference
        rgb_frame.flags.writeable = False
        results = self.hands.process(rgb_frame)
        rgb_frame.flags.writeable = True

        landmarks_list = None
        raw_landmarks = None

        if results.multi_hand_landmarks:
            # Select the most confident/primary hand (first detected)
            raw_landmarks = results.multi_hand_landmarks[0]
            landmarks_list = []
            for lm in raw_landmarks.landmark:
                # Convert to normalized (x, y, z)
                landmarks_list.append((lm.x, lm.y, lm.z))

        return frame, landmarks_list, raw_landmarks

    def get_aim_coordinates(self, landmarks_list):
        """
        Extracts index fingertip (Landmark 8) and applies EMA smoothing & margin mapping.
        Returns:
            aim_x (0.0 to 1.0): Normalized screen horizontal aim
            aim_y (0.0 to 1.0): Normalized screen vertical aim (0 = bottom, 1 = top for Unity)
            raw_x, raw_y: Unsmoothed normalized values
        """
        if not landmarks_list or len(landmarks_list) < 21:
            return None, None, None, None

        # Extract aim point (Landmark 4 = THUMB_TIP)
        aim_landmark = landmarks_list[config.AIM_LANDMARK_INDEX]
        raw_x = aim_landmark[0]
        raw_y = aim_landmark[1]

        # Expand margin so user can comfortably reach all screen edges
        # Map [margin_x, 1 - margin_x] -> [0, 1]
        x_mapped = np.clip((raw_x - config.SCREEN_MARGIN_X) / (1.0 - 2.0 * config.SCREEN_MARGIN_X), 0.0, 1.0)
        y_mapped = np.clip((raw_y - config.SCREEN_MARGIN_Y) / (1.0 - 2.0 * config.SCREEN_MARGIN_Y), 0.0, 1.0)

        # Inversion settings
        if config.INVERT_X:
            x_mapped = 1.0 - x_mapped
        if config.INVERT_Y:
            # OpenCV Y is 0 at top, 1 at bottom. Unity Screen Y is 0 at bottom, 1 at top.
            y_mapped = 1.0 - y_mapped

        # Apply Exponential Moving Average (EMA) smoothing for stability
        if not self.has_initial_coords:
            self.smoothed_x = x_mapped
            self.smoothed_y = y_mapped
            self.has_initial_coords = True
        else:
            alpha = config.SMOOTHING_ALPHA
            self.smoothed_x = alpha * x_mapped + (1.0 - alpha) * self.smoothed_x
            self.smoothed_y = alpha * y_mapped + (1.0 - alpha) * self.smoothed_y

        return round(float(self.smoothed_x), 4), round(float(self.smoothed_y), 4), raw_x, raw_y

    def reset_smoothing(self):
        """Reset filter when hand disappears to prevent laggy jumps on reappearance."""
        self.has_initial_coords = False

    def draw_landmarks(self, frame, raw_landmarks):
        """Draws hand skeleton and landmarks on the debug frame."""
        if raw_landmarks and config.DRAW_LANDMARKS:
            self.mp_draw.draw_landmarks(
                frame,
                raw_landmarks,
                self.mp_hands.HAND_CONNECTIONS,
                self.mp_draw_styles.get_default_hand_landmarks_style(),
                self.mp_draw_styles.get_default_hand_connections_style()
            )
