from Enum import *
from Comm import DataAquisition, ModBusMasterReg
from bootload import BootLoader
import time
import struct

class State:
    def __init__(self, data_communication):
        self.DataCommunication      = data_communication
        self.Bootloader             = BootLoader(self.DataCommunication)
        self.ModbusMaster           = ModBusMasterReg()
        
        self.start_time             = 0
        self.meas_started           = False
        
        self.RunInitSlave           = 0
        self.ProgramSlave           = 1
        self.ShiftSlot              = 0
        
        self.RunSlaveConfig         = 0
        
        self.EnableSlaveConfig      = 0
        
        self.RunResetAll            = 0
        
        self.TrackAmpUpateList      = []
        self.AmpSwCheck             = 0
        self.RunSwSlaveConfig       = 0
        
        self.FlashNewSwHandler      = 0
        self.RunSwFlash             = 0
        self.bootloader_offset      = 0x800
        self.program_mem_size       = 0x8000
        self.file_checksum          = 0
        
        self.RunTrackampHandler     = 0
        self.ReadTrackampHandler    = 0   
        self.readTrackampMailbox    = EnumMailbox.Mailbox1
      
    def RunFunction(self, argument):
        function = getattr(self, argument, lambda: EnumStateMachine.nok)
        
        return function()
    
    ######################################################################################
    #
    # Reset the slaves at the beginning of communication because status is unknown...
    #
    ######################################################################################
    def ResetAllSlaves(self):
        '''
        case 0
        '''
        if(self.RunResetAll == 0):
            self.ModbusMaster.HoldingReg[0] = 0xFFFF
            self.DataCommunication.WriteUDP(EnumCommand.ETHERNET_T, EnumEthernetT.ResetAll)
            self.RunResetAll += 1
            #print("ResetAllSlaves --> reset all slaves sent... --> sleep 3 seconds")
            time.sleep(2)
            return EnumStateMachine.ok

        '''
        case 1
        '''
        if(self.RunResetAll == 1):
            if(self.DataCommunication.EthernetTarget.InputReg[0] == EnumEthernetT.OK):
                self.DataCommunication.WriteUDP(EnumCommand.ETHERNET_T, EnumEthernetT.IDLE)
                self.RunResetAll += 1
                #print("ResetAllSlaves --> EthernetTarget.InputReg[0] == EnumEthernetT.OK --> sent goto to IDLE state")
                return EnumStateMachine.busy

        '''
        case 2
        '''
        if(self.RunResetAll == 2):
            if(self.DataCommunication.EthernetTarget.InputReg[0] == EnumEthernetT.IDLE):
                self.RunResetAll = 0
                #print("ResetAllSlaves --> EthernetTarget.InputReg[0] == EnumEthernetT.IDLE --> return OK.\n")
                return EnumStateMachine.ok
    
        return EnumStateMachine.busy    
    
    
    
    
    
    ######################################################################################
    #
    # config the track amplifiers
    #
    ######################################################################################    
    def InitTrackamplifiers(self):
        '''
        case 0
        '''
        if(self.RunInitSlave == 0):
            
            if(self.ProgramSlave > 10 or self.ProgramSlave > self.DataCommunication.AmountOfAmplifiers):
                #print("RunInitSlave --> TRACKBACKPLANE1 finished.")
                self.RunInitSlave += 1
                self.ShiftSlot = 0
                return EnumStateMachine.busy
            
            elif(self.ConfigureSlave(EnumSlaveInit.TRACKBACKPLANE1, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave, False) == EnumStateMachine.ok):
                #print("RunInitSlave --> slave " + str(self.ProgramSlave) + " got initialized.")
                self.ShiftSlot    += 1 
                self.ProgramSlave += 1
                return EnumStateMachine.busy
        
        '''
        case 1
        '''
        if(self.RunInitSlave == 1):
            
            if(self.ProgramSlave > 20 or self.ProgramSlave > self.DataCommunication.AmountOfAmplifiers):
                #print("RunInitSlave --> TRACKBACKPLANE2 finished.")
                self.RunInitSlave += 1
                self.ShiftSlot = 0
                return EnumStateMachine.busy
            
            elif(self.ConfigureSlave(EnumSlaveInit.TRACKBACKPLANE2, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave, False) == EnumStateMachine.ok):
                #print("RunInitSlave --> slave " + str(self.ProgramSlave) + " got initialized.")
                self.ShiftSlot    += 1 
                self.ProgramSlave += 1
                return EnumStateMachine.busy
        
        '''
        case 2
        '''        
        if(self.RunInitSlave == 2):
            
            if(self.ProgramSlave > 30 or self.ProgramSlave > self.DataCommunication.AmountOfAmplifiers):
                #print("RunInitSlave --> TRACKBACKPLANE3 finished.")
                self.RunInitSlave += 1
                self.ShiftSlot = 0
                return EnumStateMachine.busy
            
            elif(self.ConfigureSlave(EnumSlaveInit.TRACKBACKPLANE3, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave, False) == EnumStateMachine.ok):
                #print("RunInitSlave --> slave " + str(self.ProgramSlave) + " got initialized.")
                self.ShiftSlot    += 1 
                self.ProgramSlave += 1
                return EnumStateMachine.busy
  
        '''
        case 3
        '''
        if(self.RunInitSlave == 3):
            
            if(self.ProgramSlave > 40 or self.ProgramSlave > self.DataCommunication.AmountOfAmplifiers):
                #print("RunInitSlave --> TRACKBACKPLANE4 finished.")
                self.RunInitSlave += 1
                self.ShiftSlot = 0
                return EnumStateMachine.busy
            
            elif(self.ConfigureSlave(EnumSlaveInit.TRACKBACKPLANE4, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave, False) == EnumStateMachine.ok):
                #print("RunInitSlave --> slave " + str(self.ProgramSlave) + " got initialized.")
                self.ShiftSlot    += 1 
                self.ProgramSlave += 1
                return EnumStateMachine.busy
        
        '''
        case 4
        '''                        
        if(self.RunInitSlave == 4):
            
            if(self.ProgramSlave > 49 or self.ProgramSlave > self.DataCommunication.AmountOfAmplifiers):
                if(self.ProgramSlave > self.DataCommunication.AmountOfAmplifiers):
                    #print("RunInitSlave --> slave " + str(self.ProgramSlave) + " got initialized.")
                    #print("RunInitSlave --> TRACKBACKPLANE5 finished.")
                    self.RunInitSlave = 0
                    self.ShiftSlot = 0
                    self.ProgramSlave = 1
                    return EnumStateMachine.ok
                
                elif(self.ConfigureSlave(EnumSlaveInit.TRACKBACKPLANE5, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave, True) == EnumStateMachine.ok):
                    #print("RunInitSlave --> slave " + str(self.ProgramSlave) + " got initialized.")
                    #print("RunInitSlave --> TRACKBACKPLANE5 finished.")
                    self.RunInitSlave = 0
                    self.ShiftSlot = 0
                    self.ProgramSlave = 1
                    return EnumStateMachine.ok
            
            elif(self.ConfigureSlave(EnumSlaveInit.TRACKBACKPLANE5, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave, False) == EnumStateMachine.ok):
                #print("RunInitSlave --> slave " + str(self.ProgramSlave) + " got initialized.")
                self.ShiftSlot    += 1 
                self.ProgramSlave += 1
                return EnumStateMachine.busy
            
        return EnumStateMachine.busy

   
    ######################################################################################
    #
    # configuration sequencer
    #
    ######################################################################################       
    def ConfigureSlave(self, TrackBackPlaneID, AmplifierLatchSet, TrackAmplifierId, Mode):
        
        '''
        case 0
        '''        
        if(self.RunSlaveConfig == 0):
            self.DataCommunication.ModbusInterface.WriteHoldingRegisters(TrackBackPlaneID, 0x0000, 1, [AmplifierLatchSet])
            self.RunSlaveConfig += 1
            #print("RunSlaveConfig==0 --> TrackBackPlaneID: " + str(TrackBackPlaneID) + " AmplifierLatchSet: " + str(AmplifierLatchSet))
            return EnumStateMachine.busy
        
        '''
        case 1
        '''        
        if(self.RunSlaveConfig == 1):
            self.DataCommunication.ModbusInterface.WriteHoldingRegisters(EnumSlaveInit.DEFAULTMODBUSADDR, 0x2, 1, [TrackAmplifierId])
            self.RunSlaveConfig += 1
            #print("RunSlaveConfig==1 --> Set Amplifier ID: " + str(TrackAmplifierId))
            return EnumStateMachine.busy
        
        '''
        case 2
        '''        
        if(self.RunSlaveConfig == 2):
            self.DataCommunication.ModbusInterface.WriteHoldingRegisters(TrackBackPlaneID, 0x0000, 1, [0xFFFF])
            self.RunSlaveConfig = 0
            #print("RunSlaveConfig==2 --> TrackBackPlaneID: " + str(TrackBackPlaneID) + " AmplifierLatchSet: Release amplifier select line")
            return EnumStateMachine.ok        
        
        return EnumStateMachine.busy
    
    
    ######################################################################################
    #
    # Enable the track amplifiers
    #
    ######################################################################################    
    def EnableTrackamplifiers(self):
        
        '''
        case 0
        '''        
        if(self.EnableSlaveConfig == 0):
            self.DataCommunication.ModbusInterface.WriteHoldingRegisters(EnumSlaveInit.BROADCASTADDR, 0x1, 1, [0x8000])
            self.EnableSlaveConfig = 0
            #print("EnableSlaveConfig==0 --> Set all amplifiers enabled")
            return EnumStateMachine.ok
    
        return EnumStateMachine.busy
    
    
    ######################################################################################
    #
    # Check track amplifiers software
    #
    ######################################################################################        
        
    def CheckAmpSwVersion(self):
        '''
        case 0
        '''
        if(self.AmpSwCheck == 0):
            
            if(self.ProgramSlave > 10 or self.ProgramSlave > self.DataCommunication.AmountOfAmplifiers):
                #print("AmpSwCheck --> TRACKBACKPLANE1 finished.")
                self.AmpSwCheck += 1
                self.ShiftSlot = 0
                return EnumStateMachine.busy
            
            elif(self.SwVersionReadSlave(EnumSlaveInit.TRACKBACKPLANE1, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave) == EnumStateMachine.ok):
                #print("AmpSwCheck --> slave " + str(self.ProgramSlave) + " got initialized.")
                self.ShiftSlot    += 1 
                self.ProgramSlave += 1
                return EnumStateMachine.busy
        
        '''
        case 1
        '''
        if(self.AmpSwCheck == 1):
            
            if(self.ProgramSlave > 20 or self.ProgramSlave > self.DataCommunication.AmountOfAmplifiers):
                #print("AmpSwCheck --> TRACKBACKPLANE2 finished.")
                self.AmpSwCheck += 1
                self.ShiftSlot = 0
                return EnumStateMachine.busy
            
            elif(self.SwVersionReadSlave(EnumSlaveInit.TRACKBACKPLANE2, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave) == EnumStateMachine.ok):
                #print("AmpSwCheck --> slave " + str(self.ProgramSlave) + " got initialized.")
                self.ShiftSlot    += 1 
                self.ProgramSlave += 1
                return EnumStateMachine.busy
        
        '''
        case 2
        '''        
        if(self.AmpSwCheck == 2):
            
            if(self.ProgramSlave > 30 or self.ProgramSlave > self.DataCommunication.AmountOfAmplifiers):
                #print("AmpSwCheck --> TRACKBACKPLANE3 finished.")
                self.AmpSwCheck += 1
                self.ShiftSlot = 0
                return EnumStateMachine.busy
            
            elif(self.SwVersionReadSlave(EnumSlaveInit.TRACKBACKPLANE3, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave) == EnumStateMachine.ok):
                #print("AmpSwCheck --> slave " + str(self.ProgramSlave) + " got initialized.")
                self.ShiftSlot    += 1 
                self.ProgramSlave += 1
                return EnumStateMachine.busy
  
        '''
        case 3
        '''
        if(self.AmpSwCheck == 3):
            
            if(self.ProgramSlave > 40 or self.ProgramSlave > self.DataCommunication.AmountOfAmplifiers):
                #print("AmpSwCheck --> TRACKBACKPLANE4 finished.")
                self.AmpSwCheck += 1
                self.ShiftSlot = 0
                return EnumStateMachine.busy
            
            elif(self.SwVersionReadSlave(EnumSlaveInit.TRACKBACKPLANE4, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave) == EnumStateMachine.ok):
                #print("AmpSwCheck --> slave " + str(self.ProgramSlave) + " got initialized.")
                self.ShiftSlot    += 1 
                self.ProgramSlave += 1
                return EnumStateMachine.busy
        
        '''
        case 4
        '''                        
        if(self.AmpSwCheck == 4):
            
            if(self.ProgramSlave > 50 or self.ProgramSlave > self.DataCommunication.AmountOfAmplifiers):                
                #print("AmpSwCheck --> TRACKBACKPLANE5 finished.\n")
                self.AmpSwCheck = 0
                self.ShiftSlot = 0
                self.ProgramSlave = 1
                return EnumStateMachine.ok
            
            elif(self.SwVersionReadSlave(EnumSlaveInit.TRACKBACKPLANE5, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave) == EnumStateMachine.ok):
                #print("AmpSwCheck --> slave " + str(self.ProgramSlave) + " got initialized.\n")
                self.ShiftSlot    += 1 
                self.ProgramSlave += 1
                return EnumStateMachine.busy
            
        return EnumStateMachine.busy
    
    
    ######################################################################################
    #
    # SW configuration checker
    #
    ######################################################################################       
    def SwVersionReadSlave(self, TrackBackPlaneID, AmplifierLatchSet, TrackAmplifierId):
        
        '''
        case 0
        '''        
        if(self.RunSwSlaveConfig == 0):
            self.DataCommunication.ModbusInterface.WriteHoldingRegisters(TrackBackPlaneID, 0x0000, 1, [AmplifierLatchSet])
            self.RunSwSlaveConfig += 1
            #print("RunSwSlaveConfig==0 --> TrackBackPlaneID: " + str(TrackBackPlaneID) + " AmplifierLatchSet: " + str(AmplifierLatchSet))
            return EnumStateMachine.busy
        
        '''
        case 1
        '''        
        if(self.RunSwSlaveConfig == 1):
            #print("RunSwSlaveConfig==1 --> Read input register: " + str(5))
            
            self.TrackAmpUpateList.append([TrackAmplifierId,self.DataCommunication.ModbusInterface.ReadInputRegisters(EnumSlaveInit.DEFAULTMODBUSADDR, 0x5, 1)[0]])
            
            #print("RunSwSlaveConfig==1 --> TrackAmp " + str(TrackAmplifierId) + " has checksum " + str(hex(self.TrackAmpUpateList[TrackAmplifierId-1][1])))
            
            self.RunSwSlaveConfig += 1            
            return EnumStateMachine.busy
        
        '''
        case 2
        '''        
        if(self.RunSwSlaveConfig == 2):
            
            self.DataCommunication.ModbusInterface.WriteHoldingRegisters(TrackBackPlaneID, 0x0000, 1, [0xFFFF])
            
            self.RunSwSlaveConfig = 0
            #print("RunSwSlaveConfig==2 --> TrackBackPlaneID: " + str(TrackBackPlaneID) + " AmplifierLatchSet: Release amplifier select line")
            return EnumStateMachine.ok

        
        return EnumStateMachine.busy  
    
    
    ######################################################################################
    #
    # Flash new SW into Trackamplifier
    #
    ######################################################################################       
    def FlashTrackamplifiers(self):
        
        '''
        case 0
        '''
        if(self.FlashNewSwHandler == 0):
            
            if(self.ProgramSlave > 10 or self.ProgramSlave > self.DataCommunication.AmountOfAmplifiers):
                #print("FlashNewSwHandler --> TRACKBACKPLANE1 finished.")
                self.FlashNewSwHandler += 1
                self.ShiftSlot = 0
                return EnumStateMachine.busy
            
            elif(self.FlashCaller(EnumSlaveInit.TRACKBACKPLANE1, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave) == EnumStateMachine.ok):
                #print("FlashNewSwHandler --> slave " + str(self.ProgramSlave) + " got initialized.")
                self.ShiftSlot    += 1 
                self.ProgramSlave += 1
                return EnumStateMachine.busy
        
        '''
        case 1
        '''
        if(self.FlashNewSwHandler == 1):
            
            if(self.ProgramSlave > 20 or self.ProgramSlave > self.DataCommunication.AmountOfAmplifiers):
                #print("FlashNewSwHandler --> TRACKBACKPLANE2 finished.")
                self.FlashNewSwHandler += 1
                self.ShiftSlot = 0
                return EnumStateMachine.busy
            
            elif(self.FlashCaller(EnumSlaveInit.TRACKBACKPLANE2, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave) == EnumStateMachine.ok):
                #print("FlashNewSwHandler --> slave " + str(self.ProgramSlave) + " got initialized.")
                self.ShiftSlot    += 1 
                self.ProgramSlave += 1
                return EnumStateMachine.busy
        
        '''
        case 2
        '''        
        if(self.FlashNewSwHandler == 2):
            
            if(self.ProgramSlave > 30 or self.ProgramSlave > self.DataCommunication.AmountOfAmplifiers):
                #print("FlashNewSwHandler --> TRACKBACKPLANE3 finished.")
                self.FlashNewSwHandler += 1
                self.ShiftSlot = 0
                return EnumStateMachine.busy
            
            elif(self.FlashCaller(EnumSlaveInit.TRACKBACKPLANE3, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave) == EnumStateMachine.ok):
                #print("FlashNewSwHandler --> slave " + str(self.ProgramSlave) + " got initialized.")
                self.ShiftSlot    += 1 
                self.ProgramSlave += 1
                return EnumStateMachine.busy
  
        '''
        case 3
        '''
        if(self.FlashNewSwHandler == 3):
            
            if(self.ProgramSlave > 40 or self.ProgramSlave > self.DataCommunication.AmountOfAmplifiers):
                #print("FlashNewSwHandler --> TRACKBACKPLANE4 finished.")
                self.FlashNewSwHandler += 1
                self.ShiftSlot = 0
                return EnumStateMachine.busy
            
            elif(self.FlashCaller(EnumSlaveInit.TRACKBACKPLANE4, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave) == EnumStateMachine.ok):
                #print("FlashNewSwHandler --> slave " + str(self.ProgramSlave) + " got initialized.")
                self.ShiftSlot    += 1 
                self.ProgramSlave += 1
                return EnumStateMachine.busy
        
        '''
        case 4
        '''                        
        if(self.FlashNewSwHandler == 4):
            
            if(self.ProgramSlave > 50 or self.ProgramSlave > self.DataCommunication.AmountOfAmplifiers):                
                #print("FlashNewSwHandler --> TRACKBACKPLANE5 finished.")
                self.FlashNewSwHandler = 0
                self.ShiftSlot = 0
                self.ProgramSlave = 1
                time.sleep(5.0)
                return EnumStateMachine.ok
            
            elif(self.FlashCaller(EnumSlaveInit.TRACKBACKPLANE5, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave) == EnumStateMachine.ok):
                #print("FlashNewSwHandler --> slave " + str(self.ProgramSlave) + " got initialized.")
                self.ShiftSlot    += 1 
                self.ProgramSlave += 1
                return EnumStateMachine.busy
            
        return EnumStateMachine.busy

    ######################################################################################
    #
    # SW flash caller
    #
    ######################################################################################       
    def FlashCaller(self, TrackBackPlaneID, AmplifierLatchSet, TrackAmplifierId):
        
        '''
        case 0
        '''        
        if(self.RunSwFlash == 0):
            
            if(self.file_checksum == 0):
                returncode, self.file_checksum = self.Bootloader.GetFileCheckSum(self.bootloader_offset, self.program_mem_size)
                if(returncode != EnumBootloader.COMMAND_SUCCESSFUL):
                    bla = 1
                    #print("Getting Checksum from file failed!!!!\n")
            
            if(self.TrackAmpUpateList[TrackAmplifierId - 1][1] == self.file_checksum):
                #print("RunSwFlash==0 --> TrackAmplifierId: " + str(TrackAmplifierId) + " checksum = " + str(hex(self.TrackAmpUpateList[TrackAmplifierId - 1][1])) + " file checksum = " + str(hex(self.file_checksum)) + "\n\r")
                return EnumStateMachine.ok
            
            #print("RunSwFlash==0 --> TrackAmplifierId: " + str(TrackAmplifierId) + " checksum = " + str(hex(self.TrackAmpUpateList[TrackAmplifierId - 1][1])) + " file checksum = " + str(hex(self.file_checksum)) + " starting flash")
            
            #print("RunSwFlash==0 --> ResetAll slaves")
            self.DataCommunication.WriteUDP(EnumCommand.ETHERNET_T, EnumEthernetT.ResetAll)
            time.sleep(0.5)
            
            try:
                self.DataCommunication.ModbusInterface.WriteHoldingRegisters(TrackBackPlaneID, 0x0000, 1, [AmplifierLatchSet])
            except:
                #print("RunSwFlash==0 --> Write to TrackBackPlane gave not a response, passing")
                pass
            
            self.RunSwFlash += 1
            #print("RunSwFlash==0 --> TrackBackPlaneID: " + str(TrackBackPlaneID) + " AmplifierLatchSet: " + str(AmplifierLatchSet))
            return EnumStateMachine.busy
        
        '''
        case 1
        '''        
        if(self.RunSwFlash == 1):
            #print("RunSwFlash==1 --> Starting bootloader flash program.")
            self.Bootloader.FlashAuto()
            #print("RunSwFlash==1 --> bootloader flash program done.")            
            self.DataCommunication.WriteUDP(EnumCommand.ETHERNET_T, EnumEthernetT.ResetAll)
            time.sleep(0.5) 
            self.RunSwFlash = 0
            return EnumStateMachine.ok
        
    
        return EnumStateMachine.busy          
    
    
    ######################################################################################
    #
    # Get info from the slaves
    #
    ######################################################################################
    def RunTrackamplifiers(self):
        
        '''
        case 0
        '''
        if(self.RunTrackampHandler == 0):
            
            if(self.ProgramSlave > 10 or self.ProgramSlave > self.DataCommunication.AmountOfAmplifiers):
                #print("RunTrackampHandler --> TRACKBACKPLANE1 finished.")
                self.RunTrackampHandler += 1
                return EnumStateMachine.busy
            
            elif(self._ReadTrackAmplifiers(self.ProgramSlave) == EnumStateMachine.ok):
                #print("RunTrackampHandler --> slave " + str(self.ProgramSlave) + " got initialized.")
                self.ProgramSlave += 1
                return EnumStateMachine.busy
        
        '''
        case 1
        '''
        if(self.RunTrackampHandler == 1):
            
            if(self.ProgramSlave > 20 or self.ProgramSlave > self.DataCommunication.AmountOfAmplifiers):
                #print("RunTrackampHandler --> TRACKBACKPLANE2 finished.")
                self.RunTrackampHandler += 1
                return EnumStateMachine.busy
            
            elif(self._ReadTrackAmplifiers(self.ProgramSlave) == EnumStateMachine.ok):
                #print("RunTrackampHandler --> slave " + str(self.ProgramSlave) + " got initialized.")
                self.ProgramSlave += 1
                return EnumStateMachine.busy
        
        '''
        case 2
        '''        
        if(self.RunTrackampHandler == 2):
            
            if(self.ProgramSlave > 30 or self.ProgramSlave > self.DataCommunication.AmountOfAmplifiers):
                #print("RunTrackampHandler --> TRACKBACKPLANE3 finished.")
                self.RunTrackampHandler += 1
                return EnumStateMachine.busy
            
            elif(self._ReadTrackAmplifiers(self.ProgramSlave) == EnumStateMachine.ok):
                #print("RunTrackampHandler --> slave " + str(self.ProgramSlave) + " got initialized.")
                self.ProgramSlave += 1
                return EnumStateMachine.busy
  
        '''
        case 3
        '''
        if(self.RunTrackampHandler == 3):
            
            if(self.ProgramSlave > 40 or self.ProgramSlave > self.DataCommunication.AmountOfAmplifiers):
                #print("RunTrackampHandler --> TRACKBACKPLANE4 finished.")
                self.RunTrackampHandler += 1
                return EnumStateMachine.busy
            
            elif(self._ReadTrackAmplifiers(self.ProgramSlave) == EnumStateMachine.ok):
                #print("RunTrackampHandler --> slave " + str(self.ProgramSlave) + " got initialized.")
                self.ProgramSlave += 1
                return EnumStateMachine.busy
        
        '''
        case 4
        '''                        
        if(self.RunTrackampHandler == 4):
            
            if(self.ProgramSlave > 50 or self.ProgramSlave > self.DataCommunication.AmountOfAmplifiers):                
                #print("RunTrackampHandler --> TRACKBACKPLANE5 finished.")
                self.RunTrackampHandler = 0
                self.ProgramSlave = 1
                return EnumStateMachine.ok
            
            elif(self._ReadTrackAmplifiers(self.ProgramSlave) == EnumStateMachine.ok):
                #print("RunTrackampHandler --> slave " + str(self.ProgramSlave) + " got initialized.")
                self.ProgramSlave += 1
                return EnumStateMachine.busy
            
        return EnumStateMachine.busy 
    
    ######################################################################################
    #
    # Read Trackamplifier readout sequence
    #
    ######################################################################################       
    def _ReadTrackAmplifiers(self, TrackAmplifierId):
        
        '''
        case 0
        '''        
        if(self.ReadTrackampHandler == 0):
            self.DataCommunication.ModbusInterface.WriteHoldingRegisters(TrackAmplifierId, 0x0, 2, [0x0, 0x8000])            
            #print("ReadTrackampHandler==0 --> TrackAmplifierId: " + str(TrackAmplifierId) + " WriteHoldingRegisters 0 and 1")
            if(TrackAmplifierId == self.DataCommunication.AmountOfAmplifiers):
                self.ReadTrackampHandler += 1
            return EnumStateMachine.ok
        
        '''
        case 1
        '''        
        if(self.ReadTrackampHandler == 1):
            #self.start_time = time.time()
            data =  self.DataCommunication.ModbusInterface.ReadInputRegisters(TrackAmplifierId, 0x0, 2)
            #elapsed_time = time.time() - self.start_time
            ##print('Updaterate = ', str('%.9f'% elapsed_time) , 'seconds! \n')            
            self.DataCommunication.Trackamplifiers[TrackAmplifierId-1].InputReg[0] = data[0]
            self.DataCommunication.Trackamplifiers[TrackAmplifierId-1].InputReg[1] = data[1] 
            ##print("ReadTrackampHandler==1 --> TrackAmplifierId: " + str(TrackAmplifierId) + " ReadInputRegisters 0 and 1")
            if(TrackAmplifierId == self.DataCommunication.AmountOfAmplifiers):
                self.ReadTrackampHandler += 1
            return EnumStateMachine.ok
        
        '''
        case 2
        '''        
        if(self.ReadTrackampHandler == 2):
            if(self.readTrackampMailbox == EnumMailbox.Mailbox1):
                self.DataCommunication.ModbusInterface.WriteHoldingRegisters(TrackAmplifierId, 0x2, 2, [0x0, 0x0])
                #print("ReadTrackampHandler==2 --> TrackAmplifierId: " + str(TrackAmplifierId) + " WriteHoldingRegisters 2 and 3, Mailbox1")                
            
            if(self.readTrackampMailbox == EnumMailbox.Mailbox2):
                bla = 1
                #data = self.DataCommunication.ModbusInterface.ReadDiagnosticRegisters(TrackAmplifierId, 0x0, 2)
                #self.DataCommunication.Trackamplifiers[TrackAmplifierId-1].DiagReg[0] = data[0]
                #self.DataCommunication.Trackamplifiers[TrackAmplifierId-1].DiagReg[1] = data[1]                
                #print("ReadTrackampHandler==2 --> TrackAmplifierId: " + str(TrackAmplifierId) + " ReadDiagnosticRegisters 0 and 1, Mailbox2")                
            
            if(self.readTrackampMailbox == EnumMailbox.Mailbox3):
                data =  self.DataCommunication.ModbusInterface.ReadInputRegisters(TrackAmplifierId, 0x2, 2)
                self.DataCommunication.Trackamplifiers[TrackAmplifierId-1].InputReg[2] = data[0]
                self.DataCommunication.Trackamplifiers[TrackAmplifierId-1].InputReg[3] = data[1] 
                #print("ReadTrackampHandler==2 --> TrackAmplifierId: " + str(TrackAmplifierId) + " ReadInputRegisters 2 and 3, Mailbox3")                
            
            if(self.readTrackampMailbox == EnumMailbox.Mailbox4):
                data =  self.DataCommunication.ModbusInterface.ReadInputRegisters(TrackAmplifierId, 0x4, 2)
                self.DataCommunication.Trackamplifiers[TrackAmplifierId-1].InputReg[4] = data[0]
                self.DataCommunication.Trackamplifiers[TrackAmplifierId-1].InputReg[5] = data[1] 
                #print("ReadTrackampHandler==2 --> TrackAmplifierId: " + str(TrackAmplifierId) + " ReadInputRegisters 4 and 5, Mailbox4")
            
            if(TrackAmplifierId == self.DataCommunication.AmountOfAmplifiers):
                self.ReadTrackampHandler = 0
                self.readTrackampMailbox += 1
                if (self.readTrackampMailbox > EnumMailbox.Mailbox4):
                    self.readTrackampMailbox = EnumMailbox.Mailbox1
            return EnumStateMachine.ok        
        
        return EnumStateMachine.busy
