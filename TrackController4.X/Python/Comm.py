import serial
import struct

ser = serial.Serial()
ser.baudrate = 520832
ser.port = 'COM3'
ser.open()

run = True
s = []

# wait until we see 0xAA so we are in sync with whatever is coming from the micro
b = [100]
while (b[0] != 170):
    c = ser.readline()
    b = struct.unpack("B", c[0])

# now we are in sync, start collecting data
while run:

    for c in ser.readline():
        b=struct.unpack ("B", c[0])
        s.append(b)
        if (b[0] == 0x85):
            print("bla")
            


'''            
typedef struct
{
    unsigned char       Header;
    unsigned char       SlaveNumber;
    unsigned int        HoldingReg[4];
    unsigned int        InputReg[6];
    unsigned int        DiagReg[2];
    unsigned int        MbReceiveCounter;
    unsigned int        MbSentCounter;
    SLAVE_DATA          MbCommError;
    unsigned char       MbExceptionCode;
    unsigned int        SpiCommErrorCounter;
    unsigned char       Footer;
    
}SLAVE_INFO;
'''