from pymycobot import MyCobot320Socket
import time

# ---------------- CONFIG ----------------
IP = "10.42.0.1"
SPEED = 30
HOVER_MM = 80

PICK = [41.9, 238.5, 241.3, 179.36, 3.16, 64.87]
PLACE = [-287.0, -8.4, 223.9, -179.75, 0.4, 126.38]

MODE_JOINT = 0
MODE_LINEAR = 1
# ----------------------------------------

PICK_HOVER = PICK.copy();  PICK_HOVER[2] += HOVER_MM
PLACE_HOVER = PLACE.copy(); PLACE_HOVER[2] += HOVER_MM

mc = MyCobot320Socket(IP, 9000)
time.sleep(1)


def pause(msg):
    input(f"\n>>> next: {msg}  (press Enter)")


def move(label, coords, mode, settle=6):
    print(f"[{label}] -> {coords}")
    mc.send_coords(coords, SPEED, mode)
    time.sleep(settle)
    print(f"   landed: {mc.get_coords()}")


def grip_open():
    print("   gripper OPEN ->", mc.set_pro_gripper_open()); time.sleep(3)


def grip_close():
    print("   gripper CLOSE ->", mc.set_pro_gripper_close()); time.sleep(3)


print("Start:", mc.get_coords())
grip_open()

pause("swing over to HOVER above the can")
move("hover-pick", PICK_HOVER, MODE_JOINT)

pause("drop straight DOWN onto the can")
move("pick", PICK, MODE_LINEAR, settle=4)

pause("CLOSE gripper on the can")
grip_close()

pause("lift the can straight UP")
move("lift", PICK_HOVER, MODE_LINEAR, settle=4)

pause("carry across to HOVER above the box")
move("hover-place", PLACE_HOVER, MODE_JOINT)

pause("lower into the box")
move("place", PLACE, MODE_LINEAR, settle=4)

pause("OPEN gripper to release")
grip_open()

pause("lift straight UP out of the box")
move("retreat", PLACE_HOVER, MODE_LINEAR, settle=4)

print("\nDone. Arm hovering above the box, gripper open.")