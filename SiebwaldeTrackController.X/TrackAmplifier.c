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
#include "TrackAmplifier.h"



char TrackAmplifier_Write(unsigned char address, unsigned char *writecommand);
char TrackAmplifier_Read(unsigned char address, unsigned char *data);

const unsigned char rW = 0, Rw = 1;                                             // I2C Write and Read
unsigned char addrW, addrR;
unsigned int DataReceived = 0;
unsigned char ReadCommand[3] = {0xAA,0,'\0'};
unsigned char WriteCommand[4] = {0x55,0,0,'\0'};
unsigned char *DataReceivedToLoc;
char _return_val;

/******************************************************************************
 * Function:        TrackAmplifierxReadxAPI(unsigned char address, unsigned char data)
 *
 * PreCondition:    None
 *
 * Input:           Address of amp, api index to read, pointer to ram data to return the data to
 *
 * Output:          ACK, NACK, TIMEOUT
 *
 * Side Effects:    None
 *
 * Overview:        Read data from API, involves sending the read command(255) 
 *                  to the slave first, next get the data
 *****************************************************************************/

char TrackAmplifierxReadxAPI(unsigned char address, unsigned char api_index, unsigned char *data){
    
    ReadCommand[1] = api_index;                                                 // fill in the api mem register to read
        
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
        
        case TIMEOUT: return(TIMEOUT);
        
        default : return(NACK);
        break;
    }    
}

/******************************************************************************
 * Function:        TrackAmplifierxWritexAPI(unsigned char address, unsigned char data)
 *
 * PreCondition:    None
 *
 * Input:           Address of slave, api index to be written to and the data (null terminated)
 *
 * Output:          ACK, NACK, WCOL
 *
 * Side Effects:    None
 *
 * Overview:        None
 *****************************************************************************/

char TrackAmplifierxWritexAPI(unsigned char address, unsigned char api_index, unsigned char data){
    
    WriteCommand[1] = api_index;                                                // fill in the api mem register to write to
    WriteCommand[2] = data;
    
    switch (TrackAmplifier_Write(address, WriteCommand)){
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
 * Overview:        Write data to an address ( data null terminated!! )
 *****************************************************************************/

char TrackAmplifier_Write(unsigned char address, unsigned char *writecommand){
    
    _return_val = 0;
    addrW = (address << 1) | rW; // shift the address due to LSB is read/write bit (0 for write)
    StartI2C2();
    IdleI2C2();    
    switch (WriteI2C2(addrW))
    {
        case ACK : _return_val = 0;
        break;

        case NACK: _return_val = NACK;
        break;

        case WCOL: _return_val = WCOL;
        break;

        default : _return_val = NACK;
        break;
    }
    
    if (_return_val != ACK)
    {
        IdleI2C2();
        StopI2C2();
        IdleI2C2();
        return(_return_val);
    }
    
    IdleI2C2();
    
    switch (putsI2C2(writecommand))
    {
        case ACK : _return_val = ACK;
            break;

        case NACK: _return_val = NACK;
        break;
        
        case WCOL: _return_val = WCOL;
        break;
        
        default : _return_val = NACK;
        break;
    } 
    IdleI2C2();
    StopI2C2();
    IdleI2C2();
    return(_return_val);
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
    StartI2C2();
    IdleI2C2();	
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
    IdleI2C2();	
    DataReceived = ReadI2C2();
    if (DataReceived > 0x7FFF){
        IdleI2C2();
        StopI2C2();
        IdleI2C2();
        return(TIMEOUT);
    }
    else{
        *DataReceivedToLoc = DataReceived;                                      // write the data received to the pointed location
        IdleI2C2();
        StopI2C2();
        IdleI2C2();
        return(ACK);
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