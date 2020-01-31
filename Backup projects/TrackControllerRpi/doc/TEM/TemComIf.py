#-----------------------------------------------------------------------------#
# (C) Copyright 2016, 3T BV
# Any reproduction without written permission is prohibited by law.
#-----------------------------------------------------------------------------#
#
# Project   : ASM966
# Subject   : Io Test module software
# File type : Python script.
#
#-----------------------------------------------------------------------------#
#
# Revision  : $Revision: 5622 $
# Date      : $Date: 2016-12-20 13:02:27 +0100 (Tue, 20 Dec 2016) $
#
#-----------------------------------------------------------------------------#

#-----------------------------------------------------------------------------#
#                                                                             #
#                              Python script                                  #
#                                                                             #
#-----------------------------------------------------------------------------#
#
# Author      : W. Klaren
# Version     : 1.0
# Description : I/O module communication interface class.
#
# History
# 2016-12-12 : Creation
# 2017-01-19 : ASML MWAA: let temperature and voltage functions return float
#-----------------------------------------------------------------------------#

#-----------------------------------------------------------------------------#
#                         Documentation String                                #
#-----------------------------------------------------------------------------#

#-----------------------------------------------------------------------------#
#                         System and ASML imports                             #
#-----------------------------------------------------------------------------#
# import system stuff...
import sys
from collections import namedtuple

# import other stuff
try:
    import serial
    import modbus_tk.modbus_rtu
    import modbus_tk.defines
except:
    print('')
    print('Importing serial library failed')
    print('Serial connection not possible')
    print('')
    sys.exit(-1)

#-----------------------------------------------------------------------------#
#                             Global Variables                                #
#-----------------------------------------------------------------------------#

#-----------------------------------------------------------------------------#
#                            Class Definitions                                #
#-----------------------------------------------------------------------------#

ModuleInfo = namedtuple('ModuleInfo', 'address type version')
ModuleStatistics = namedtuple('ModuleStatistics', 'onTime moduleReceived totalReceived errorCount triggerCount')

