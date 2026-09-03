import json
import time
from pymycobot import MyCobot320Socket

# ---------------- CONFIG ----------------
IP = "10.42.0.1"
JOG_SPEED = 25
SETTLE = 2.5
STEP_DEFAULT = 5
MIN_Z = 120
# ----------------------------------------

mc = MyCobot320Socket(IP, 9000)
time.sleep(1)


def read6(fn):
    for _ in range(12):
        v = fn()
        if isinstance(v, (list, tuple)) and len(v) == 6:
            return list(v)
        time.sleep(0.4)
    return None


def relock():
    mc.set_color(0, 0, 255)
    try:
        mc.focus_all_servos()
    except Exception as e:
        print("focus failed, power_on:", e)
        mc.power_on()
    time.sleep(1.5)


def capture(label):
    coords = read6(mc.get_coords)
    angles = read6(mc.get_angles)
    if not coords or not angles:
        print(f"!! {label} read failed (coords={coords}, angles={angles}). Hold still.")
        input("   Press Enter to retry the capture...")
        coords = read6(mc.get_coords)
        angles = read6(mc.get_angles)
    if not coords or not angles:
        print(f"!! {label} capture failed twice. Aborting so nothing is saved wrong.")
        raise SystemExit
    return {"coords": coords, "angles": angles}


def teach_hover(label):
    input(f"\nHold the arm, press Enter to RELEASE and hand-teach the {label} HOVER...")
    mc.release_all_servos()
    mc.set_color(0, 255, 0)
    print(f"GREEN: move the OPEN gripper by hand to hover above the {label} spot, pointing down. Keep a hand on it.")
    input("Press Enter to capture the hover...")
    pose = capture(f"{label} hover")
    input("Hold the arm, press Enter to RE-LOCK...")
    relock()
    print(f"{label} HOVER captured: {pose['coords']}")
    return pose


def jog_descend(label, hover_coords):
    x, y, z, rx, ry, rz = hover_coords
    step = STEP_DEFAULT
    print(f"\n--- Fine-tune {label} grab height ---")

    def go():
        mc.send_coords([x, y, z, rx, ry, rz], JOG_SPEED, 0)
        time.sleep(SETTLE)
        print(f"   z={z} step={step}  landed={mc.get_coords()}  grip={mc.get_pro_gripper_status()}")

    go()
    while True:
        print("   keys:  [Enter]=DOWN  u=UP  4/6=X-/X+  8/2=Y+/Y-  +/-=step size  q=LOCK")
        cmd = input(f"[{label}] z={z} step={step} > ").strip().lower()
        if cmd == "q":
            break
        elif cmd in ("", "d"):
            new_z = z - step
            if new_z < MIN_Z:
                print(f"   floor {MIN_Z} reached, not going lower")
                continue
            z = new_z
        elif cmd == "u":
            z += step
        elif cmd == "4":
            x -= step
        elif cmd == "6":
            x += step
        elif cmd == "8":
            y += step
        elif cmd == "2":
            y -= step
        elif cmd == "+":
            step = min(20, step + 1)
            print(f"   step -> {step}")
            continue
        elif cmd == "-":
            step = max(1, step - 1)
            print(f"   step -> {step}")
            continue
        else:
            print("   ?")
            continue
        go()

    pose = capture(f"{label} grab")
    print(f"{label} GRAB locked: {pose['coords']}")
    return pose


# ================= TEACH SEQUENCE =================
mc.set_pro_gripper_open()
time.sleep(2)

pick_hover = teach_hover("PICK")
mc.set_pro_gripper_open()
time.sleep(2)
pick_grab = jog_descend("PICK", pick_hover["coords"])

input("\nPress Enter to CLOSE the gripper on the object...")
mc.set_pro_gripper_close()
time.sleep(3)
print("grip status:", mc.get_pro_gripper_status())

print("\nLifting back to pick hover with the object...")
mc.send_coords(pick_hover["coords"], JOG_SPEED, 0)
time.sleep(4)

place_hover = teach_hover("PLACE")
place_grab = jog_descend("PLACE", place_hover["coords"])

input("\nPress Enter to OPEN the gripper and release...")
mc.set_pro_gripper_open()
time.sleep(3)
print("Ascending to place hover...")
mc.send_coords(place_hover["coords"], JOG_SPEED, 0)
time.sleep(4)

task = {
    "pick": {"hover": pick_hover, "grab": pick_grab},
    "place": {"hover": place_hover, "grab": place_grab},
}
with open("task.json", "w") as f:
    json.dump(task, f, indent=2)

print("\nSaved task.json:")
print(json.dumps(task, indent=2))