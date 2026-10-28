import os
os.environ['TF_CPP_MIN_LOG_LEVEL'] = '3'
import warnings
warnings.filterwarnings('ignore')

import cv2
import time
import sys
import math
import numpy as np

import config
from hand_tracker import HandTracker
from gesture_detector import GestureDetector
from unity_sender import UnitySender

def draw_hud(frame, fps, hand_detected, aim_x, aim_y, shoot, gesture_state, debug_info):
    """Renders HUD with thumb-aim reticle and index touch trigger indicators."""
    h, w, _ = frame.shape

    # 1. Top HUD Header Bar
    cv2.rectangle(frame, (0, 0), (w, 55), (20, 20, 25), -1)
    cv2.line(frame, (0, 55), (w, 55), (60, 60, 80), 1)

    # Title & FPS
    cv2.putText(frame, "IMXR THUMB-AIM", (12, 34), cv2.FONT_HERSHEY_DUPLEX, 0.70, (0, 230, 255), 2)
    cv2.putText(frame, f"FPS: {int(fps)}", (w - 95, 34), cv2.FONT_HERSHEY_SIMPLEX, 0.55, (200, 200, 200), 1)

    # Hand Status Badge
    if hand_detected:
        hand_text = "HAND: DETECTED"
        hand_color = (0, 255, 120)
    else:
        hand_text = "HAND: SEARCHING..."
        hand_color = (0, 140, 255)
    cv2.putText(frame, hand_text, (195, 34), cv2.FONT_HERSHEY_SIMPLEX, 0.50, hand_color, 2)

    # Gesture State Badge
    state_color = (180, 180, 180)
    display_state = f"GESTURE: {gesture_state}"
    if shoot or gesture_state == "TOUCH_FIRE":
        state_color = (0, 0, 255)
        display_state = "GESTURE: 💥 TOUCH FIRE!"
    elif gesture_state == "AIMING":
        state_color = (0, 240, 255)
        display_state = "GESTURE: THUMB AIM (READY)"
    elif gesture_state == "COOLDOWN":
        state_color = (0, 210, 255)
        display_state = "GESTURE: COOLDOWN"
    elif gesture_state == "RELEASE_TO_REARM":
        state_color = (180, 180, 180)
        display_state = "GESTURE: LIFT INDEX TO RE-ARM"

    cv2.putText(frame, display_state, (370, 34), cv2.FONT_HERSHEY_SIMPLEX, 0.50, state_color, 2)

    # 2. Detailed Metric Overlay (Left side floating badges)
    if hand_detected and debug_info:
        # Touch Distance Progress Bar
        touch_dist = debug_info.get("touch_distance", 0.5)
        is_touching = (touch_dist <= config.TOUCH_FIRE_THRESHOLD)
        p_txt = f"TOUCH DIST: {touch_dist:.2f}"
        p_col = (0, 0, 255) if is_touching else ((0, 255, 100) if touch_dist < config.TOUCH_RELEASE_THRESHOLD else (0, 230, 255))
        cv2.putText(frame, p_txt, (15, 85), cv2.FONT_HERSHEY_SIMPLEX, 0.50, p_col, 2)

        # Draw mini progress bar for touch threshold
        bar_x, bar_y, bar_w, bar_h = 15, 95, 130, 8
        cv2.rectangle(frame, (bar_x, bar_y), (bar_x + bar_w, bar_y + bar_h), (50, 50, 60), -1)
        fill_w = int(np.clip((1.0 - (touch_dist / 0.5)), 0.0, 1.0) * bar_w)
        cv2.rectangle(frame, (bar_x, bar_y), (bar_x + fill_w, bar_y + bar_h), p_col, -1)

        # Armed / Ready status
        is_armed = debug_info.get("is_armed", True)
        arm_txt = "TRIGGER: ARMED (TOUCH TO FIRE)" if is_armed else "TRIGGER: LIFT INDEX TO RE-ARM"
        arm_col = (0, 255, 120) if is_armed else (0, 180, 255)
        cv2.putText(frame, arm_txt, (15, 125), cv2.FONT_HERSHEY_SIMPLEX, 0.48, arm_col, 2)

        cd_rem = debug_info.get("cooldown_remaining", 0.0)
        if cd_rem > 0:
            cv2.putText(frame, f"COOLDOWN: {cd_rem:.2f}s", (15, 148), cv2.FONT_HERSHEY_SIMPLEX, 0.45, (0, 200, 255), 1)

    # 3. Dynamic Touch Connector Line between Thumb Tip and Index Tip
    if hand_detected and config.DRAW_TOUCH_CONNECTOR and debug_info:
        t_pos = debug_info.get("thumb_pos", (0.5, 0.5))
        i_pos = debug_info.get("index_pos", (0.5, 0.5))

        px_t = int(np.clip(t_pos[0], 0.0, 1.0) * w)
        py_t = int(np.clip(t_pos[1], 0.0, 1.0) * h)
        px_i = int(np.clip(i_pos[0], 0.0, 1.0) * w)
        py_i = int(np.clip(i_pos[1], 0.0, 1.0) * h)

        is_touching = (debug_info.get("touch_distance", 0.5) <= config.TOUCH_FIRE_THRESHOLD)
        line_color = (0, 0, 255) if is_touching else ((0, 255, 120) if debug_info.get("is_armed", True) else (150, 150, 150))
        cv2.line(frame, (px_t, py_t), (px_i, py_i), line_color, 3 if is_touching else 2)
        cv2.circle(frame, (px_i, py_i), 7, (0, 230, 255), -1)  # Index hammer marker

    # 4. Aiming Crosshair on Thumb Tip
    if hand_detected and aim_x is not None and aim_y is not None:
        px = int(np.clip(aim_x, 0.0, 1.0) * w)
        py = int(np.clip((1.0 - aim_y) if config.INVERT_Y else aim_y, 0.0, 1.0) * h)

        crosshair_color = (0, 255, 255)
        crosshair_radius = 22

        if shoot or gesture_state == "TOUCH_FIRE":
            crosshair_color = (0, 0, 255)
            crosshair_radius = 36
            cv2.circle(frame, (px, py), 42, (0, 140, 255), 3)
            cv2.putText(frame, "TOUCH BANG!", (px + 20, py - 20), cv2.FONT_HERSHEY_DUPLEX, 0.85, (0, 50, 255), 2)
        else:
            crosshair_color = (0, 255, 100)

        # Draw crosshair target on thumb tip
        cv2.circle(frame, (px, py), crosshair_radius, crosshair_color, 2)
        cv2.circle(frame, (px, py), 4, crosshair_color, -1)
        cv2.line(frame, (px - crosshair_radius - 8, py), (px + crosshair_radius + 8, py), crosshair_color, 1)
        cv2.line(frame, (px, py - crosshair_radius - 8), (px, py + crosshair_radius + 8), crosshair_color, 1)

        coord_str = f"THUMB ({aim_x:.2f}, {aim_y:.2f})"
        cv2.putText(frame, coord_str, (px + 15, py + 25), cv2.FONT_HERSHEY_PLAIN, 1.0, (255, 255, 255), 1)

    # 5. Bottom Guidance Bar
    cv2.rectangle(frame, (0, h - 35), (w, h), (15, 15, 20), -1)
    guide_text = "Thumb = Aim | Touch Thumb with Index Finger = Shoot | Lift Index = Re-arm | 'q'=Quit"
    cv2.putText(frame, guide_text, (15, h - 12), cv2.FONT_HERSHEY_SIMPLEX, 0.44, (180, 220, 255), 1)

