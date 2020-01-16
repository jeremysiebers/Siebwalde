
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
    CONTROLLER                                  				= 10
    MBUS                                        				= 20
    FWHANDLER                                   				= 30   

class EnumTaskStates:    
    ABORT                                       				= 4
    BUSY                                        				= 5
    CONNECTED                                   				= 6
    DONE                                        				= 7
    COMMAND                                     				= 8
    ERROR                                       				= 9

class EnumCommand:
    #/* FW HANDLER TASK COMMANDS */
    FWHANDLERINIT                                                               = 31
    FWFILEDOWNLOAD								= 32
    FWCONFIGWORDDOWNLOAD                                                        = 33
    FWFLASHSLAVES                                                               = 34
    FWFLASHSEQUENCER                                                            = 35    
    
    #/* MBUS COMMANDS */							/* case states cannot have high numbers! */		
    EXEC_MBUS_STATE_SLAVES_ON                   				= 100
    EXEC_MBUS_STATE_SLAVE_DETECT                				= 101
    EXEC_MBUS_STATE_SLAVES_BOOT_WAIT            				= 102
    EXEC_MBUS_STATE_SLAVE_FW_FLASH              				= 103
    EXEC_MBUS_STATE_SLAVE_INIT                  				= 104
    EXEC_MBUS_STATE_SLAVE_ENABLE                				= 105
    EXEC_MBUS_STATE_START_DATA_UPLOAD           				= 106
    EXEC_MBUS_STATE_RESET                       				= 107
    					
    #/* FWHANDLER COMMANDS */						
    EXEC_FW_STATE_RECEIVE_FW_FILE         					= 120
    EXEC_FW_STATE_RECEIVE_CONFIG_WORD						= 121
    EXEC_FW_STATE_FLASH_ALL_SLAVES            				        = 122
    EXEC_FW_STATE_SELECT_SLAVE                                                  = 123
    EXEC_FW_STATE_GET_BOOTLOADER_VERSION            			        = 124
    EXEC_FW_STATE_ERASE_FLASH            					= 125
    EXEC_FW_STATE_WRITE_FLASH            					= 126
    EXEC_FW_STATE_WRITE_CONFIG            					= 127
    EXEC_FW_STATE_CHECK_CHECKSUM            					= 128
    EXEC_FW_STATE_DESELECT_SLAVE                                                = 129
    		
    #/* FWFILEDOWNLOAD COMMANDS */		
    FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY					= 140
    FILEDOWNLOAD_STATE_FW_DATA_RECEIVE       					= 141
    FILEDOWNLOAD_STATE_FW_DATA_DOWNLOAD_DONE 					= 142
    FILEDOWNLOAD_STATE_FW_CHECKSUM						= 143
    
    #/* FWCONFIGWORDDOWNLOAD COMMANDS */								
    CONFIGWORDDOWNLOAD_STATE_RECEIVE_CONFIG_WORD_STANDBY		        = 150
    CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_RECEIVE       		        = 151
    CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_DOWNLOAD_DONE 		        = 152
    
    #/* FWFLASHSLAVES */
    FWFLASHSLAVES_STATE_CHECK_CHECKSUM                                          = 161
    FWFLASHSLAVES_STATE_SLAVE_FLASH                                             = 162
                                                                                
    #/* FWFLASHSEQUENCER */                                                     
    FWFLASHSEQUENCER_STATE_FLASHED_SLAVE                                        = 170
    FWFLASHSEQUENCER_STATE_GET_BOOTLOADER_VERSION                               = 171
    FWFLASHSEQUENCER_STATE_ERASE_FLASH                                          = 172
    FWFLASHSEQUENCER_STATE_WRITE_FLASH                                          = 173
    FWFLASHSEQUENCER_STATE_WRITE_CONFIG                                         = 174
    FWFLASHSEQUENCER_STATE_CHECK_CHECKSUM                                       = 175
    FWFLASHSEQUENCER_STATE_RESET_SLAVE                                          = 176

    #/* SLAVEBOOTLOADERROUTINES */                                              
    BOOTLOADER_DATA_RECEIVE_ERROR                                               = 180
    BOOTLOADER_START_BYTE_ERROR                                                 = 181

    GET_BOOTLOADER_VERSION                                                      = 192
    GET_BOOTLOADER_VERSION_RECEIVE_DATA_TIMEOUT                                 = 193
    GET_BOOTLOADER_VERSION_OK                                                   = 194
    GET_BOOTLOADER_VERSION_NOK                                                  = 195

    ERASE_FLASH                                                                 = 200
    ERASE_FLASH_RECEIVE_DATA_TIMEOUT                                            = 201
    ERASE_FLASH_RETURNED_OK                                                     = 202
    ERASE_FLASH_RETURNED_NOK                                                    = 203

    WRITE_FLASH                                                                 = 210
    WRITE_FLASH_RECEIVE_DATA_TIMEOUT                                            = 211
    WRITE_FLASH_RETURNED_OK                                                     = 212
    WRITE_FLASH_RETURNED_NOK                                                    = 213

    WRITE_CONFIG                                                                = 220
    WRITE_CONFIG_RECEIVE_DATA_TIMEOUT                                           = 221
    WRITE_CONFIG_RETURNED_OK                                                    = 222
    WRITE_CONFIG_RETURNED_NOK                                                   = 223

    CHECK_CHECKSUM_CONFIG                                                       = 230
    CHECK_CHECKSUM_CONFIG_RECEIVE_DATA_TIMEOUT                                  = 231
    CHECK_CHECKSUM_CONFIG_RETURNED_OK                                           = 232
    CHECK_CHECKSUM_CONFIG_RETURNED_NOK                                          = 233
    
    RESET_SLAVE                                                                 = 240
    RESET_SLAVE_DATA_TIMEOUT                                                    = 241
    RESET_SLAVE_OK                                                              = 242
    RESET_SLAVE_NOK                                                             = 243    

