/* ************************************************************************** */
/** Descriptive File Name

  @Company
    Company Name

  @File Name
    filename.h

  @Summary
    Brief description of the file.

  @Description
    Describe the purpose of this file.
 */
/* ************************************************************************** */

#ifndef ENUMS_H    /* Guard against multiple inclusion */
#define ENUMS_H


/* ************************************************************************** */
/* ************************************************************************** */
/* Section: Included Files                                                    */
/* ************************************************************************** */
/* ************************************************************************** */

/* This section lists the other files that are included in this file.
 */

/* TODO:  Include other files here if needed. */


/* Provide C++ Compatibility */
#ifdef __cplusplus
extern "C" {
#endif


    /* ************************************************************************** */
    /* ************************************************************************** */
    /* Section: Constants                                                         */
    /* ************************************************************************** */
    /* ************************************************************************** */

    /*  A brief description of a section can be given directly below the section
        banner.
     */


    /* ************************************************************************** */
    /** Descriptive Constant Name

      @Summary
        Brief one-line summary of the constant.
    
      @Description
        Full description, explaining the purpose and usage of the constant.
        <p>
        Additional description in consecutive paragraphs separated by HTML 
        paragraph breaks, as necessary.
        <p>
        Type "JavaDoc" in the "How Do I?" IDE toolbar for more information on tags.
    
      @Remarks
        Any additional remarks
     */
#define HEADER 0xAA
#define FOOTER 0x55
#define SLAVEINFO 0xFF
    
#define NUMBER_OF_SLAVES 55                                                     // 0 is for the master self. 50 Track slaves and 5 backplane slaves => 55
#define NUMBER_OF_SLAVES_SIZE (NUMBER_OF_SLAVES + 1)
#define NUMBER_OF_AMPLIFIERS (NUMBER_OF_SLAVES - 5)
    
#define SLAVE_BOOT_LOADER_OFFSET 0x800
#define SLAVE_FLASH_END 0x8000
#define SLAVE_FLASH_SIZE (SLAVE_FLASH_END - SLAVE_BOOT_LOADER_OFFSET)
#define BLOCKWIDTH  64                                                            // erasing in the PIC is only done with 32 bytes = 2 rows of program memory!
#define LINEWIDTH 16
    
#define SECONDS (126000000)
#define MILISECONDS (SECONDS / 1000)
#define MICROSECONDS (SECONDS / 1000000)

    // *****************************************************************************
    // *****************************************************************************
    // Section: Data Types
    // *****************************************************************************
    // *****************************************************************************

    /*  A brief description of a section can be given directly below the section
        banner.
     */

    // *****************************************************************************
    
// *****************************************************************************

/** Descriptive Data Type Name

  @Summary
    Brief one-line summary of the data type.

  @Description
    Full description, explaining the purpose and usage of the data type.
    <p>
    Additional description in consecutive paragraphs separated by HTML 
    paragraph breaks, as necessary.
    <p>
    Type "JavaDoc" in the "How Do I?" IDE toolbar for more information on tags.

  @Remarks
    Any additional remarks
    <p>
    Describe enumeration elements and structure and union members above each 
    element or member.
 */

typedef enum
{
    CONTROLLER                                  				= 10,					
    MBUS                                        				= 20,					
    FWHANDLER                                   				= 30,	
} TASK_ID;				
				
typedef enum				
{				
    ABORT                                       				= 4,
    BUSY                                        				= 5,
    CONNECTED                                   				= 6,
    DONE                                        				= 7,
    COMMAND                                     				= 8,
    ERROR                                       				= 9,
} TASK_STATE;		
		
typedef enum		
{	
    /* FW HANDLER COMMANDS */
    FWHANDLERINIT                                 				= 31,
    FWFILEDOWNLOAD												= 32,
	FWCONFIGWORDDOWNLOAD										= 33,
    FWFLASHSLAVES                                               = 34,
    FWFLASHSEQUENCER                                            = 35,
            
    /* MBUS COMMANDS */											/* case states cannot have high numbers! */		
	EXEC_MBUS_STATE_SLAVES_ON                   				= 100,
	EXEC_MBUS_STATE_SLAVE_DETECT                				= 101,
	EXEC_MBUS_STATE_SLAVES_BOOT_WAIT            				= 102,
	EXEC_MBUS_STATE_SLAVE_FW_FLASH              				= 103,
	EXEC_MBUS_STATE_SLAVE_INIT                  				= 104,
	EXEC_MBUS_STATE_SLAVE_ENABLE                				= 105,
	EXEC_MBUS_STATE_START_DATA_UPLOAD           				= 106,
	EXEC_MBUS_STATE_RESET                       				= 107,
						
	/* FWHANDLER COMMANDS */						
    EXEC_FW_STATE_RECEIVE_FW_FILE         						= 120,
	EXEC_FW_STATE_RECEIVE_CONFIG_WORD							= 121,
    EXEC_FW_STATE_FLASH_ALL_SLAVES            				    = 122,
    EXEC_FW_STATE_SELECT_SLAVE                                  = 123,
    EXEC_FW_STATE_GET_BOOTLOADER_VERSION            			= 124,
    EXEC_FW_STATE_ERASE_FLASH            						= 125,
    EXEC_FW_STATE_WRITE_FLASH            						= 126,
    EXEC_FW_STATE_WRITE_CONFIG            						= 127,
    EXEC_FW_STATE_CHECK_CHECKSUM            					= 128,
    EXEC_FW_STATE_DESELECT_SLAVE                                = 129,
			
	/* FWFILEDOWNLOAD COMMANDS */		
	FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY					= 140,
	FILEDOWNLOAD_STATE_FW_DATA_RECEIVE       					= 141,
	FILEDOWNLOAD_STATE_FW_DATA_DOWNLOAD_DONE 					= 142,
	FILEDOWNLOAD_STATE_FW_CHECKSUM								= 143,
	
	/* FWCONFIGWORDDOWNLOAD COMMANDS */								
	CONFIGWORDDOWNLOAD_STATE_RECEIVE_CONFIG_WORD_STANDBY		= 150,
	CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_RECEIVE       		= 151,
	CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_DOWNLOAD_DONE 		= 152,
    
    /* FWFLASHSLAVES */
    FWFLASHSLAVES_STATE_CHECK_CHECKSUM                          = 161,
    FWFLASHSLAVES_STATE_SLAVE_FLASH                             = 162,
    
    /* FWFLASHSEQUENCER */
    FWFLASHSEQUENCER_STATE_FLASHED_SLAVE                        = 170,
    FWFLASHSEQUENCER_STATE_GET_BOOTLOADER_VERSION               = 171,
    FWFLASHSEQUENCER_STATE_ERASE_FLASH                          = 172,
    FWFLASHSEQUENCER_STATE_WRITE_FLASH                          = 173,
    FWFLASHSEQUENCER_STATE_WRITE_CONFIG                         = 174,
    FWFLASHSEQUENCER_STATE_CHECK_CHECKSUM                       = 175,
    
    /* SLAVEBOOTLOADERROUTINES */
    BOOTLOADER_DATA_RECEIVE_ERROR                               = 180,
    BOOTLOADER_START_BYTE_ERROR                                 = 181,
    
    GET_BOOTLOADER_VERSION                                      = 192,
    GET_BOOTLOADER_VERSION_RECEIVE_DATA_TIMEOUT                 = 193,
    GET_BOOTLOADER_VERSION_OK                                   = 194,
    GET_BOOTLOADER_VERSION_NOK                                  = 195,
    
    ERASE_FLASH                                                 = 200,
    ERASE_FLASH_RECEIVE_DATA_TIMEOUT                            = 201,
    ERASE_FLASH_RETURNED_OK                                     = 202,
    ERASE_FLASH_RETURNED_NOK                                    = 203,
    
    WRITE_FLASH                                                 = 210,
    WRITE_FLASH_RECEIVE_DATA_TIMEOUT                            = 211,
    WRITE_FLASH_RETURNED_OK                                     = 212,
    WRITE_FLASH_RETURNED_NOK                                    = 213,
    
    WRITE_CONFIG                                                = 220,
    WRITE_CONFIG_RECEIVE_DATA_TIMEOUT                           = 221,
    WRITE_CONFIG_RETURNED_OK                                    = 222,
    WRITE_CONFIG_RETURNED_NOK                                   = 223,
    
    CHECK_CHECKSUM_CONFIG                                       = 230,
    CHECK_CHECKSUM_CONFIG_RECEIVE_DATA_TIMEOUT                  = 231,
    CHECK_CHECKSUM_CONFIG_RETURNED_OK                           = 232,
    CHECK_CHECKSUM_CONFIG_RETURNED_NOK                          = 233,
    
} TASK_COMMAND;

typedef enum
{
    NONE                                        				= 100,
    RECEIVED_WRONG_COMMAND                      				= 101,
    RECEIVED_UNKNOWN_COMMAND                    				= 102,
	RECEIVED_BAD_COMMAND										= 103,
    RECEIVED_CHECKSUM_OK                        				= 104,
    RECEIVED_CHECKSUM_NOK                       				= 105,
    SWITCH_OUT_OF_BOUNDS                        				= 106,
    SLAVE_ID_OUT_OF_BOUNDS                                      = 107,
} TASK_MESSAGES;				
				
				
//typedef struct{				
//    TASK_ID         task_id;				
//    TASK_COMMAND    task_command;				
//    TASK_STATE      task_state;				
//    TASK_MESSAGES   task_message;				
//} RETURN_STATUS;				
				
				
typedef enum{				
    CLIENT_CONNECTION_REQUEST                   				= 250,
					
} CLIENT_COMMANDS;				
				
typedef enum				
{    				
    WAIT_TIME  = 5000,				
    WAIT_TIME2 = 65000,				
} WAIT_TYPES;				
				
typedef enum				
{				
    WRITE                                       				= 0xAA,
    READ                                        				= 0x55,
    HOLDINGREG0                                 				= 0, 
    HOLDINGREG1                                 				= 1, 
    HOLDINGREG2                                 				= 2, 
    HOLDINGREG3                                 				= 3, 
    HOLDINGREG4                                 				= 4, 
    HOLDINGREG5                                 				= 5, 
    HOLDINGREG6                                 				= 6, 
    HOLDINGREG7                                 				= 7, 
    HOLDINGREG8                                 				= 8, 
    HOLDINGREG9                                 				= 9, 
    HOLDINGREG10                                				= 10,
    HOLDINGREG11                                				= 11,
    HOLDINGREG12                                				= 12,
    SLOT1                                       				= 0x1,
    TRACKBACKPLANE1                             				= 51,
    TRACKBACKPLANE2                             				= 52,
    TRACKBACKPLANE3                             				= 53,
    TRACKBACKPLANE4                             				= 54,
    TRACKBACKPLANE5                             				= 55,
    SLAVE_INITIAL_ADDR                          				= 0xAA,
    BROADCAST_ADDRESS                           				= 0,
    WAIT                                        				= 99,
    SLAVEOK                                     				= 100,
    SLAVENOK                                    				= 101,
    SLAVEBUSY                                   				= 102,
            
} MISC;
    
typedef struct
{
    uint8_t header;
    uint8_t command;
    uint8_t data[80]; 
}udpTrans_t;

/*******************************************************************************
  Function:
    uint32_t READxCORExTIMER(void);

  Summary:
    MPLAB Harmony Demo application tasks function

  Description:
    This routine is the Harmony Demo application's tasks function.  It
    defines the application's state machine and core logic.

  Precondition:
    The system and application initialization ("SYS_Initialize") should be
    called before calling this.

  Parameters:
    None.

  Returns:
    None.

  Example:
    <code>
    MBUS_Tasks();
    </code>

  Remarks:
    This routine must be called from SYS_Tasks() routine.
 */
uint32_t READxCORExTIMER(void);

// *****************************************************************************
// Section: Application Callback Routines
// *****************************************************************************
// *****************************************************************************
/* These routines are called by drivers when certain events occur.
*/
//extern bool RETURNEDxRESULTxHANDLER (RETURN_STATUS result, TASK_COMMAND task_command);

    /* Provide C++ Compatibility */
#ifdef __cplusplus
}
#endif

#endif /* _EXAMPLE_FILE_NAME_H */

/* *****************************************************************************
 End of File
 */