class TemComIf(object):
    # MODBUS register address of the device type.
    REG_MODULE_TYPE                 = 0x0000
    # MODBUS register address of major version number.
    REG_MAJOR_VERSION               = 0x0001
    # MODBUS register address of major version number.
    REG_MINOR_VERSION               = 0x0002
    # MODBUS register address of major version number.
    REG_BUILD_NUMBER                = 0x0003
    # MODBUS register address of software 8NC.
    REG_SOFTWARE_8NC                = 0x0004
    # MODBUS register address of hardware 8NC.
    REG_HARDWARE_8NC                = 0x0006
    # MODBUS register address of temperature in 0.01 deg Celcius.
    REG_TEMPERATURE_C               = 0x0008
    # MODBUS register address of supply voltage in 0.01 Volt.
    REG_24VOLT_POWER_V              = 0x0009
    # MODBUS register address of the power on time.
    REG_POWER_ON_TIME               = 0x000A
    # MODBUS register address of the total number of received messages for this module.
    REG_MODULE_MESSAGE_COUNTER      = 0x000C
    # MODBUS register address of the total number of received messages.
    REG_TOTAL_MESSAGE_COUNTER       = 0x000E
    # MODBUS register address of the total number of received messages with errors.
    REG_ERRONEOUS_MESSAGE_COUNTER   = 0x0010
    # MODBUS register address of the total number of recieved triggers.
    REG_TOTAL_TRIGGER_COUNTER       = 0x0012
    # MODBUS register address, write this register to send a trigger.
    REG_SEND_TRIGGER                = 0x0014
    # MODBUS register address to control which counter to reset
    REG_RESET_COUNTER_CONTROL       = 0x0015
    # MODBUS register address, write to this register to reset selected counter.
    REG_RESET_COUNTER               = 0x0016
    # MODBUS register address of command.
    REG_COMMAND                     = 0x0017

    # MODBUS register address of trigger control.
    REG_TRIGGER_CONTROL             = 0x0020
    # MODBUS register address of trigger timeout channel 1.
    REG_TRIGGER_TIMEOUT_CH1         = 0x0021
    # MODBUS register address of trigger delay channel 1.
    REG_TRIGGER_DELAY_CH1           = 0x0022
    # MODBUS register address of trigger timeout channel 2.
    REG_TRIGGER_TIMEOUT_CH2         = 0x0023
    # MODBUS register address of trigger delay channel 2.
    REG_TRIGGER_DELAY_CH2           = 0x0024
    # MODBUS register address of the number of triggers that timed out.
    REG_TRIGGER_TIMEOUT_COUNTER     = 0x0025

    #/* Digital output module register numbers
    # *
    # */
    # MODBUS register address of Digital Output Module control. */
    REG_DO_CONTROL                  = 0x0040
    # MODBUS register address of Current output value of channel 1 */
    REG_DO_CURRENT_OUTPUT_VALUE_CH1 = 0x0041
    # MODBUS register address of Current output value of channel 2 */
    REG_DO_CURRENT_OUTPUT_VALUE_CH2 = 0x0042
    # MODBUS register address of Current output value of channel 3 */
    REG_DO_CURRENT_OUTPUT_VALUE_CH3 = 0x0043
    # MODBUS register address of Current output value of channel 4 */
    REG_DO_CURRENT_OUTPUT_VALUE_CH4 = 0x0044
    # MODBUS register address of Current output value of channel 5 */
    REG_DO_CURRENT_OUTPUT_VALUE_CH5 = 0x0045
    # MODBUS register address of Current output value of channel 6 */
    REG_DO_CURRENT_OUTPUT_VALUE_CH6 = 0x0046
    # MODBUS register address of Current output value of channel 7 */
    REG_DO_CURRENT_OUTPUT_VALUE_CH7 = 0x0047
    # MODBUS register address of Current output value of channel 8 */
    REG_DO_CURRENT_OUTPUT_VALUE_CH8 = 0x0048
    # MODBUS register address of Preload output value of channel 1 */
    REG_DO_PRELOAD_OUTPUT_VALUE_CH1 = 0x0049
    # MODBUS register address of Preload output value of channel 2 */
    REG_DO_PRELOAD_OUTPUT_VALUE_CH2 = 0x004A
    # MODBUS register address of Preload output value of channel 3 */
    REG_DO_PRELOAD_OUTPUT_VALUE_CH3 = 0x004B
    # MODBUS register address of Preload output value of channel 4 */
    REG_DO_PRELOAD_OUTPUT_VALUE_CH4 = 0x004C
    # MODBUS register address of Preload output value of channel 5 */
    REG_DO_PRELOAD_OUTPUT_VALUE_CH5 = 0x004D
    # MODBUS register address of Preload output value of channel 6 */
    REG_DO_PRELOAD_OUTPUT_VALUE_CH6 = 0x004E
    # MODBUS register address of Preload output value of channel 7 */
    REG_DO_PRELOAD_OUTPUT_VALUE_CH7 = 0x004F
    # MODBUS register address of Preload output value of channel 8 */
    REG_DO_PRELOAD_OUTPUT_VALUE_CH8 = 0x0050

    #/* Digital input module register numbers
    # *
    # */
    # MODBUS register address of Current value of input A of channel 1-8. */
    REG_DI_CURRENT_VALUE_INPUT_A    = 0x0060
    # MODBUS register address of Latched value of input A of channel 1-8. */
    REG_DI_LATCHED_VALUE_INPUT_A    = 0x0061
    # MODBUS register address of Current value of input B of channel 1-8. */
    REG_DI_CURRENT_VALUE_INPUT_B    = 0x0062
    # MODBUS register address of Latched value of input B of channel 1-8. */
    REG_DI_LATCHED_VALUE_INPUT_B    = 0x0063

    # Broadcast address.
    BROADCAST_ADDRESS               = 0
    # First module address.
    FIRST_MODULE_ADDRESS            = 1
    # Last module address.
    LAST_MODULE_ADDRESS             = 254

    # Maximum trigger time value (applies to both delay and timeout).
    TRIGGER_TIME_MAX                = 99.0

    def __init__(self, port):
        self.master = None
        self.interface = None

        # Open serial port.
        self.interface = serial.Serial(port=port, baudrate=500000, bytesize=8, parity='N', stopbits=1, xonxoff=0)
        # Initialize MODBUS master.
        self.master = modbus_tk.modbus_rtu.RtuMaster(self.interface)
        self.master.set_timeout(0.2)
        self.master.open()

    def __del__ (self):
        # Close MODBUS master.
        if (self.master != None):
            self.master.close()
            del self.master
            self.master = None

        # Close serial port.
        if (self.interface != None):
            self.interface.close()
            del self.interface
            self.interface = None

    #
    # Low level (register level) access functions.
    #

    # Read one or more registers from an I/O module.
    def ReadRegisters(self, slaveAddress, startRegister, registerCount):
        return self.master.execute(slaveAddress, modbus_tk.defines.READ_HOLDING_REGISTERS, startRegister, registerCount)

    # Write one or more registers to an I/O module.
    def WriteRegisters(self, slaveAddress, startRegister, registerCount, data):
        self.master.execute(slaveAddress, modbus_tk.defines.WRITE_MULTIPLE_REGISTERS, startRegister, registerCount, data)

    #
    # Intermediate level access functions.
    #

    # Read a 16-bit value from an I/O module.
    def ReadWord(self, slaveAddress, register):
        registerData = self.ReadRegisters(slaveAddress, register, 1)
        return registerData[0]

    # Read a 32-bit value from an I/O module.
    def ReadLongWord(self, slaveAddress, register):
        registerData = self.ReadRegisters(slaveAddress, register, 2)
        return (registerData[0] * 65536) + registerData[1]

    # Write a 16-bit value to an I/O module.
    def WriteWord(self, slaveAddress, register, outputValue):
        registerData = outputValue,
        self.WriteRegisters(slaveAddress, register, 1, registerData)

    # Write a 32-bit value to an I/O module.
    def WriteLongWord(self, slaveAddress, register, outputValue):
        registerData = ((outputValue / 65536) & 0xFFFF), (outputValue & 0xFFFF)
        self.WriteRegisters(slaveAddress, register, 2, registerData)

    #
    # High level access functions.
    #

    # Scan the bus for I/O modules.
    def ScanBus(self):
        savedTimeout = self.master.get_timeout()
        modules = []
        try:
            self.master.set_timeout(0.1)
            for address in range(self.FIRST_MODULE_ADDRESS, self.LAST_MODULE_ADDRESS):
                try:
                    hardwareType = self.GetHardwareType(address)
                    version = self.GetModuleVersion(address)
                    modules.append(ModuleInfo(address = address, type = hardwareType, version = version))
                except modbus_tk.modbus.ModbusError:
                    # Slave returned an error, probably not an I/O module, ignore it.
                    pass
                except modbus_tk.modbus.ModbusInvalidResponseError:
                    # Invalid response/timeout, ignore I/O module.
                    pass
            return tuple(modules)
        except Exception:
            raise
        finally:
            self.master.set_timeout(savedTimeout)

    # Get the hardware type of an I/O module.
    def GetHardwareType(self, slaveAddress):
        return self.ReadWord(slaveAddress, self.REG_MODULE_TYPE)

    # Get the software version of an I/O module.
    def GetModuleVersion(self, slaveAddress):
        majorVersion = self.ReadWord(slaveAddress, self.REG_MAJOR_VERSION)
        minorVersion = self.ReadWord(slaveAddress, self.REG_MINOR_VERSION)
        buildNumber = self.ReadWord(slaveAddress, self.REG_BUILD_NUMBER)
        return str(majorVersion) + '.' + str(minorVersion) + '.' + str(buildNumber)

    # Get the temperature of an I/O module.
    def GetTemperature(self, slaveAddress):
        return float (self.ReadWord(slaveAddress, self.REG_TEMPERATURE_C)) / 100.0

    # Get the supply voltage of an I/O module.
    def GetVoltage(self, slaveAddress):
        return float (self.ReadWord(slaveAddress, self.REG_24VOLT_POWER_V)) / 100.0

    # Get power in time and message counters of an I/O module.
    def GetStatistics(self, slaveAddress):
        onTime = self.ReadLongWord(slaveAddress, self.REG_POWER_ON_TIME)
        moduleReceived = self.ReadLongWord(slaveAddress, self.REG_MODULE_MESSAGE_COUNTER)
        totalReceived = self.ReadLongWord(slaveAddress, self.REG_TOTAL_MESSAGE_COUNTER)
        errorCount = self.ReadLongWord(slaveAddress, self.REG_ERRONEOUS_MESSAGE_COUNTER)
        triggerCount = self.ReadLongWord(slaveAddress, self.REG_TOTAL_TRIGGER_COUNTER)
        return ModuleStatistics(onTime = onTime, moduleReceived = moduleReceived, totalReceived = totalReceived, errorCount = errorCount, triggerCount = triggerCount)

    #
    # Configure a trigger input. Does not arm the trigger.
    #
    def ConfigureTrigger(self,address, trigger, level, delay, timeout):
        if (level < 0) or (level > 1):
            raise ValueError('Invalid trigger level ' + str(level))
        if (delay < 0) or (delay > self.TRIGGER_TIME_MAX):
            raise ValueError('Invalid trigger delay ' + str(delay))
        if (timeout < 0) or (timeout > self.TRIGGER_TIME_MAX):
            raise ValueError('Invalid trigger timeout ' + str(timeout))

        currentTriggerControl = self.ReadWord(address, self.REG_TRIGGER_CONTROL)
        delayValue = int(delay * 100.0)
        timeoutValue = int(timeout * 100.0)

        if (trigger == 0):
            currentTriggerControl &= 0xFFF8
            currentTriggerControl |= 4 if (level != 0) else 0

            self.WriteWord(address, self.REG_TRIGGER_TIMEOUT_CH1, timeoutValue)
            self.WriteWord(address, self.REG_TRIGGER_DELAY_CH1, delayValue)
        elif (trigger == 1):
            currentTriggerControl &= 0xFFF4
            currentTriggerControl |= 8 if (level != 0) else 0

            self.WriteWord(address, self.REG_TRIGGER_TIMEOUT_CH2, timeoutValue)
            self.WriteWord(address, self.REG_TRIGGER_DELAY_CH2, delayValue)
        else:
            raise ValueError('Invalid trigger number ' + str(trigger))

        self.WriteWord(address, self.REG_TRIGGER_CONTROL, currentTriggerControl)

    #
    # Arm one or both trigger inputs of the TEMSY.
    #
    def ArmTrigger(self,address, armTrigger1, armTrigger2):
        if armTrigger1 or armTrigger2:
            triggerTimeout = 0.0
            if armTrigger1:
                timeoutValue = self.ReadWord(address, self.REG_TRIGGER_TIMEOUT_CH1) / 100.0
                triggerTimeout = max(triggerTimeout, timeoutValue)
            if armTrigger2:
                timeoutValue = self.ReadWord(address, self.REG_TRIGGER_TIMEOUT_CH2) / 100.0
                triggerTimeout = max(triggerTimeout, timeoutValue)

            savedTimeout = self.master.get_timeout()
            currentTriggerControl = self.ReadWord(address, self.REG_TRIGGER_CONTROL)

            try:
                # Set the communication timeout to the maximum trigger timeout plus two seconds.
                self.master.set_timeout(triggerTimeout + 2.0)

                # Arm the triggers.
                currentTriggerControl &= 0xFFFC
                currentTriggerControl |= 1 if (armTrigger1) else 0
                currentTriggerControl |= 2 if (armTrigger2) else 0
                self.WriteWord(address, self.REG_TRIGGER_CONTROL, currentTriggerControl)
            except Exception:
                raise
            finally:
                self.master.set_timeout(savedTimeout)
        else:
            # Both triggers should not be armed, return immediately.
            pass

#-----------------------------------------------------------------------------#
#                            Support Functions                                #
#-----------------------------------------------------------------------------#

#-----------------------------------------------------------------------------#
#                            Main Starts Here                                 #
#-----------------------------------------------------------------------------#

# No main.

#----------------------------------- End of file -----------------------------#
