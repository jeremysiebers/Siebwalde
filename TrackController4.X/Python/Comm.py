from __future__ import print_function
import serial
import struct

ser = serial.Serial()
ser.baudrate = 520832
ser.port = 'COM3'
ser.open()

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

Trackamplifiers = list()
for i in range(51):
    Trackamplifiers.append(TrackAmplifier())

# get in sync, wait for \n
run = True
while run:
    line = ser.read(1)
    data = struct.unpack ("<B", line)
    if (data[0] == 10):
        run = False
        print("Start of new message found!")

run = True
# start collecting data
while run:
    
    line = ser.read(36)
    
    if(len(line) == 36):
        data = struct.unpack ("<2B4H6H2H2HBBHBB", line)
        if(data[0] == 170 and data[19] == 85):
            Trackamplifiers[data[1]].SlaveNumber          = data[1]
            Trackamplifiers[data[1]].HoldingReg[0]        = data[2]
            Trackamplifiers[data[1]].HoldingReg[1]        = data[3]
            Trackamplifiers[data[1]].HoldingReg[2]        = data[4]
            Trackamplifiers[data[1]].HoldingReg[3]        = data[5]
            Trackamplifiers[data[1]].InputReg[0]          = data[6]
            Trackamplifiers[data[1]].InputReg[1]          = data[7]
            Trackamplifiers[data[1]].InputReg[2]          = data[8]
            Trackamplifiers[data[1]].InputReg[3]          = data[9]
            Trackamplifiers[data[1]].InputReg[4]          = data[10]
            Trackamplifiers[data[1]].InputReg[5]          = data[11]
            Trackamplifiers[data[1]].DiagReg[0]           = data[12]
            Trackamplifiers[data[1]].DiagReg[1]           = data[13]                
            Trackamplifiers[data[1]].MbReceiveCounter     = data[14]
            Trackamplifiers[data[1]].MbSentCounter        = data[15]
            Trackamplifiers[data[1]].MbCommError          = data[16]
            Trackamplifiers[data[1]].MbExceptionCode      = data[17]
            Trackamplifiers[data[1]].SpiCommErrorCounter  = data[18]
        else:
            print ('Bad data received!\n')
    else:
        print ('Received string length nok!', line , ' \n')    

        
        
        
        
