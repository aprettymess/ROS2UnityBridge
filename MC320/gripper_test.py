from pymycobot import MyCobot320Socket
import time

addr = input("Enter Pi IP: ").strip()
mc = MyCobot320Socket(addr, 9000)
time.sleep(1)


def report(tag):
    print(f"[{tag}] angle={mc.get_pro_gripper_angle()}  status={mc.get_pro_gripper_status()}")


report("start")

print("\nOpening...")
print("  ret:", mc.set_pro_gripper_open())
time.sleep(3)
report("after open")

print("\nClosing...")
print("  ret:", mc.set_pro_gripper_close())
time.sleep(3)
report("after close")

print("\nOpening again (leaving it open, ready for the pick)...")
print("  ret:", mc.set_pro_gripper_open())
time.sleep(3)
report("final")