#-----------------------------------------------------------------------------#
#                                                                             #
#                              Python script                                  #
#                                                                             #
#-----------------------------------------------------------------------------#
#
# Author      : J.Siebers
# Version     : 1.0
# Description : I/O module communication interface class.
#
# History
# 2019-03-13 : Creation
#-----------------------------------------------------------------------------#

#-----------------------------------------------------------------------------#
#                         Documentation String                                #
#-----------------------------------------------------------------------------#

#-----------------------------------------------------------------------------#
#                         System and imports                             #
#-----------------------------------------------------------------------------#
# import system stuff...
import sys
from collections import namedtuple

sys.path.append('./modbusje')

# import other stuff
#try:
import serial
import modbus_rtu
import defines
'''
except:
    print('')
    print('Importing serial library failed')
    print('Serial connection not possible')
    print('')
    sys.exit(-1)
'''    


#-----------------------------------------------------------------------------#
#                             Global Variables                                #
#-----------------------------------------------------------------------------#

#-----------------------------------------------------------------------------#
#                            Class Definitions                                #
#-----------------------------------------------------------------------------#

ModuleInfo = namedtuple('ModuleInfo', 'address type version')
ModuleStatistics = namedtuple('ModuleStatistics', 'onTime moduleReceived totalReceived errorCount triggerCount')

class TemComIf(object):
    # MODBUS register addresses of the registers.
    HOLDING_REG_0                 = 0x0000
    HOLDING_REG_1                 = 0x0001
    HOLDING_REG_2                 = 0x0002
    HOLDING_REG_3                 = 0x0003
    
    INPUT_REG_0                   = 0x0000
    INPUT_REG_1                   = 0x0001
    INPUT_REG_2                   = 0x0002
    INPUT_REG_3                   = 0x0003      
    INPUT_REG_4                   = 0x0004 
    INPUT_REG_5                   = 0x0005 
    
    DIAGNOSTIC_REG_0              = 0x0000
    DIAGNOSTIC_REG_1              = 0x0001
    
    # Broadcast address.
    BROADCAST_ADDRESS               = 0
    # First module address.
    FIRST_MODULE_ADDRESS            = 1
    # Last module address.
    LAST_MODULE_ADDRESS             = 55


    def __init__(self, port):
        self.master = None
        self.interface = None

        # Open serial port.
        self.interface = serial.Serial(port=port, baudrate=2000000, bytesize=8, parity='N', stopbits=1, xonxoff=0)
        # Initialize MODBUS master.
        self.master = modbus_rtu.RtuMaster(self.interface)
        self.master.set_timeout(0.05)
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
    def ReadHoldingRegisters(self, slaveAddress, startRegister, registerCount):
        return self.master.execute(slaveAddress, defines.READ_HOLDING_REGISTERS, startRegister, registerCount)
    
    # Read one or more registers from an I/O module.
    def ReadInputRegisters(self, slaveAddress, startRegister, registerCount):
        return self.master.execute(slaveAddress, defines.READ_INPUT_REGISTERS, startRegister, registerCount)    
    
    # Read one or more registers from an I/O module.
    def ReadDiagnosticRegisters(self, slaveAddress, startRegister, registerCount):
        return self.master.execute(slaveAddress, defines.DIAGNOSTIC, startRegister, registerCount)    
    

    # Write one or more registers to an I/O module.
    def WriteHoldingRegisters(self, slaveAddress, startRegister, registerCount, data):
        self.master.execute(slaveAddress, defines.WRITE_MULTIPLE_REGISTERS, startRegister, registerCount, data)


    # Write raw serial data for bootloader
    def WriteBootloadData(self, TX):
        self.interface.write(TX)
    
    def ReadBootloadData(self, Bytes):
        return (self.interface.read(Bytes))    

    #
    # Intermediate level access functions.
    #

    # Read a 16-bit value from an I/O module.
    def ReadWord(self, slaveAddress, register):
        registerData = self.ReadRegisters(slaveAddress, register, 1)
        return registerData[0]

    # Read a 16-bit value to an I/O module.
    def ReadLongWord(self, slaveAddress, register):
        registerData = self.ReadRegisters(slaveAddress, register, 2)
        return (registerData[0] * 65536) + registerData[1]

    # Write a 32-bit value from an I/O module.
    def WriteWord(self, slaveAddress, register, outputValue):
        registerData = outputValue,
        self.WriteHoldingRegisters(slaveAddress, register, 1, registerData)

    # Write a 32-bit value to an I/O module.
    def WriteLongWord(self, slaveAddress, register, outputValue):
        registerData = ((outputValue / 65536) & 0xFFFF), (outputValue & 0xFFFF)
        self.WriteHoldingRegisters(slaveAddress, register, 2, registerData)

#-----------------------------------------------------------------------------#
#                            Support Functions                                #
#-----------------------------------------------------------------------------#

#-----------------------------------------------------------------------------#
#                            Main Starts Here                                 #
#-----------------------------------------------------------------------------#

# No main.

#----------------------------------- End of file -----------------------------#
