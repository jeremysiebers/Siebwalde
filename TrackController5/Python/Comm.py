from __future__ import print_function
#import serial
import socket
import struct
import time
import copy
from Enum import *

class TrackAmplifier:
    def __init__(self):
        self.MbHeader            = 0
        self.SlaveNumber         = 0
        self.SlaveDetected       = 0
        self.HoldingReg          = [0,0,0,0,0,0,0,0,0,0,0,0]        
        self.MbReceiveCounter    = 0
        self.MbSentCounter       = 0
        self.MbCommError         = 0
        self.MbExceptionCode     = 0
        self.SpiCommErrorCounter = 0
        self.MbFooter            = 0

class EtherNetFeedBack:
    def __init__(self):
        self.taskid              = 0
        self.taskstate           = 0
        self.feedback            = 0
    
    def ClearOldData(self):
        self.taskid              = 0
        self.taskstate           = 0
        self.feedback            = 0        

class Bootloader:
    def __init__(self):
        self.rx_data         = [] 


class DataAquisition:
    def __init__(self, AmountOfAmplifiers):
        self.IPAddr = socket.gethostbyname('TRACKCONTROL') 
        
        self.UDP_IP_RECV = ''
        self.UDP_PORT_RECV = 10001 
        self.sock_recv = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.sock_recv.bind((self.UDP_IP_RECV, self.UDP_PORT_RECV)) 
        self.sock_recv.setblocking(0)
                
        self.UDP_IP_TRANS = self.IPAddr# '192.168.1.200'
        self.UDP_PORT_TRANS = 10000
        self.sock_trans = socket.socket(socket.AF_INET, socket.SOCK_DGRAM,0)
        self.sock_trans.bind(('0.0.0.0', self.UDP_PORT_TRANS))
        self.sock_trans.connect((self.UDP_IP_TRANS, self.UDP_PORT_TRANS))

        self.Trackamplifiers = list()
        self.Bootloader      = Bootloader()
        self.EthernetTarget  = EtherNetFeedBack()
        self.line            = 0
        self.run             = True
        self.header          = 170
        self.header_index    = 0
        self.footer          = 'U'
        self.message_found   = False
        self.data            = []
        self.bootloader_data = []
        self.TxData          = []
        self.UpdateTick      = False
        
        for i in range(AmountOfAmplifiers):
            self.Trackamplifiers.append(TrackAmplifier())
    

    def GetData(self):
        return self.Trackamplifiers

    def StartSerialReadThread(self):
        threading.Thread(target=self._SerialRead()).start()
            
    def StopSerialReadThread(self):
        self.thread1.stop()    
    
    def WriteSerial(self, function, command, data):
        
        if(function == EnumCommand.MODBUS):
            send = [0,0,0,0,0,0,0,0]
            j = 0
            k = 1
            for i in range(0,4,1):
                send[j] = (data.HoldingReg[i] & 0xFF)
                send[k] = ((data.HoldingReg[i] & 0xFF00) >> 8) 
                j += 2
                k += 2                
            
            tx = struct.pack("<11B", 0xAA, function, send[0], send[1], send[2], send[3], send[4], send[5], send[6], send[7], 0x55)
            self.sock_trans.send(tx)
        
        if(function == EnumCommand.ETHERNET_T):
            tx = struct.pack("<4B", 0xAA, command, data, 0x55)
            self.sock_trans.send(tx)
        
        if(function == EnumCommand.BOOTLOADER):
            tx = struct.pack("<2B", 0xAA, function)
            tx = tx + data
            self.sock_trans.send(tx)
            
        if(function == EnumCommand.DUMMY_CMD):
            self.sock_trans.send(data.encode())           
    
    
    def ReadSerial(self):
        
        try:
            self.line, addr = self.sock_recv.recvfrom(200)            
        except:
            pass
        
        try:
            while (len(self.line) > 0):
            
                self.header_index = self.line.find(self.header)
                            
                if(self.header_index != 0):
                    self.line = self.line[self.header_index:]
                    print ("header found at " + str(self.header_index))            
                
                try:
                    self.data = struct.unpack ("<4B", self.line[:4])
                except:
                    return
                
                #print(self.data[1])

                if(len(self.line) > 81):
                    # Check if data is amplifier data
                    if (self.data[0] == 170 and self.data[1] == EnumCommand.MODBUS):
                        self.data = struct.unpack ("<6B12H2HLBBB", self.line[:41]) #seems that from IPstack from PIC some padding is added after L!
                        if(self.data[2] == 170 and self.data[23] == 85):
                            self.Trackamplifiers[self.data[3]].MbHeader             = self.data[2 ]
                            self.Trackamplifiers[self.data[3]].SlaveNumber          = self.data[3 ]
                            self.Trackamplifiers[self.data[3]].SlaveDetected        = self.data[4 ]
                            self.Trackamplifiers[self.data[3]].HoldingReg[0]        = self.data[6 ]
                            self.Trackamplifiers[self.data[3]].HoldingReg[1]        = self.data[7 ]
                            self.Trackamplifiers[self.data[3]].HoldingReg[2]        = self.data[8 ]
                            self.Trackamplifiers[self.data[3]].HoldingReg[3]        = self.data[9 ]
                            self.Trackamplifiers[self.data[3]].HoldingReg[4]        = self.data[10]
                            self.Trackamplifiers[self.data[3]].HoldingReg[5]        = self.data[11]
                            self.Trackamplifiers[self.data[3]].HoldingReg[6]        = self.data[12]
                            self.Trackamplifiers[self.data[3]].HoldingReg[7]        = self.data[13]
                            self.Trackamplifiers[self.data[3]].HoldingReg[8]        = self.data[14]
                            self.Trackamplifiers[self.data[3]].HoldingReg[9]        = self.data[15]
                            self.Trackamplifiers[self.data[3]].HoldingReg[10]       = self.data[16]
                            self.Trackamplifiers[self.data[3]].HoldingReg[11]       = self.data[17]                
                            self.Trackamplifiers[self.data[3]].MbReceiveCounter     = self.data[18]
                            self.Trackamplifiers[self.data[3]].MbSentCounter        = self.data[19]
                            self.Trackamplifiers[self.data[3]].MbCommError          = self.data[20]
                            self.Trackamplifiers[self.data[3]].MbExceptionCode      = self.data[21]
                            self.Trackamplifiers[self.data[3]].SpiCommErrorCounter  = self.data[22]
                            self.Trackamplifiers[self.data[3]].MbFooter             = self.data[23]
                            #print("data received for amp: " + str(self.data[1]) + "\n")
                        else:
                            print ('Bad data received!\n')
                                    
                    elif (self.data[0] == 170 and self.data[1] == EnumCommand.BOOTLOADER):
                        self.data = struct.unpack ("<37B", self.line[:37])
                        self.bootloader_data = copy.copy(self.data)
                    
                    elif (self.data[0] == 170 and (self.data[1] == EnumStatusMessages.CONTROLLER or self.data[1] == EnumStatusMessages.MBUS)):
                        self.data = struct.unpack ("<4B", self.line[:4])
                        self.EthernetTarget.taskid               = self.data[1 ]
                        self.EthernetTarget.taskstate            = self.data[2 ]
                        self.EthernetTarget.feedback             = self.data[3 ]                        
                    
                    self.line = 0
                
                elif(len(self.line) == 4):
                    # Check if data command feedback
                    self.data = struct.unpack ("<4B", self.line[:4])
                    if(self.data[0] == 170):
                        self.EthernetTarget.taskid               = self.data[1 ]
                        self.EthernetTarget.taskstate            = self.data[2 ]
                        self.EthernetTarget.feedback             = self.data[3 ]
                    
                    self.line = 0
                
                else:
                    self.line = 0
    
        except:
            pass        




class ModBusMasterReg:
    def __init__(self):        
        self.HoldingReg          = [0,0,0,0]