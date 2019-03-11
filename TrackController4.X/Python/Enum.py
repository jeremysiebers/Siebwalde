
class EnumStateMachine:
    busy  = 'busy'
    ok    = 'ok'
    nok   = 'nok'
    
    ResetAllSlaves        = 'ResetAllSlaves'
    InitTrackamplifiers   = 'InitTrackamplifiers'
    EnableTrackamplifiers = 'EnableTrackamplifiers'
    CheckAmpSwVersion     = 'CheckAmpSwVersion'
    FlashTrackamplifiers  = 'FlashTrackamplifiers'
    run                   = 'run'
    
class EnumSlaveInit:
    SLOT1             = 0x1
    SLOT2             = 0x2
    SLOT3             = 0x4
    SLOT4             = 0x8
    SLOT5             = 0x10
    SLOT6             = 0x20
    SLOT7             = 0x40
    SLOT8             = 0x80
    SLOT9             = 0x100
    SLOT10            = 0x200
    TRACKBACKPLANE1   = 51
    TRACKBACKPLANE2   = 52
    TRACKBACKPLANE3   = 53
    TRACKBACKPLANE4   = 54
    TRACKBACKPLANE5   = 55
    DEFAULTMODBUSADDR = 0xAA
    
class EnumSlaveConfig:    
    OK          = 0x02 
    NOK         = 0x04 
    _BUSY       = 0x01
    IDLE        = 0x00
    MODE_MAN    = 0b1111111111111110
    MODE_AUTO   = 0b1111111111111111
    
    WRITE       = 0b1111111111111111
    READ        = 0b1111111111111101
    
    HOLDINGREG  = 0b1111111111000111
    INPUTREG    = 0b1111111111001011
    DIAGREG     = 0b1111111111010011        
    BOOTLOAD    = 0b1111111111100011
    EXEC        = 0b1111111111111111
    HALT        = 0b0111111111111111    

class EnumCommand:
    MODBUS      = 0xFF
    BOOTLOADER  = 0xFE
    ETHERNET_T  = 0xFD
    
class EnumEthernetT:
    ResetAll          = 0xFF
    ReleaseAll        = 0xFE
    INIT_PHASE_FALSE  = 0xFD
    INIT_PHASE_TRUE   = 0xFC
    OK                = 0x82
    NOK               = 0x83
    BUSY              = 0x81
    IDLE              = 0x80
    SEND_BOOTLOADER   = 0x01
    SEND_SLAVEIODATA  = 0x00
    
class EnumBootloader:
    COMMAND_SUCCESSFUL   = 0x01 # Command Successful
    COMMAND_UNSUPPORTED  = 0xFF # Command Unsupported
    ADDRESS_ERROR        = 0Xfe # Address Error
    COMMAND_UNSUCCESSFUL = 0xfd # Command unSuccessful 