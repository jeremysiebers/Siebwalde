using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiebwaldeApp.EcosEmu
{
    public class DummyHardwareBackend : IHardwareBackend
    {
        public void SetPower(bool on)
        {
            Console.WriteLine($"[HW] Power {(on ? "ON" : "OFF")}");
        }

        public void SetLocoSpeed(int address, int ecosSpeed, int direction)
        {
            Console.WriteLine($"[HW] Loc address={address}, speed={ecosSpeed}, dir={direction}");
        }
    }
}
