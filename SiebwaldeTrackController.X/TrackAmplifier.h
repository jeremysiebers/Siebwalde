/* 
 * File:   TrackAmplifier.h
 * Author: Jeremy Siebers
 *
 * Created on January 21, 2017, 12:04 AM
 */

#ifndef TRACKAMPLIFIER_H
#define	TRACKAMPLIFIER_H

#ifdef	__cplusplus
extern "C" {
#endif

    extern char TrackAmplifierxWritexAPI(unsigned char address, unsigned char data);
    extern char TrackAmplifierxReadxAPI(unsigned char address, unsigned char data);

#ifdef	__cplusplus
}
#endif

#endif	/* TRACKAMPLIFIER_H */

