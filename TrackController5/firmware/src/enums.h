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
    CONTROLLER                                  = 0x80,
    MBUS                                        = 0x81,
    FWHANDLER                                   = 0x82,
} TASK_ID;

typedef enum
{
    ABORT                                       = 0xF9,
    BUSY                                        = 0xFA,
    CONNECTED                                   = 0xFB,
    DONE                                        = 0xFC,
    COMMAND                                     = 0xFD,
    ERROR                                       = 0xFE,
} TASK_STATE;

typedef enum
{
    NONE                                        = 0x00,
    RECEIVED_WRONG_COMMAND                      = 0x01,
    RECEIVED_UNKNOWN_COMMAND                    = 0x02,
    RECEIVED_CHECKSUM_OK                        = 0x03,
    RECEIVED_CHECKSUM_NOK                       = 0x04,
    SWITCH_OUT_OF_BOUNDS                        = 0x05,
} TASK_MESSAGES;

typedef enum
{
    /* MBUS COMMANDS */
    EXEC_MBUS_STATE_SLAVES_ON                   = 0x01,
    EXEC_MBUS_STATE_SLAVE_DETECT                = 0x02,
    EXEC_MBUS_STATE_SLAVES_BOOT_WAIT            = 0x03,
    EXEC_MBUS_STATE_SLAVE_FW_FLASH              = 0x04,
    EXEC_MBUS_STATE_SLAVE_INIT                  = 0x05,
    EXEC_MBUS_STATE_SLAVE_ENABLE                = 0x06,
    EXEC_MBUS_STATE_START_DATA_UPLOAD           = 0x07,
    EXEC_MBUS_STATE_RESET                       = 0x08,
            
	/* FWHANDLER COMMANDS */
    EXEC_FW_STATE_RECEIVE_FW_FILE               = 0x09,
    EXEC_FW_STATE_RECEIVE_FW_FILE_STANDBY       = 0x0A,
    EXEC_FW_STATE_FW_DATA                       = 0x0B,
    EXEC_FW_STATE_FW_DATA_DOWNLOAD_DONE         = 0x0C,
    EXEC_FW_STATE_FW_CHECKSUM                   = 0x0D,
    EXEC_FW_STATE_RECEIVE_CONFIG_WORD           = 0x0E,
    EXEC_FW_STATE_RECEIVE_CONFIG_WORD_STANDBY   = 0x0F,
    EXEC_FW_STATE_FLASH_SLAVES                  = 0x10,
	EXEC_FW_STATE_CONFIG_DATA                   = 0x11,
	EXEC_FW_STATE_CONFIG_DATA_DOWNLOAD_DONE     = 0x12,
    
} TASK_COMMAND;


typedef struct{
    TASK_ID         task_id;
    TASK_COMMAND    task_command;
    TASK_STATE      task_state;
    TASK_MESSAGES   task_message;
} RETURN_STATUS;


typedef enum{
    CLIENT_CONNECTION_REQUEST                   = 0x80,
    
} CLIENT_COMMANDS;

typedef enum
{    
    WAIT_TIME  = 5000,
    WAIT_TIME2 = 65000,
} WAIT_TYPES;

typedef enum
{
    WRITE                                       = 0xAA,
    READ                                        = 0x55,
    HOLDINGREG0                                 = 0, 
    HOLDINGREG1                                 = 1, 
    HOLDINGREG2                                 = 2, 
    HOLDINGREG3                                 = 3, 
    HOLDINGREG4                                 = 4, 
    HOLDINGREG5                                 = 5, 
    HOLDINGREG6                                 = 6, 
    HOLDINGREG7                                 = 7, 
    HOLDINGREG8                                 = 8, 
    HOLDINGREG9                                 = 9, 
    HOLDINGREG10                                = 10,
    HOLDINGREG11                                = 11,
    HOLDINGREG12                                = 12,
    SLOT1                                       = 0x1,
    SLOT2                                       = 0x2,
    SLOT3                                       = 0x4,
    SLOT4                                       = 0x8,
    SLOT5                                       = 0x10,
    SLOT6                                       = 0x20,
    SLOT7                                       = 0x40,
    SLOT8                                       = 0x80,
    SLOT9                                       = 0x100,
    SLOT10                                      = 0x200,
    TRACKBACKPLANE1                             = 51,
    TRACKBACKPLANE2                             = 52,
    TRACKBACKPLANE3                             = 53,
    TRACKBACKPLANE4                             = 54,
    TRACKBACKPLANE5                             = 55,
    SLAVE_INITIAL_ADDR                          = 0xAA,
    BROADCAST_ADDRESS                           = 0,
    WAIT                                        = 99,
    SLAVEOK                                     = 100,
    SLAVENOK                                    = 101,
    SLAVEBUSY                                   = 102,
            
} MISC;
    
typedef struct
{
    uint8_t header;
    uint8_t command;
    uint8_t data[80]; 
}udpTrans_t;


// *****************************************************************************
// Section: Application Callback Routines
// *****************************************************************************
// *****************************************************************************
/* These routines are called by drivers when certain events occur.
*/
extern bool RETURNEDxRESULTxHANDLER (RETURN_STATUS result, TASK_COMMAND task_command);

    /* Provide C++ Compatibility */
#ifdef __cplusplus
}
#endif

#endif /* _EXAMPLE_FILE_NAME_H */

/* *****************************************************************************
 End of File
 */
