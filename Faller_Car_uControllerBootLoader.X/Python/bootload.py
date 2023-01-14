from __future__ import print_function
import serial   
import sys
import struct
import ctypes
import time
import progressbar

start_time = time.time()

COMMAND_SUCCESSFUL          = 0x01              # � Command Successful
COMMAND_UNSUPPORTED         = 0xFF              # � Command Unsupported
ERROR_ADDRESS_OUT_OF_RANGE  = 0XFE              # � Address Error
COMMAND_UNSUCCESSFUL        = 0xFD              # - Command unSuccessful

SERIALSPEED             = 115200                # Serial baudrate

BOOTLOADEROFFSET        = 0x520                 # PIC16 has 32 write latches hence it can handle data per 32 which means that the offset has to be a multiple of 32!
PICPROGRAMMEMSIZE       = 0x2000

WRITE_FLASH_BLOCKSIZE   = 0x20 #32
ERASE_FLASH_BLOCKSIZE   = 0x20 #32

try:
    file_object  = open("C:\\Users\\Jerem\\Siebwalde\\Faller_Car_ucontroller2.X\\dist\\Offset\\production\\Faller_Car_ucontroller2.X.production.hex", 'r')
except:
    print('failed to open file !!!! \n')
    exit()


# open the serial port
try:
    # open the com port
    ser = serial.Serial("COM5", SERIALSPEED, timeout = 300)
    
    # flush the buffer
    #r = self._ser.read ()
    #while (r != ''):
    #    r = self._ser.read ()
        
    # we should do some handshaking to make sure we are connected to HexaLog
    print("Port opened!\n")
except:
    print("Could not open serial port:", sys.exc_info ()[0])
    print("Make sure correct port is set and not occupied")
    print("")
    exit()
    
#---------------------------------------------------------------------------------------------------------------------------#

def GetBootloaderVersion(): 
    
    print("------------- Get bootloader version: -------------\n")

    ser.write(b'\x55\x00\x00\x00\x00\x00\x00\x00\x00\x00')
    
    rx = ser.read(26)
    received = []
    
    if(rx == ''):
        print('Get bootloader version nok, no answer from target!\n')
        return
    
    for ch in rx[0:26]:
        #print (ord(ch))
        #received.append(ord(ch))
        received.append(ch)
        
    print('Bootloader Version: ', hex(received[11]) , ' ', hex(received[10]), '\n')
    print('Max Packet size   : ', hex(received[13]) , ' ', hex(received[12]), '\n')
    print('Not Used          : ', hex(received[15]) , ' ', hex(received[14]), '\n')
    print('Device ID         : ', hex(received[17]) , ' ', hex(received[16]), '\n')
    print('Not Used          : ', hex(received[19]) , ' ', hex(received[18]), '\n')
    print('Erase Row Size    : ', hex(received[20]) , '\n')
    print('Write Latches     : ', hex(received[21]) , '\n')
    print('Config Words      : ', hex(received[25]) , ' ', hex(received[24]), ' ', hex(received[23]), ' ', hex(received[22]), '\n')
    

#---------------------------------------------------------------------------------------------------------------------------#
def EraseFlash(bootloader_offset, program_mem_size):
    
    print("------------- Erase Flash: ------------------------\n")
    print('Bootloader offset: ', hex(bootloader_offset), '\n')
    print('program Mem size: ', hex(program_mem_size), '\n')
    
    EraseFlash = 3    
    EraseRows = int((program_mem_size - bootloader_offset)/ERASE_FLASH_BLOCKSIZE)
    
    EraseRowsLOW = (EraseRows & 0x00FF)
    EraseRowsHIGH= (EraseRows >>8) & 0x00FF
    
    BootOffLOW  = (bootloader_offset & 0x00FF)
    BootOffHIGH = (bootloader_offset >>8) & 0x00FF
    
    tx = struct.pack('<10B', 0x55, EraseFlash, EraseRowsLOW, EraseRowsHIGH, 0x55, 0xAA, BootOffLOW, BootOffHIGH, 0x00, 0x00)
    
    #for ch in tx:
       #print ord(ch), ")",
    
    ser.write(tx)
    
    rx = ser.read(11)
    received = []
    
    if(rx == ''):
        print('Erase flash nok, no answer from target!\n')
        return(COMMAND_UNSUCCESSFUL)
    
    for ch in rx[0:26]:
        received.append(ch)
       
    SuccessCode = (received[9] << 8) + received[10]
        
    if(SuccessCode != COMMAND_SUCCESSFUL):
        if(SuccessCode == ERROR_ADDRESS_OUT_OF_RANGE):
            print('Erase flash nok, target returned ERROR_ADDRESS_OUT_OF_RANGE!\n')
        else:
            print('Erase flash nok, target returned:{}\n',SuccessCode)
        return(COMMAND_SUCCESSFUL)            
    
    print('Erase flash successful')
    
    return(COMMAND_SUCCESSFUL)