#class Enum
    MODBUS                              				        = 0xFF
    ETHERNET_T                          					= 0x10
    BOOTLOADER                          					= 0x11

class EnumTaskMessages:
    NONE                                        				= 100
    RECEIVED_WRONG_COMMAND                      				= 101 
    RECEIVED_UNKNOWN_COMMAND                    				= 102 
    RECEIVED_BAD_COMMAND							= 103 
    RECEIVED_CHECKSUM_OK                        				= 104 
    RECEIVED_CHECKSUM_NOK                       				= 105 
    SWITCH_OUT_OF_BOUNDS                        				= 106 
    SLAVE_ID_OUT_OF_BOUNDS                                                      = 107 
    
class EnumClientCommands:	
    CLIENT_CONNECTION_REQUEST           	                                = 250

class EnumMbusStatus:	                                                        
    MBUS_STATE_INIT                     	                                = 0x00
    MBUS_STATE_WAIT                     	                                = 0x01
    MBUS_STATE_SLAVES_ON                	                                = 0x02
    MBUS_STATE_SLAVES_BOOT_WAIT         	                                = 0x03
    MBUS_STATE_SLAVE_DETECT             	                                = 0x04
    MBUS_STATE_SLAVE_FW_FLASH              	                                = 0x05
    MBUS_STATE_SLAVE_INIT               	                                = 0x06
    MBUS_STATE_SLAVE_ENABLE             	                                = 0x07
    MBUS_STATE_START_DATA_UPLOAD        	                                = 0x08
    MBUS_STATE_SERVICE_TASKS            	                                = 0x09
    MBUS_STATE_RESET                    	                                = 0x0A

class EnumBootloader:                                           
    COMMAND_SUCCESSFUL   					                = 0x01 # Command Successful
    COMMAND_UNSUCCESSFUL 					                = 0xfd # Command unSuccessful 