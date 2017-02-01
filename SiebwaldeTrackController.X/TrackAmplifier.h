/* 
 * File:   TrackAmplifier.h
 * Author: Jeremy Siebers
 *
 * Created on January 21, 2017, 12:04 AM
 */

#ifndef TRACKAMPLIFIER_H
#define	TRACKAMPLIFIER_H

#define ACK         0
#define WCOL        -1
#define NACK        -2
#define TIMEOUT     -3
#define WRITE       11
#define READ        10


#ifdef	__cplusplus
extern "C" {
#endif
    extern void TrackAmplifierxSynchronizexSetpoints(void);
    extern char TrackAmplifierxWritexAPI(unsigned char address, unsigned char api_index, unsigned char data);
    extern char TrackAmplifierxReadxAPI(unsigned char address, unsigned char api_index, unsigned char *data);

#ifdef	__cplusplus
}
#endif

#endif	/* TRACKAMPLIFIER_H */

