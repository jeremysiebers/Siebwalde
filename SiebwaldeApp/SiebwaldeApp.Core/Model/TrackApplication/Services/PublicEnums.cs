namespace SiebwaldeApp.Core
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

        public static bool operator ==(ReceivedMessage c1, ReceivedMessage c2)
        {
            return c1.Equals(c2);
        }

        public static bool operator !=(ReceivedMessage c1, ReceivedMessage c2)
        {
            return !c1.Equals(c2);
        }
    }

    public struct SendMessage
    {        
        public byte Command;
        public byte[] Data;

        public SendMessage(byte command, byte[] data)
        {
            Command = command;
            Data = data;
        }

        public static bool operator ==(SendMessage c1, SendMessage c2)
        {
            return c1.Equals(c2);
        }

        public static bool operator !=(SendMessage c1, SendMessage c2)
        {
            return !c1.Equals(c2);
        }
    }

    public static class Enums
    {
        public const byte HEADER = 0xAA;
        public const byte FOOTER = 0x55;

        public const byte SLAVEINFO = 0xFF;

        //public const string HOMEPATH = @"c:\localdata\Siebwalde\";
        //public const string LOGGING = "Logging\\";
        public const string TRACKTARGET = "TRACKCONTROLLER";
        public const string SLAVEHEXFILE = "TrackAmplifier4.X.production.hex";
        public const int SPACELENGTH = 40;

        public const uint Busy = 0;
        public const uint Finished = 1;
        public const uint Next = 2;
        public const uint Standby = 2;
        public const uint Error = 11;

        /// <summary>
        /// Bootloader ENUMS
        /// </summary>
        public const ushort PROGMEMSIZE = 0x8000;
        public const ushort BOOTLOADEROFFSET = 0x800;
        public const ushort HEXROWWIDTH = 16;
        public const ushort JUMPSIZE = 4;

    }

    public static class TaskId
    {
        public const byte CONTROLLER = 10;
        public const byte MBUS = 20;
        public const byte FWHANDLER = 30;
    }

    public static class TaskStates
    {
        public const byte ABORT = 4;
        public const byte BUSY = 5;
        public const byte CONNECTED = 6;
        public const byte DONE = 7;
        public const byte COMMAND = 8;
        public const byte ERROR = 9;
    }

    public static class TrackCommand
    {
        /* FW HANDLER TASK COMMANDS */
        public const byte FWHANDLERINIT = 31;
        public const byte FWFILEDOWNLOAD = 32;
        public const byte FWCONFIGWORDDOWNLOAD = 33;
        public const byte FWFLASHSLAVES = 34;
        public const byte FWFLASHSEQUENCER = 35;

        /* MBUS COMMANDS */                            /* case states cannot have high numbers! */
        public const byte EXEC_MBUS_STATE_SLAVES_ON = 100;
        public const byte EXEC_MBUS_STATE_SLAVE_DETECT = 101;
        public const byte EXEC_MBUS_STATE_SLAVES_BOOT_WAIT = 102;
        public const byte EXEC_MBUS_STATE_SLAVE_FW_FLASH = 103;
        public const byte EXEC_MBUS_STATE_SLAVE_INIT = 104;
        public const byte EXEC_MBUS_STATE_SLAVE_ENABLE = 105;
        public const byte EXEC_MBUS_STATE_START_DATA_UPLOAD = 106;
        public const byte EXEC_MBUS_STATE_RESET = 107;

        /* FWHANDLER COMMANDS */
        public const byte EXEC_FW_STATE_RECEIVE_FW_FILE = 120;
        public const byte EXEC_FW_STATE_RECEIVE_CONFIG_WORD = 121;
        public const byte EXEC_FW_STATE_FLASH_ALL_SLAVES = 122;
        public const byte EXEC_FW_STATE_SELECT_SLAVE = 123;
        public const byte EXEC_FW_STATE_GET_BOOTLOADER_VERSION = 124;
        public const byte EXEC_FW_STATE_ERASE_FLASH = 125;
        public const byte EXEC_FW_STATE_WRITE_FLASH = 126;
        public const byte EXEC_FW_STATE_WRITE_CONFIG = 127;
        public const byte EXEC_FW_STATE_CHECK_CHECKSUM = 128;
        public const byte EXEC_FW_STATE_SLAVE_RESET = 129;
        public const byte EXIT_SLAVExFWxHANDLER = 130;

        /* FWFILEDOWNLOAD COMMANDS */
        public const byte FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY = 140;
        public const byte FILEDOWNLOAD_STATE_FW_DATA_RECEIVE = 141;
        public const byte FILEDOWNLOAD_STATE_FW_DATA_DOWNLOAD_DONE = 142;
        public const byte FILEDOWNLOAD_STATE_FW_CHECKSUM = 143;

        /* FWCONFIGWORDDOWNLOAD COMMANDS */
        public const byte CONFIGWORDDOWNLOAD_STATE_RECEIVE_CONFIG_WORD_STANDBY = 150;
        public const byte CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_RECEIVE = 151;
        public const byte CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_DOWNLOAD_DONE = 152;

        /* FWFLASHSLAVES */
        public const byte FWFLASHSLAVES_STATE_CHECK_CHECKSUM = 161;
        public const byte FWFLASHSLAVES_STATE_SLAVE_FLASH = 162;

        /* FWFLASHSEQUENCER */
        public const byte FWFLASHSEQUENCER_STATE_FLASHED_SLAVE = 170;
        public const byte FWFLASHSEQUENCER_STATE_GET_BOOTLOADER_VERSION = 171;
        public const byte FWFLASHSEQUENCER_STATE_ERASE_FLASH = 172;
        public const byte FWFLASHSEQUENCER_STATE_WRITE_FLASH = 173;
        public const byte FWFLASHSEQUENCER_STATE_WRITE_CONFIG = 174;
        public const byte FWFLASHSEQUENCER_STATE_CHECK_CHECKSUM = 175;
        public const byte FWFLASHSEQUENCER_STATE_RESET_SLAVE = 176;

        /* SLAVEBOOTLOADERROUTINES */
        public const byte BOOTLOADER_DATA_RECEIVE_ERROR = 180;
        public const byte BOOTLOADER_START_BYTE_ERROR = 181;

        public const byte GET_BOOTLOADER_VERSION = 192;
        public const byte GET_BOOTLOADER_VERSION_RECEIVE_DATA_TIMEOUT = 193;
        public const byte GET_BOOTLOADER_VERSION_OK = 194;
        public const byte GET_BOOTLOADER_VERSION_NOK = 195;

        public const byte ERASE_FLASH = 200;
        public const byte ERASE_FLASH_RECEIVE_DATA_TIMEOUT = 201;
        public const byte ERASE_FLASH_RETURNED_OK = 202;
        public const byte ERASE_FLASH_RETURNED_NOK = 203;

        public const byte WRITE_FLASH = 210;
        public const byte WRITE_FLASH_RECEIVE_DATA_TIMEOUT = 211;
        public const byte WRITE_FLASH_RETURNED_OK = 212;
        public const byte WRITE_FLASH_RETURNED_NOK = 213;

        public const byte WRITE_CONFIG = 220;
        public const byte WRITE_CONFIG_RECEIVE_DATA_TIMEOUT = 221;
        public const byte WRITE_CONFIG_RETURNED_OK = 222;
        public const byte WRITE_CONFIG_RETURNED_NOK = 223;

        public const byte CHECK_CHECKSUM_CONFIG = 230;
        public const byte CHECK_CHECKSUM_CONFIG_RECEIVE_DATA_TIMEOUT = 231;
        public const byte CHECK_CHECKSUM_CONFIG_RETURNED_OK = 232;
        public const byte CHECK_CHECKSUM_CONFIG_RETURNED_NOK = 233;

        public const byte RESET_SLAVE = 240;
        public const byte RESET_SLAVE_DATA_TIMEOUT = 241;
        public const byte RESET_SLAVE_OK = 242;
        public const byte RESET_SLAVE_NOK = 243;
    }

    public class TaskMessages
    {
        public const byte NONE = 100;
        public const byte RECEIVED_WRONG_COMMAND = 101;
        public const byte RECEIVED_UNKNOWN_COMMAND = 102;
        public const byte RECEIVED_BAD_COMMAND = 103;
        public const byte RECEIVED_CHECKSUM_OK = 104;
        public const byte RECEIVED_CHECKSUM_NOK = 105;
        public const byte SWITCH_OUT_OF_BOUNDS = 106;
        public const byte SLAVE_ID_OUT_OF_BOUNDS = 107;
     }

    public class EnumClientCommands
    {
        public const byte CLIENT_CONNECTION_REQUEST = 250;
    }

    public class EnumMbusStatus
    {
        public const byte MBUS_STATE_INIT = 0x00;
        public const byte MBUS_STATE_WAIT = 0x01;
        public const byte MBUS_STATE_SLAVES_ON = 0x02;
        public const byte MBUS_STATE_SLAVES_BOOT_WAIT = 0x03;
        public const byte MBUS_STATE_SLAVE_DETECT = 0x04;
        public const byte MBUS_STATE_SLAVE_FW_FLASH = 0x05;
        public const byte MBUS_STATE_SLAVE_INIT = 0x06;
        public const byte MBUS_STATE_SLAVE_ENABLE = 0x07;
        public const byte MBUS_STATE_START_DATA_UPLOAD = 0x08;
        public const byte MBUS_STATE_SERVICE_TASKS = 0x09;
        public const byte MBUS_STATE_RESET = 0x0A;
    }

    public class EnumBootloader
    {
        public const byte COMMAND_SUCCESSFUL = 0x01;     // Command Successful
        public const byte COMMAND_UNSUCCESSFUL = 0xfd;   // Command unSuccessful 
    }
}

