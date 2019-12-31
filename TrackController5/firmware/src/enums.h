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
#define MAILBOXSIZE 5
#define HEADER 0xAA
#define FOOTER 0x55
#define SLAVEINFO 0xFF
    
#define NUMBER_OF_SLAVES 55                                                     // 0 is for the master self. 50 Track slaves and 5 backplane slaves => 55
#define NUMBER_OF_SLAVES_SIZE (NUMBER_OF_SLAVES + 1)
#define NUMBER_OF_AMPLIFIERS (NUMBER_OF_SLAVES - 5)

    // *****************************************************************************
    // *****************************************************************************
    // Section: Data Types
    // *****************************************************************************
    // *****************************************************************************

    /*  A brief description of a section can be given directly below the section
        banner.
     */


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
    
typedef struct
{
    uint8_t header;
    uint8_t command;
    uint8_t data[80]; 
}udpTrans_t;

typedef enum
{
    CONNECTED                       = 0xFB,
    DONE                            = 0xFC,
    COMMAND                         = 0xFD,
    ERROR                           = 0xFE,
    CONTROLLER                      = 0x80,
    MBUS                            = 0x81,
    FWHANDLER                       = 0x82,
} STATUS_MESSAGES;


enum
{	
    EXEC_MBUS_STATE_SLAVES_ON           = 0x01,
    EXEC_MBUS_STATE_SLAVE_DETECT        = 0x02,
    EXEC_MBUS_STATE_SLAVES_BOOT_WAIT    = 0x03,
    EXEC_MBUS_STATE_SLAVE_FW_DOWNLOAD   = 0x04,
    EXEC_MBUS_STATE_SLAVE_INIT          = 0x05,
    EXEC_MBUS_STATE_SLAVE_ENABLE        = 0x06,
    EXEC_MBUS_STATE_START_DATA_UPLOAD   = 0x07,
    EXEC_MBUS_STATE_RESET               = 0x08,    
} CONTROLLER_COMMANDS;

typedef enum
{	
    EXEC_FW_STATE_RECEIVE_FW_FILE           = 0x09,
    EXEC_FW_STATE_RECEIVE_FW_FILE_STANDBY   = 0x0A,
    EXEC_FW_STATE_FW_DATA                   = 0x0B,
    EXEC_FW_STATE_FW_DATA_DOWNLOAD_DONE     = 0x0C,
    EXEC_FW_STATE_FW_CHECKSUM               = 0x0D,
    EXEC_FW_STATE_FLASH_SLAVES              = 0x0E
    
} FWHANDLER_COMMANDS;

enum{
    CLIENT_CONNECTION_REQUEST           = 0x0D,
} CLIENT_COMMANDS;

    /* Provide C++ Compatibility */
#ifdef __cplusplus
}
#endif

#endif /* _EXAMPLE_FILE_NAME_H */

/* *****************************************************************************
 End of File
 */
