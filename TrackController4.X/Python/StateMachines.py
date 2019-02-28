from Enum import *
from Comm import DataAquisition, ModBusMasterReg
import time

class State:
    def __init__(self, amplifiers):
        self.RunInitSlave           = 0
        self.ProgramSlave           = 1
        self.ShiftSlot              = 0
        
        self.RunSlaveConfig         = 0
        
        self.RunResetAll            = 0
        
        self.Amplifiers             = amplifiers
        self.ModbusMaster           = ModBusMasterReg()
      
    def RunFunction(self, argument):
        function = getattr(self, argument, lambda: EnumStateMachine.nok)
        return function()
    
    
    def ResetAllSlaves(self):
        '''
        case 0
        '''
        if(self.RunResetAll == 0):
            self.Amplifiers.WriteSerial(EnumCommand.ETHERNET_T, EnumEthernetT.ResetAll)
            self.RunResetAll += 1
            return EnumStateMachine.busy

        '''
        case 1
        '''
        if(self.RunResetAll == 1):
            if(self.Amplifiers.EthernetTarget.InputReg[0] == EnumEthernetT.OK):
                self.Amplifiers.WriteSerial(EnumCommand.ETHERNET_T, EnumEthernetT.IDLE)
                self.RunResetAll += 1
                return EnumStateMachine.busy

        '''
        case 2
        '''
        if(self.RunResetAll == 2):
            if(self.Amplifiers.EthernetTarget.InputReg[0] == EnumEthernetT.IDLE):
                self.RunResetAll = 0
                return EnumStateMachine.ok
    
        return EnumStateMachine.busy    
    
    
    def InitTrackamplifiers(self):
        '''
        case 0
        '''
        if(self.RunInitSlave == 0):
            
            if(self.ProgramSlave > 10):
                self.RunInitSlave += 1
                self.ShiftSlot = 0
                return EnumStateMachine.busy
            
            elif(self.ConfigureSlave(EnumSlaveInit.TRACKBACKPLANE1, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave, False) == EnumStateMachine.ok):
                self.ShiftSlot    += 1 
                self.ProgramSlave += 1
                return EnumStateMachine.busy
        
        '''
        case 1
        '''
        if(self.RunInitSlave == 1):
            
            if(self.ProgramSlave > 20):
                self.RunInitSlave += 1
                self.ShiftSlot = 0
                return EnumStateMachine.busy
            
            elif(ConfigureSlave(EnumSlaveInit.TRACKBACKPLANE2, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave, False) == EnumStateMachine.ok):
                self.ShiftSlot    += 1 
                self.ProgramSlave += 1
                return EnumStateMachine.busy
        
        '''
        case 2
        '''        
        if(self.RunInitSlave == 2):
            
            if(self.ProgramSlave > 30):
                self.RunInitSlave += 1
                self.ShiftSlot = 0
                return EnumStateMachine.busy
            
            elif(ConfigureSlave(EnumSlaveInit.TRACKBACKPLANE3, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave, False) == EnumStateMachine.ok):
                self.ShiftSlot    += 1 
                self.ProgramSlave += 1
                return EnumStateMachine.busy
  
        '''
        case 3
        '''
        if(self.RunInitSlave == 3):
            
            if(self.ProgramSlave > 40):
                self.RunInitSlave += 1
                self.ShiftSlot = 0
                return EnumStateMachine.busy
            
            elif(ConfigureSlave(EnumSlaveInit.TRACKBACKPLANE4, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave, False) == EnumStateMachine.ok):
                self.ShiftSlot    += 1 
                self.ProgramSlave += 1
                return EnumStateMachine.busy
        
        '''
        case 4
        '''                        
        if(self.RunInitSlave == 4):
            
            if(self.ProgramSlave > 49):
                if(ConfigureSlave(EnumSlaveInit.TRACKBACKPLANE5, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave, True) == EnumStateMachine.ok):
                    self.RunInitSlave = 0
                    self.ShiftSlot = 0
                    return EnumStateMachine.ok
            
            elif(ConfigureSlave(EnumSlaveInit.TRACKBACKPLANE5, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave, False) == EnumStateMachine.ok):
                self.ShiftSlot    += 1 
                self.ProgramSlave += 1
                return EnumStateMachine.busy
            
        return EnumStateMachine.busy

   
    
    def ConfigureSlave(self, TrackBackPlaneID, AmplifierLatchSet, TrackAmplifierId, Mode):
        
        '''
        case 0
        '''        
        if(self.RunSlaveConfig == 0):
            self.ModbusMaster.HoldingReg[1] = TrackBackPlaneID
            self.ModbusMaster.HoldingReg[2] = AmplifierLatchSet
            self.ModbusMaster.HoldingReg[3] = 0
            self.ModbusMaster.HoldingReg[0] = EnumSlaveConfig.MODE_MAN & EnumSlaveConfig.WRITE & EnumSlaveConfig.HOLDINGREG & EnumSlaveConfig.EXEC
            self.Amplifiers.WriteSerial(EnumCommand.MODBUS, self.ModbusMaster)
            self.RunSlaveConfig += 1
            return EnumStateMachine.busy
        
        '''
        case 1
        '''        
        if(self.RunSlaveConfig == 1):
            if(self.Amplifiers.Trackamplifiers[0].InputReg[0] == EnumSlaveConfig.OK):
                self.ModbusMaster.HoldingReg[0] = EnumSlaveConfig.MODE_MAN & EnumSlaveConfig.WRITE & EnumSlaveConfig.HOLDINGREG & EnumSlaveConfig.HALT
                self.Amplifiers.WriteSerial(EnumCommand.MODBUS, self.ModbusMaster)
                self.RunSlaveConfig += 1
                return EnumStateMachine.busy
        
        '''
        case 2
        '''        
        if(self.RunSlaveConfig == 2):
            if(self.Amplifiers.Trackamplifiers[0].InputReg[0] == EnumSlaveConfig.IDLE):
                self.RunSlaveConfig += 1
                return EnumStateMachine.busy
            
        return EnumStateMachine.busy
    
    
    
    
    
    
    
    
    
    def check_sw_version(self):
        print("Check SW version called")
        return e.ok