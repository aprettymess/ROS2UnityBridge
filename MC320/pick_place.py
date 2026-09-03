from pymycobot import MyCobot320Socket
import time

# ---------------- CONFIG ----------------
IP = "10.42.0.1"
SPEED = 30
HOVER_MM = 80
STEP_MODE = True   # True = Enter before each move | False = one-shot

# ----- PICK (tune PICK_Z per object via zfinder) -----
PICK_X, PICK_Y = 41.9, 238.5
PICK_RX, PICK_RY, PICK_RZ = 179.36, 3.16, 64.87
PICK_Z = 285

# ----- PLACE (raise PLACE_Z for taller objects) -----
PLACE_X, PLACE_Y = -287.0, -8.4
PLACE_RX, PLACE_RY, PLACE_RZ = -179.75, 0.4, 126.38
PLACE_Z = 275
# ----------------------------------------

PICK = [PICK_X, PICK_Y, PICK_Z, PICK_RX, PICK_RY, PICK_RZ]
PLACE = [PLACE_X, PLACE_Y, PLACE_Z, PLACE_RX, PLACE_RY, PLACE_RZ]
PICK_HOVER = PICK.copy();  PICK_HOVER[2] += HOVER_MM
PLACE_HOVER = PLACE.copy(); PLACE_HOVER[2] += HOVER_MM

mc = MyCobot320Socket(IP, 9000)
time.sleep(1)


def pause(msg):
    if STEP_MODE:
        input(f"\n>>> next: {msg}  (press Enter)")
    else:
        print(f"\n-> {msg}"); time.sleep(0.3)


def move(label, coords, settle=6):
    print(f"[{label}] -> {coords}")
    mc.send_coords(coords, SPEED, 0)
    time.sleep(settle)
    print(f"   landed: {mc.get_coords()}")


def grip_open():
    print("   OPEN ->", mc.set_pro_gripper_open()); time.sleep(3)


def grip_close():
    print("   CLOSE ->", mc.set_pro_gripper_close()); time.sleep(3)
    print("   grip status:", mc.get_pro_gripper_status())


print("Start:", mc.get_coords())
grip_open()

pause("hover above the object")
move("hover-pick", PICK_HOVER)

pause("descend to grab height")
move("pick", PICK, settle=4)

pause("close on the object")
grip_close()

pause("lift to hover")
move("lift", PICK_HOVER, settle=4)

pause("carry to hover above the box")
move("hover-place", PLACE_HOVER)

pause("lower into the box")
move("place", PLACE, settle=4)

pause("open to release")
grip_open()

pause("lift out of the box")
move("retreat", PLACE_HOVER, settle=4)

print("\nDone.")