from Enum import *
from Comm import DataAquisition, ModBusMasterReg
from bootload import BootLoader
import time

class State:
    def __init__(self, amplifiers):
        self.Amplifiers             = amplifiers
        self.Bootloader             = BootLoader(self.Amplifiers)
        self.ModbusMaster           = ModBusMasterReg()
        
#        self.RunInitSlave           = 0
#        self.ProgramSlave           = 1
#        self.ShiftSlot              = 0
#        
#        self.RunSlaveConfig         = 0
#        
#        self.EnableSlaveConfig      = 0
        
        self.RunResetAll            = 0        
        self.ConnectToEth           = 0
        self.SlaveDetect            = 0
        self.UploadData             = 0
        self.TrackamplifiersInit    = 0
        self.TrackamplifiersEnable  = 0
        
#        self.TrackAmpUpateList      = []
#        self.AmpSwCheck             = 0
#        self.RunSwSlaveConfig       = 0
#        
#        self.FlashNewSwHandler      = 0
#        self.RunSwFlash             = 0
#        self.bootloader_offset      = 0x800
#        self.program_mem_size       = 0x8000
#        self.file_checksum          = 0
      
    def RunFunction(self, argument):
        function = getattr(self, argument, lambda: EnumStateMachine.nok)
        
        return function()
    
    ######################################################################################
    #
    # Connect to the ethernet target
    #
    ######################################################################################
    def ConnectToEthernetTarget(self):
        '''
        case 0
        '''
        if(self.ConnectToEth == 0):
            self.Amplifiers.WriteSerial(EnumCommand.ETHERNET_T, EnumClientCommands.CLIENT_CONNECTION_REQUEST, 0)
            self.ConnectToEth += 1
            print("Connecting to: " + self.Amplifiers.IPAddr)
            #time.sleep(1)
            return EnumStateMachine.busy

        '''
        case 1
        '''
        if(self.ConnectToEth == 1):
            if(self.Amplifiers.EthernetTarget.taskid == EnumStatusMessages.CONTROLLER and self.Amplifiers.EthernetTarget.taskstate == EnumStatusMessages.CONNECTED and 
               self.Amplifiers.EthernetTarget.feedback == EnumStatusMessages.DONE):
                print("Connected to: " + self.Amplifiers.IPAddr + '\n')
                self.Amplifiers.EthernetTarget.ClearOldData()
                self.ConnectToEth = 0
                return EnumStateMachine.ok
    
        return EnumStateMachine.busy    
    
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
            self.Amplifiers.WriteSerial(EnumCommand.ETHERNET_T, EnumCommand.EXEC_MBUS_STATE_RESET, 0)
            self.RunResetAll += 1
            print("ResetAllSlaves --> EXEC_MBUS_STATE_RESET")
            #time.sleep(1)
            return EnumStateMachine.busy

        '''
        case 1
        '''
        if(self.RunResetAll == 1):
            if(self.Amplifiers.EthernetTarget.taskid == EnumStatusMessages.MBUS and self.Amplifiers.EthernetTarget.taskstate == EnumMbusStatus.MBUS_STATE_RESET and 
               self.Amplifiers.EthernetTarget.feedback == EnumStatusMessages.DONE):
                print("ResetAllSlaves --> EXEC_MBUS_STATE_RESET --> done.")
                self.Amplifiers.EthernetTarget.ClearOldData()
                self.Amplifiers.WriteSerial(EnumCommand.ETHERNET_T, EnumCommand.EXEC_MBUS_STATE_SLAVES_ON, 0)
                self.RunResetAll += 1
                print("ResetAllSlaves --> EXEC_MBUS_STATE_SLAVES_ON")
                return EnumStateMachine.busy

        '''
        case 2
        '''
        if(self.RunResetAll == 2):
            if(self.Amplifiers.EthernetTarget.taskid == EnumStatusMessages.MBUS and self.Amplifiers.EthernetTarget.taskstate == EnumMbusStatus.MBUS_STATE_SLAVES_ON and 
               self.Amplifiers.EthernetTarget.feedback == EnumStatusMessages.DONE):
                print("ResetAllSlaves --> EXEC_MBUS_STATE_SLAVES_ON --> done.")
                self.Amplifiers.EthernetTarget.ClearOldData()
                self.RunResetAll = 0
                print("ResetAllSlaves --> return OK.\n")
                return EnumStateMachine.ok
    
        return EnumStateMachine.busy    
    
    
    ######################################################################################
    #
    # Start data upload
    #
    ######################################################################################
    def DataUploadStart(self):
        '''
        case 0
        '''
        if(self.UploadData == 0):
            self.Amplifiers.WriteSerial(EnumCommand.ETHERNET_T, EnumCommand.EXEC_MBUS_STATE_START_DATA_UPLOAD, 0)
            self.UploadData += 1
            print("DataUploadStart --> EXEC_MBUS_STATE_START_DATA_UPLOAD")
            return EnumStateMachine.busy

        '''
        case 1
        '''
        if(self.UploadData == 1):
            if(self.Amplifiers.EthernetTarget.taskid == EnumStatusMessages.MBUS and self.Amplifiers.EthernetTarget.taskstate == EnumMbusStatus.MBUS_STATE_START_DATA_UPLOAD and 
               self.Amplifiers.EthernetTarget.feedback == EnumStatusMessages.DONE):
                print("DataUploadStart --> EXEC_MBUS_STATE_START_DATA_UPLOAD --> done.\n")
                self.Amplifiers.EthernetTarget.ClearOldData()                
                self.UploadData = 0                
                return EnumStateMachine.ok        
    
        return EnumStateMachine.busy    
    
    ######################################################################################
    #
    # Execute slave detection
    #
    ######################################################################################
    def DetectSlaves(self):
        '''
        case 0
        '''
        if(self.SlaveDetect == 0):
            self.Amplifiers.WriteSerial(EnumCommand.ETHERNET_T, EnumCommand.EXEC_MBUS_STATE_SLAVE_DETECT, 0)
            self.SlaveDetect += 1
            print("DetectSlaves --> EXEC_MBUS_STATE_SLAVE_DETECT")
            return EnumStateMachine.busy

        '''
        case 1
        '''
        if(self.SlaveDetect == 1):
            if(self.Amplifiers.EthernetTarget.taskid == EnumStatusMessages.MBUS and self.Amplifiers.EthernetTarget.taskstate == EnumMbusStatus.MBUS_STATE_SLAVE_DETECT and 
               self.Amplifiers.EthernetTarget.feedback == EnumStatusMessages.DONE):
                print("DetectSlaves --> EXEC_MBUS_STATE_SLAVE_DETECT --> done.\n")
                self.Amplifiers.EthernetTarget.ClearOldData()                
                self.SlaveDetect = 0                
                return EnumStateMachine.ok        
    
        return EnumStateMachine.busy    
    
    ######################################################################################
    #
    # Slave Initialization (write communication address to slaves)
    #
    ######################################################################################
    def InitTrackamplifiers(self):
        '''
        case 0
        '''
        if(self.TrackamplifiersInit == 0):
            self.Amplifiers.WriteSerial(EnumCommand.ETHERNET_T, EnumCommand.EXEC_MBUS_STATE_SLAVE_INIT, 0)
            self.TrackamplifiersInit += 1
            print("TrackamplifiersInit --> EXEC_MBUS_STATE_SLAVE_INIT")
            return EnumStateMachine.busy

        '''
        case 1
        '''
        if(self.TrackamplifiersInit == 1):
            if(self.Amplifiers.EthernetTarget.taskid == EnumStatusMessages.MBUS and self.Amplifiers.EthernetTarget.taskstate == EnumMbusStatus.MBUS_STATE_SLAVE_INIT and 
               self.Amplifiers.EthernetTarget.feedback == EnumStatusMessages.DONE):
                print("TrackamplifiersInit --> EXEC_MBUS_STATE_SLAVE_INIT --> done.\n")
                self.Amplifiers.EthernetTarget.ClearOldData()                
                self.TrackamplifiersInit = 0                
                return EnumStateMachine.ok        
    
        return EnumStateMachine.busy    
    
    
    ######################################################################################
    #
    # Slave Initialization (write communication address to slaves)
    #
    ######################################################################################
    def EnableTrackamplifiers(self):
        '''
        case 0
        '''
        if(self.TrackamplifiersEnable == 0):
            self.Amplifiers.WriteSerial(EnumCommand.ETHERNET_T, EnumCommand.EXEC_MBUS_STATE_SLAVE_ENABLE, 0)
            self.TrackamplifiersEnable += 1
            print("TrackamplifiersEnable --> EXEC_MBUS_STATE_SLAVE_ENABLE")
            return EnumStateMachine.busy

        '''
        case 1
        '''
        if(self.TrackamplifiersEnable == 1):
            if(self.Amplifiers.EthernetTarget.taskid == EnumStatusMessages.MBUS and self.Amplifiers.EthernetTarget.taskstate == EnumMbusStatus.MBUS_STATE_SLAVE_ENABLE and 
               self.Amplifiers.EthernetTarget.feedback == EnumStatusMessages.DONE):
                print("TrackamplifiersEnable --> EXEC_MBUS_STATE_SLAVE_ENABLE --> done.\n")
                self.Amplifiers.EthernetTarget.ClearOldData()                
                self.TrackamplifiersEnable = 0                
                return EnumStateMachine.ok        
    
        return EnumStateMachine.busy
    
    
    
