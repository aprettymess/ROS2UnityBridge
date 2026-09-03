from pymycobot import MyCobot320Socket
import time


def read_pose(mc, label):
    while True:
        angles = mc.get_angles()
        coords = mc.get_coords()
        if angles and len(angles) == 6 and coords and len(coords) == 6:
            print(f"\n[{label}]")
            print(f"  angles = {angles}")
            print(f"  coords = {coords}")
            return angles, coords
        print(f"  read failed (angles={angles}, coords={coords}) - hold still, retrying...")
        time.sleep(0.5)


def reenable(mc):
    try:
        mc.focus_all_servos()
    except Exception as e:
        print(f"focus_all_servos failed ({e}), trying power_on()...")
        mc.power_on()
    time.sleep(1)


def main():
    addr = input("Enter Pi IP: ").strip()
    mc = MyCobot320Socket(addr, 9000)
    time.sleep(1)

    check = mc.get_angles()
    if not check or len(check) != 6:
        print(f"Connection/read problem, get_angles() returned: {check}")
        return
    print(f"Connected. Current angles: {check}")

    input("\nHold the arm firmly, then press Enter to RELEASE servos for hand teaching...")
    mc.release_all_servos()
    mc.set_color(0, 255, 0)
    print("Servos released (damping mode). Move the arm by hand. Keep a hand on it at all times.")

    input("\nPosition the OPEN gripper around the can at the PICK spot, then press Enter...")
    pick_angles, pick_coords = read_pose(mc, "PICK")

    input("\nPosition the gripper where the can drops into the box (PLACE), then press Enter...")
    place_angles, place_coords = read_pose(mc, "PLACE")

    input("\nHold the arm firmly, then press Enter to RE-ENABLE servos...")
    mc.set_color(0, 0, 255)
    reenable(mc)
    print(f"Servos re-enabled (is_all_servo_enable = {mc.is_all_servo_enable()}).")

    print("\n================ COPY THIS ================")
    print(f"PICK_ANGLES  = {pick_angles}")
    print(f"PICK_COORDS  = {pick_coords}")
    print(f"PLACE_ANGLES = {place_angles}")
    print(f"PLACE_COORDS = {place_coords}")
    print("==========================================")


if __name__ == "__main__":
    main()