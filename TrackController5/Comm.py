from __future__ import print_function
from TemComIf import TemComIf
import socket
import struct
import time
import copy
from Enum import *
import serial

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

class Bootloader:
    def __init__(self):
        self.rx_data         = [] 


class DataAquisition:
    def __init__(self, AmountOfAmplifiers):
        self.UDP_IP_RECV = ''
        self.UDP_PORT_RECV = 65531  
        self.sock_recv = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.sock_recv.bind((self.UDP_IP_RECV, self.UDP_PORT_RECV)) 
        
        self.UDP_IP_TRANS = '192.168.1.95'
        self.UDP_PORT_TRANS = 60000
        self.sock_trans = socket.socket(socket.AF_INET, socket.SOCK_DGRAM,0)
        self.sock_trans.connect((self.UDP_IP_TRANS, self.UDP_PORT_TRANS))
                
        self.ModbusInterface = TemComIf ('COM3')
        
        self.Trackamplifiers = list()
        self.Bootloader      = Bootloader()
        self.EthernetTarget  = TrackAmplifier()
        self.line            = ''
        self.run             = True
        self.header          = '\xaa'
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
    
    def WriteUDP(self, command, data):
        
        if(command == EnumCommand.ETHERNET_T):
            tx = struct.pack("<4B", 0xAA, command, data, 0x55)
            self.sock_trans.send(tx)    
    
    def ReadUDP(self):
        
        self.line, addr = self.sock_recv.recvfrom(100)
        
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
                        
            if(len(self.line) > 36):
                # Check if data is amplifier data
                if (self.data[0] == 170 and self.data[1] == EnumCommand.MODBUS):
                    self.data = struct.unpack ("<4B4H6H2H2HBBHB", self.line[:37])
                    if(self.data[2] == 170 and self.data[21] == 85):
                        self.Trackamplifiers[self.data[3]].SlaveNumber          = self.data[3 ]
                        self.Trackamplifiers[self.data[3]].HoldingReg[0]        = self.data[4 ]
                        self.Trackamplifiers[self.data[3]].HoldingReg[1]        = self.data[5 ]
                        self.Trackamplifiers[self.data[3]].HoldingReg[2]        = self.data[6 ]
                        self.Trackamplifiers[self.data[3]].HoldingReg[3]        = self.data[7 ]
                        self.Trackamplifiers[self.data[3]].InputReg[0]          = self.data[8 ]
                        self.Trackamplifiers[self.data[3]].InputReg[1]          = self.data[9 ]
                        self.Trackamplifiers[self.data[3]].InputReg[2]          = self.data[10]
                        self.Trackamplifiers[self.data[3]].InputReg[3]          = self.data[11]
                        self.Trackamplifiers[self.data[3]].InputReg[4]          = self.data[12]
                        self.Trackamplifiers[self.data[3]].InputReg[5]          = self.data[13]
                        self.Trackamplifiers[self.data[3]].DiagReg[0]           = self.data[14]
                        self.Trackamplifiers[self.data[3]].DiagReg[1]           = self.data[15]                
                        self.Trackamplifiers[self.data[3]].MbReceiveCounter     = self.data[16]
                        self.Trackamplifiers[self.data[3]].MbSentCounter        = self.data[17]
                        self.Trackamplifiers[self.data[3]].MbCommError          = self.data[18]
                        self.Trackamplifiers[self.data[3]].MbExceptionCode      = self.data[19]
                        self.Trackamplifiers[self.data[3]].SpiCommErrorCounter  = self.data[20]
                        #print("data received for amp: " + str(self.data[1]) + "\n")
                    else:
                        print ('Bad data received!\n')
                        
                    self.line = self.line[37:]
                
                elif (self.data[0] == 170 and self.data[1] == EnumCommand.ETHERNET_T):
                    self.data = struct.unpack ("<4B4H6H2H2HBBHB", self.line[:37])
                    if(self.data[2] == 170 and self.data[21] == 85):
                        self.EthernetTarget.SlaveNumber          = self.data[3 ]
                        self.EthernetTarget.HoldingReg[0]        = self.data[4 ]
                        self.EthernetTarget.HoldingReg[1]        = self.data[5 ]
                        self.EthernetTarget.HoldingReg[2]        = self.data[6 ]
                        self.EthernetTarget.HoldingReg[3]        = self.data[7 ]
                        self.EthernetTarget.InputReg[0]          = self.data[8 ]
                        self.EthernetTarget.InputReg[1]          = self.data[9 ]
                        self.EthernetTarget.InputReg[2]          = self.data[10]
                        self.EthernetTarget.InputReg[3]          = self.data[11]
                        self.EthernetTarget.InputReg[4]          = self.data[12]
                        self.EthernetTarget.InputReg[5]          = self.data[13]
                        self.EthernetTarget.DiagReg[0]           = self.data[14]
                        self.EthernetTarget.DiagReg[1]           = self.data[15]                
                        self.EthernetTarget.MbReceiveCounter     = self.data[16]
                        self.EthernetTarget.MbSentCounter        = self.data[17]
                        self.EthernetTarget.MbCommError          = self.data[18]
                        self.EthernetTarget.MbExceptionCode      = self.data[19]
                        self.EthernetTarget.SpiCommErrorCounter  = self.data[20]
                        #print("data received for amp: " + str(self.data[1]) + "\n")
                        self.UpdateTick = not self.UpdateTick
                    else:
                        print ('Bad data received!\n')
                        
                    self.line = self.line[37:]
            
            if(len(self.line) > 36):    
                # Check if data is bootloader data
                if (self.data[0] == 170 and self.data[1] == EnumCommand.BOOTLOADER):
                    self.data = struct.unpack ("<37B", self.line[:37])
                    self.bootloader_data = copy.copy(self.data)
                    self.line = self.line[37:]
            
            else:
                self.line = self.line[37:]




class ModBusMasterReg:
    def __init__(self):        
        self.HoldingReg          = [0,0,0,0]