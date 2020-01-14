from Enum import *
from Comm import DataAquisition, ModBusMasterReg
from bootload import BootLoader
import time
import struct

class State:
    def __init__(self, amplifiers):
        self.Amplifiers             = amplifiers
        self.Bootloader             = BootLoader(self.Amplifiers)
                
        self.RunResetAll            = 0        
        self.ConnectToEth           = 0
        self.SlaveDetect            = 0
        self.UploadData             = 0
        self.TrackamplifiersInit    = 0
        self.TrackamplifiersEnable  = 0
        self.FlashNewSwHandler      = 0
        
        self.file_checksum          = 0
        self.bootloader_offset      = 0x800
        self.program_mem_size       = 0x8000
        self.ByteArray              = []
        self.HexRowWidth            = 16 #bytes
        self.ProcessLines           = int((self.program_mem_size - self.bootloader_offset) / self.HexRowWidth)
        self.jumpsize               = 4
        self.iteration              = self.ProcessLines - self.jumpsize
        self.leftover               = self.ProcessLines % self.jumpsize
        self.Count                  = 0  
        self.ConfigDataArray        = []
        self.FwFlashRequired        = 0
        
        self.start_time             = 0
      
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
            if(self.Amplifiers.EthernetTarget.taskid == EnumTaskId.CONTROLLER and self.Amplifiers.EthernetTarget.taskstate == EnumTaskStates.DONE and 
               self.Amplifiers.EthernetTarget.taskcommand == EnumTaskStates.CONNECTED):
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
            if(self.Amplifiers.EthernetTarget.taskid == EnumTaskId.MBUS and self.Amplifiers.EthernetTarget.taskstate == EnumTaskStates.DONE and 
               self.Amplifiers.EthernetTarget.taskcommand == EnumMbusStatus.MBUS_STATE_RESET):
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
            if(self.Amplifiers.EthernetTarget.taskid == EnumTaskId.MBUS and self.Amplifiers.EthernetTarget.taskstate == EnumTaskStates.DONE and 
               self.Amplifiers.EthernetTarget.taskcommand == EnumMbusStatus.MBUS_STATE_SLAVES_ON):
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
            if(self.Amplifiers.EthernetTarget.taskid == EnumTaskId.MBUS and self.Amplifiers.EthernetTarget.taskstate == EnumTaskStates.DONE and 
               self.Amplifiers.EthernetTarget.taskcommand == EnumMbusStatus.MBUS_STATE_START_DATA_UPLOAD):
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
            if(self.Amplifiers.EthernetTarget.taskid == EnumTaskId.MBUS and self.Amplifiers.EthernetTarget.taskstate == EnumTaskStates.DONE and 
               self.Amplifiers.EthernetTarget.taskcommand == EnumMbusStatus.MBUS_STATE_SLAVE_DETECT):
                print("DetectSlaves --> EXEC_MBUS_STATE_SLAVE_DETECT --> done.\n")
                self.Amplifiers.EthernetTarget.ClearOldData() 
                self.start_time = time.time()
                self.SlaveDetect += 1                
                return EnumStateMachine.busy        
    
        '''
        case 2
        '''
        if(self.SlaveDetect == 2):
            if((time.time() - self.start_time) > 1):    # wait some time so that the detection data is transferred to the client from the ethernet target.
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
            if(self.Amplifiers.EthernetTarget.taskid == EnumTaskId.MBUS and self.Amplifiers.EthernetTarget.taskstate == EnumTaskStates.DONE and 
               self.Amplifiers.EthernetTarget.taskcommand == EnumMbusStatus.MBUS_STATE_SLAVE_INIT):
                print("TrackamplifiersInit --> EXEC_MBUS_STATE_SLAVE_INIT --> done.\n")
                self.Amplifiers.EthernetTarget.ClearOldData()                
                self.TrackamplifiersInit = 0                
                return EnumStateMachine.ok        
    
        return EnumStateMachine.busy    
    
    
    ######################################################################################
    #
    # Enable the slaves
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
            if(self.Amplifiers.EthernetTarget.taskid == EnumTaskId.MBUS and self.Amplifiers.EthernetTarget.taskstate == EnumTaskStates.DONE and 
               self.Amplifiers.EthernetTarget.taskcommand == EnumMbusStatus.MBUS_STATE_SLAVE_ENABLE):
                print("TrackamplifiersEnable --> EXEC_MBUS_STATE_SLAVE_ENABLE --> done.\n")
                self.Amplifiers.EthernetTarget.ClearOldData()                
                self.TrackamplifiersEnable = 0                
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
            
            self.Count = 0
            
            if(self.file_checksum == 0):
                returncode, self.file_checksum = self.Bootloader.GetFileCheckSum(self.bootloader_offset, self.program_mem_size)
                if(returncode != EnumBootloader.COMMAND_SUCCESSFUL):
                    print("Getting Checksum from file failed!!!!\n")
                    return EnumStateMachine.nok
            
            self.FwFlashRequired = 0
            
            for x in range(1, 51):
                if ((self.Amplifiers.Trackamplifiers[x].HoldingReg[11] != self.file_checksum) and 
                    (self.Amplifiers.Trackamplifiers[x].SlaveDetected == True)):
                    self.FwFlashRequired += 1
                    
            if(self.FwFlashRequired > 0):
                print("FlashTrackamplifiers " + str(self.FwFlashRequired) + " slaves require flashing!!\n")
                self.ByteArray = self.Bootloader.ReadHexFileToBuf(self.bootloader_offset, self.program_mem_size)
                self.FlashNewSwHandler += 1
                self.Amplifiers.WriteSerial(EnumCommand.ETHERNET_T, EnumCommand.EXEC_MBUS_STATE_SLAVE_FW_FLASH, 0)                
                print("FlashTrackamplifiers --> EXEC_MBUS_STATE_SLAVE_FW_FLASH")
                return EnumStateMachine.busy
            else:
                self.FlashNewSwHandler = 0
                return EnumStateMachine.ok
        
        '''
        case 1
        '''
        if(self.FlashNewSwHandler == 1):
            if(self.Amplifiers.EthernetTarget.taskid      == EnumTaskId.FWHANDLER and 
               self.Amplifiers.EthernetTarget.taskstate   == EnumTaskStates.CONNECTED and 
               self.Amplifiers.EthernetTarget.taskcommand == EnumCommand.FWHANDLERINIT):
                
                print("FlashTrackamplifiers --> EXEC_MBUS_STATE_SLAVE_FW_FLASH --> connected.")
                self.Amplifiers.EthernetTarget.ClearOldData()
                self.Amplifiers.WriteSerial(EnumCommand.ETHERNET_T, EnumCommand.EXEC_FW_STATE_RECEIVE_FW_FILE, 0)
                self.FlashNewSwHandler += 1
                return EnumStateMachine.busy            
            

        '''
        case 2
        '''
        if(self.FlashNewSwHandler == 2):
            if(self.Amplifiers.EthernetTarget.taskid   == EnumCommand.FWFILEDOWNLOAD and 
            self.Amplifiers.EthernetTarget.taskstate   == EnumTaskStates.DONE and 
            self.Amplifiers.EthernetTarget.taskcommand == EnumCommand.FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY):
                
                print("FlashTrackamplifiers --> FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY --> waiting for data.")
                self.Amplifiers.EthernetTarget.ClearOldData()
                self.FlashNewSwHandler += 1                
                return EnumStateMachine.busy
        
        '''
        case 3
        '''
        if(self.FlashNewSwHandler == 3):
            if( self.Count > self.iteration):
                print("FlashTrackamplifiers --> EXEC_FW_STATE_RECEIVE_FW_FILE done.")
                self.Count = 0
                self.FlashNewSwHandler = 5
                return EnumStateMachine.busy
            else:
                
                data = struct.pack("<2B", 0xAA, EnumCommand.FILEDOWNLOAD_STATE_FW_DATA_RECEIVE)
                
                for j in range(self.Count, (self.Count + self.jumpsize)):
                    #print("j = " + str(j))
                    for val in self.ByteArray[j][1]:
                        #print (str(val))
                        data += struct.pack('<B', val)
                
                self.Amplifiers.WriteSerial(EnumCommand.ETHERNET_T, 0, data)
                
                self.Count += self.jumpsize
                
                self.FlashNewSwHandler = 4
            
            return EnumStateMachine.busy
            
        '''
        case 4
        '''
        if(self.FlashNewSwHandler == 4):            
                if( self.Amplifiers.EthernetTarget.taskid      == EnumCommand.FWFILEDOWNLOAD and 
                   (self.Amplifiers.EthernetTarget.taskcommand == EnumCommand.FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY or 
                    self.Amplifiers.EthernetTarget.taskcommand == EnumCommand.FILEDOWNLOAD_STATE_FW_DATA_DOWNLOAD_DONE) and                     
                    self.Amplifiers.EthernetTarget.taskstate   == EnumTaskStates.DONE):
                    
                    #print("FlashTrackamplifiers --> EXEC_FW_STATE_RECEIVE_FW_FILE_STANDBY --> self.Count = " + str(self.Count) + ".")
                    self.Amplifiers.EthernetTarget.ClearOldData()
                    self.FlashNewSwHandler = 3
        
        
        '''
        case 5
        '''
        if(self.FlashNewSwHandler == 5):
                if(self.Amplifiers.EthernetTarget.taskid      == EnumCommand.FWFILEDOWNLOAD and 
                   self.Amplifiers.EthernetTarget.taskcommand == EnumCommand.FILEDOWNLOAD_STATE_FW_CHECKSUM and
                   self.Amplifiers.EthernetTarget.taskstate   == EnumTaskStates.DONE and 
                   self.Amplifiers.EthernetTarget.taskmessage == EnumTaskMessages.RECEIVED_CHECKSUM_OK):                    
                    
                    print("FlashTrackamplifiers --> FILEDOWNLOAD_STATE_FW_CHECKSUM --> RECEIVED_CHECKSUM_OK.\n")
                    self.Amplifiers.EthernetTarget.ClearOldData()
                    self.FlashNewSwHandler = 6               
                    return EnumStateMachine.busy
                
                elif(self.Amplifiers.EthernetTarget.taskid      == EnumCommand.FWFILEDOWNLOAD and 
                     self.Amplifiers.EthernetTarget.taskcommand == EnumCommand.FILEDOWNLOAD_STATE_FW_CHECKSUM and
                     self.Amplifiers.EthernetTarget.taskstate   == EnumTaskStates.DONE and 
                     self.Amplifiers.EthernetTarget.taskmessage == EnumTaskMessages.RECEIVED_CHECKSUM_NOK):
                    
                    print("FlashTrackamplifiers --> FILEDOWNLOAD_STATE_FW_CHECKSUM --> RECEIVED_CHECKSUM_NOK, try again.")
                    self.Amplifiers.EthernetTarget.ClearOldData()
                    self.Amplifiers.WriteSerial(EnumCommand.ETHERNET_T, EnumCommand.EXEC_FW_STATE_RECEIVE_FW_FILE, 0)
                    self.FlashNewSwHandler = 2               
                    return EnumStateMachine.busy
                
        '''
        case 6
        '''
        if(self.FlashNewSwHandler == 6):            
            if(self.Amplifiers.EthernetTarget.taskid == EnumTaskId.FWHANDLER and 
               self.Amplifiers.EthernetTarget.taskstate == EnumTaskStates.DONE and 
               self.Amplifiers.EthernetTarget.taskcommand == EnumCommand.EXEC_FW_STATE_RECEIVE_FW_FILE):
                
                print("FlashTrackamplifiers --> EXEC_FW_STATE_RECEIVE_FW_FILE done.")
                self.Amplifiers.EthernetTarget.ClearOldData()
                self.ConfigDataArray = self.Bootloader.GetConfigData()
                self.Bootloader.file_object.close()
                self.Amplifiers.WriteSerial(EnumCommand.ETHERNET_T, EnumCommand.EXEC_FW_STATE_RECEIVE_CONFIG_WORD, 0)
                print("FlashTrackamplifiers --> EXEC_FW_STATE_RECEIVE_CONFIG_WORD")
                self.FlashNewSwHandler += 1
                return EnumStateMachine.busy        
        
        '''
        case 7
        '''
        if(self.FlashNewSwHandler == 7):
            if(self.Amplifiers.EthernetTarget.taskid == EnumCommand.FWCONFIGWORDDOWNLOAD and 
               self.Amplifiers.EthernetTarget.taskstate == EnumTaskStates.DONE and 
               self.Amplifiers.EthernetTarget.taskcommand == EnumCommand.CONFIGWORDDOWNLOAD_STATE_RECEIVE_CONFIG_WORD_STANDBY):
                
                print("FlashTrackamplifiers --> CONFIGWORDDOWNLOAD_STATE_RECEIVE_CONFIG_WORD_STANDBY --> waiting for data.")
                self.Amplifiers.EthernetTarget.ClearOldData()
                self.FlashNewSwHandler += 1                
                return EnumStateMachine.busy              
        
        '''
        case 8
        '''
        if(self.FlashNewSwHandler == 8):
            data = struct.pack("<2B", 0xAA, EnumCommand.CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_RECEIVE)
            for val in self.ConfigDataArray[0]:
                #print (str(val))
                data += struct.pack('<B', val)
        
            self.Amplifiers.WriteSerial(EnumCommand.ETHERNET_T, 0, data)            
            self.FlashNewSwHandler += 1
            return EnumStateMachine.busy
        
        '''
        case 9
        '''
        if(self.FlashNewSwHandler == 9):            
            if(self.Amplifiers.EthernetTarget.taskid == EnumCommand.FWCONFIGWORDDOWNLOAD and 
               self.Amplifiers.EthernetTarget.taskstate == EnumTaskStates.DONE and 
               self.Amplifiers.EthernetTarget.taskcommand == EnumCommand.CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_DOWNLOAD_DONE):
                
                print("FlashTrackamplifiers --> CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_DOWNLOAD_DONE.")
                self.Amplifiers.EthernetTarget.ClearOldData()
                self.FlashNewSwHandler += 1
                return EnumStateMachine.busy
            
        '''
        case 10
        '''
        if(self.FlashNewSwHandler == 10):            
            if(self.Amplifiers.EthernetTarget.taskid == EnumTaskId.FWHANDLER and 
               self.Amplifiers.EthernetTarget.taskstate == EnumTaskStates.DONE and 
               self.Amplifiers.EthernetTarget.taskcommand == EnumCommand.EXEC_FW_STATE_RECEIVE_CONFIG_WORD):
                
                print("FlashTrackamplifiers --> EXEC_FW_STATE_RECEIVE_CONFIG_WORD done.")
                self.Amplifiers.EthernetTarget.ClearOldData()
                self.Amplifiers.WriteSerial(EnumCommand.ETHERNET_T, EnumCommand.EXEC_FW_STATE_FLASH_ALL_SLAVES, 0)
                self.start_time = time.time()
                print("FlashTrackamplifiers --> EXEC_FW_STATE_FLASH_ALL_SLAVES")
                self.FlashNewSwHandler += 1
                return EnumStateMachine.busy
            
        '''
        case 11
        '''
        if(self.FlashNewSwHandler == 11):            
            if(self.Amplifiers.EthernetTarget.taskid == EnumTaskId.FWHANDLER and 
               self.Amplifiers.EthernetTarget.taskstate == EnumTaskStates.DONE and 
               self.Amplifiers.EthernetTarget.taskcommand == EnumCommand.EXEC_FW_STATE_FLASH_ALL_SLAVES):
                
                elapsed_time = time.time() - self.start_time
                print("FlashTrackamplifiers --> EXEC_FW_STATE_FLASH_ALL_SLAVES done.")
                print("Flashing took: " + str('%.2f'% elapsed_time)  + " seconds, that is on average "+ str('%.2f'% (elapsed_time / self.FwFlashRequired)) + " seconds per slave.")
                self.Amplifiers.EthernetTarget.ClearOldData()
                return EnumStateMachine.ok

            elif(self.Amplifiers.EthernetTarget.taskid      != 0 and 
                 self.Amplifiers.EthernetTarget.taskstate   != 0 and 
                 self.Amplifiers.EthernetTarget.taskcommand != 0):
                print("FlashTrackamplifiers --> EXEC_FW_STATE_FLASH_ALL_SLAVES received message:")
                
                try:                    
                    name = next(name for name, value in vars(EnumCommand).items() if value == self.Amplifiers.EthernetTarget.taskid)
                    print("taskid               --> " +  name + ".")
                    name = next(name for name, value in vars(EnumCommand).items() if value == self.Amplifiers.EthernetTarget.taskcommand)
                    print("taskcommand          --> " +  name + ".")
                    name = next(name for name, value in vars(EnumTaskStates).items() if value == self.Amplifiers.EthernetTarget.taskstate)
                    print("taskstate            --> " +  name + ".")
                    name = next(name for name, value in vars(EnumTaskMessages).items() if value == self.Amplifiers.EthernetTarget.taskmessage)
                    print("taskmessage          --> " +  name + ".")
                    
                except:
                    print("taskid               --> " + str(self.Amplifiers.EthernetTarget.taskid)      + ".")
                    print("taskcommand          --> " + str(self.Amplifiers.EthernetTarget.taskcommand) + ".")
                    print("taskstate            --> " + str(self.Amplifiers.EthernetTarget.taskstate)   + ".")
                    print("taskmessage          --> " + str(self.Amplifiers.EthernetTarget.taskmessage) + ".")
                
                self.Amplifiers.EthernetTarget.ClearOldData()
                
        return EnumStateMachine.busy