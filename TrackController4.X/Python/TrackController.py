#!/usr/bin/env python
import time
import struct
from Comm import DataAquisition, ModBusMasterReg
from StateMachines import State


class MAIN():
    def __init__(self):
        self.Amplifiers = DataAquisition(51)
        self.ModbusMaster = ModBusMasterReg()
        self.StateMachine = State()
        self.cnt = 0        

    def start(self):
        while True:
            
            #time.sleep(0.001)
            self.cnt = self.cnt + 1
            
            self.Amplifiers.ReadSerial()
            
            #print(Amplifiers.Trackamplifiers[1].MbReceiveCounter, "\n",)
            
            if (self.cnt > 5000):
                print("Send data to PIC")
                TX = struct.pack("<10B", 0xAA, 1, 2, 3, 4, 5, 6, 7, 8, 0x55)
                self.Amplifiers.WriteSerial(TX)
                self.cnt = 0    




if __name__ == '__main__':
    # execute only if run as the entry point into the program
    main = MAIN()
    main.start()