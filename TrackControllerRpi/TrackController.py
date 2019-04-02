#!/usr/bin/env python
import time
import struct
from Comm import DataAquisition
from StateMachines import State
from Enum import *
import sys
import threading
import time

#run on specific core : sudo taskset 0x00000004 sudo python3 "/path"
# /boot/cmdline.txt
# isolcpus=3
# sudo nice -n -20 taskset -c 3 python3 /home/pi/Downloads/TrackController5/TrackController.py


class MAIN():
    def __init__(self):
        print(sys.version)
        self.DataCommunication = DataAquisition(50)
        self.StateMachine = State(self.DataCommunication)
        self.cnt = 0  
        self.state = EnumStateMachine.ResetAllSlaves
        self.UpdateTickCount = 0
        self.init = True
        self.start_time = 0
        self.elapsed_time = 0
        self.UpdateRate = 0.0012
        self.t          = 0
        
    def start(self):
    
        #print(self.StateMachine.Bootloader.RequestChecksum(0x800, 0x8000))
    
        while self.init:
            self.start_time = time.time()  
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
                    self.state = EnumStateMachine.RunTrackamplifiers
            
                        
            #######################
            ## Run the slaves    ##
            #######################
            
            elif(self.state == EnumStateMachine.RunTrackamplifiers):
                returned = self.StateMachine.RunFunction(EnumStateMachine.RunTrackamplifiers)
                if(returned == EnumStateMachine.ok):
                    self.state = EnumStateMachine.RunTrackamplifiers
                
                
                    #self.init = False
                    #self.t = threading.Timer(self.UpdateRate, self.RunAuto)
                    #self.t.start()   
            
            self.elapsed_time = time.time() - self.start_time
            
            #print('Execution time = ', str('%.9f'% self.elapsed_time) , 'seconds! \n')  
            
            while(self.elapsed_time < self.UpdateRate):
                self.elapsed_time = time.time() - self.start_time
            
            #print('After waiting  = ', str('%.9f'% self.elapsed_time) , 'seconds! \n') 


    def RunAuto(self):
        self.StateMachine.RunFunction(EnumStateMachine.RunTrackamplifiers)
        self.t = threading.Timer(self.UpdateRate, self.RunAuto)
        self.t.start()


if __name__ == '__main__':
    # execute only if run as the entry point into the program
    main = MAIN()
    main.start()
