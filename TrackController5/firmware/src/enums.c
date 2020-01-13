#include <xc.h>
#include <stdint.h>
#include <stdbool.h>
#include <stddef.h>
#include <stdlib.h>
#include "enums.h"

/*#--------------------------------------------------------------------------#*/
/*  Description: uint32_t READxCORExTIMER(void)
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
 */
/*#--------------------------------------------------------------------------#*/
uint32_t READxCORExTIMER(void)
{
    uint32_t count;
    asm volatile("mfc0 %0, $9" : "=r"(count));
    return(count);
} 

/*#--------------------------------------------------------------------------#*/
/*  Description: bool RETURNEDxRESULTxHANDLER (RETURN_STATUS result, 
 *                                             TASK_COMMAND task_command)
 *
 *  Input(s)   : 
 *
 *  Output(s)  : 
 *
 *  Returns    : Init done
 *
 *  Pre.Cond.  : Checksum calculated from new FW file
 *
 *  Post.Cond. :
 *
 *  Notes      :  
 */
/*#--------------------------------------------------------------------------#*/

//bool RETURNEDxRESULTxHANDLER (RETURN_STATUS result, TASK_COMMAND task_command){
//    
//    bool return_val = false;
//    
//    switch(result.task_state){
//        case BUSY:                                                              // log here the sub status 
//        {   
//            if(result.task_message != NONE){                                    // when a sub state, while busy, has a message to be send to client
//                CREATExTASKxSTATUSxMESSAGE(
//                        result.task_id,
//                        result.task_state,
//                        result.task_command,                                    
//                        result.task_message);
//            }
//            break;
//        }        
//        default:                                                                // log here the reason for ABORT or DONE
//        {
//            CREATExTASKxSTATUSxMESSAGE(
//            result.task_id,
//            result.task_state,
//            task_command,                                                       
//            result.task_message);
//            return_val = true;
//            break;
//        }
//    }
//    return (return_val);
//}