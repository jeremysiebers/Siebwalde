
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

class EnumCommand:
    EXEC_MBUS_STATE_SLAVES_ON           	= 0x01
    EXEC_MBUS_STATE_SLAVE_DETECT        	= 0x02
    EXEC_MBUS_STATE_SLAVES_BOOT_WAIT    	= 0x03
    EXEC_MBUS_STATE_SLAVE_FW_FLASH       	= 0x04
    EXEC_MBUS_STATE_SLAVE_INIT          	= 0x05
    EXEC_MBUS_STATE_SLAVE_ENABLE        	= 0x06
    EXEC_MBUS_STATE_START_DATA_UPLOAD   	= 0x07
    EXEC_MBUS_STATE_RESET               	= 0x08

    EXEC_FW_STATE_RECEIVE_FW_FILE               = 0x09
    EXEC_FW_STATE_RECEIVE_FW_FILE_STANDBY       = 0x0A
    EXEC_FW_STATE_FW_DATA                       = 0x0B
    EXEC_FW_STATE_FW_DATA_DOWNLOAD_DONE         = 0x0C
    EXEC_FW_STATE_FW_CHECKSUM                   = 0x0D
    EXEC_FW_STATE_RECEIVE_CONFIG_WORD           = 0x0E
    EXEC_FW_STATE_RECEIVE_CONFIG_WORD_STANDBY   = 0x0F
    EXEC_FW_STATE_FLASH_SLAVES                  = 0x10
    EXEC_FW_STATE_CONFIG_DATA                   = 0x11
    EXEC_FW_STATE_CONFIG_DATA_DOWNLOAD_DONE     = 0x12

#class Enum
    MODBUS                              	= 0xFF
    ETHERNET_T                          	= 0x10
    BOOTLOADER                          	= 0x11

class EnumTaskId:
    CONTROLLER                          	= 0x80
    MBUS                                	= 0x81
    FWHANDLER                           	= 0x82    

class EnumTaskStates:    
    ABORT                                       = 0xF9
    BUSY                                        = 0xFA    
    CONNECTED                           	= 0xFB
    DONE                                	= 0xFC
    COMMAND                             	= 0xFD
    ERROR                               	= 0xFE    

class EnumClientCommands:	
    CLIENT_CONNECTION_REQUEST           	= 0x80

class EnumMbusStatus:	
    MBUS_STATE_INIT                     	= 0x00
    MBUS_STATE_WAIT                     	= 0x01
    MBUS_STATE_SLAVES_ON                	= 0x02
    MBUS_STATE_SLAVES_BOOT_WAIT         	= 0x03
    MBUS_STATE_SLAVE_DETECT             	= 0x04
    MBUS_STATE_SLAVE_FW_FLASH              	= 0x05
    MBUS_STATE_SLAVE_INIT               	= 0x06
    MBUS_STATE_SLAVE_ENABLE             	= 0x07
    MBUS_STATE_START_DATA_UPLOAD        	= 0x08
    MBUS_STATE_SERVICE_TASKS            	= 0x09
    MBUS_STATE_RESET                    	= 0x0A

class EnumBootloader:
    COMMAND_SUCCESSFUL   					= 0x01 # Command Successful
    COMMAND_UNSUCCESSFUL 					= 0xfd # Command unSuccessful 