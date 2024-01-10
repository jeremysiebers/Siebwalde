from Enum import *
from Comm import DataAquisition
import time
import struct

class State:
    def __init__(self):

        self.RunResetAll            = 0        
        self.ConnectToEth           = 0
        self.dataAquisition         = DataAquisition()

        self.start_time             = 0
        self.loopcounter            = 1
        self.attemptmax             = 3
      
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
            self.dataAquisition.StartSerialReadThread()
            self.ConnectToEth += 1
            print("Connecting to: " + self.dataAquisition.IPAddr)
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