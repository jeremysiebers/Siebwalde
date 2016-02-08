using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Siebwalde_Application
{
    public class FiddleYardMip50
    {
        public FiddleYardApplicationVariables FYAppVar;                             // IOHanlder connects the event handlers via this way
        public Log2LoggingFile FiddleYardMIP50Logging;                              // Logging directive       
        private FiddleYardIOHandleVariables m_FYIOHandleVar;                        // connect variable to connect to FYIOH class for defined interfaces  
        private iFiddleYardIOHandle m_iFYIOH;
        private FiddleYardApplicationVariables m_FYAppVar;                          // connect variable to connect to FYAppVar class for defined interfaces

        /*#--------------------------------------------------------------------------#*/
        /*  Description: Application variables
         * 
         *  Input(s)   :
         *
         *  Output(s)  : 
         *
         *  Returns    :
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : 
         *  
         */
        /*#--------------------------------------------------------------------------#*/
        
        private string m_instance = null;
        private string path = null;
        private enum State { CR, LF, Head, X0, X0_C0, X0_C1, E, E_C, M, Reset };
        private State MIP50ReceivedData;
        private int MIP50TransmitData;
        private string MIP50ReceivedDataBuffer = null;        
        private uint MIP50_Rec_Cmd_Counter_R = 0;
        private uint MIP50_Rec_Cmd_Counter_W = 0;
        private const uint MIP50RECCMDARRAY = 8;
        private string[] MIP50_Rec_Cmd = new string[MIP50RECCMDARRAY];        
        private string Layer = null;

        private int ActualPosition = 0;
        private bool ActualPositionUpdated = false;
        private uint Next_Track = 0;
        private uint Current_Track = 0;
        private const int TEMPOFFSET = 700;
        private uint[] TrackForward = new uint[12] { 0, 0, 42800, 85600, 128400, 171200, 214000, 256800, 299600, 342400, 385200, 428000 };// New track coordinates forward movement 1 --> 11
        private uint[] TrackBackwardOffset = new uint[12] { 0, TEMPOFFSET, TEMPOFFSET, TEMPOFFSET, TEMPOFFSET, TEMPOFFSET, TEMPOFFSET, TEMPOFFSET, TEMPOFFSET, TEMPOFFSET, TEMPOFFSET, 0 };   // New track coordinates forward movement 11 --> 1 offset number

        /*#--------------------------------------------------------------------------#*/
        /*  Description: FiddleYardApplication constructor
         * 
         *  Input(s)   : instance == TOP || BOTTOM, FYIOHancleVar (FY IO handle variables class)
                         and interface to FY IO Handler
         *
         *  Output(s)  : 
         *
         *  Returns    :
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : 
         *  
         */
        /*#--------------------------------------------------------------------------#*/
        public FiddleYardMip50(string instance, FiddleYardIOHandleVariables FYIOHandleVar, iFiddleYardIOHandle iFYIOH, FiddleYardApplicationVariables FYAppVar)
        {
            m_instance = instance;
            m_FYIOHandleVar = FYIOHandleVar;
            m_iFYIOH = iFYIOH;
            m_FYAppVar = FYAppVar;

            if ("TOP" == m_instance)
            {
                path = @"c:\localdata\Siebwalde\" + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_FiddleYardMIP50TOP.txt"; //  different logging file per target, this is default
                FiddleYardMIP50Logging = new Log2LoggingFile(path);
            }
            else if ("BOT" == m_instance)
            {
                path = @"c:\localdata\Siebwalde\" + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_FiddleYardMIP50BOT.txt"; //  different logging file per target, this is default
                FiddleYardMIP50Logging = new Log2LoggingFile(path);
            }           
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: Start: initializing and attaching all sensors, actuators,
         *               messages etc.
         * 
         *  Input(s)   :
         *
         *  Output(s)  : 
         *
         *  Returns    :
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : 
         *  
         */
        /*#--------------------------------------------------------------------------#*/
        public void Start()
        {
            //Sensors for update kick and logging, variables are updated in FiddleYardApplicationVariables            
            Sensor Sns_Mip50Rec = new Sensor("Mip50Rec", " Mip50ReceivedCmd ", 0, (name, val, log) => ReceivedMIP50Data(name, val, log)); // initialize and subscribe sensors
            m_FYIOHandleVar.Mip50Rec.Attach(Sns_Mip50Rec);

            MIP50ReceivedData = State.CR;
            MIP50TransmitData = 0;

            if (m_instance == "TOP")
                Layer = "p";
            else if (m_instance == "BOT")
                Layer = "q";
            
            FiddleYardMIP50Logging.StoreText("### Fiddle Yard MIP50 API started ###");
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: MIP50Reset
         * 
         *  Input(s)   :
         *
         *  Output(s)  : Resets all local variables
         *
         *  Returns    :
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : 
         *  
         */
        /*#--------------------------------------------------------------------------#*/
        public void MIP50Reset()
        {
            MIP50TransmitData = 0;
            MIP50ReceivedDataBuffer = null;            
            Next_Track = 0;
            MIP50ReceivedData = State.CR;
            ActualPosition = 0;
            ActualPositionUpdated = false;
            m_FYAppVar.FYHomed.BoolVariable = false;
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: ReceivedMIP50Data
         * 
         *  Input(s)   : Data received byte per byte fro MIP50
         *               Data is recieved by trailing CR/LF
         *
         *  Output(s)  : 
         *
         *  Returns    :
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : 
         */
        /*#--------------------------------------------------------------------------#*/
        private void ReceivedMIP50Data(string indicator, int val, string log)
        {
            switch (MIP50ReceivedData)
            {

                /*---------------------------------------CHECK FOR CR and LF -----------------------------------------------------------------------------------------------------------------
                */

                case State.CR:
                    if (val == 0xD)                                                     // Check if received char is CR
                    {
                        MIP50ReceivedDataBuffer = null;                                 // Clear buffer of previous data                        
                        MIP50ReceivedData = State.LF;                                   // If CR then check next char for LF
                    }
                    break;

                case State.LF:
                    if (val == 0xA)                                                     // Check if second received char is LF
                    {
                        MIP50ReceivedData = State.Head;                                 // If LF then the next chars are data from MIP check Header X / M / E
                    }
                    else { MIP50ReceivedData = State.CR; }                              // If the second char is not LF then the whole message is carbage reset switch
                    break;

                /*---------------------------------------CHECK FOR X, M, E or P -----------------------------------------------------------------------------------------------------------------
                */

                case State.Head:
                    if (val == 0x58)                                                    // When received data is X0 message
                    {
                        MIP50ReceivedData = State.X0;                                   // Next a 0x30 is expected from the X0
                    }
                    else if (val == 0x4D)                                               // When received data is M# Message
                    {
                        MIP50ReceivedDataBuffer += Convert.ToChar(val);                 // store 'M' in string array
                        MIP50ReceivedData = State.M;                                    // Next characters of the Message M are expected
                    }
                    else if (val == 0x45)                                               // When received data is E# message
                    {
                        MIP50ReceivedDataBuffer += Convert.ToChar(val);                 // store 'E' in string array
                        MIP50ReceivedData = State.E;                                    // Next characters of the Message E are expected
                    }
                    else if (val == 0xD)                                                // When the next char data is a CR then the end of the data was not found and new data is coming
                    {
                        MIP50ReceivedDataBuffer = null;                                 // clear string array
                        MIP50ReceivedData = State.LF;                                   // Goto the check for new data so next a LF must come...                    
                    }
                    else if (val == 0x50)
                    {
                        MIP50ReceivedDataBuffer += Convert.ToChar(val);                 // store 'P' in string array
                        MIP50ReceivedData = State.M;                                    // Next characters of the MIP50 power ON Message are expected
                    }
                    break;

                /*---------------------------------------X0 Message -----------------------------------------------------------------------------------------------------------------
                */

                case State.X0:
                    if (val == 0x30)                                                    // When received data is 0 from X0
                    {
                        FiddleYardMIP50Logging.StoreText("Received Acknowledge from MIP50: X0");
                        MIP50xRECxCMDxW("X0");                                          // Put X0 into mailbox
                        MIP50ReceivedData = State.CR;                                   // Start again with checking for CR
                    }
                    else { MIP50ReceivedData = State.CR; }                              // else data not OK, reset switch
                    break;                

                /*---------------------------------------M Message -----------------------------------------------------------------------------------------------------------------
                */

                case State.M:
                    if (val == 0xD)                                                     // When the next char data is a CR then the end of the data is found and next a LF is expected
                    {
                        FiddleYardMIP50Logging.StoreText(MIP50ReceivedDataBuffer);      // Log original received data fom MIP in logging file
                        MIP50xRECxCMDxW(MIP50ReceivedDataBuffer);                       // Put received data into mailbox
                        MIP50ReceivedData = State.LF;                                   // Goto the check for LF for next telegram                        
                    }
                    else
                    {
                        MIP50ReceivedDataBuffer += Convert.ToChar(val);                 // store incoming data into string array
                    }
                    break;

                /*---------------------------------------E Message -----------------------------------------------------------------------------------------------------------------
                */

                case State.E:
                    if (val == 0xD)                                                     // When the next char data is a CR then the end of the data is found and next a LF is expected
                    {
                        MIP50ReceivedData = State.E_C;                                 // Goto the check for LF in order to close the telegram                        
                    }
                    else
                    {
                        MIP50ReceivedDataBuffer += Convert.ToChar(val);                 // store incoming data into string array
                    }
                    break;

                case State.E_C:
                    if (val == 0xA)                                                     // Check if second received char is LF
                    {
                        FiddleYardMIP50Logging.StoreText(MIP50ReceivedDataBuffer);      // Log original received data fom MIP in logging file
                        MIP50xRECxCMDxW(MIP50ReceivedDataBuffer);                       // Put received data into mailbox
                        MIP50ReceivedData = State.CR;                                   // If LF then the data from the MIP is closed Reset switch                        
                    }
                    else
                    {
                        MIP50ReceivedDataBuffer = null;                                 // Clear array: the second char is not LF then the whole message is carbage reset switch
                        MIP50ReceivedData = State.CR;                                   // Start again with checking for CR                                
                    }
                    break;

                default: break;
            }
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: MIP50xRECxCMDxW 
         * 
         *  Input(s)   : String received from MIP
         *
         *  Output(s)  : Increments wright counter MIP50_Rec_Cmd_Counter_W
         *
         *  Returns    : 
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : 
         */
        /*#--------------------------------------------------------------------------#*/
        private void MIP50xRECxCMDxW(string Cmd)
        {
            MIP50_Rec_Cmd[MIP50_Rec_Cmd_Counter_W] = Cmd;
            MIP50_Rec_Cmd_Counter_W++;
            if (MIP50_Rec_Cmd_Counter_W >= MIP50RECCMDARRAY)
            {
                MIP50_Rec_Cmd_Counter_W = 0;
            }
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: MIP50xRECxCMDxR 
         * 
         *  Input(s)   : 
         *
         *  Output(s)  : When mailbox is filled returns next mail item
         *
         *  Returns    : 
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : 
         */
        /*#--------------------------------------------------------------------------#*/
        private string MIP50xRECxCMDxR()
        {
            string _Return = "";

            if (MIP50_Rec_Cmd_Counter_R != MIP50_Rec_Cmd_Counter_W)
            {
                _Return = MIP50_Rec_Cmd[MIP50_Rec_Cmd_Counter_R];
                MIP50_Rec_Cmd_Counter_R++;
                if (MIP50_Rec_Cmd_Counter_R >= MIP50RECCMDARRAY)
                {
                    MIP50_Rec_Cmd_Counter_R = 0;
                }
            }
            
            return (_Return);
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: MIP50xMOVExCALCULATE next move with correction for direction
         * 
         *  Input(s)   : Absolute new track number
         *
         *  Output(s)  : Sets variable Next_Track with absolute track number
         *
         *  Returns    : 
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : 
         */
        /*#--------------------------------------------------------------------------#*/
        public void MIP50xMOVExCALC(uint New_Track)
        {
            FiddleYardMIP50Logging.StoreText("---------------------------------------------------------------------");
            MIP50xReadxPosition();            
            Next_Track = New_Track;            
            if (New_Track < 12)
            {
                Current_Track = New_Track; // Already store track te become.(memory when next time track++ or track-- is used)                
                FiddleYardMIP50Logging.StoreText("MIP50 Start Absolute Move to track " + Convert.ToString(Next_Track));
            }
            else
            {
                if (New_Track == 12 && Current_Track < 11)
                {
                    FiddleYardMIP50Logging.StoreText("MIP50 Start Absolute Move to track " + Convert.ToString(Current_Track + 1));
                }
                else if (New_Track == 13 && Current_Track > 1)
                {
                    FiddleYardMIP50Logging.StoreText("MIP50 Start Absolute Move to track " + Convert.ToString(Current_Track - 1));
                }                
            }  
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: MIP50 command Move (absolute moves)
         * 
         *  Input(s)   : 
         *
         *  Output(s)  : Char by char over UDP to uController to RS232 to MIP50
         *
         *  Returns    : When done returns "Finished"
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : Keep calling until finished or error
         */
        /*#--------------------------------------------------------------------------#*/
        public string MIP50xMOVE()
        {
            string _Return = "Running";
            string m_MIP50xRECxCMDxR = null;

            switch (MIP50TransmitData)
            {
                case 0:
                    m_MIP50xRECxCMDxR = MIP50xRECxCMDxR();
                    if (m_MIP50xRECxCMDxR == "X0")
                    {
                        MIP50TransmitData = 1;
                    }
                    else if (m_MIP50xRECxCMDxR != "")
                    {
                        if (MIP50xTranslatexME(m_MIP50xRECxCMDxR) == false)                     // Translate message or error and set local variables accordingly, when no error but message a true is returned
                        {
                            MIP50TransmitData = 0;
                            _Return = "Error";
                        }
                    }                   
                    break;

                case 1:
                    if (ActualPositionUpdated == true)                                              // Check if actualposition is updated
                    {
                        ActualPositionUpdated = false;                                              // Set local bool to false for future next update-and-check

                        if (Next_Track == 12 && Current_Track < 11)                                 // Track ++
                        {
                            Next_Track = TrackForward[Current_Track + 1];
                            Current_Track += 1;
                            MIP50TransmitData = 2;
                        }
                        else if (Next_Track == 13 && Current_Track > 1)                             // Track --
                        {
                            Next_Track = TrackForward[Current_Track - 1] - TrackBackwardOffset[Current_Track - 1];
                            Current_Track -= 1;
                            MIP50TransmitData = 2;
                        }
                        else if (Next_Track != 13 && Next_Track != 12)                              // Check for bounds when still track++ or track-- is pressed
                        {
                            if (TrackForward[Next_Track] > ActualPosition)
                            {
                                Next_Track = TrackForward[Next_Track];
                            }
                            else if (TrackForward[Next_Track] < ActualPosition)
                            {
                                Next_Track = TrackForward[Next_Track] - TrackBackwardOffset[Next_Track];
                            }
                            MIP50TransmitData = 2;
                        }
                        else
                        {
                            FiddleYardMIP50Logging.StoreText("MIP50 New position same as current position");
                            FiddleYardMIP50Logging.StoreText("MIP50 Absolute Move Routine Finished.");
                            MIP50TransmitData = 0;
                            _Return = "Finished";
                        }
                    }
                    else
                    {
                        MIP50TransmitData = 0;
                        MIP50xReadxPosition();                                                          // when position was not read, execute again and restart
                    }
                    break;

                case 2:
                    FiddleYardMIP50Logging.StoreText("MIP50 Start Absolute Move to " + Convert.ToString(Next_Track));
                    MIP50xActivatexPosxReg();                    
                    MIP50TransmitData = 3;
                    break;

                case 3:
                    m_MIP50xRECxCMDxR = MIP50xRECxCMDxR();
                    if (m_MIP50xRECxCMDxR == "X0")
                    {
                        MIP50TransmitData = 4;
                    }
                    else if (m_MIP50xRECxCMDxR != "")
                    {
                        if (MIP50xTranslatexME(m_MIP50xRECxCMDxR) == false)                     // Translate message or error and set local variables accordingly, when no error but message a true is returned
                        {
                            MIP50TransmitData = 0;
                            _Return = "Error";
                        }
                    }
                    break;

                case 4:
                    MIP50xAbs_Pos(Next_Track);
                    MIP50TransmitData = 5;
                    break;

                case 5:
                    m_MIP50xRECxCMDxR = MIP50xRECxCMDxR();
                    if (m_MIP50xRECxCMDxR == "X0")
                    {
                        MIP50TransmitData = 6;
                    }
                    else if (m_MIP50xRECxCMDxR != "")
                    {
                        if (MIP50xTranslatexME(m_MIP50xRECxCMDxR) == false)                     // Translate message or error and set local variables accordingly, when no error but message a true is returned
                        {
                            MIP50TransmitData = 0;
                            _Return = "Error";
                        }
                    }
                    break;

                case 6:
                    MIP50xDeactivatexPosxReg();
                    MIP50TransmitData = 7;
                    break;

                case 7:
                    m_MIP50xRECxCMDxR = MIP50xRECxCMDxR();
                    if (m_MIP50xRECxCMDxR == "X0")
                    {
                        MIP50xReadxPosition();
                        MIP50TransmitData = 8;
                    }
                    else if (m_MIP50xRECxCMDxR != "")
                    {
                        if (MIP50xTranslatexME(m_MIP50xRECxCMDxR) == false)                     // Translate message or error and set local variables accordingly, when no error but message a true is returned
                        {
                            MIP50TransmitData = 0;
                            _Return = "Error";
                        }
                    }
                    break;               

                case 8:
                    m_MIP50xRECxCMDxR = MIP50xRECxCMDxR();
                    if (m_MIP50xRECxCMDxR == "X0")
                    {
                        MIP50TransmitData = 0;
                        ActualPositionUpdated = false;                                          // Set local bool to false for future next update-and-check
                        FiddleYardMIP50Logging.StoreText("MIP50 Absolute Move Routine Finished.");
                        _Return = "Finished";
                    }
                    else if (m_MIP50xRECxCMDxR != "")
                    {
                        if (MIP50xTranslatexME(m_MIP50xRECxCMDxR) == false)                     // Translate message or error and set local variables accordingly, when no error but message a true is returned
                        {
                            MIP50TransmitData = 0;
                            _Return = "Error";
                        }
                    }
                    break;

                default: break;
            }
            return (_Return);
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: MIP50 command HOME
         * 
         *  Input(s)   : Execute Homing procedure on MIP50
         *
         *  Output(s)  : Char by char over UDP to uController to RS232 to MIP50
         *
         *  Returns    : When done returns "Finished"
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : Keep calling until finished or error
         */
        /*#--------------------------------------------------------------------------#*/
        public string MIP50xHOME()
        {
            string _Return = "Running";
            string m_MIP50xRECxCMDxR = null;

            switch (MIP50TransmitData)
            {
                case 0:
                    FiddleYardMIP50Logging.StoreText("---------------------------------------------------------------------");
                    FiddleYardMIP50Logging.StoreText("MIP50 Start Homing Routine:");                    
                    MIP50xDeactivatexPosxReg();                    
                    MIP50TransmitData = 1;
                    break;

                case 1:
                    m_MIP50xRECxCMDxR = MIP50xRECxCMDxR();
                    if (m_MIP50xRECxCMDxR == "X0")
                    {
                        MIP50TransmitData = 2;
                    }
                    else if(m_MIP50xRECxCMDxR != "")
                    {
                        if(MIP50xTranslatexME(m_MIP50xRECxCMDxR) == false)                     // Translate message or error and set local variables accordingly, when no error but message a true is returned
                        {
                            MIP50TransmitData = 0;
                            _Return = "Error";                            
                        }
                    }                    
                    break;

                case 2:
                    MIP50xClearxError();                    
                    MIP50TransmitData = 3;
                    break;

                case 3:
                    m_MIP50xRECxCMDxR = MIP50xRECxCMDxR();
                    if (m_MIP50xRECxCMDxR == "X0")
                    {
                        MIP50TransmitData = 4;
                    }
                    else if (m_MIP50xRECxCMDxR != "")
                    {
                        if (MIP50xTranslatexME(m_MIP50xRECxCMDxR) == false)                     // Translate message or error and set local variables accordingly, when no error but message a true is returned
                        {
                            MIP50TransmitData = 0;
                            _Return = "Error";
                        }
                    }
                    break;

                case 4:
                    MIP50xSetxAcceleration();                    
                    MIP50TransmitData = 5;
                    break;

                case 5:
                    m_MIP50xRECxCMDxR = MIP50xRECxCMDxR();
                    if (m_MIP50xRECxCMDxR == "X0")
                    {
                        MIP50TransmitData = 6;
                    }
                    else if (m_MIP50xRECxCMDxR != "")
                    {
                        if (MIP50xTranslatexME(m_MIP50xRECxCMDxR) == false)                     // Translate message or error and set local variables accordingly, when no error but message a true is returned
                        {
                            MIP50TransmitData = 0;
                            _Return = "Error";
                        }
                    }
                    break;

                case 6:
                    MIP50xSetxPositioningxVelxDefault();                    
                    MIP50TransmitData = 7;
                    break;

                case 7:
                    m_MIP50xRECxCMDxR = MIP50xRECxCMDxR();
                    if (m_MIP50xRECxCMDxR == "X0")
                    {
                        MIP50TransmitData = 8;
                    }
                    else if (m_MIP50xRECxCMDxR != "")
                    {
                        if (MIP50xTranslatexME(m_MIP50xRECxCMDxR) == false)                     // Translate message or error and set local variables accordingly, when no error but message a true is returned
                        {
                            MIP50TransmitData = 0;
                            _Return = "Error";
                        }
                    }
                    break;

                case 8:
                    MIP50xActivatexPosxReg();                    
                    MIP50TransmitData = 9;
                    break;

                case 9:
                    m_MIP50xRECxCMDxR = MIP50xRECxCMDxR();
                    if (m_MIP50xRECxCMDxR == "X0")
                    {
                        MIP50TransmitData = 10;
                    }
                    else if (m_MIP50xRECxCMDxR != "")
                    {
                        if (MIP50xTranslatexME(m_MIP50xRECxCMDxR) == false)                     // Translate message or error and set local variables accordingly, when no error but message a true is returned
                        {
                            MIP50TransmitData = 0;
                            _Return = "Error";
                        }
                    }
                    break;

                case 10:
                    MIP50xHomexAxis();                    
                    MIP50TransmitData = 11;
                    break;

                case 11:
                    m_MIP50xRECxCMDxR = MIP50xRECxCMDxR();
                    if (m_MIP50xRECxCMDxR == "X0")
                    {
                        MIP50TransmitData = 12;
                    }
                    else if (m_MIP50xRECxCMDxR != "")
                    {
                        if (MIP50xTranslatexME(m_MIP50xRECxCMDxR) == false)                     // Translate message or error and set local variables accordingly, when no error but message a true is returned
                        {
                            MIP50TransmitData = 0;
                            _Return = "Error";
                        }
                    }
                    break;

                case 12:
                    MIP50xDeactivatexPosxReg();                    
                    MIP50TransmitData = 13;
                    break;

                case 13:
                    m_MIP50xRECxCMDxR = MIP50xRECxCMDxR();
                    if (m_MIP50xRECxCMDxR == "X0")
                    {
                        MIP50TransmitData = 0;
                        Current_Track = 1;
                        FiddleYardMIP50Logging.StoreText("MIP50 Current Track = 1.");
                        m_FYAppVar.FYHomed.BoolVariable = true;
                        FiddleYardMIP50Logging.StoreText("MIP50 Homing Routine FYAppVar.FYHomed = true");
                        FiddleYardMIP50Logging.StoreText("MIP50 Homing Routine Finished.");                        
                        _Return = "Finished";
                    }
                    else if (m_MIP50xRECxCMDxR != "")
                    {
                        if (MIP50xTranslatexME(m_MIP50xRECxCMDxR) == false)                     // Translate message or error and set local variables accordingly, when no error but message a true is returned
                        {
                            MIP50TransmitData = 0;
                            _Return = "Error";
                        }
                    }
                    break;

                default: break;
            }
            return (_Return);
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: MIP50xAbs_Pos
         * 
         *  Input(s)   : New_Track in raw count value
         *
         *  Output(s)  : Char by char over UDP to uController to RS232 to MIP50
         *
         *  Returns    : 
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : Start absolute move
         */
        /*#--------------------------------------------------------------------------#*/
        private void MIP50xAbs_Pos(uint New_Track)
        {
            string m_New_Track = Convert.ToString(New_Track);

            m_iFYIOH.ActuatorCmd("", Layer + "A" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "1" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "x" + "\r");

            foreach (char c in m_New_Track)
            {
                m_iFYIOH.ActuatorCmd("", Layer + c + "\r");
            }

            m_iFYIOH.ActuatorCmd("", Layer + "G" + "\r");
            MIP50xCRLFxAppend();
            FiddleYardMIP50Logging.StoreText("MIP50 Start Absolute Move...");
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: MIP50xHomexAxis
         * 
         *  Input(s)   : 
         *
         *  Output(s)  : Char by char over UDP to uController to RS232 to MIP50
         *
         *  Returns    : 
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : Execute Homing procedure
         */
        /*#--------------------------------------------------------------------------#*/
        private void MIP50xHomexAxis()
        {
            m_iFYIOH.ActuatorCmd("", Layer + "H" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "1" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "x" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "G" + "\r");
            MIP50xCRLFxAppend();
            FiddleYardMIP50Logging.StoreText("MIP50 Start Homing...");
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: MIP50xSetxPositioningxVelxDefault
         * 
         *  Input(s)   : 
         *
         *  Output(s)  : Char by char over UDP to uController to RS232 to MIP50
         *
         *  Returns    : 
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : Set Velocity parameter to MIP
         */
        /*#--------------------------------------------------------------------------#*/
        private void MIP50xSetxPositioningxVelxDefault()
        {
            m_iFYIOH.ActuatorCmd("", Layer + "V" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "1" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "x" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "1" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "0" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "0" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "G" + "\r");
            MIP50xCRLFxAppend();
            FiddleYardMIP50Logging.StoreText("MIP50 Set Velocity to 100 Qc/ms");
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: MIP50xSetxAcceleration
         * 
         *  Input(s)   : 
         *
         *  Output(s)  : Char by char over UDP to uController to RS232 to MIP50
         *
         *  Returns    : 
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : Set Acceleration parameter to MIP
         */
        /*#--------------------------------------------------------------------------#*/
        private void MIP50xSetxAcceleration()
        {
            m_iFYIOH.ActuatorCmd("", Layer + "C" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "1" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "x" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "2" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "r" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "2" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "t" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "0" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "." + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "2" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "G" + "\r");
            MIP50xCRLFxAppend();
            FiddleYardMIP50Logging.StoreText("MIP50 Set Accleration and deceleration to 0.2 Qc/ms^2");
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: MIP50xClearxError
         * 
         *  Input(s)   : 
         *
         *  Output(s)  : Char by char over UDP to uController to RS232 to MIP50
         *
         *  Returns    : 
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : Try to clear error on MIP50 (if there is a error or warning
         *               if not the MIP will respond with message or error?
         */
        /*#--------------------------------------------------------------------------#*/
        private void MIP50xClearxError()
        {
            m_iFYIOH.ActuatorCmd("", Layer + "E" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "1" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "x" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "G" + "\r");
            MIP50xCRLFxAppend();
            FiddleYardMIP50Logging.StoreText("MIP50 Try Clear All Errors");
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: MIP50xActivatexPosxReg
         *               
         *  Input(s)   : 
         *
         *  Output(s)  : Char by char over UDP to uController to RS232 to MIP50
         *
         *  Returns    : 
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : Enable position regulation
         */
        /*#--------------------------------------------------------------------------#*/
        private void MIP50xActivatexPosxReg()
        {
            m_iFYIOH.ActuatorCmd("", Layer + "n" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "1" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "x" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "G" + "\r");
            MIP50xCRLFxAppend();
            FiddleYardMIP50Logging.StoreText("MIP50 Activate Position Regulation");
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: MIP50xDeactivatexPosxReg
         * 
         *  Input(s)   : Disable position regulation
         *
         *  Output(s)  : Char by char over UDP to uController to RS232 to MIP50
         *
         *  Returns    : 
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : 
         */
        /*#--------------------------------------------------------------------------#*/
        private void MIP50xDeactivatexPosxReg()
        {
            m_iFYIOH.ActuatorCmd("", Layer + "f" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "1" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "x" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "G" + "\r");
            MIP50xCRLFxAppend();
            FiddleYardMIP50Logging.StoreText("MIP50 Deactivate Position Regulation");
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: MIP50xReadxPosition
         * 
         *  Input(s)   : Command to send back actual position
         *
         *  Output(s)  : Char by char over UDP to uController to RS232 to MIP50
         *
         *  Returns    : 
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : Response must be captured and interpreterd
         */
        /*#--------------------------------------------------------------------------#*/
        private void MIP50xReadxPosition()
        {
            m_iFYIOH.ActuatorCmd("", Layer + "P" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "1" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "x" + "\r");
            m_iFYIOH.ActuatorCmd("", Layer + "G" + "\r");
            MIP50xCRLFxAppend();
            FiddleYardMIP50Logging.StoreText("MIP50 Read Actual Position");
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: MIP50xCRLFxAppend
         * 
         *  Input(s)   : 
         *
         *  Output(s)  : Char by char over UDP to uController to RS232 to MIP50
         *
         *  Returns    : 
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : Sends additional CR/LF for terminal clearity, not used\required
         *               by MIP50
         */
        /*#--------------------------------------------------------------------------#*/
        private void MIP50xCRLFxAppend()                                      
        {
            m_iFYIOH.ActuatorCmd("", Layer + "\n" + "\r"); // Carriage return
            m_iFYIOH.ActuatorCmd("", Layer + "\r" + "\r"); // Line feed
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: TransmitMIP50Data
         * 
         *  Input(s)   : Data Transmitted byte per byte to MIP50
         *               Data is transmitted and closed by G + CR/LF for terminal overview
         *               W.r.t. m_instance data is send to TOP or BOT MIP50
         *
         *  Output(s)  : Char by char over UDP to uController to RS232 to MIP50
         *
         *  Returns    : When done returns "Finished"
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : Used by higher level routined who want to send strings to MIP50
         */
        /*#--------------------------------------------------------------------------#*/
        private string TransmitMIP50Data(string command)
        {
            string _Return = "Running";
            string m_command = command;

            switch (MIP50TransmitData)
            {
                case 0:
                    //bla
                    break;

                default: break;
            }
            return (_Return);
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: MIP50xTranslatexME
         * 
         *  Input(s)   : 
         *
         *  Output(s)  : Sets appropiate variables regarding received message or error
         *
         *  Returns    : 
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : Translates received messages and errors to readable text for
         *               logging and sets appropiate variables accordingly
         */
        /*#--------------------------------------------------------------------------#*/
        bool MIP50xTranslatexME(string MIP50ReceivedDataBuffer)
        {
            bool _Return = false;           
            string[] m_MIP50ReceivedDataBuffer = MIP50ReceivedDataBuffer.Split('#',' ');
            int i = 0, j = 0;
            StringBuilder LogString = new StringBuilder();

            if (m_MIP50ReceivedDataBuffer[0] == "M")
            {
                switch(m_MIP50ReceivedDataBuffer[1])
                {
                    case "20": // M#20 %bn %bv
                        LogString.Append("Error number (after a clear error command), axis nr: ");
                        for (i = 2; i < m_MIP50ReceivedDataBuffer.Length; i++)
                        {
                            if (m_MIP50ReceivedDataBuffer[i] != "")
                            {
                                if (j == 0)
                                {
                                    LogString.Append(m_MIP50ReceivedDataBuffer[i] + " , Error: ");
                                }
                                else if (j == 1)
                                {
                                    LogString.Append(m_MIP50ReceivedDataBuffer[i]);
                                }
                                j++;
                            }
                        }
                        FiddleYardMIP50Logging.StoreText(LogString.ToString());
                        _Return = true;
                        break;

                    case "21": // M#21 %bn %lv --> 3 items
                        ActualPosition = Convert.ToInt32(m_MIP50ReceivedDataBuffer[m_MIP50ReceivedDataBuffer.Length - 1]);      // In case of command position, the position data comes as last in the array
                        ActualPositionUpdated = true;                        
                        LogString.Append( "Message: Momentary or command position, axis nr: ");
                        for (i = 2; i < m_MIP50ReceivedDataBuffer.Length; i++)
                        {
                            if (m_MIP50ReceivedDataBuffer[i] != "")
                            {
                                if (j == 0)
                                {
                                    LogString.Append(m_MIP50ReceivedDataBuffer[i] + " , position: ");
                                }
                                else if (j == 1)
                                {
                                    LogString.Append(m_MIP50ReceivedDataBuffer[i]);
                                }
                                j++;
                            }
                        }
                        FiddleYardMIP50Logging.StoreText(LogString.ToString());
                        _Return = true;
                        break;

                    default: break;
                }
            }           

            else if (m_MIP50ReceivedDataBuffer[0] == "E")
            {
                
            }
            else if (m_MIP50ReceivedDataBuffer[0] == "P0")
            {
                //nop
            }

            return (_Return);                          
        }
    }
}