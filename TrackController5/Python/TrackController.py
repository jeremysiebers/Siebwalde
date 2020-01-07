#!/usr/bin/env python
import time
import struct
from Comm import DataAquisition
from StateMachines import State
from Enum import *

class MAIN():
    def __init__(self):
        self.Amplifiers = DataAquisition(56)
        self.StateMachine = State(self.Amplifiers)
        self.cnt = 0  
        self.state = EnumStateMachine.ConnectToEthernetTarget
        self.UpdateTickCount = 0

    def start(self):
        while True:
            self.Amplifiers.ReadSerial() 
            
            ######################
            ## Connect          ##
            ######################
            if(self.state == EnumStateMachine.ConnectToEthernetTarget):
                returned = self.StateMachine.RunFunction(EnumStateMachine.ConnectToEthernetTarget)
                if(returned == EnumStateMachine.ok):
                    self.state = EnumStateMachine.ResetAllSlaves            
            
            ######################
            ## Reset the slaves ##
            ######################
            elif(self.state == EnumStateMachine.ResetAllSlaves):
                returned = self.StateMachine.RunFunction(EnumStateMachine.ResetAllSlaves)
                if(returned == EnumStateMachine.ok):
                    self.state = EnumStateMachine.DataUploadStart
            
            ######################
            ## DataUploadStart  ##
            ######################
            elif(self.state == EnumStateMachine.DataUploadStart):
                returned = self.StateMachine.RunFunction(EnumStateMachine.DataUploadStart)
                if(returned == EnumStateMachine.ok):
                    self.state = EnumStateMachine.DetectSlaves
            
            ######################
            ## Detect the slaves##
            ######################
            elif(self.state == EnumStateMachine.DetectSlaves):
                returned = self.StateMachine.RunFunction(EnumStateMachine.DetectSlaves)
                if(returned == EnumStateMachine.ok):
                    self.state = EnumStateMachine.FlashTrackamplifiers
            
            ######################
            ## Flash new SW     ##
            ######################        
            elif(self.state == EnumStateMachine.FlashTrackamplifiers):
                returned = self.StateMachine.RunFunction(EnumStateMachine.FlashTrackamplifiers)
                if(returned == EnumStateMachine.ok):
                    self.state = EnumStateMachine.InitTrackamplifiers
                elif(returned == EnumStateMachine.nok):
                    while True:
                        pass
            
            ######################
            ## Init  the slaves ##
            ######################
            elif(self.state == EnumStateMachine.InitTrackamplifiers):
                returned = self.StateMachine.RunFunction(EnumStateMachine.InitTrackamplifiers)
                if(returned == EnumStateMachine.ok):
                    self.state = EnumStateMachine.EnableTrackamplifiers
            
            #######################
            ## Enable the slaves ##
            #######################
            elif(self.state == EnumStateMachine.EnableTrackamplifiers):
                returned = self.StateMachine.RunFunction(EnumStateMachine.EnableTrackamplifiers)
                if(returned == EnumStateMachine.ok):
                    self.state = EnumStateMachine.run  





if __name__ == '__main__':
    # execute only if run as the entry point into the program
    main = MAIN()
    main.start()