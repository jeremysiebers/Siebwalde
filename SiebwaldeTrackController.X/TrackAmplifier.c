/*
 * File:   TrackAmplifier.c
 * Author: Jeremy Siebers
 *
 * Created on Januari 20, 2011, 23:59 PM
 */
#include "Fiddle_Yard.h"
#include "Diagnostic_ret.h"
#include "I2c2/i2c.h"
#include "api.h"

#define ACK      0
#define WCOL    -1
#define NACK    -2
#define TIMEOUT -3

char TrackAmplifier_Write(unsigned char address, unsigned char *data);
char TrackAmplifier_Read(unsigned char address, unsigned char *data);

const unsigned char rW = 0, Rw = 1;                                             // I2C Write and Read
unsigned char addrW, addrR;
unsigned char DataReceived = 0;
unsigned char ReadCommand[3] = {255,0,'\0'};
unsigned char *DataReceivedToLoc;

/******************************************************************************
 * Function:        TrackAmplifierxReadxAPI(unsigned char address, unsigned char data)
 *
 * PreCondition:    None
 *
 * Input:           Address of amp, api index to read, pointer to ram data to return the data to
 *
 * Output:          None
 *
 * Side Effects:    None
 *
 * Overview:        Read data from API, involves sending the read command(255) 
 *                  to the slave first, next get the data
 *****************************************************************************/

char TrackAmplifierxReadxAPI(unsigned char address, unsigned char api_index, unsigned char *data){
    
    ReadCommand[1] = api_index;                                                       // fill in the api mem register to read
        
    switch (TrackAmplifier_Write(address, ReadCommand)){
        case ACK : break;

        case NACK: return(NACK);
        break;
        
        case WCOL: return(WCOL);
        break;
        
        default : return(NACK);
        break;
    }
    
    switch (TrackAmplifier_Read(address, data)){
        case ACK : return(ACK);
        break;

        case NACK: return(NACK);
        break;
        
        case WCOL: return(WCOL);
        break;
        
        default : return(NACK);
        break;
    }    
}

/******************************************************************************
 * Function:        TrackAmplifierxWritexAPI(unsigned char address, unsigned char data)
 *
 * PreCondition:    None
 *
 * Input:           None
 *
 * Output:          None
 *
 * Side Effects:    None
 *
 * Overview:        None
 *****************************************************************************/

char TrackAmplifierxWritexAPI(unsigned char address, unsigned char *data){
    
    switch (TrackAmplifier_Write(address, data)){
        case ACK : return(ACK);
        break;

        case NACK: return(NACK);
        break;
        
        case WCOL: return(WCOL);
        break;
        
        default : return(NACK);
        break;
    }
}

/******************************************************************************
 * Function:        TrackAmplifier_Write(unsigned char address, unsigned char data)
 *
 * PreCondition:    None
 *
 * Input:           Address and data
 *
 * Output:          None
 *
 * Side Effects:    None
 *
 * Overview:        Write data to an address ( null terminated!! )
 *****************************************************************************/

char TrackAmplifier_Write(unsigned char address, unsigned char *data){

    addrW = (address << 1) | rW; // shift the address due to LSB is read/write bit (0 for write)
    IdleI2C2();
	StartI2C2();
    switch (WriteI2C2(addrW))
    {
        case ACK : break;

        case NACK: return(NACK);
        break;
        
        case WCOL: return(WCOL);
        break;
        
        default : return(NACK);
        break;
    }
    
    switch (putsI2C2(data))
    {
        case ACK : break;

        case NACK: return(NACK);
        break;
        
        case WCOL: return(WCOL);
        break;
        
        default : return(NACK);
        break;
    }
    StopI2C2();
    return(ACK);
}

/******************************************************************************
 * Function:        TrackAmplifier_Read(unsigned char address, unsigned char data)
 *
 * PreCondition:    None
 *
 * Input:           Address, pointer to ram data to output data been read
 *
 * Output:          None
 *
 * Side Effects:    None
 *
 * Overview:        Read data from an address
 *****************************************************************************/

