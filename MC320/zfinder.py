from pymycobot import MyCobot320Socket
import time

IP = "10.42.0.1"
SPEED = 25
STEP = 5

# x, y, rx, ry, rz taken from your taught pick (reused). Only Z gets tuned.
X, Y, RX, RY, RZ = 41.9, 238.5, 179.36, 3.16, 64.87
START_Z = 300

mc = MyCobot320Socket(IP, 9000)
time.sleep(1)
mc.set_pro_gripper_open()
time.sleep(2)

z = START_Z


def go(z):
    mc.send_coords([X, Y, z, RX, RY, RZ], SPEED, 0)
    time.sleep(3)
    print(f"   commanded z={z}  readback={mc.get_coords()}  gripstatus={mc.get_pro_gripper_status()}")


print("Moving to start height above the object...")
go(z)
print("\nJog to the right grab height (watch the open jaws around the object):")
print("  [Enter]=down 5mm   u=up 5mm   q=done")

while True:
    cmd = input(f"z={z} > ").strip().lower()
    if cmd == "q":
        break
    z += STEP if cmd == "u" else -STEP
    go(z)

print(f"\n=== grab height for this object: PICK_Z = {z} ===")