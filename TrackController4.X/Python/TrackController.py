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

    def start(self):
        while True:
            self.Amplifiers.ReadSerial()
            
            
            if(self.state == EnumStateMachine.ResetAllSlaves):
                returned = self.StateMachine.RunFunction(EnumStateMachine.ResetAllSlaves)
                if(returned == EnumStateMachine.ok):
                    self.state = EnumStateMachine.InitTrackamplifiers

            elif(self.state == EnumStateMachine.InitTrackamplifiers):
                returned = self.StateMachine.RunFunction(EnumStateMachine.InitTrackamplifiers)
                if(returned == EnumStateMachine.ok):
                    self.state = EnumStateMachine.run
             
            
            
            
            
            
            
            
            
            '''
            #time.sleep(0.001)
            self.cnt = self.cnt + 1
            
            #print(Amplifiers.Trackamplifiers[1].MbReceiveCounter, "\n",)
            
            if (self.cnt > 50):
                print("Send data to PIC")
                TX = struct.pack("<11B", 0xAA, 0, 1, 2, 3, 4, 5, 6, 7, 8, 0x55)
                self.Amplifiers.WriteSerial(TX)
                self.cnt = 0  
            '''




if __name__ == '__main__':
    # execute only if run as the entry point into the program
    main = MAIN()
    main.start()