#---------------------------------------------------------------------------------------------------------------------------#
    
def WriteFlash(bootloader_offset, program_mem_size):
    
    print("------------- Write to flash started...------------\n")
    
    if (file_object == ''):
        print('Write flash nok, no file loaded/found!\n')
        return(COMMAND_UNSUCCESSFUL)
    
    HexRowWidth = WRITE_FLASH_BLOCKSIZE #words per line in hex file
    
    ByteArray = []
    ByteArrayChecksum = []
    
    ProcessLines = int((program_mem_size - bootloader_offset) / HexRowWidth)

    address = BOOTLOADEROFFSET
    
    for i in range(ProcessLines):        
        buff1 = file_object.readline()
        buff2 = file_object.readline()
        buff3 = file_object.readline()
        buff4 = file_object.readline()
        #address = buff1[3:7]
        #print(address)
        
        if(address < bootloader_offset):
            print('Write flash nok, first address to write is within bootloader block!\n')
            return(COMMAND_UNSUCCESSFUL)
        if(address > program_mem_size ): #since we are writing 32 words per time, hex file size is 2x longer on 16 words per row
            print('Write flash nok, address to write is greater then memory size!\n')
            return(COMMAND_UNSUCCESSFUL)
        
        data = buff1[9:41] + buff2[9:41] + buff3[9:41] + buff4[9:41]
        #ByteArray.append([bytearray.fromhex(address), bytearray.fromhex(data)])
        ByteArray.append([address, bytearray.fromhex(data)])
        ByteArrayChecksum.append([bytearray.fromhex(data)])

        address = address + WRITE_FLASH_BLOCKSIZE
    
    
    CalcChecksumFile = 0

    ByteArrayChecksum[ProcessLines - 1][0][63] = 0
    ByteArrayChecksum[ProcessLines - 1][0][62] = 0
    ByteArrayChecksum[ProcessLines - 1][0][61] = 0
    ByteArrayChecksum[ProcessLines - 1][0][60] = 0
        
    for j in range(ProcessLines):
        for x in range(0, 64, 2):
            CalcChecksumFile = ((ByteArrayChecksum[j][0][x] + (ByteArrayChecksum[j][0][x + 1] << 8)) + CalcChecksumFile) & 0xFFFF
    
    run = True
    jumpsize = 1
    iteration = ProcessLines - jumpsize
    leftover  = ProcessLines % jumpsize
    i = 0
    cmd_returnval = 0
    time_seg = (100.0 / (iteration/jumpsize))
    time_calc = 0
    bar = progressbar.ProgressBar(maxval=101, widgets=[progressbar.Bar('=', '[', ']'), ' ' , progressbar.Percentage()])
    bar.start()
    
    while (run == True):
        cmd_returnval = _WriteLinesOfFlash(i, jumpsize, ByteArray)
        if(cmd_returnval != COMMAND_SUCCESSFUL):
            print('Write flash nok received stopping write flash!\n')
            return COMMAND_UNSUCCESSFUL, 0          
        
        #print('>> Writing %d%%' % time_calc, end='\r')
        time_calc = time_calc + time_seg
        bar.update(time_calc)
                
        if( i == iteration):
            time.sleep(1)
            if(leftover == 0):
                run = False
                print('\nChecksum of sent data : ', hex(CalcChecksumFile))
                print('Write to flash successful, leftover == 0.\n')
                return COMMAND_SUCCESSFUL, CalcChecksumFile               
            else:
                if(_WriteLinesOfFlash(i, leftover, ByteArray)['cmd'] != COMMAND_SUCCESSFUL):
                    print('Write flash nok received, stopping write flash!\n')
                    return COMMAND_UNSUCCESSFUL, 0
                
                else:
                    run = False
                    print('Checksum of sent data : ', hex(CalcChecksumFile))
                    print('Write to flash successful\n')
                    return COMMAND_SUCCESSFUL, CalcChecksumFile

        i += jumpsize        
        
#---------------------------------------------------------------------------------------------------------------------------#
def _WriteLinesOfFlash(line, incr, array):
    
    WriteFlash = 2
    byteslength = incr * 64

    bla1 = (array[line][0] & 0xFF00) >> 8
    bla2 = array[line][0] & 0xFF
    
    tx = struct.pack('<6BBB2B', 0x55, WriteFlash, byteslength, 0, 0x55, 0xAA, (array[line][0] & 0xFF), ((array[line][0] & 0xFF00) >> 8), 0, 0)

    #print('line = ',str(line), '\n')

    for j in range(line, (line + incr)):
        for val in array[j][1]:
            #print (val)
            tx += struct.pack('<B', val)

    ser.write(tx)
    
    rx = ser.read(11)
    received = []
    
    if(rx == ''):
        print('Write flash nok, no answer from target!\n')
        return COMMAND_UNSUCCESSFUL
    
    for ch in rx[0:11]:
        received.append(ch)
    
    SuccessCode = (received[9] << 8) + received[10]
    
    #print('Success Code      : ', str(hex(SuccessCode)), '\n')
    
    if(SuccessCode != COMMAND_SUCCESSFUL):
        print('Write flash nok, target returned error on writing!\n')
        return COMMAND_UNSUCCESSFUL
    
    return COMMAND_SUCCESSFUL
    
