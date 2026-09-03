from pymycobot import MyCobot320Socket
import time

addr=input("Enter ip: ")

mc = MyCobot320Socket(addr, 9000)
time.sleep(1)

print("Angles:", mc.get_angles())
print("Coords:", mc.get_coords())