def main():
    print("=" * 60)
    print("  IMXR: 3D BALLOON SHOOTER - THUMB AIM + INDEX TOUCH GUN")
    print(f"  Target UDP: {config.UDP_IP}:{config.UDP_PORT}")
    print("=" * 60)

    # 1. Initialize Webcam
    cap = cv2.VideoCapture(config.CAMERA_INDEX)
    cap.set(cv2.CAP_PROP_FRAME_WIDTH, config.FRAME_WIDTH)
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, config.FRAME_HEIGHT)
    cap.set(cv2.CAP_PROP_FPS, config.TARGET_FPS)

    if not cap.isOpened():
        print(f"[ERROR] Cannot open webcam index {config.CAMERA_INDEX}.")
        print("Please verify your webcam is connected or try CAMERA_INDEX = 1 in config.py.")
        sys.exit(1)

    print("[INFO] Webcam connected successfully.")
    print("[INFO] Initializing MediaPipe Hand Tracking...")

    tracker = HandTracker()
    gesture_detector = GestureDetector()
    sender = UnitySender(config.UDP_IP, config.UDP_PORT)

    print("[INFO] System ready! Streaming tracking data to Unity...")
    print("[INFO] Thumb Tip = Aim Crosshair. Touch Thumb with Index Finger = Shoot.")
    print("[INFO] Press 'q' inside the OpenCV window to exit.")

    prev_time = time.time()
    fps = 30.0
    show_debug = config.SHOW_DEBUG_WINDOW

    try:
        while True:
            ret, frame = cap.read()
            if not ret or frame is None:
                print("[WARNING] Empty frame received from webcam. Retrying...")
                time.sleep(0.01)
                continue

            curr_time = time.time()
            dt = curr_time - prev_time
            prev_time = curr_time
            if dt > 0:
                fps = 0.9 * fps + 0.1 * (1.0 / dt)

            # Process frame for hand landmarks
            frame, landmarks_list, raw_landmarks = tracker.process_frame(frame)

            hand_detected = (landmarks_list is not None and len(landmarks_list) >= 21)
            aim_x, aim_y = 0.5, 0.5
            shoot = False
            gesture_state = "NO_HAND"
            is_aiming = False
            debug_info = {}

            if hand_detected:
                aim_x, aim_y, _, _ = tracker.get_aim_coordinates(landmarks_list)
                shoot, gesture_state, is_aiming, debug_info = gesture_detector.detect_gesture(landmarks_list)
            else:
                tracker.reset_smoothing()

            # Transmit packet to Unity (100% compatible with HandTrackingReceiver.cs)
            sender.send_data(
                hand_detected=hand_detected,
                aim_x=aim_x,
                aim_y=aim_y,
                shoot=shoot,
                gesture_state=gesture_state,
                is_finger_gun=is_aiming,
                confidence=1.0 if hand_detected else 0.0
            )

            # Render visual debug HUD
            if show_debug:
                if config.DRAW_LANDMARKS and raw_landmarks:
                    tracker.draw_landmarks(frame, raw_landmarks)

                draw_hud(frame, fps, hand_detected, aim_x, aim_y, shoot, gesture_state, debug_info)
                cv2.imshow(config.WINDOW_TITLE, frame)

                key = cv2.waitKey(1) & 0xFF
                if key == ord('q') or key == 27:
                    print("[INFO] Exit signal received.")
                    break
                elif key == ord('d'):
                    config.DRAW_LANDMARKS = not config.DRAW_LANDMARKS
                    print(f"[INFO] Landmark drawing toggled: {config.DRAW_LANDMARKS}")

    except KeyboardInterrupt:
        print("[INFO] Interrupted by user.")
    finally:
        print("[INFO] Cleaning up resources...")
        cap.release()
        cv2.destroyAllWindows()
        sender.close()
        print("[INFO] Shutdown complete. Goodbye!")

if __name__ == "__main__":
    main()
