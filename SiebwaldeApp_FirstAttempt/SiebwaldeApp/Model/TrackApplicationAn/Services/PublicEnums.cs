using System.Collections;
using System.Collections.Generic;

namespace SiebwaldeApp
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

        public const byte CONTROLLERDATA = 60;
        public const byte CONTROLLERALIVE = 0xFF;
        public const byte TIME = 132;

        //public const string HOMEPATH = @"c:\localdata\Siebwalde\";
        //public const string LOGGING = "Logging\\";
        public const string TRACKTARGET = "192.168.1.177"; //"192.168.1.50"; //"TRACKCONTROLLER";

        public static readonly Dictionary<int, string> NumberTextMap = new Dictionary<int, string>
        {
            { 1,  "MAIN_LOOP" },
            { 10, "MAIN_STATION_TOP" },
            { 20, "MAIN_STATION_BOT" },
            { 30, "WALDSEE" },
            { 40, "SIEBWALDE" },
            { 50, "WALDBERG" },
            { 60, "DATA" },

            { 81, "STNTRACK1" },
            { 82, "STNTRACK2" },
            { 83, "STNTRACK3" },
            { 84, "MTNTRACK1" },
            { 85, "MTNTRACK2" },
            { 86, "MTNTRACK7" },
            { 87, "MTNTRACK8" },
            { 88, "VOLTAGE_DETECTED" },

            {100, "busy"},
            {101, "connected"},
            {102, "done"},
            {103, "command"},
            {104, "nop"},
            {105, "INIT"},
            {106, "INIT2"},
            {107, "RUN"},
            {108, "WAIT"},
            {109, "IDLE"},
            {110, "STN_TO_SIEBWALDE"},
            {111, "SIEBWALDE_TO_STN"},
            {112, "SIEBWALDE_SWITCHT4T5_OUT"},
            {113, "SIEBWALDE_SWITCHT4T5_IN"},
            {114, "SIEBWALDE_SWITCHT4T5"},
            {115, "SIEWALDE_SET_NEXT"},
            {116, "STN_INBOUND"},
            {117, "STN_OUTBOUND"},
            {118, "STN_PASSING"},
            {119, "STN_WAIT"},
            {120, "STN_IDLE"},
            {121, "SEQ_IDLE"},
            {122, "SEQ_WAIT"},
            {123, "SEQ_SET_OCC"},
            {124, "SEQ_CHK_TRAIN"},
            {125, "SEQ_CHK_PASSED"},
            {126, "SEQ_OUTBOUND_LEFT_STATTION"},
            {127, "SEQ_INBOUND_BRAKE_TIME"},
            {128, "SEQ_OUTBOUND_BRAKE_TIME"},
            {129, "SIG_RED"},
            {130, "SIG_GREEN"},
            {131, "INVERT"},
            {132, "TIME"},
            {133, "SET_PATH_WAY"},
            {134, "SET_SIGNAL"},

            {150, "NONE"},
            {151, "T1"},
            {152, "T2"},
            {153, "T3"},
            {154, "T4"},
            {155, "T5"},
            {157, "T7"},
            {158, "T8"},
            {159, "TRACK1"},
            {160, "TRACK2"},
            {161, "TRACK3"},
            {162, "TRACK10"},
            {163, "TRACK11"},
            {164, "TRACK12"},
        };

        public static string TranslateNumber(int number)
        {
            if (NumberTextMap.ContainsKey(number))
            {
                return NumberTextMap[number];
            }
            else
            {
                return $"No translation found for the number: {number}";
            }
        }


    }


}

