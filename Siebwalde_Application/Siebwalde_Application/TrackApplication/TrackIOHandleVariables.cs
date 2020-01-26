using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Siebwalde_Application.TrackApplication
{
    public class TrackIOHandleVariables
    {
        public const byte SLAVEINFO = 0xFF;
        public const byte HEADER = 0xAA;
        public const byte FOOTER = 0x55;



        /*#--------------------------------------------------------------------------#*/
        /*  Description: Track Application variables
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
        public TrackAmplifierUpdater[] TrackAmplifierInt;
                
        /*#--------------------------------------------------------------------------#*/
        /*  Description: TrackVariables constructor
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
        public TrackIOHandleVariables()
        {
            TrackAmplifierInt = new TrackAmplifierUpdater[56];

            

            //Sensor Sns_CL_10_Heart = new Sensor("CL10Heart", " CL 10 Heart ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.CL10Heart.Attach(Sns_CL_10_Heart);
        }

        
    }
}
