/* 
 * File:   processio.h
 * Author: Jeremy Siebers
 *
 * Created on September 22, 2018, 2:44 PM
 */
#include <stdint.h>
#ifndef PROCESSIO_H
#define	PROCESSIO_H

#ifdef	__cplusplus
extern "C" {
#endif

    uint16_t            MEASURExBMF (void);
    uint16_t            ADCxIO      (void);

#ifdef	__cplusplus
}
#endif

#endif	/* PROCESSIO_H */

