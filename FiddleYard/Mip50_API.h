/* 
 * File:   Mip50_API.h
 * Author: siebwalde
 *
 * Created on 4 januari 2016, 14:27
 */

#ifndef MIP50_API_H
#define	MIP50_API_H

#ifdef	__cplusplus
extern "C" {
#endif

extern unsigned char MIP50xHOME(unsigned char ASL);
extern unsigned char MIP50xMOVE(unsigned char ASL, int New_Track);
extern void MIP50xRECEIVEDxDATA(unsigned char ASL);

#ifdef	__cplusplus
}
#endif

#endif	/* MIP50_API_H */

