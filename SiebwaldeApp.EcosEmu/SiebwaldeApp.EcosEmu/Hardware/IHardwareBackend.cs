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

        /// <summary>
        /// Sets the speed and direction of a locomotive identified by its address.
        /// </summary>
        /// <remarks>Ensure that the address corresponds to a valid locomotive in the system. The speed
        /// value must be within the acceptable range for the system,  and the direction must be a valid value (e.g., 0
        /// or 1). Invalid inputs may result in undefined behavior.</remarks>
        /// <param name="address">The unique address of the locomotive to control.</param>
        /// <param name="ecosSpeed">The speed value to set, typically ranging from 0 (stop) to a maximum value defined by the system.</param>
        /// <param name="direction">The direction of the locomotive, where 0 typically represents forward and 1 represents reverse.</param>
        void SetLocoSpeed(int address, int ecosSpeed, int direction);

        /// <summary>
        /// Sets the state of a specific output on a decoder.
        /// </summary>
        /// <remarks>This method allows controlling individual outputs on a decoder by specifying the
        /// decoder's address and the output index. Ensure that the decoder address and output index are valid for the
        /// system configuration to avoid unexpected behavior.</remarks>
        /// <param name="decoderAddress">The address of the decoder to control. Must be a valid, non-negative integer.</param>
        /// <param name="outputIndex">The index of the output to set. Must be within the valid range of outputs for the specified decoder.</param>
        /// <param name="on">A value indicating whether the output should be turned on (<see langword="true"/>) or off (<see
        /// langword="false"/>).</param>
        void SetSwitch(int decoderAddress, int outputIndex, bool on);
    }
}
