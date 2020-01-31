/* 
 * File:   ausart.h
 * Author: siebwalde
 *
 * Created on 4 januari 2016, 16:48
 */

#ifndef _ausart_H
#define _ausart_H

/**
  Section: Included Files
 */

#include <stdint.h>  

#ifdef __cplusplus  // Provide C++ Compatibility

extern "C" {

#endif

    
 
    /**
      Section: Macro Declarations
     */

#define ausart_DataReady  (ausartRxCount)
#define ausart_AddressDetect (ausartAddressBit)

    /**
      Section: Data Type Definitions
     */

    /**
     Section: Global variables
     */
    extern volatile uint8_t ausartTxBufferRemaining;
    extern volatile uint8_t ausartRxCount;
    extern volatile uint8_t ausartAddressBit;

    /**
      Section: ausart APIs
     */

    /**
      @Summary
        Initialization routine that takes inputs from the ausart GUI.

      @Description
        This routine initializes the ausart driver.
        This routine must be called before any other ausart routine is called.

      @Preconditions
        None

      @Param
        None

      @Returns
        None

      @Comment

      @Example
     */
    extern void ausart_Initialize(void);

    /**
      @Summary
        Read a byte of data from the ausart.

      @Description
        This routine reads a byte of data from the ausart.

      @Preconditions
        ausart_Initialize() function should have been called
        before calling this function. The transfer status should be checked to see
        if the receiver is not empty before calling this function.
	
        ausart_DataReady is a macro which checks if any byte is received.
        Call this macro before using this function.

      @Param
        None

      @Returns
        A data byte received by the driver.
	
      @Example
        <code>
                void main(void) {
                                    // initialize the device
                                    SYSTEM_Initialize();
                                    uint8_t data;
								
                                    // Enable the Global Interrupts
                                    INTERRUPT_GlobalInterruptEnable();
								
                                    // Enable the Peripheral Interrupts
                                    INTERRUPT_PeripheralInterruptEnable();
								
                                    printf("\t\tTEST CODE\n\r");		//Enable redirect STDIO to USART before using printf statements
                                    printf("\t\t---- ----\n\r");
                                    printf("\t\tECHO TEST\n\r");
                                    printf("\t\t---- ----\n\n\r");
                                    printf("Enter any string: ");
                                    do{
                                    data = ausart_Read();		// Read data received
                                    EUSART_Write(data);			// Echo back the data received
                                    }while(!ausart_DataReady);		//check if any data is received
								
                                }
        </code>
     */
    extern uint8_t ausart_Read(void);

    /**
     @Summary
       Writes a byte of data to the ausart.

     @Description
       This routine writes a byte of data to the ausart.

     @Preconditions
       ausart_Initialize() function should have been called
       before calling this function. The transfer status should be checked to see
       if transmitter is not busy before calling this function.

     @Param
       txData  - Data byte to write to the ausart

     @Returns
       None
  
     @Example
         <code>
             Refer to ausart_Read() for an example	
         </code>
     */
    extern void ausart_Write(uint8_t txData);

    /**
      @Summary
        Maintains the driver's transmitter state machine and implements its ISR.

      @Description
        This routine is used to maintain the driver's internal transmitter state
        machine.This interrupt service routine is called when the state of the
        transmitter needs to be maintained in a non polled manner.

      @Preconditions
        ausart_Initialize() function should have been called
        for the ISR to execute correctly.

      @Param
        None

      @Returns
        None
     */
    extern void ausart_Transmit_ISR(void);

    /**
      @Summary
        Maintains the driver's receiver state machine and implements its ISR

      @Description
        This routine is used to maintain the driver's internal receiver state
        machine.This interrupt service routine is called when the state of the
        receiver needs to be maintained in a non polled manner.

      @Preconditions
        ausart_Initialize() function should have been called
        for the ISR to execute correctly.

      @Param
        None

      @Returns
        None
     */
    extern void ausart_Receive_ISR(void);

#ifdef __cplusplus  // Provide C++ Compatibility

}

#endif

#endif  // _ausart_H
/**
 End of File
 */
