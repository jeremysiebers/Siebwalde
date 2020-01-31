from __future__ import print_function
import serial
import struct
import thread
import threading
import time
import Queue
import copy

class TrackAmplifier:
    def __init__(self):
        self.SlaveNumber         = 0
        self.HoldingReg          = [0,0,0,0]
        self.InputReg            = [0,0,0,0,0,0,]
        self.DiagReg             = [0,0]
        self.MbReceiveCounter    = 0
        self.MbSentCounter       = 0
        self.MbCommError         = 0
        self.SpiCommErrorCounter = 0



class DataAquisition:
    def __init__(self):
        self.ser = serial.Serial()
        self.ser.baudrate = 520832
        self.ser.port = 'COM3'
        self.ser.open()        
        self._Trackamplifier = TrackAmplifier()
        self.line            = ''
        self.run             = True
        self.header          = '\xaa'
        self.header_index    = 0
        self.footer          = 'U'
        self.message_found   = False
        self.data            = []
        self.queue           = Queue.Queue()
                
        
        thread = threading.Thread(target=self.ReadSerial, args=())
        thread.daemon = True                            # Daemonize thread
        thread.start()                                  # Start the execution        
    
    

    def GetData(self):
        return self.Trackamplifiers

    def StartSerialReadThread(self):
        threading.Thread(target=self._SerialRead()).start()
            
    def StopSerialReadThread(self):
        self.thread1.stop()    
    
    def WriteSerial(self, tx):
        self.ser.write(tx)
    
    def ReadSerial(self):
        while True:
            
            self.line += self.ser.read(40)
            
            while (len(self.line) > 35):        
            
                self.header_index = self.line.find(self.header)
                
                while (self.message_found == False):
                
                    if(self.header_index != 0):
                        self.line = self.line[self.header_index:]
                        print ("header found at " + str(self.header_index))
                    
                    self.message_found = True       
                
                if(len(self.line) > 35):
                    
                    self.data = struct.unpack ("<2B4H6H2H2HBBHB", self.line[:35])
                    if(self.data[0] == 170 and self.data[19] == 85):
                        self._Trackamplifier.SlaveNumber          = self.data[1]
                        self._Trackamplifier.HoldingReg[0]        = self.data[2]
                        self._Trackamplifier.HoldingReg[1]        = self.data[3]
                        self._Trackamplifier.HoldingReg[2]        = self.data[4]
                        self._Trackamplifier.HoldingReg[3]        = self.data[5]
                        self._Trackamplifier.InputReg[0]          = self.data[6]
                        self._Trackamplifier.InputReg[1]          = self.data[7]
                        self._Trackamplifier.InputReg[2]          = self.data[8]
                        self._Trackamplifier.InputReg[3]          = self.data[9]
                        self._Trackamplifier.InputReg[4]          = self.data[10]
                        self._Trackamplifier.InputReg[5]          = self.data[11]
                        self._Trackamplifier.DiagReg[0]           = self.data[12]
                        self._Trackamplifier.DiagReg[1]           = self.data[13]                
                        self._Trackamplifier.MbReceiveCounter     = self.data[14]
                        self._Trackamplifier.MbSentCounter        = self.data[15]
                        self._Trackamplifier.MbCommError          = self.data[16]
                        self._Trackamplifier.MbExceptionCode      = self.data[17]
                        self._Trackamplifier.SpiCommErrorCounter  = self.data[18]
                        self.queue.put(self._Trackamplifier)
                        print(str(self._Trackamplifier.SlaveNumber))
                    else:
                        print ('Bad data received!\n')
                        
                    self.line = self.line[35:]
                
                self.message_found = False



class ModBusMasterReg:
    def __init__(self):        
        self.HoldingReg          = [0,0,0,0]

class State:
    def RunFunction(self, argument):
        function = getattr(self, argument, lambda: "function does not exist")
        return function()
    
    def init(self):
        print ("Init called")
        return "ok"
    
    def check_sw_version(self):
        print("Check SW version called")
        return "ok"




def main():
    AmountOfAmplifiers = 51
    #q = Queue.Queue()
    Trackamplifiers = list()
    for i in range(AmountOfAmplifiers):
        Trackamplifiers.append(TrackAmplifier())    
    
    Data = DataAquisition()
    ModbusMaster = ModBusMasterReg()
    StateMachine = State()
    cnt = 0
        
    while True:
        
        
        #time.sleep(0.001)
        #cnt = cnt + 1
        
        if( not Data.queue.empty()):
            SlaveData = Data.queue.get() 
            Data.queue.task_done()
            SlaveNumber = int(SlaveData.SlaveNumber)
            Trackamplifiers[SlaveNumber] = copy.copy(SlaveData)
            print("Received SlaveData for slave: " + str(SlaveNumber) + "\n")
        
        #print(Trackamplifiers[1].MbReceiveCounter, "\n",)
        '''
        if (cnt == 500):
            #print("Send data to PIC")
            TX = struct.pack("<10B", 0xAA, 1, 2, 3, 4, 5, 6, 7, 8, 0x55)
            Data.WriteSerial(TX)
            cnt = 0
        '''    


if __name__ == '__main__':
    # execute only if run as the entry point into the program
    main()