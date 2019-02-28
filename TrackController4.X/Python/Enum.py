
class EnumStateMachine:
    busy  = 'busy'
    ok    = 'ok'
    nok   = 'nok'
    
    ResetAllSlaves        = 'ResetAllSlaves'
    InitTrackamplifiers   = 'InitTrackamplifiers'
    run                   = 'run'
    
class EnumSlaveInit:
    SLOT1           = 0x1
    SLOT2           = 0x2
    SLOT3           = 0x4
    SLOT4           = 0x8
    SLOT5           = 0x10
    SLOT6           = 0x20
    SLOT7           = 0x40
    SLOT8           = 0x80
    SLOT9           = 0x100
    SLOT10          = 0x200
    TRACKBACKPLANE1 = 51
    TRACKBACKPLANE2 = 52
    TRACKBACKPLANE3 = 53
    TRACKBACKPLANE4 = 54
    TRACKBACKPLANE5 = 55
    
class EnumSlaveConfig:    
    OK          = 0x02 
    NOK         = 0x04 
    _BUSY       = 0x01
    IDLE        = 0x00
    MODE_MAN    = 0b1111111111111110
    MODE_AUTO   = 0b1111111111111111    
    WRITE       = 0b1111111111111111
    READ        = 0b1111111111111101
    HOLDINGREG  = 0b1111111111100111
    INPUTREG    = 0b1111111111101011
    DIAGREG     = 0b1111111111110011
    EXEC        = 0b1111111111111111
    HALT        = 0b1111111111011111    

class EnumCommand:
    MODBUS      = 0x00
    BOOTLOADER  = 0x01
    ETHERNET_T  = 0x02
    
class EnumEthernetT:
    ResetAll    = 0xFF
    ReleaseAll  = 0xFE
    OK          = 0x82
    NOK         = 0x83
    BUSY        = 0x81
    IDLE        = 0x80