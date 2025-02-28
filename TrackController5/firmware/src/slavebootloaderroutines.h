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

#ifndef SLAVEBOOTLOADERROUTINES_H    /* Guard against multiple inclusion */
#define SLAVEBOOTLOADERROUTINES_H


/* ************************************************************************** */
/* ************************************************************************** */
/* Section: Included Files                                                    */
/* ************************************************************************** */
/* ************************************************************************** */

/* This section lists the other files that are included in this file.
 */

#include "enums.h"


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


    // *****************************************************************************
    // *****************************************************************************
    // Section: Data Types
    // *****************************************************************************
    // *****************************************************************************

    /*  A brief description of a section can be given directly below the section
        banner.
     */


/*******************************************************************************
  Function:
    bool GETxBOOTxLOADERxVERSION(void)

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

void          SLAVExBOOTLOADERxDATAxRETURN    (uint8_t data);
uint32_t      GETxBOOTxLOADERxVERSION         (void);
uint32_t      ERASExFLASH                     (uint16_t flash_start_address, uint16_t flash_end_address);
uint32_t      WRITExFLASH                     (uint16_t flash_bootloader_offset, uint16_t flash_end_address, uint8_t *fwfile);
uint32_t      WRITExCONFIG                    (uint8_t *config_data);
uint32_t      CHECKxCHECKSUM                  (uint16_t flash_bootloader_offset, uint16_t flash_end_address, uint16_t file_checksum);

typedef struct
{
    uint8_t         sequence;
    uint8_t         btldr_sequence;
    uint8_t         btldr_data_return_sequence;
    uint8_t         btldr_datacount;
    uint8_t         *p_btldr;
    bool            btldr_receive_error;
    uint16_t        flashrowcounter;
} BTLDR_DATA;

BTLDR_DATA btldrData;

typedef struct
{
    uint8_t     bootloader_start_byte;
    uint8_t     command;
    uint8_t     data_length_low;
    uint8_t     data_length_high;
    uint8_t     unlock_low;
    uint8_t     unlock_hgh;
    uint8_t     address_low;
    uint8_t     address_hgh;
    uint8_t     address_upp;
    uint8_t     address_ext;
    uint8_t     data[64];
} BTDR_SEND_DATA_FORMAT;

BTDR_SEND_DATA_FORMAT btldrDataSend;

typedef struct
{
    uint8_t     bootloader_start_byte;
    uint8_t     command;
    uint8_t     data_length_low;
    uint8_t     data_length_high;
    uint8_t     unlock_low;
    uint8_t     unlock_hgh;
    uint8_t     address_low;
    uint8_t     address_hgh;
    uint8_t     address_upp;
    uint8_t     address_ext;
    uint8_t     status[64];
} BTDR_RECEIVED_DATA_FORMAT;

BTDR_RECEIVED_DATA_FORMAT btldrDataReceive;

typedef enum
{
    CMD_READ_VERSION       = 0x00,
    CMD_READ_FLASH         = 0x01,
    CMD_WRITE_FLASH        = 0x02,
    CMD_ERASE_FLASH        = 0x03,
    CMD_READ_EE_DATA       = 0x04,
    CMD_WRITE_EE_DATA      = 0x05,
    CMD_READ_CONFIG        = 0x06,
    CMD_WRITE_CONFIG       = 0x07,
    CMD_CALC_CHECKSUM      = 0x08,
    CMD_RESET_DEVICE       = 0x09
    
} BOOTLOADER_COMMAND_SET;

    /* Provide C++ Compatibility */
#ifdef __cplusplus
}
#endif

#endif /* _EXAMPLE_FILE_NAME_H */

/* *****************************************************************************
 End of File
 */
