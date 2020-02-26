using System.Collections.Generic;

namespace Siebwalde_Application
{
    public struct ReceivedMessage
    {
        public ushort TaskId;
        public ushort Taskcommand;
        public ushort Taskstate;
        public ushort Taskmessage;

        public ReceivedMessage(ushort taskid, ushort taskcommand, ushort taskstate, ushort taskmessage)
        {
            TaskId = taskid;
            Taskcommand = taskcommand;
            Taskstate = taskstate;
            Taskmessage = taskmessage;
        }
    }

    public static class PublicEnums
    {
        public const byte HEADER = 0xAA;
        public const byte FOOTER = 0x55;

        public const byte SLAVEINFO = 0xFF;

        public const string TRACKTARGET = "TRACKCONTROL";
    }

    public static class TaskId
    {
        public const ushort CONTROLLER = 10;
        public const ushort MBUS = 20;
        public const ushort FWHANDLER = 30;
    }

    public static class TaskStates
    {
        public const ushort ABORT = 4;
        public const ushort BUSY = 5;
        public const ushort CONNECTED = 6;
        public const ushort DONE = 7;
        public const ushort COMMAND = 8;
        public const ushort ERROR = 9;
    }

    public static class TrackCommand
    {
        /* FW HANDLER TASK COMMANDS */
        public const ushort FWHANDLERINIT = 31;
        public const ushort FWFILEDOWNLOAD = 32;
        public const ushort FWCONFIGWORDDOWNLOAD = 33;
        public const ushort FWFLASHSLAVES = 34;
        public const ushort FWFLASHSEQUENCER = 35;

        /* MBUS COMMANDS */                            /* case states cannot have high numbers! */
        public const ushort EXEC_MBUS_STATE_SLAVES_ON = 100;
        public const ushort EXEC_MBUS_STATE_SLAVE_DETECT = 101;
        public const ushort EXEC_MBUS_STATE_SLAVES_BOOT_WAIT = 102;
        public const ushort EXEC_MBUS_STATE_SLAVE_FW_FLASH = 103;
        public const ushort EXEC_MBUS_STATE_SLAVE_INIT = 104;
        public const ushort EXEC_MBUS_STATE_SLAVE_ENABLE = 105;
        public const ushort EXEC_MBUS_STATE_START_DATA_UPLOAD = 106;
        public const ushort EXEC_MBUS_STATE_RESET = 107;

        /* FWHANDLER COMMANDS */
        public const ushort EXEC_FW_STATE_RECEIVE_FW_FILE = 120;
        public const ushort EXEC_FW_STATE_RECEIVE_CONFIG_WORD = 121;
        public const ushort EXEC_FW_STATE_FLASH_ALL_SLAVES = 122;
        public const ushort EXEC_FW_STATE_SELECT_SLAVE = 123;
        public const ushort EXEC_FW_STATE_GET_BOOTLOADER_VERSION = 124;
        public const ushort EXEC_FW_STATE_ERASE_FLASH = 125;
        public const ushort EXEC_FW_STATE_WRITE_FLASH = 126;
        public const ushort EXEC_FW_STATE_WRITE_CONFIG = 127;
        public const ushort EXEC_FW_STATE_CHECK_CHECKSUM = 128;
        public const ushort EXEC_FW_STATE_SLAVE_RESET = 129;
        public const ushort EXIT_SLAVExFWxHANDLER = 130;

        /* FWFILEDOWNLOAD COMMANDS */
        public const ushort FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY = 140;
        public const ushort FILEDOWNLOAD_STATE_FW_DATA_RECEIVE = 141;
        public const ushort FILEDOWNLOAD_STATE_FW_DATA_DOWNLOAD_DONE = 142;
        public const ushort FILEDOWNLOAD_STATE_FW_CHECKSUM = 143;

        /* FWCONFIGWORDDOWNLOAD COMMANDS */
        public const ushort CONFIGWORDDOWNLOAD_STATE_RECEIVE_CONFIG_WORD_STANDBY = 150;
        public const ushort CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_RECEIVE = 151;
        public const ushort CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_DOWNLOAD_DONE = 152;

        /* FWFLASHSLAVES */
        public const ushort FWFLASHSLAVES_STATE_CHECK_CHECKSUM = 161;
        public const ushort FWFLASHSLAVES_STATE_SLAVE_FLASH = 162;

