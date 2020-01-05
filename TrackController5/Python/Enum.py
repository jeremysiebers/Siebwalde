
class EnumStateMachine:
    busy  = 'busy'
    ok    = 'ok'
    nok   = 'nok'
    
    ConnectToEthernetTarget = 'ConnectToEthernetTarget'
    ResetAllSlaves          = 'ResetAllSlaves'
    DataUploadStart         = 'DataUploadStart'
    DetectSlaves            = 'DetectSlaves'
    InitTrackamplifiers     = 'InitTrackamplifiers'
    EnableTrackamplifiers   = 'EnableTrackamplifiers'
    CheckAmpSwVersion       = 'CheckAmpSwVersion'
    FlashTrackamplifiers    = 'FlashTrackamplifiers'
    run                     = 'run'
    
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
    EXEC_MBUS_STATE_SLAVES_ON           = 0x01
    EXEC_MBUS_STATE_SLAVE_DETECT        = 0x02
    EXEC_MBUS_STATE_SLAVES_BOOT_WAIT    = 0x03
    EXEC_MBUS_STATE_SLAVE_FW_DOWNLOAD   = 0x04
    EXEC_MBUS_STATE_SLAVE_INIT          = 0x05
    EXEC_MBUS_STATE_SLAVE_ENABLE        = 0x06
    EXEC_MBUS_STATE_START_DATA_UPLOAD   = 0x07
    EXEC_MBUS_STATE_RESET               = 0x08
    
    EXEC_FW_STATE_RECEIVE_FW_FILE           = 0x09
    EXEC_FW_STATE_RECEIVE_FW_FILE_STANDBY   = 0x0A
    EXEC_FW_STATE_FW_DATA                   = 0x0B
    EXEC_FW_STATE_FW_DATA_DOWNLOAD_DONE     = 0x0C
    EXEC_FW_STATE_FW_CHECKSUM               = 0x0D
    EXEC_FW_STATE_RECEIVE_CONFIG_WORD       = 0x0E
    EXEC_FW_STATE_FLASH_SLAVES              = 0x0F
    
    MODBUS                              = 0xFF
    ETHERNET_T                          = 0x10
    BOOTLOADER                          = 0x11
    DUMMY_CMD                           = 0x12
    
    
class EnumStatusMessages:
    CONTROLLER                          = 0x80
    MBUS                                = 0x81
    FWHANDLER                           = 0x82
    CONNECTED                           = 0xFB
    DONE                                = 0xFC
    COMMAND                             = 0xFD
    ERROR                               = 0xFE    
    
class EnumClientCommands:
    CLIENT_CONNECTION_REQUEST           = 0x0D
    
class EnumMbusStatus:
    MBUS_STATE_INIT                     = 0x00
    MBUS_STATE_WAIT                     = 0x01
    MBUS_STATE_SLAVES_ON                = 0x02
    MBUS_STATE_SLAVES_BOOT_WAIT         = 0x03
    MBUS_STATE_SLAVE_DETECT             = 0x04
    MBUS_STATE_SLAVE_FW_DOWNLOAD        = 0x05
    MBUS_STATE_SLAVE_INIT               = 0x06
    MBUS_STATE_SLAVE_ENABLE             = 0x07
    MBUS_STATE_START_DATA_UPLOAD        = 0x08
    MBUS_STATE_SERVICE_TASKS            = 0x09
    MBUS_STATE_RESET                    = 0x0A

class EnumBootloader:
    COMMAND_SUCCESSFUL   = 0x01 # Command Successful
    COMMAND_UNSUPPORTED  = 0xFF # Command Unsupported
    ADDRESS_ERROR        = 0Xfe # Address Error
    COMMAND_UNSUCCESSFUL = 0xfd # Command unSuccessful 