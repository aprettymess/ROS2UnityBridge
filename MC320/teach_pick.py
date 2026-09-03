from pymycobot import MyCobot320Socket
import time

addr = input("Enter Pi IP: ").strip()
mc = MyCobot320Socket(addr, 9000)
time.sleep(1)

check = mc.get_angles()
if not check or len(check) != 6:
    print(f"Read problem: {check}")
    raise SystemExit

print("Connected. angles:", check)
input("\nHold the arm firmly, then press Enter to RELEASE for teaching...")
mc.release_all_servos()
mc.set_color(0, 255, 0)
print("Released (damping). Move by hand. Keep a hand on it.")

input("\nPosition the OPEN gripper around the can at the CORRECT grab height, then press Enter...")
while True:
    a = mc.get_angles(); c = mc.get_coords()
    if a and len(a) == 6 and c and len(c) == 6:
        break
    print("  read failed, hold still..."); time.sleep(0.5)

input("\nHold the arm, then press Enter to RE-LOCK servos...")
mc.set_color(0, 0, 255)
try:
    mc.focus_all_servos()
except Exception as e:
    print("focus failed, power_on:", e); mc.power_on()
time.sleep(1)
print("Locked. is_all_servo_enable =", mc.is_all_servo_enable())

print("\n=========== COPY THIS ===========")
print(f"PICK_ANGLES = {a}")
print(f"PICK_COORDS = {c}")
print("=================================")