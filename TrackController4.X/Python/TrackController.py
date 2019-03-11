#!/usr/bin/env python
import time
import struct
from Comm import DataAquisition
from StateMachines import State
from Enum import *

class MAIN():
    def __init__(self):
        self.Amplifiers = DataAquisition(51)
        self.StateMachine = State(self.Amplifiers)
        self.cnt = 0  
        self.state = EnumStateMachine.ResetAllSlaves
        self.UpdateTickCount = 0

    def start(self):
        while True:
            self.Amplifiers.ReadSerial()
            
            '''
            ##################################
            ## Wait till all I/O is updated ##
            ##################################            
            if(self.Amplifiers.UpdateTick):               
                self.Amplifiers.UpdateTick = False
                self.UpdateTickCount += 1
                
            if(self.UpdateTickCount > 0):
                self.UpdateTickCount = 0                
            '''    
            ######################
            ## Reset the slaves ##
            ######################
            if(self.state == EnumStateMachine.ResetAllSlaves):
                returned = self.StateMachine.RunFunction(EnumStateMachine.ResetAllSlaves)
                if(returned == EnumStateMachine.ok):
                    self.state = EnumStateMachine.CheckAmpSwVersion
            
            ######################
            ## Check SW version ##
            ######################        
            elif(self.state == EnumStateMachine.CheckAmpSwVersion):
                returned = self.StateMachine.RunFunction(EnumStateMachine.CheckAmpSwVersion)
                if(returned == EnumStateMachine.ok):
                    self.state = EnumStateMachine.FlashTrackamplifiers
            
            ######################
            ## Flash new SW     ##
            ######################        
            elif(self.state == EnumStateMachine.FlashTrackamplifiers):
                returned = self.StateMachine.RunFunction(EnumStateMachine.FlashTrackamplifiers)
                if(returned == EnumStateMachine.ok):
                    self.state = EnumStateMachine.InitTrackamplifiers
            
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