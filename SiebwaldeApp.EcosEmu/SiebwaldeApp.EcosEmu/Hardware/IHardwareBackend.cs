using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiebwaldeApp.EcosEmu
{
    public interface IHardwareBackend
    {
        void SetPower(bool on);

        /// <param name="address">“Decoderadres” van de loc (of jouw eigen ID)</param>
        /// <param name="ecosSpeed">0..127</param>
        /// <param name="direction">0/1 of -1/+1, wat jij wil</param>
        void SetLocoSpeed(int address, int ecosSpeed, int direction);
    }
}