char TrackAmplifier_Read(unsigned char address, unsigned char *data){
    
    DataReceivedToLoc = data;                                                   // get the address of the location were the data has to go
    
    addrR = (address << 1) | Rw; // shift the address due to LSB is read/write bit (1 for read)
    IdleI2C2();
	StartI2C2();
    switch (WriteI2C2(addrR))
    {
        case ACK : break;

        case NACK: return(NACK);
        break;
        
        case WCOL: return(WCOL);
        break;
        
        default : return(NACK);
        break;
    }
    
    switch (DataReceived = ReadI2C2())
    {
        case 255 :   return(TIMEOUT);
        break;

        default :   *DataReceivedToLoc = DataReceived;                          // write the data received to the pointed location
                    StopI2C2();
                    return(ACK);
                    break;
    }
    
}

/*
       
    wrptr = tx_data;
    addrW = (address << 1) | rW; // shift the address due to LSB is read/write bit (0 for write)
    addrR = (address << 1) | Rw; // shift the address due to LSB is read/write bit (1 for read)
	
    
    switch(sw){
		case 0 :    IdleI2C2();
					StartI2C2();
					NextAddr++;
					if(NextAddr == 1){
						address = 0x50;
					}
					else if (NextAddr == 2){
						address = 0x51;
					}
					else if (NextAddr == 3){
						address = 0x52;
					}
					else if (NextAddr == 4){
						address = 0x53;
						NextAddr = 0;
					}
					
					addrW = (address << 1) | rW; // shift the address due to LSB is read/write bit (0 for write)
					addrR = (address << 1) | Rw; // shift the address due to LSB is read/write bit (1 for read)
					
					sw = 1;
					break;
		
		case 1 :    switch (WriteI2C2(addrW))
					{
						case 0 : sw = 2;
						break;
		
						case -2: sw = 99;
						break;
		
						default : sw = 99;
						break;
					}
					break;
		
		case 2 :    switch (putsI2C2(wrptr))
					{
						case 0 : sw = 3;
						break;
		
						case -2: sw = 99;
						break;
		
						default : sw = 99;
						break;
					}
					break;
		
		case 3 :    StopI2C2();  
					IdleI2C2();
					StartI2C2();
					sw = 4;
					break;
					
		case 4 :    switch (WriteI2C2(addrR))
					{
						case 0 : sw = 5;
						break;
		
						case -2: sw = 99;
						break;
		
						default : sw = 99;
						break;
					}
					break;
				
		case 5 :    switch (DataReceived = ReadI2C2())
					{
						case 255 :   sw = 100;
						break;
						
						default :   DataReceived ++;
									if (DataReceived > 15){
										DataReceived = 0;
										sw = 101;
									}
									else{
										sw = 6;}
									break;
					}
					
					break;
					
		case 6 :    StopI2C2();  
					IdleI2C2();
					StartI2C2();
					sw = 7;
					break;
					
		case 7 :    switch (WriteI2C2(addrW))
					{
						case 0 : sw = 8;
						break;
		
						case -2: sw = 99;
						break;
		
						default : sw = 99;
						break;
					}
					break;
					
		case 8 :    tx_data2[0] = 1;
					tx_data2[1] = (unsigned char)DataReceived;
					switch (putsI2C2(tx_data2))
					{
						case 0 : sw = 99;
						break;
		
						case -2: sw = 99;
						break;
		
						default : sw = 99;
						break;
					}
					break;
		
		case 99 :   StopI2C2();
					IdleI2C2();
					//Delay10KTCYx(255);
					//Delay10KTCYx(255);
					
					sw = 0;
					//Enable_State_Machine_Update = False;
					T1CON = 0xB1;
					break;
					
		case 100 :  CloseI2C1();
					OpenI2C2(MASTER,SLEW_OFF);
					sw = 99;
					break;
					
		case 101 :  StopI2C2();  
					IdleI2C2();                            
					StartI2C2();
					switch (WriteI2C2(0))
					{
						case 0 : sw = 102;
						break;
		
						case -2: sw = 99;
						break;
		
						default : sw = 99;
						break;
					}
					break;
					
		case 102 :  switch (putcI2C2('R'))
					{
						case 0 : sw = 99;
						break;
		
						case -2: sw = 99;
						break;
		
						default : sw = 99;
						break;
					}
					break;
					
		case 103 :  break;
		
		default : sw = 99;
			break;                       

    }
*/