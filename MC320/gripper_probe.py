from pymycobot import MyCobot320Socket
import time

addr = input("Enter Pi IP: ").strip()
mc = MyCobot320Socket(addr, 9000)
time.sleep(1)


def probe(name, fn):
    try:
        print(f"  {name}: {fn()}")
    except Exception as e:
        print(f"  {name}: not available ({type(e).__name__}: {e})")


print("Connected:", mc.get_angles())

print("\n--- Force-controlled (pro) gripper reads ---")
probe("get_pro_gripper_angle()", lambda: mc.get_pro_gripper_angle())
probe("get_pro_gripper_status()", lambda: mc.get_pro_gripper_status())

print("\n--- Standard adaptive/parallel gripper reads ---")
probe("get_gripper_value()", lambda: mc.get_gripper_value())