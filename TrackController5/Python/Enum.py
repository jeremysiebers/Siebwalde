
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

class EnumTaskId:
    CONTROLLER                                  				= 100
					
    MBUS                                        				= 200
					
    FWHANDLER                                   				= 300
	FWFILEDOWNLOAD												= 301
	FWCONFIGWORDDOWNLOAD										= 302

class EnumTaskStates:    
    ABORT                                       				= 10
    BUSY                                        				= 20
    CONNECTED                                   				= 30
    DONE                                        				= 40
    COMMAND                                     				= 50
    ERROR                                       				= 60

class EnumCommand:
    /* MBUS COMMANDS */											/* case states cannot have high numbers! */		
	EXEC_MBUS_STATE_SLAVES_ON                   				= 0
	EXEC_MBUS_STATE_SLAVE_DETECT                				= 1
	EXEC_MBUS_STATE_SLAVES_BOOT_WAIT            				= 2
	EXEC_MBUS_STATE_SLAVE_FW_FLASH              				= 3
	EXEC_MBUS_STATE_SLAVE_INIT                  				= 4
	EXEC_MBUS_STATE_SLAVE_ENABLE                				= 5
	EXEC_MBUS_STATE_START_DATA_UPLOAD           				= 6
	EXEC_MBUS_STATE_RESET                       				= 7
						
	/* FWHANDLER COMMANDS */						
    EXEC_FW_STATE_RECEIVE_FW_FILE         						= 1100
	EXEC_FW_STATE_RECEIVE_CONFIG_WORD							= 1101
    EXEC_FW_STATE_FLASH_SLAVES            						= 1102
			
	/* FWFILEDOWNLOAD COMMANDS */		
	FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY					= 1200
	FILEDOWNLOAD_STATE_FW_DATA_RECEIVE       					= 1201
	FILEDOWNLOAD_STATE_FW_DATA_DOWNLOAD_DONE 					= 1202
	FILEDOWNLOAD_STATE_FW_CHECKSUM								= 1203
	
	/* FWCONFIGWORDDOWNLOAD COMMANDS */								
	CONFIGWORDDOWNLOAD_STATE_RECEIVE_CONFIG_WORD_STANDBY		= 1300
	CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_RECEIVE       		= 1301
	CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_DOWNLOAD_DONE 		= 1302

#class Enum
    MODBUS                              						= 0xFF
    ETHERNET_T                          						= 0x10
    BOOTLOADER                          						= 0x11

class EnumTaskMessages:
	NONE                                        				= 10000
    RECEIVED_WRONG_COMMAND                      				= 10001
    RECEIVED_UNKNOWN_COMMAND                    				= 10002
	RECEIVED_BAD_COMMAND										= 10003
    RECEIVED_CHECKSUM_OK                        				= 10004
    RECEIVED_CHECKSUM_NOK                       				= 10005
    SWITCH_OUT_OF_BOUNDS                        				= 10006


class EnumClientCommands:	
    CLIENT_CONNECTION_REQUEST           	                    = 0x80
											                    
class EnumMbusStatus:	                                        
    MBUS_STATE_INIT                     	                    = 0x00
    MBUS_STATE_WAIT                     	                    = 0x01
    MBUS_STATE_SLAVES_ON                	                    = 0x02
    MBUS_STATE_SLAVES_BOOT_WAIT         	                    = 0x03
    MBUS_STATE_SLAVE_DETECT             	                    = 0x04
    MBUS_STATE_SLAVE_FW_FLASH              	                    = 0x05
    MBUS_STATE_SLAVE_INIT               	                    = 0x06
    MBUS_STATE_SLAVE_ENABLE             	                    = 0x07
    MBUS_STATE_START_DATA_UPLOAD        	                    = 0x08
    MBUS_STATE_SERVICE_TASKS            	                    = 0x09
    MBUS_STATE_RESET                    	                    = 0x0A
											                    
class EnumBootloader:                                           
    COMMAND_SUCCESSFUL   					                    = 0x01 # Command Successful
    COMMAND_UNSUCCESSFUL 					                    = 0xfd # Command unSuccessful 