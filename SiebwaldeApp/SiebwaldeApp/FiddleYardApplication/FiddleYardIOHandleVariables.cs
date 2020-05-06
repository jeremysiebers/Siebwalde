namespace SiebwaldeApp
{
    public class FiddleYardIOHandleVariables
    {
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
        public SensorUpdater CL10Heart;
        public SensorUpdater F11;
        public SensorUpdater EOS10;
        public SensorUpdater EOS11;
        public SensorUpdater F13;
        public SensorUpdater F12;
        public SensorUpdater Block5B;
        public SensorUpdater Block8A;
        public SensorUpdater TrackPower;
        public SensorUpdater Block5BIn;
        public SensorUpdater Block6In;
        public SensorUpdater Block7In;
        public SensorUpdater Resistor;
        public SensorUpdater Block6;
        public SensorUpdater Block7;
        public SensorUpdater F10;
        public SensorUpdater M10;
        public SensorUpdater TrackNo;
        public SensorUpdater TrackPower15V;                 // This is sensor indicator
        public SensorUpdater CmdBusy;
        public SensorUpdater Mip50Rec;

        // Create Messages
        //public MessageUpdater FiddleOneLeft;
        //public MessageUpdater FiddleOneRight;
        //public MessageUpdater FiddleMultipleLeft;
        //public MessageUpdater FiddleMultipleRight;
        //public MessageUpdater TrainDetection;
        //public MessageUpdater TrainOn5B;
        //public MessageUpdater TrainOn8A;
        public MessageUpdater FiddleYardReset;
        //public MessageUpdater OccfromBlock6;
        //public MessageUpdater SensorF12High;
        //public MessageUpdater OccfromBlock6AndSensorF12;
        //public MessageUpdater LastTrack;
        //public MessageUpdater UniversalError;
        public MessageUpdater uControllerReady;             // ready for next command, C# has to assume ucontroller is busy after sending a command
        public MessageUpdater TrackPower15VDown;            // Quick message to indicate track power is down, halt FYApplication!!
        //public MessageUpdater EndOffStroke11Assert;
        //public MessageUpdater EndOffStroke10Assert;
        //public MessageUpdater FiddleYardMoveAndF12Assert;
        //public MessageUpdater FiddleYardMoveAndF13Assert;
        

        /*#--------------------------------------------------------------------------#*/
        /*  Description: FiddleYardVariables constructor
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
        public FiddleYardIOHandleVariables()
        {
            CL10Heart = new SensorUpdater();
            F11 = new SensorUpdater();
            EOS10 = new SensorUpdater();
            EOS11 = new SensorUpdater();
            F13 = new SensorUpdater();
            F12 = new SensorUpdater();
            Block5B = new SensorUpdater();
            Block8A = new SensorUpdater();
            TrackPower = new SensorUpdater();
            Block5BIn = new SensorUpdater();
            Block6In = new SensorUpdater();
            Block7In = new SensorUpdater();
            Resistor = new SensorUpdater();
            Block6 = new SensorUpdater();
            Block7 = new SensorUpdater();
            F10 = new SensorUpdater();
            M10 = new SensorUpdater();
            TrackNo = new SensorUpdater();
            TrackPower15V = new SensorUpdater();
            CmdBusy = new SensorUpdater();
            Mip50Rec = new SensorUpdater();

            // Instantiate messages
            //FiddleOneLeft = new MessageUpdater();
            //FiddleOneRight = new MessageUpdater();
            //FiddleMultipleLeft = new MessageUpdater();
            //FiddleMultipleRight = new MessageUpdater();
            //TrainDetection = new MessageUpdater();
            //TrainOn5B = new MessageUpdater();
            //TrainOn8A = new MessageUpdater();
            FiddleYardReset = new MessageUpdater();
            //OccfromBlock6 = new MessageUpdater();
            //SensorF12High = new MessageUpdater();
            //OccfromBlock6AndSensorF12 = new MessageUpdater();
            //LastTrack = new MessageUpdater();
            //UniversalError = new MessageUpdater();
            uControllerReady = new MessageUpdater();
            TrackPower15VDown = new MessageUpdater();
            //EndOffStroke11Assert = new MessageUpdater();
            //EndOffStroke10Assert = new MessageUpdater();   
            //FiddleYardMoveAndF12Assert = new MessageUpdater();
            //FiddleYardMoveAndF13Assert = new MessageUpdater();    
        }
    }
}