"""
Simulated Test Sender for Unity.
Generates automated circular/figure-8 aiming movements and periodic test shots.
Use this script to test and verify the Unity game receiver without needing a webcam!
"""

import math
import time
import json
import socket
import config

def run_test_simulation():
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    print("=" * 60)
    print(f"  RUNNING SIMULATED UDP SENDER -> {config.UDP_IP}:{config.UDP_PORT}")
    print("  Testing Unity Receiver & Gameplay without webcam...")
    print("  Press Ctrl+C to stop.")
    print("=" * 60)

    start_time = time.time()
    last_shot_time = start_time

    try:
        while True:
            t = time.time() - start_time
            # Figure-8 or smooth Lissajous aim path centered at (0.5, 0.5)
            aim_x = round(0.5 + 0.35 * math.sin(t * 1.5), 4)
            aim_y = round(0.5 + 0.25 * math.cos(t * 3.0), 4)

            # Fire a test shot every 2.0 seconds
            shoot = False
            gesture_state = "READY_TO_SHOOT"
            if (time.time() - last_shot_time) > 2.0:
                shoot = True
                last_shot_time = time.time()
                gesture_state = "FIRED"
                print(f"[TEST SHOT FIRED] at ({aim_x}, {aim_y})")

            payload = {
                "handDetected": True,
                "indexX": aim_x,
                "indexY": aim_y,
                "shoot": shoot,
                "gestureState": gesture_state,
                "isFingerGun": True,
                "confidence": 1.0,
                "timestamp": round(time.time(), 3),
                "seq": int(t * 60)
            }

            message = json.dumps(payload).encode('utf-8')
            sock.sendto(message, (config.UDP_IP, config.UDP_PORT))

            time.sleep(0.016)  # ~60 FPS
    except KeyboardInterrupt:
        print("\n[INFO] Test simulation stopped.")
    finally:
        sock.close()

if __name__ == "__main__":
    run_test_simulation()