#    ######################################################################################
#    #
#    # config the track amplifiers
#    #
#    ######################################################################################    
#    def InitTrackamplifiers(self):
#        '''
#        case 0
#        '''
#        if(self.RunInitSlave == 0):
#            
#            if(self.ProgramSlave > 10):
#                print("RunInitSlave --> TRACKBACKPLANE1 finished.")
#                self.RunInitSlave += 1
#                self.ShiftSlot = 0
#                return EnumStateMachine.busy
#            
#            elif(self.ConfigureSlave(EnumSlaveInit.TRACKBACKPLANE1, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave, False) == EnumStateMachine.ok):
#                print("RunInitSlave --> slave " + str(self.ProgramSlave) + " got initialized.\n\r\n\r")
#                self.ShiftSlot    += 1 
#                self.ProgramSlave += 1
#                return EnumStateMachine.busy
#        
#        '''
#        case 1
#        '''
#        if(self.RunInitSlave == 1):
#            
#            if(self.ProgramSlave > 20):
#                print("RunInitSlave --> TRACKBACKPLANE2 finished.")
#                self.RunInitSlave += 1
#                self.ShiftSlot = 0
#                return EnumStateMachine.busy
#            
#            elif(self.ConfigureSlave(EnumSlaveInit.TRACKBACKPLANE2, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave, False) == EnumStateMachine.ok):
#                print("RunInitSlave --> slave " + str(self.ProgramSlave) + " got initialized.\n\r\n\r")
#                self.ShiftSlot    += 1 
#                self.ProgramSlave += 1
#                return EnumStateMachine.busy
#        
#        '''
#        case 2
#        '''        
#        if(self.RunInitSlave == 2):
#            
#            if(self.ProgramSlave > 30):
#                print("RunInitSlave --> TRACKBACKPLANE3 finished.")
#                self.RunInitSlave += 1
#                self.ShiftSlot = 0
#                return EnumStateMachine.busy
#            
#            elif(self.ConfigureSlave(EnumSlaveInit.TRACKBACKPLANE3, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave, False) == EnumStateMachine.ok):
#                print("RunInitSlave --> slave " + str(self.ProgramSlave) + " got initialized.\n\r\n\r")
#                self.ShiftSlot    += 1 
#                self.ProgramSlave += 1
#                return EnumStateMachine.busy
#  
#        '''
#        case 3
#        '''
#        if(self.RunInitSlave == 3):
#            
#            if(self.ProgramSlave > 40):
#                print("RunInitSlave --> TRACKBACKPLANE4 finished.")
#                self.RunInitSlave += 1
#                self.ShiftSlot = 0
#                return EnumStateMachine.busy
#            
#            elif(self.ConfigureSlave(EnumSlaveInit.TRACKBACKPLANE4, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave, False) == EnumStateMachine.ok):
#                print("RunInitSlave --> slave " + str(self.ProgramSlave) + " got initialized.\n\r\n\r")
#                self.ShiftSlot    += 1 
#                self.ProgramSlave += 1
#                return EnumStateMachine.busy
#        
#        '''
#        case 4
#        '''                        
#        if(self.RunInitSlave == 4):
#            
#            if(self.ProgramSlave > 49):
#                if(self.ConfigureSlave(EnumSlaveInit.TRACKBACKPLANE5, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave, True) == EnumStateMachine.ok):
#                    print("RunInitSlave --> slave " + str(self.ProgramSlave) + " got initialized.\n\r\n\r")
#                    print("RunInitSlave --> TRACKBACKPLANE5 finished.")
#                    self.RunInitSlave = 0
#                    self.ShiftSlot = 0
#                    self.ProgramSlave = 1
#                    return EnumStateMachine.ok
#            
#            elif(self.ConfigureSlave(EnumSlaveInit.TRACKBACKPLANE5, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave, False) == EnumStateMachine.ok):
#                print("RunInitSlave --> slave " + str(self.ProgramSlave) + " got initialized.\n\r\n\r")
#                self.ShiftSlot    += 1 
#                self.ProgramSlave += 1
#                return EnumStateMachine.busy
#            
#        return EnumStateMachine.busy
#
#   
#    ######################################################################################
#    #
#    # configuration sequencer
#    #
#    ######################################################################################       
#    def ConfigureSlave(self, TrackBackPlaneID, AmplifierLatchSet, TrackAmplifierId, Mode):
#        
#        '''
#        case 0
#        '''        
#        if(self.RunSlaveConfig == 0):
#            self.ModbusMaster.HoldingReg[1] = TrackBackPlaneID
#            self.ModbusMaster.HoldingReg[2] = AmplifierLatchSet
#            self.ModbusMaster.HoldingReg[3] = 0
#            self.ModbusMaster.HoldingReg[0] = EnumSlaveConfig.MODE_MAN & EnumSlaveConfig.WRITE & EnumSlaveConfig.HOLDINGREG & EnumSlaveConfig.EXEC
#            self.Amplifiers.WriteSerial(EnumCommand.MODBUS, self.ModbusMaster)
#            self.RunSlaveConfig += 1
#            print("RunSlaveConfig==0 --> TrackBackPlaneID: " + str(TrackBackPlaneID) + " AmplifierLatchSet: " + str(AmplifierLatchSet))
#            return EnumStateMachine.busy
#        
#        '''
#        case 1
#        '''        
#        if(self.RunSlaveConfig == 1):
#            if(self.Amplifiers.Trackamplifiers[0].InputReg[0] == EnumSlaveConfig.OK):
#                print("RunSlaveConfig==1 --> EnumSlaveConfig.OK")
#                self.ModbusMaster.HoldingReg[0] = EnumSlaveConfig.MODE_MAN & EnumSlaveConfig.WRITE & EnumSlaveConfig.HOLDINGREG & EnumSlaveConfig.HALT
#                self.Amplifiers.WriteSerial(EnumCommand.MODBUS, self.ModbusMaster)
#                self.RunSlaveConfig += 1
#                return EnumStateMachine.busy
#        
#        '''
#        case 2
#        '''        
#        if(self.RunSlaveConfig == 2):
#            if(self.Amplifiers.Trackamplifiers[0].InputReg[0] == EnumSlaveConfig.IDLE):
#                print("RunSlaveConfig==2 --> EnumSlaveConfig.IDLE")
#                self.RunSlaveConfig += 1
#                return EnumStateMachine.busy
#        
#        
#        '''
#        case 3
#        '''        
#        if(self.RunSlaveConfig == 3):
#            self.ModbusMaster.HoldingReg[1] = EnumSlaveInit.DEFAULTMODBUSADDR
#            self.ModbusMaster.HoldingReg[2] = TrackAmplifierId
#            self.ModbusMaster.HoldingReg[3] = 2
#            self.ModbusMaster.HoldingReg[0] = EnumSlaveConfig.MODE_MAN & EnumSlaveConfig.WRITE & EnumSlaveConfig.HOLDINGREG & EnumSlaveConfig.EXEC
#            self.Amplifiers.WriteSerial(EnumCommand.MODBUS, self.ModbusMaster)
#            self.RunSlaveConfig += 1
#            print("RunSlaveConfig==3 --> Set Amplifier ID: " + str(TrackAmplifierId))
#            return EnumStateMachine.busy
#        
#        '''
#        case 4
#        '''        
#        if(self.RunSlaveConfig == 4):
#            if(self.Amplifiers.Trackamplifiers[0].InputReg[0] == EnumSlaveConfig.OK):
#                print("RunSlaveConfig==4 --> EnumSlaveConfig.OK")
#                self.ModbusMaster.HoldingReg[0] = EnumSlaveConfig.MODE_MAN & EnumSlaveConfig.WRITE & EnumSlaveConfig.HOLDINGREG & EnumSlaveConfig.HALT
#                self.Amplifiers.WriteSerial(EnumCommand.MODBUS, self.ModbusMaster)
#                self.RunSlaveConfig += 1
#                return EnumStateMachine.busy
#        
#        '''
#        case 5
#        '''        
#        if(self.RunSlaveConfig == 5):
#            if(self.Amplifiers.Trackamplifiers[0].InputReg[0] == EnumSlaveConfig.IDLE):
#                print("RunSlaveConfig==5 --> EnumSlaveConfig.IDLE")
#                self.RunSlaveConfig += 1
#                return EnumStateMachine.busy        
#        
#        
#        '''
#        case 6
#        '''        
#        if(self.RunSlaveConfig == 6):
#            self.ModbusMaster.HoldingReg[1] = TrackBackPlaneID
#            self.ModbusMaster.HoldingReg[2] = 0xFFFF;
#            self.ModbusMaster.HoldingReg[3] = 0
#            self.ModbusMaster.HoldingReg[0] = EnumSlaveConfig.MODE_MAN & EnumSlaveConfig.WRITE & EnumSlaveConfig.HOLDINGREG & EnumSlaveConfig.EXEC
#            self.Amplifiers.WriteSerial(EnumCommand.MODBUS, self.ModbusMaster)
#            self.RunSlaveConfig += 1
#            print("RunSlaveConfig==6 --> TrackBackPlaneID: " + str(TrackBackPlaneID) + " AmplifierLatchSet: Release amplifier select line")
#            return EnumStateMachine.busy
#        
#        '''
#        case 7
#        '''        
#        if(self.RunSlaveConfig == 7):
#            if(self.Amplifiers.Trackamplifiers[0].InputReg[0] == EnumSlaveConfig.OK):
#                print("RunSlaveConfig==7 --> EnumSlaveConfig.OK")
#                self.ModbusMaster.HoldingReg[0] = EnumSlaveConfig.MODE_MAN & EnumSlaveConfig.WRITE & EnumSlaveConfig.HOLDINGREG & EnumSlaveConfig.HALT
#                self.Amplifiers.WriteSerial(EnumCommand.MODBUS, self.ModbusMaster)
#                self.RunSlaveConfig += 1
#                return EnumStateMachine.busy
#        
#        '''
#        case 8
#        '''        
#        if(self.RunSlaveConfig == 8):
#            if(self.Amplifiers.Trackamplifiers[0].InputReg[0] == EnumSlaveConfig.IDLE):
#                print("RunSlaveConfig==8 --> EnumSlaveConfig.IDLE")
#                if(Mode == True):
#                    self.ModbusMaster.HoldingReg[0] = EnumSlaveConfig.MODE_AUTO & EnumSlaveConfig.WRITE & EnumSlaveConfig.HOLDINGREG & EnumSlaveConfig.HALT
#                self.RunSlaveConfig = 0
#                return EnumStateMachine.ok        
#            
#        return EnumStateMachine.busy
#    
#    
#    ######################################################################################
#    #
#    # Enable the track amplifiers
#    #
#    ######################################################################################    
#    def EnableTrackamplifiers(self):
#        
#        '''
#        case 0
#        '''        
#        if(self.EnableSlaveConfig == 0):
#            self.ModbusMaster.HoldingReg[1] = 0
#            self.ModbusMaster.HoldingReg[2] = 0x8000
#            self.ModbusMaster.HoldingReg[3] = 1
#            self.ModbusMaster.HoldingReg[0] = EnumSlaveConfig.MODE_AUTO & EnumSlaveConfig.WRITE & EnumSlaveConfig.HOLDINGREG & EnumSlaveConfig.EXEC
#            self.Amplifiers.WriteSerial(EnumCommand.MODBUS, self.ModbusMaster)
#            self.EnableSlaveConfig += 1
#            print("EnableSlaveConfig==0 --> Set all amplifiers enabled")
#            return EnumStateMachine.busy
#        
#        '''
#        case 1
#        '''        
#        if(self.EnableSlaveConfig == 1):
#            if(self.Amplifiers.Trackamplifiers[0].InputReg[0] == EnumSlaveConfig.OK):
#                print("EnableSlaveConfig==1 --> EnumSlaveConfig.OK")
#                self.ModbusMaster.HoldingReg[0] = EnumSlaveConfig.MODE_AUTO & EnumSlaveConfig.WRITE & EnumSlaveConfig.HOLDINGREG & EnumSlaveConfig.HALT
#                self.Amplifiers.WriteSerial(EnumCommand.MODBUS, self.ModbusMaster)
#                self.EnableSlaveConfig += 1
#                return EnumStateMachine.busy
#        
#        '''
#        case 2
#        '''        
#        if(self.EnableSlaveConfig == 2):
#            if(self.Amplifiers.Trackamplifiers[0].InputReg[0] == EnumSlaveConfig.IDLE):
#                self.Amplifiers.WriteSerial(EnumCommand.ETHERNET_T, EnumEthernetT.INIT_PHASE_FALSE)
#                print("EnableSlaveConfig==2 --> EnumSlaveConfig.IDLE")
#                print("EnableSlaveConfig --> done")
#                print("EnableSlaveConfig --> INIT_PHASE_TRUE\n\r\n\r")
#                self.EnableSlaveConfig = 0
#                return EnumStateMachine.ok        
#    
#        return EnumStateMachine.busy
#    
#    
#    ######################################################################################
#    #
#    # Check track amplifiers software
#    #
#    ######################################################################################        
#        
#    def CheckAmpSwVersion(self):
#        '''
#        case 0
#        '''
#        if(self.AmpSwCheck == 0):
#            
#            if(self.ProgramSlave > 10):
#                print("AmpSwCheck --> TRACKBACKPLANE1 finished.")
#                self.AmpSwCheck += 1
#                self.ShiftSlot = 0
#                return EnumStateMachine.busy
#            
#            elif(self.SwVersionReadSlave(EnumSlaveInit.TRACKBACKPLANE1, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave) == EnumStateMachine.ok):
#                print("AmpSwCheck --> slave " + str(self.ProgramSlave) + " got initialized.\n\r\n\r")
#                self.ShiftSlot    += 1 
#                self.ProgramSlave += 1
#                return EnumStateMachine.busy
#        
#        '''
#        case 1
#        '''
#        if(self.AmpSwCheck == 1):
#            
#            if(self.ProgramSlave > 20):
#                print("AmpSwCheck --> TRACKBACKPLANE2 finished.")
#                self.AmpSwCheck += 1
#                self.ShiftSlot = 0
#                return EnumStateMachine.busy
#            
#            elif(self.SwVersionReadSlave(EnumSlaveInit.TRACKBACKPLANE2, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave) == EnumStateMachine.ok):
#                print("AmpSwCheck --> slave " + str(self.ProgramSlave) + " got initialized.\n\r\n\r")
#                self.ShiftSlot    += 1 
#                self.ProgramSlave += 1
#                return EnumStateMachine.busy
#        
#        '''
#        case 2
#        '''        
#        if(self.AmpSwCheck == 2):
#            
#            if(self.ProgramSlave > 30):
#                print("AmpSwCheck --> TRACKBACKPLANE3 finished.")
#                self.AmpSwCheck += 1
#                self.ShiftSlot = 0
#                return EnumStateMachine.busy
#            
#            elif(self.SwVersionReadSlave(EnumSlaveInit.TRACKBACKPLANE3, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave) == EnumStateMachine.ok):
#                print("AmpSwCheck --> slave " + str(self.ProgramSlave) + " got initialized.\n\r\n\r")
#                self.ShiftSlot    += 1 
#                self.ProgramSlave += 1
#                return EnumStateMachine.busy
#  
#        '''
#        case 3
#        '''
#        if(self.AmpSwCheck == 3):
#            
#            if(self.ProgramSlave > 40):
#                print("AmpSwCheck --> TRACKBACKPLANE4 finished.")
#                self.AmpSwCheck += 1
#                self.ShiftSlot = 0
#                return EnumStateMachine.busy
#            
#            elif(self.SwVersionReadSlave(EnumSlaveInit.TRACKBACKPLANE4, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave) == EnumStateMachine.ok):
#                print("AmpSwCheck --> slave " + str(self.ProgramSlave) + " got initialized.\n\r\n\r")
#                self.ShiftSlot    += 1 
#                self.ProgramSlave += 1
#                return EnumStateMachine.busy
#        
#        '''
#        case 4
#        '''                        
#        if(self.AmpSwCheck == 4):
#            
#            if(self.ProgramSlave > 50):                
#                print("AmpSwCheck --> TRACKBACKPLANE5 finished.\n")
#                self.AmpSwCheck = 0
#                self.ShiftSlot = 0
#                self.ProgramSlave = 1
#                return EnumStateMachine.ok
#            
#            elif(self.SwVersionReadSlave(EnumSlaveInit.TRACKBACKPLANE5, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave) == EnumStateMachine.ok):
#                print("AmpSwCheck --> slave " + str(self.ProgramSlave) + " got initialized.\n")
#                self.ShiftSlot    += 1 
#                self.ProgramSlave += 1
#                return EnumStateMachine.busy
#            
#        return EnumStateMachine.busy
#    
#    
#    ######################################################################################
#    #
#    # SW configuration checker
#    #
#    ######################################################################################       
#    def SwVersionReadSlave(self, TrackBackPlaneID, AmplifierLatchSet, TrackAmplifierId):
#        
#        '''
#        case 0
#        '''        
#        if(self.RunSwSlaveConfig == 0):
#            self.ModbusMaster.HoldingReg[1] = TrackBackPlaneID
#            self.ModbusMaster.HoldingReg[2] = AmplifierLatchSet
#            self.ModbusMaster.HoldingReg[3] = 0
#            self.ModbusMaster.HoldingReg[0] = EnumSlaveConfig.MODE_MAN & EnumSlaveConfig.WRITE & EnumSlaveConfig.HOLDINGREG & EnumSlaveConfig.EXEC
#            self.Amplifiers.WriteSerial(EnumCommand.MODBUS, self.ModbusMaster)
#            self.RunSwSlaveConfig += 1
#            print("RunSwSlaveConfig==0 --> TrackBackPlaneID: " + str(TrackBackPlaneID) + " AmplifierLatchSet: " + str(AmplifierLatchSet))
#            return EnumStateMachine.busy
#        
#        '''
#        case 1
#        '''        
#        if(self.RunSwSlaveConfig == 1):
#            if(self.Amplifiers.Trackamplifiers[0].InputReg[0] == EnumSlaveConfig.OK):
#                print("RunSwSlaveConfig==1 --> EnumSlaveConfig.OK")
#                self.ModbusMaster.HoldingReg[0] = EnumSlaveConfig.MODE_MAN & EnumSlaveConfig.WRITE & EnumSlaveConfig.HOLDINGREG & EnumSlaveConfig.HALT
#                self.Amplifiers.WriteSerial(EnumCommand.MODBUS, self.ModbusMaster)
#                self.RunSwSlaveConfig += 1
#                return EnumStateMachine.busy
#        
#        '''
#        case 2
#        '''        
#        if(self.RunSwSlaveConfig == 2):
#            if(self.Amplifiers.Trackamplifiers[0].InputReg[0] == EnumSlaveConfig.IDLE):
#                print("RunSwSlaveConfig==2 --> EnumSlaveConfig.IDLE")
#                self.RunSwSlaveConfig += 1
#                return EnumStateMachine.busy
#        
#        
#        '''
#        case 3
#        '''        
#        if(self.RunSwSlaveConfig == 3):
#            self.ModbusMaster.HoldingReg[1] = EnumSlaveInit.DEFAULTMODBUSADDR
#            self.ModbusMaster.HoldingReg[2] = 1 # 1 register to read
#            self.ModbusMaster.HoldingReg[3] = 5 # register address
#            self.ModbusMaster.HoldingReg[0] = EnumSlaveConfig.MODE_MAN & EnumSlaveConfig.READ & EnumSlaveConfig.INPUTREG & EnumSlaveConfig.EXEC
#            self.Amplifiers.WriteSerial(EnumCommand.MODBUS, self.ModbusMaster)
#            self.RunSwSlaveConfig += 1
#            print("RunSwSlaveConfig==3 --> Read input register: " + str(self.ModbusMaster.HoldingReg[3]))
#            return EnumStateMachine.busy
#        
#        '''
#        case 4
#        '''        
#        if(self.RunSwSlaveConfig == 4):
#            if(self.Amplifiers.Trackamplifiers[0].InputReg[0] == EnumSlaveConfig.OK):
#                self.TrackAmpUpateList.append([TrackAmplifierId,self.Amplifiers.Trackamplifiers[0].InputReg[1]])
#                #self.TrackAmpUpateList.append([self.Amplifiers.Trackamplifiers[0].InputReg[1]])
#                print("RunSwSlaveConfig==6 --> TrackAmp " + str(TrackAmplifierId) + " has checksum " + str(hex(self.Amplifiers.Trackamplifiers[0].InputReg[1])))
#                print("RunSwSlaveConfig==4 --> EnumSlaveConfig.OK")
#                self.ModbusMaster.HoldingReg[0] = EnumSlaveConfig.MODE_MAN & EnumSlaveConfig.WRITE & EnumSlaveConfig.HOLDINGREG & EnumSlaveConfig.HALT
#                self.Amplifiers.WriteSerial(EnumCommand.MODBUS, self.ModbusMaster)
#                self.RunSwSlaveConfig += 1
#                return EnumStateMachine.busy
#        
#        '''
#        case 5
#        '''        
#        if(self.RunSwSlaveConfig == 5):
#            if(self.Amplifiers.Trackamplifiers[0].InputReg[0] == EnumSlaveConfig.IDLE):
#                print("RunSwSlaveConfig==5 --> EnumSlaveConfig.IDLE")
#                self.RunSwSlaveConfig += 1
#                return EnumStateMachine.busy        
#        
#        
#        '''
#        case 6
#        '''        
#        if(self.RunSwSlaveConfig == 6):
#            self.ModbusMaster.HoldingReg[1] = TrackBackPlaneID
#            self.ModbusMaster.HoldingReg[2] = 0xFFFF;
#            self.ModbusMaster.HoldingReg[3] = 0
#            self.ModbusMaster.HoldingReg[0] = EnumSlaveConfig.MODE_MAN & EnumSlaveConfig.WRITE & EnumSlaveConfig.HOLDINGREG & EnumSlaveConfig.EXEC
#            self.Amplifiers.WriteSerial(EnumCommand.MODBUS, self.ModbusMaster)
#            self.RunSwSlaveConfig += 1
#            print("RunSwSlaveConfig==6 --> TrackBackPlaneID: " + str(TrackBackPlaneID) + " AmplifierLatchSet: Release amplifier select line")
#            return EnumStateMachine.busy
#        
#        '''
#        case 7
#        '''        
#        if(self.RunSwSlaveConfig == 7):
#            if(self.Amplifiers.Trackamplifiers[0].InputReg[0] == EnumSlaveConfig.OK):
#                print("RunSwSlaveConfig==7 --> EnumSlaveConfig.OK")
#                self.ModbusMaster.HoldingReg[0] = EnumSlaveConfig.MODE_MAN & EnumSlaveConfig.WRITE & EnumSlaveConfig.HOLDINGREG & EnumSlaveConfig.HALT
#                self.Amplifiers.WriteSerial(EnumCommand.MODBUS, self.ModbusMaster)
#                self.RunSwSlaveConfig += 1
#                return EnumStateMachine.busy
#        
#        '''
#        case 8
#        '''        
#        if(self.RunSwSlaveConfig == 8):
#            if(self.Amplifiers.Trackamplifiers[0].InputReg[0] == EnumSlaveConfig.IDLE):
#                print("RunSwSlaveConfig==8 --> EnumSlaveConfig.IDLE")                
#                self.RunSwSlaveConfig = 0
#                return EnumStateMachine.ok        
#            
#        return EnumStateMachine.busy  
#    
#    
#    ######################################################################################
#    #
#    # Flash new SW into Trackamplifier
#    #
#    ######################################################################################       
#    def FlashTrackamplifiers(self):
#        
#        '''
#        case 0
#        '''
#        if(self.FlashNewSwHandler == 0):
#            
#            if(self.ProgramSlave > 10):
#                print("FlashNewSwHandler --> TRACKBACKPLANE1 finished.")
#                self.FlashNewSwHandler += 1
#                self.ShiftSlot = 0
#                return EnumStateMachine.busy
#            
#            elif(self.FlashCaller(EnumSlaveInit.TRACKBACKPLANE1, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave) == EnumStateMachine.ok):
#                print("FlashNewSwHandler --> slave " + str(self.ProgramSlave) + " got initialized.\n\r\n\r")
#                self.ShiftSlot    += 1 
#                self.ProgramSlave += 1
#                return EnumStateMachine.busy
#        
#        '''
#        case 1
#        '''
#        if(self.FlashNewSwHandler == 1):
#            
#            if(self.ProgramSlave > 20):
#                print("FlashNewSwHandler --> TRACKBACKPLANE2 finished.")
#                self.FlashNewSwHandler += 1
#                self.ShiftSlot = 0
#                return EnumStateMachine.busy
#            
#            elif(self.FlashCaller(EnumSlaveInit.TRACKBACKPLANE2, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave) == EnumStateMachine.ok):
#                print("FlashNewSwHandler --> slave " + str(self.ProgramSlave) + " got initialized.\n\r\n\r")
#                self.ShiftSlot    += 1 
#                self.ProgramSlave += 1
#                return EnumStateMachine.busy
#        
#        '''
#        case 2
#        '''        
#        if(self.FlashNewSwHandler == 2):
#            
#            if(self.ProgramSlave > 30):
#                print("FlashNewSwHandler --> TRACKBACKPLANE3 finished.")
#                self.FlashNewSwHandler += 1
#                self.ShiftSlot = 0
#                return EnumStateMachine.busy
#            
#            elif(self.FlashCaller(EnumSlaveInit.TRACKBACKPLANE3, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave) == EnumStateMachine.ok):
#                print("FlashNewSwHandler --> slave " + str(self.ProgramSlave) + " got initialized.\n\r\n\r")
#                self.ShiftSlot    += 1 
#                self.ProgramSlave += 1
#                return EnumStateMachine.busy
#  
#        '''
#        case 3
#        '''
#        if(self.FlashNewSwHandler == 3):
#            
#            if(self.ProgramSlave > 40):
#                print("FlashNewSwHandler --> TRACKBACKPLANE4 finished.")
#                self.FlashNewSwHandler += 1
#                self.ShiftSlot = 0
#                return EnumStateMachine.busy
#            
#            elif(self.FlashCaller(EnumSlaveInit.TRACKBACKPLANE4, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave) == EnumStateMachine.ok):
#                print("FlashNewSwHandler --> slave " + str(self.ProgramSlave) + " got initialized.\n\r\n\r")
#                self.ShiftSlot    += 1 
#                self.ProgramSlave += 1
#                return EnumStateMachine.busy
#        
#        '''
#        case 4
#        '''                        
#        if(self.FlashNewSwHandler == 4):
#            
#            if(self.ProgramSlave > 50):                
#                print("FlashNewSwHandler --> TRACKBACKPLANE5 finished.")
#                self.FlashNewSwHandler = 0
#                self.ShiftSlot = 0
#                self.ProgramSlave = 1
#                return EnumStateMachine.ok
#            
#            elif(self.FlashCaller(EnumSlaveInit.TRACKBACKPLANE5, (EnumSlaveInit.SLOT1 << self.ShiftSlot), self.ProgramSlave) == EnumStateMachine.ok):
#                print("FlashNewSwHandler --> slave " + str(self.ProgramSlave) + " got initialized.\n\r\n\r")
#                self.ShiftSlot    += 1 
#                self.ProgramSlave += 1
#                return EnumStateMachine.busy
#            
#        return EnumStateMachine.busy
#
#    ######################################################################################
#    #
#    # SW flash caller
#    #
#    ######################################################################################       
#    def FlashCaller(self, TrackBackPlaneID, AmplifierLatchSet, TrackAmplifierId):
#        
#        '''
#        case 0
#        '''        
#        if(self.RunSwFlash == 0):
#            
#            if(self.file_checksum == 0):
#                returncode, self.file_checksum = self.Bootloader.GetFileCheckSum(self.bootloader_offset, self.program_mem_size)
#                if(returncode != EnumBootloader.COMMAND_SUCCESSFUL):
#                    print("Getting Checksum from file failed!!!!\n")
#            
#            if(self.TrackAmpUpateList[TrackAmplifierId - 1][1] == self.file_checksum):
#                print("RunSwFlash==0 --> TrackAmplifierId: " + str(TrackAmplifierId) + " checksum = " + str(hex(self.TrackAmpUpateList[TrackAmplifierId - 1][1])) + " file checksum = " + str(hex(self.file_checksum)) + "\n\r")
#                return EnumStateMachine.ok
#            
#            print("RunSwFlash==0 --> TrackAmplifierId: " + str(TrackAmplifierId) + " checksum = " + str(hex(self.TrackAmpUpateList[TrackAmplifierId - 1][1])) + " file checksum = " + str(hex(self.file_checksum)) + " starting flash")
#            
#            self.Amplifiers.WriteSerial(EnumCommand.ETHERNET_T, EnumEthernetT.ResetAll)
#            time.sleep(0.01)
#            
#            self.ModbusMaster.HoldingReg[1] = TrackBackPlaneID
#            self.ModbusMaster.HoldingReg[2] = AmplifierLatchSet
#            self.ModbusMaster.HoldingReg[3] = 0
#            self.ModbusMaster.HoldingReg[0] = EnumSlaveConfig.MODE_MAN & EnumSlaveConfig.WRITE & EnumSlaveConfig.HOLDINGREG & EnumSlaveConfig.EXEC
#            self.Amplifiers.WriteSerial(EnumCommand.MODBUS, self.ModbusMaster)
#            self.RunSwFlash += 1
#            print("RunSwFlash==0 --> TrackBackPlaneID: " + str(TrackBackPlaneID) + " AmplifierLatchSet: " + str(AmplifierLatchSet))
#            return EnumStateMachine.busy
#        
#        '''
#        case 1
#        '''        
#        if(self.RunSwFlash == 1):
#            if(self.Amplifiers.Trackamplifiers[0].InputReg[0] == EnumSlaveConfig.OK):
#                print("RunSwFlash==1 --> EnumSlaveConfig.OK")
#                self.ModbusMaster.HoldingReg[0] = EnumSlaveConfig.MODE_MAN & EnumSlaveConfig.WRITE & EnumSlaveConfig.HOLDINGREG & EnumSlaveConfig.HALT
#                self.Amplifiers.WriteSerial(EnumCommand.MODBUS, self.ModbusMaster)
#                self.RunSwFlash += 1
#                return EnumStateMachine.busy
#        
#        '''
#        case 2
#        '''        
#        if(self.RunSwFlash == 2):
#            if(self.Amplifiers.Trackamplifiers[0].InputReg[0] == EnumSlaveConfig.IDLE):
#                print("RunSwFlash==2 --> EnumSlaveConfig.IDLE")
#                self.RunSwFlash += 1
#                return EnumStateMachine.busy
#        
#        
#        '''
#        case 3
#        '''        
#        if(self.RunSwFlash == 3):
#            self.ModbusMaster.HoldingReg[1] = 0
#            self.ModbusMaster.HoldingReg[2] = 0
#            self.ModbusMaster.HoldingReg[3] = 0
#            self.ModbusMaster.HoldingReg[0] = EnumSlaveConfig.MODE_MAN & EnumSlaveConfig.READ & EnumSlaveConfig.BOOTLOAD & EnumSlaveConfig.EXEC
#            self.Amplifiers.WriteSerial(EnumCommand.MODBUS, self.ModbusMaster)
#            time.sleep(0.5)
#            self.RunSwFlash += 1
#            print("RunSwFlash==3 --> Set ModbusMaster in silent (bootloader) mode")
#            return EnumStateMachine.busy
#        
#        '''
#        case 4
#        '''        
#        if(self.RunSwFlash == 4):
#            print("RunSwFlash==4 --> Set Ethernet Target in Bootloader communication mode")
#            self.Amplifiers.WriteSerial(EnumCommand.ETHERNET_T, EnumEthernetT.SEND_BOOTLOADER)
#            self.RunSwFlash += 1
#            return EnumStateMachine.busy
#        
#        '''
#        case 5
#        '''        
#        if(self.RunSwFlash == 5):
#            #print("RunSwFlash==1 --> Starting bootloader flash program.")
#            self.Bootloader.FlashAuto()
#            #print("RunSwFlash==1 --> bootloader flash program done.")            
#            self.DataCommunication.WriteUDP(EnumCommand.ETHERNET_T, EnumEthernetT.ResetAll)
#            time.sleep(0.5) 
#            self.RunSwFlash = 0
#            return EnumStateMachine.ok
#        
#        return EnumStateMachine.busy          