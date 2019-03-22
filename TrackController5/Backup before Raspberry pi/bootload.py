from __future__ import print_function
from Enum import *   
import sys
import struct
import ctypes
import time

COMMAND_SUCCESSFUL   = 0x01 # Command Successful
COMMAND_UNSUPPORTED  = 0xFF # Command Unsupported
ADDRESS_ERROR        = 0Xfe # Address Error
COMMAND_UNSUCCESSFUL = 0xfd # Command unSuccessful 

try:
    file_object  = open("C:\GIT-REPOS\Siebwalde\TrackAmplifier4.X\dist\Offset\production\TrackAmplifier4.X.production.hex", 'r')
except:
    print('failed to open file !!!! \n')



class BootLoader:
    def __init__(self, data_communication):
        self.DataCommunication          = data_communication        
        self.GetBootloaderVersionState  = 0        
        self.EraseFlashState            = 0
        self.start_time                 = 0
        
        self.cmd_returnval              = 0
        self.CalcChecksumFile           = 0
        self.ChecksumRequest            = 0         

    
    #---------------------------------------------------------------------------------------------------------------------------#
    
    def GetFileCheckSum(self, bootloader_offset, program_mem_size):
        ByteArrayChecksum = []
        HexRowWidth = 16 #bytes
        ProcessLines = int(((program_mem_size - bootloader_offset) / HexRowWidth)  + 1)
        
        for i in range(ProcessLines):        
            buff = file_object.readline()
            address = buff[3:7]
            
            if(int(address, 16) != 0):                
                if(int(address, 16) < bootloader_offset):
                    print('Write flash nok, first address to write is within bootloader block!\n')
                    return(COMMAND_UNSUCCESSFUL)            
                if(int(address, 16) > program_mem_size):
                    print('Write flash nok, address to write is greater then memory size!\n')
                    return(COMMAND_UNSUCCESSFUL)
                
                data = buff[9:41]
                ByteArrayChecksum.append(bytearray.fromhex(data))
                #ByteArrayChecksum.append(data)
        
        CalcChecksumFile = 0
        
        ByteArrayChecksum[ProcessLines-2][14] = 0
        ByteArrayChecksum[ProcessLines-2][15] = 0
            
        for j in range(ProcessLines-1):
            for x in range(0, 16, 2):
                CalcChecksumFile = ((ByteArrayChecksum[j][x] + (ByteArrayChecksum[j][x + 1] << 8)) + CalcChecksumFile) & 0xFFFF            
        
        #CalcChecksumFile = ((CalcChecksumFile & 0xFF00) >> 8) + ((CalcChecksumFile & 0x00FF) << 8)
                
        return(EnumBootloader.COMMAND_SUCCESSFUL, CalcChecksumFile)
    
    #---------------------------------------------------------------------------------------------------------------------------#
    
    def GetBootloaderVersion(self): 
        
        if(self.GetBootloaderVersionState == 0):
            print("------------- Get bootloader version: -------------\n")        
            self.DataCommunication.WriteUDP(EnumCommand.BOOTLOADER , b'\x55\x00\x00\x00\x00\x00\x00\x00\x00\x00')
            self.GetBootloaderVersionState += 1
            return EnumStateMachine.busy
        
        
        received = []
        
        if(self.GetBootloaderVersionState == 1):
            if(self.DataCommunication.bootloader_data[0] != 0):
                for ch in self.DataCommunication.bootloader_data:
                    #print ord(ch), ")",
                    received.append(ord(ch))                
        
                print('Bootloader Version: ', hex(received[11]) , ' ', hex(received[10]), '\n')
                print('Max Packet size   : ', hex(received[13]) , ' ', hex(received[12]), '\n')
                print('Not Used          : ', hex(received[15]) , ' ', hex(received[14]), '\n')
                print('Device ID         : ', hex(received[17]) , ' ', hex(received[16]), '\n')
                print('Not Used          : ', hex(received[19]) , ' ', hex(received[18]), '\n')
                print('Erase Row Size    : ', hex(received[20]) , '\n')
                print('Write Latches     : ', hex(received[21]) , '\n')
                print('Config Words      : ', hex(received[25]) , ' ', hex(received[24]), ' ', hex(received[23]), ' ', hex(received[22]), '\n')
                
                self.GetBootloaderVersionState = 0
                return EnumStateMachine.ok
        
    
    #---------------------------------------------------------------------------------------------------------------------------#
    def EraseFlash(self, bootloader_offset, program_mem_size):
        
        self.start_time = time.time()
                   
        print("------------- Erase Flash: ------------------------\n")
        print('Bootloader offset: ', hex(bootloader_offset), '\n')
        print('program Mem size: ', hex(program_mem_size), '\n')
        
        EraseFlash = 3
        RowWidth = 128
        EraseRows = int((program_mem_size - bootloader_offset)/RowWidth)
        
        tx = struct.pack('<BBBBBBIBB', 0x55, EraseFlash, EraseRows, 0, 0x55, 0xAA, bootloader_offset, 0x00, 0x00)
        
        self.DataCommunication.ModbusInterface.WriteBootloadData(tx)
        
        rx = self.DataCommunication.ModbusInterface.ReadBootloadData(11)
        
        received = []
        
        if(rx == ''):
                print('Erase flash nok, no answer from target!\n')
                return(COMMAND_UNSUCCESSFUL)        

        SuccessCode = (rx[9] << 8) + rx[10]
            
        if(SuccessCode != EnumBootloader.COMMAND_SUCCESSFUL):
            print('Erase flash nok, target returned error on erasing!\n')
            return(EnumBootloader.COMMAND_UNSUCCESSFUL)            
        
        print('Erase flash successful')
        
        return(EnumBootloader.COMMAND_SUCCESSFUL)
    
    #---------------------------------------------------------------------------------------------------------------------------#
        
    def WriteFlash(self, bootloader_offset, program_mem_size):
        
        print("------------- Write to flash started...------------\n")
        
        if (file_object == ''):
            print('Write flash nok, no file loaded/found!\n')
            return(COMMAND_UNSUCCESSFUL)
        
        HexRowWidth = 16 #bytes
        
        ByteArray = []
        ByteArrayChecksum = []
        
        ProcessLines = int((program_mem_size - bootloader_offset) / HexRowWidth)
        
        file_object.seek(0)
        
        for i in range(ProcessLines):        
            buff = file_object.readline()
            address = buff[3:7]
            
            if(int(address, 16) < bootloader_offset):
                print('Write flash nok, first address to write is within bootloader block!\n')
                return(COMMAND_UNSUCCESSFUL)            
            if(int(address, 16) > program_mem_size):
                print('Write flash nok, address to write is greater then memory size!\n')
                return(COMMAND_UNSUCCESSFUL)
            
            data = buff[9:41]
            ByteArray.append([bytearray.fromhex(address), bytearray.fromhex(data)])
            ByteArrayChecksum.append([bytearray.fromhex(data)])
        
        
        CalcChecksumFile = 0
        
        ByteArrayChecksum[ProcessLines-1][0][14] = 0
        ByteArrayChecksum[ProcessLines-1][0][15] = 0
            
        for j in range(ProcessLines):
            for x in range(0, 16, 2):
                CalcChecksumFile = ((ByteArrayChecksum[j][0][x] + (ByteArrayChecksum[j][0][x + 1] << 8)) + CalcChecksumFile) & 0xFFFF
        
        run = True
        jumpsize = 4
        iteration = ProcessLines - jumpsize
        leftover  = ProcessLines % jumpsize
        i = 0
        cmd_returnval = 0        
                
        while (run == True):
            cmd_returnval = self._WriteLinesOfFlash(i, jumpsize, ByteArray)
            if(cmd_returnval != COMMAND_SUCCESSFUL):
                print('Write flash nok received stopping write flash!\n')
                return COMMAND_UNSUCCESSFUL, 0          
            
                                
            if( i == iteration):
                if(leftover == 0):
                    run = False
                    print('Checksum of sent data : ', hex(CalcChecksumFile))
                    print('Write to flash successful\n')
                    return COMMAND_SUCCESSFUL, CalcChecksumFile               
                else:
                    if(self._WriteLinesOfFlash(i, leftover, ByteArray)['cmd'] != COMMAND_SUCCESSFUL):
                        print('Write flash nok received, stopping write flash!\n')
                        return COMMAND_UNSUCCESSFUL, 0
                    
                    else:
                        run = False
                        print('Checksum of sent data : ', hex(CalcChecksumFile))
                        print('Write to flash successful\n')
                        return COMMAND_SUCCESSFUL, CalcChecksumFile
    
            i += jumpsize        
            
    #---------------------------------------------------------------------------------------------------------------------------#
    
    def _WriteLinesOfFlash(self,line, incr, array):
        
        WriteFlash = 2
        byteslength = incr * 16
        
        tx = struct.pack('<6BBB2B', 0x55, WriteFlash, byteslength, 0, 0x55, 0xAA, array[line][0][1], array[line][0][0], 0, 0)
        
        #print('line = '),str(line), '\n')
        
        for j in range(line, (line + incr)):
            for val in array[j][1]:
                #print val
                tx += struct.pack('<B', val)
    
        #print tx
        
        self.DataCommunication.ModbusInterface.WriteBootloadData(tx)
        
        rx = self.DataCommunication.ModbusInterface.ReadBootloadData(11)
                
        if(rx == ''):
            print('Write flash nok, no answer from target!\n')
            return COMMAND_UNSUCCESSFUL
        
        SuccessCode = (rx[9] << 8) + rx[10]
        
        #print('Success Code      : ', str(hex(SuccessCode)), '\n')
        
        if(SuccessCode != COMMAND_SUCCESSFUL):
            print('Write flash nok, target returned error on writing!\n')
            return COMMAND_UNSUCCESSFUL
        
        return COMMAND_SUCCESSFUL
        
    #---------------------------------------------------------------------------------------------------------------------------#
    
    def WriteConfig(self):
        
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
        
        self.DataCommunication.ModbusInterface.WriteBootloadData(tx)
        
        rx = self.DataCommunication.ModbusInterface.ReadBootloadData(11)
        
        if(rx == ''):
            print('Write flash nok, no answer from target!\n')
            return COMMAND_UNSUCCESSFUL
        
        SuccessCode = (rx[9] << 8) + rx[10]
        
        #print('Success Code      : ', str(hex(SuccessCode)), '\n')
        
        if(SuccessCode != COMMAND_SUCCESSFUL):
            print('Write config nok, target returned error on writing!\n')
            return COMMAND_UNSUCCESSFUL
        
        print('Write config ok!\n')
        return COMMAND_SUCCESSFUL
    
    #---------------------------------------------------------------------------------------------------------------------------#
    
    def RequestChecksum(self,bootloader_offset, program_mem_size):
        
        print("------------- Request checksum...------------------\n")
        
        print('Bootloader offset: ', hex(bootloader_offset), '\n')
        print('program Mem size: ', hex(program_mem_size), '\n')
        
        DataLength = program_mem_size - bootloader_offset - 2
        Checksum = 0x0000
        CalcChecksum = 8
        
        tx = struct.pack('<BBHBBHBB', 0x55, CalcChecksum, DataLength, 0x00, 0x00, bootloader_offset, 0x00, 0x00)
        
        self.DataCommunication.ModbusInterface.WriteBootloadData(tx)
        
        rx = self.DataCommunication.ModbusInterface.ReadBootloadData(12)
        
        if(rx == ''):
            print('Erase flash nok, no answer from target!\n')
            return(COMMAND_UNSUCCESSFUL)
        
        Checksum = (rx[11] << 8) + rx[10]
        
        print('Checksum received : ', hex(Checksum), '\n')
            
        return(Checksum)    
    
    #---------------------------------------------------------------------------------------------------------------------------#
    
    def ResetDevice(self):
        
        print("------------- Reset Target...----------------------\n")
        
        Reset = 9
        
        tx = struct.pack('<BBQ', 0x55, Reset, 0x00)
        
        self.DataCommunication.ModbusInterface.WriteBootloadData(tx)
        
        rx = self.DataCommunication.ModbusInterface.ReadBootloadData(11)
        
        if(rx == ''):
            print('Reset target cmd readback nok, no answer from target!\n')
            return(COMMAND_UNSUCCESSFUL)
        
        print('Reset target sent!\n')
        return COMMAND_SUCCESSFUL    
    
    #---------------------------------------------------------------------------------------------------------------------------#
    
    def FlashAuto(self):
        
        self.cmd_returnval    = 0
        self.CalcChecksumFile = 0
        self.ChecksumRequest  = 0     
        if(self.EraseFlash(0x800, 0x8000) == COMMAND_SUCCESSFUL):    
            self.cmd_returnval, self.CalcChecksumFile = self.WriteFlash(0x800, 0x8000)
            if(self.cmd_returnval == COMMAND_SUCCESSFUL):        
                if(self.WriteConfig() == COMMAND_SUCCESSFUL): 
                    self.ChecksumRequest = self.RequestChecksum(0x800, 0x8000)
                    if(self.CalcChecksumFile == self.ChecksumRequest):
                        print('Checksum match between file and PIC!\n')
                        if(self.ResetDevice() == COMMAND_SUCCESSFUL):
                            print("------------- Done...------------------------------\n")
                            print('Programming device done closing program')
                        else:
                            print('Reset device error, closing program')
                    else:
                        print('Checksum mismatch between file and PIC, programming stopped!\n')
                else:
                    print('Writing config words to device failed!\n')
            else:
                print('Programming stopped!\n')
                
        else:
            print('Programming stopped!\n')
        
        elapsed_time = time.time() - self.start_time
        print('Programming time : ', str('%.2f'% elapsed_time) , 'seconds! \n')
        