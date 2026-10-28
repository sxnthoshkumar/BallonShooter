"""
UnitySender Module.
Sends tracking coordinates, gesture events, and system states to Unity via fast UDP JSON packets.
"""

import socket
import json
import time
import config

class UnitySender:
    def __init__(self, ip=config.UDP_IP, port=config.UDP_PORT):
        self.ip = ip
        self.port = port
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        # Set socket options for low latency
        self.sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        self.packet_count = 0
        self.last_send_time = 0.0

    def send_data(self, hand_detected, aim_x, aim_y, shoot, gesture_state, is_finger_gun, confidence=1.0):
        """
        Package tracking parameters into a lightweight JSON payload and transmit via UDP.
        """
        payload = {
            "handDetected": bool(hand_detected),
            "indexX": float(aim_x) if aim_x is not None else 0.5,
            "indexY": float(aim_y) if aim_y is not None else 0.5,
            "shoot": bool(shoot),
            "gestureState": str(gesture_state),
            "isFingerGun": bool(is_finger_gun),
            "confidence": round(float(confidence), 3),
            "timestamp": round(time.time(), 3),
            "seq": self.packet_count
        }

        try:
            message = json.dumps(payload).encode('utf-8')
            self.sock.sendto(message, (self.ip, self.port))
            self.packet_count += 1
            self.last_send_time = time.time()
            return True
        except Exception as e:
            if self.packet_count % 100 == 0:
                print(f"[UDP Warning] Failed to send packet: {e}")
            return False

    def close(self):
        """Close UDP socket gracefully."""
        try:
            self.sock.close()
        except:
            pass