        /* FWFLASHSEQUENCER */
        public const ushort FWFLASHSEQUENCER_STATE_FLASHED_SLAVE = 170;
        public const ushort FWFLASHSEQUENCER_STATE_GET_BOOTLOADER_VERSION = 171;
        public const ushort FWFLASHSEQUENCER_STATE_ERASE_FLASH = 172;
        public const ushort FWFLASHSEQUENCER_STATE_WRITE_FLASH = 173;
        public const ushort FWFLASHSEQUENCER_STATE_WRITE_CONFIG = 174;
        public const ushort FWFLASHSEQUENCER_STATE_CHECK_CHECKSUM = 175;
        public const ushort FWFLASHSEQUENCER_STATE_RESET_SLAVE = 176;

        /* SLAVEBOOTLOADERROUTINES */
        public const ushort BOOTLOADER_DATA_RECEIVE_ERROR = 180;
        public const ushort BOOTLOADER_START_BYTE_ERROR = 181;

        public const ushort GET_BOOTLOADER_VERSION = 192;
        public const ushort GET_BOOTLOADER_VERSION_RECEIVE_DATA_TIMEOUT = 193;
        public const ushort GET_BOOTLOADER_VERSION_OK = 194;
        public const ushort GET_BOOTLOADER_VERSION_NOK = 195;

        public const ushort ERASE_FLASH = 200;
        public const ushort ERASE_FLASH_RECEIVE_DATA_TIMEOUT = 201;
        public const ushort ERASE_FLASH_RETURNED_OK = 202;
        public const ushort ERASE_FLASH_RETURNED_NOK = 203;

        public const ushort WRITE_FLASH = 210;
        public const ushort WRITE_FLASH_RECEIVE_DATA_TIMEOUT = 211;
        public const ushort WRITE_FLASH_RETURNED_OK = 212;
        public const ushort WRITE_FLASH_RETURNED_NOK = 213;

        public const ushort WRITE_CONFIG = 220;
        public const ushort WRITE_CONFIG_RECEIVE_DATA_TIMEOUT = 221;
        public const ushort WRITE_CONFIG_RETURNED_OK = 222;
        public const ushort WRITE_CONFIG_RETURNED_NOK = 223;

        public const ushort CHECK_CHECKSUM_CONFIG = 230;
        public const ushort CHECK_CHECKSUM_CONFIG_RECEIVE_DATA_TIMEOUT = 231;
        public const ushort CHECK_CHECKSUM_CONFIG_RETURNED_OK = 232;
        public const ushort CHECK_CHECKSUM_CONFIG_RETURNED_NOK = 233;

        public const ushort RESET_SLAVE = 240;
        public const ushort RESET_SLAVE_DATA_TIMEOUT = 241;
        public const ushort RESET_SLAVE_OK = 242;
        public const ushort RESET_SLAVE_NOK = 243;
    }

    class TaskMessages
    {
        public const ushort NONE = 100;
        public const ushort RECEIVED_WRONG_COMMAND = 101;
        public const ushort RECEIVED_UNKNOWN_COMMAND = 102;
        public const ushort RECEIVED_BAD_COMMAND = 103;
        public const ushort RECEIVED_CHECKSUM_OK = 104;
        public const ushort RECEIVED_CHECKSUM_NOK = 105;
        public const ushort SWITCH_OUT_OF_BOUNDS = 106;
        public const ushort SLAVE_ID_OUT_OF_BOUNDS = 107;
     }

    class EnumClientCommands
    {
        public const ushort CLIENT_CONNECTION_REQUEST = 250;
    }

    class EnumMbusStatus
    {
        public const ushort MBUS_STATE_INIT = 0x00;
        public const ushort MBUS_STATE_WAIT = 0x01;
        public const ushort MBUS_STATE_SLAVES_ON = 0x02;
        public const ushort MBUS_STATE_SLAVES_BOOT_WAIT = 0x03;
        public const ushort MBUS_STATE_SLAVE_DETECT = 0x04;
        public const ushort MBUS_STATE_SLAVE_FW_FLASH = 0x05;
        public const ushort MBUS_STATE_SLAVE_INIT = 0x06;
        public const ushort MBUS_STATE_SLAVE_ENABLE = 0x07;
        public const ushort MBUS_STATE_START_DATA_UPLOAD = 0x08;
        public const ushort MBUS_STATE_SERVICE_TASKS = 0x09;
        public const ushort MBUS_STATE_RESET = 0x0A;
    }

    class EnumBootloader
    {
        public const ushort COMMAND_SUCCESSFUL = 0x01;     // Command Successful
        public const ushort COMMAND_UNSUCCESSFUL = 0xfd;   // Command unSuccessful 
    }
}

