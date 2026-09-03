import json
import time
from pymycobot import MyCobot320Socket

# ---------------- CONFIG ----------------
IP = "10.42.0.1"
SPEED = 30
SETTLE_HOVER = 5
SETTLE_GRAB = 4
STEP_MODE = False   # True = Enter before each move | False = fully autonomous
LOOPS = 1          # repeat the taught cycle N times
# ----------------------------------------

mc = MyCobot320Socket(IP, 9000)
time.sleep(1)

with open("task.json") as f:
    task = json.load(f)
pk, pl = task["pick"], task["place"]


def pause(msg):
    if STEP_MODE:
        input(f"\n>>> {msg}  (Enter)")
    else:
        print(f"-> {msg}")
        time.sleep(0.2)


def to_coords(pose, settle):
    mc.send_coords(pose["coords"], SPEED, 0)
    time.sleep(settle)
    print(f"   at {mc.get_coords()}")


def to_angles(pose, settle):
    mc.send_angles(pose["angles"], SPEED)
    time.sleep(settle)
    print(f"   at {mc.get_coords()}")


def grip_open():
    mc.set_pro_gripper_open()
    time.sleep(3)


def grip_close():
    mc.set_pro_gripper_close()
    time.sleep(3)
    print("   grip status:", mc.get_pro_gripper_status())


for i in range(LOOPS):
    print(f"\n===== cycle {i + 1}/{LOOPS} =====")
    grip_open()
    pause("pick hover");    to_coords(pk["hover"], SETTLE_HOVER)
    pause("pick descend");  to_angles(pk["grab"], SETTLE_GRAB)
    pause("close");         grip_close()
    pause("lift");          to_coords(pk["hover"], SETTLE_GRAB)
    pause("place hover");   to_coords(pl["hover"], SETTLE_HOVER)
    pause("place descend"); to_angles(pl["grab"], SETTLE_GRAB)
    pause("release");       grip_open()
    pause("retreat");       to_coords(pl["hover"], SETTLE_GRAB)

print("\nDone.")