#---------------------------------------------------------------------------------------------------------------------------#

def WriteConfig():
    
    print("------------- Write Config Words...----------------\n")
    
    ByteArray = []
    
    run = True
    
    while(run):
        
        buff = file_object.readline()
        config = buff[2:3]
        
        if(int(config, 16) == 0x0C):
            run = False
            
    data = buff[9:33]
    ByteArray.append([bytearray.fromhex(data)])
    
    WriteConfig = 7
    
    tx = struct.pack('<10B', 0x55, WriteConfig, 0x0C, 0x00, 0x55, 0xAA, 0x00, 0x00, 0x30, 0x00)
    
    for val in ByteArray[0][0]:
        #print val
        tx += struct.pack('<B', val)
    
    ser.write(tx)
    
    rx = ser.read(11)
    received = []
    
    if(rx == ''):
        print('Write flash nok, no answer from target!\n')
        return COMMAND_UNSUCCESSFUL
    
    for ch in rx[0:11]:
        received.append(ch)
    
    SuccessCode = (received[9] << 8) + received[10]
    
    #print('Success Code      : ', str(hex(SuccessCode)), '\n')
    
    if(SuccessCode != COMMAND_SUCCESSFUL):
        print('Write config nok, target returned error on writing!\n')
        return COMMAND_UNSUCCESSFUL
    
    print('Write config ok!\n')
    return COMMAND_SUCCESSFUL

#---------------------------------------------------------------------------------------------------------------------------#

def RequestChecksum(bootloader_offset, program_mem_size):
    
    print("------------- Request checksum...------------------\n")
    
    print('Bootloader offset: ', hex(bootloader_offset), '\n')
    print('program Mem size: ', hex(program_mem_size), '\n')
    
    DataLength = (program_mem_size - bootloader_offset - 2) * 2
    Checksum = 0x0000
    CalcChecksum = 8
    
    tx = struct.pack('<BBHBBBBBB', 0x55, CalcChecksum, DataLength, 0x00, 0x00, (bootloader_offset & 0xFF), ((bootloader_offset & 0xFF00) >> 8), 0x00, 0x00)
    
    ser.write(tx)
    
    rx = ser.read(12)
    received = []
    
    if(rx == ''):
        print('Erase flash nok, no answer from target!\n')
        return(COMMAND_UNSUCCESSFUL)
    
    for ch in rx[0:12]:
        received.append(ch)
       
    Checksum = (received[11] << 8) + received[10]
    
    print('Checksum received : ', hex(Checksum), '\n')
        
    return(Checksum)    

#---------------------------------------------------------------------------------------------------------------------------#

def ResetDevice():
    
    print("------------- Reset Target...----------------------\n")
    
    Reset = 9
    
    tx = struct.pack('<BBQ', 0x55, Reset, 0x00)
    
    ser.write(tx)
    
    rx = ser.read(11)
    received = []
    
    if(rx == ''):
        print('Reset target cmd readback nok, no answer from target!\n')
        return(COMMAND_UNSUCCESSFUL)
    
    print('Reset target sent!\n')
    return COMMAND_SUCCESSFUL    

#---------------------------------------------------------------------------------------------------------------------------#

cmd_returnval = 0
CalcChecksumFile = 0
ChecksumRequest = 0

GetBootloaderVersion()

if(EraseFlash(BOOTLOADEROFFSET, PICPROGRAMMEMSIZE) == COMMAND_SUCCESSFUL):    
    cmd_returnval, CalcChecksumFile = WriteFlash(BOOTLOADEROFFSET, PICPROGRAMMEMSIZE)
    if(cmd_returnval == COMMAND_SUCCESSFUL):
        ChecksumRequest = RequestChecksum(BOOTLOADEROFFSET, PICPROGRAMMEMSIZE)
        if(CalcChecksumFile == ChecksumRequest):
            print('Checksum match between file and PIC!\n')
            if(ResetDevice() == COMMAND_SUCCESSFUL):
                print("------------- Done...------------------------------\n")
                print('Programming device done closing program')
            else:
                print('Reset device error, closing program')
        else:
            print('Checksum mismatch between file and PIC, programming stopped!\n')

        print('Programming stopped!\n')
else:
    print('Programming stopped!\n')

file_object.close()

elapsed_time = time.time() - start_time
print('Programming time : ', str('%.2f'% elapsed_time) , 'seconds! \n')
