#ifndef _FIDDLE_YARD_H
#define _FIDDLE_YARD_H


#define Led3						PORTBbits.RB2	//Output
#define Led2						PORTAbits.RA4	//Output
#define Led1						PORTAbits.RA5	//Output
#define Output_Enable				PORTBbits.RB4	//Output
#define IO_Expander_Enable			PORTBbits.RB3	//Output
#define To_Externall_WDT_Pulse		PORTBbits.RB5	//Output

#define TOP 1
#define BOTTOM 0
#define True 1
#define False 0
#define On 1
#define Off 0
#define True 1
#define False 0

typedef unsigned char bool;
#define true 1
#define false 0

#ifndef int8_t
typedef signed char int8_t;
#define int8_t int8_t
#define INT8_MIN (-128)
#define INT8_MAX (127)
#endif

#ifndef int16_t
typedef signed int int16_t;
#define int16_t int16_t
#define INT16_MIN (-32768)
#define INT16_MAX (32767)
#endif

#ifndef __CCI__
#ifndef int24_t
typedef signed short long int int24_t;
#define int24_t int24_t
#define INT24_MIN (-8388608L)
#define INT24_MAX (8388607L)
#endif
#endif

#ifndef int32_t
typedef signed long int int32_t;
#define int32_t int32_t
#define INT32_MIN (-2147483648L)
#define INT32_MAX (2147483647L)
#endif

#ifndef uint8_t
typedef unsigned char uint8_t;
#define uint8_t uint8_t
#define UINT8_MAX (255)
#endif

#ifndef uint16_t
typedef unsigned int uint16_t;
#define uint16_t uint16_t
#define UINT16_MAX (65535U)
#endif

#ifndef __CCI__
#ifndef uint24_t
typedef unsigned short long int uint24_t;
#define uint24_t uint24_t
#define UINT24_MAX (16777215UL)
#endif
#endif

#ifndef uint32_t
typedef unsigned long int uint32_t;
#define uint32_t uint32_t
#define UINT32_MAX (4294967295UL)
#endif

  /* types that are at least as wide */

#ifndef int_least8_t
typedef signed char int_least8_t;
#define int_least8_t int_least8_t
#define INT_LEAST8_MIN (-128)
#define INT_LEAST8_MAX (127)
#endif

#ifndef int_least16_t
typedef signed int int_least16_t;
#define int_least16_t int_least16_t
#define INT_LEAST16_MIN (-32768)
#define INT_LEAST16_MAX (32767)
#endif

#ifndef int_least24_t
#ifdef __CCI__
typedef signed long int int_least24_t;
#define INT_LEAST24_MIN (-2147483648L)
#define INT_LEAST24_MAX (2147483647L)
#else
typedef signed short long int int_least24_t;
#define INT_LEAST24_MIN (-8388608L)
#define INT_LEAST24_MAX (8388607L)
#endif
#define int_least24_t int_least24_t
#endif

#ifndef int_least32_t
typedef signed long int int_least32_t;
#define int_least32_t int_least32_t
#define INT_LEAST32_MIN (-2147483648L)
#define INT_LEAST32_MAX (2147483647L)
#endif

#ifndef uint_least8_t
typedef unsigned char uint_least8_t;
#define uint_least8_t uint_least8_t
#define UINT_LEAST8_MAX (255)
#endif

#ifndef uint_least16_t
typedef unsigned int uint_least16_t;
#define uint_least16_t uint_least16_t
#define UINT_LEAST16_MAX (65535UL)
#endif

#ifndef uint_least24_t
#ifdef __CCI__
typedef unsigned long int uint_least24_t;
#define UINT_LEAST24_MAX (4294967295UL)
#else
typedef unsigned short long int uint_least24_t;
#define UINT_LEAST24_MAX (16777215UL)
#endif
#define uint_least24_t uint_least24_t
#endif

#ifndef uint_least32_t
typedef unsigned long int uint_least32_t;
#define uint_least32_t uint_least32_t
#define UINT_LEAST32_MAX (4294967295UL)
#endif


  /* types that are at least as wide and are usually the fastest */

#ifndef int_fast8_t
typedef signed char int_fast8_t;
#define int_fast8_t int_fast8_t
#define INT_FAST8_MIN (-128)
#define INT_FAST8_MAX (127)
#endif

#ifndef int_fast16_t
typedef signed int int_fast16_t;
#define int_fast16_t int_fast16_t
#define INT_FAST16_MIN (-32768)
#define INT_FAST16_MAX (32767)
#endif

#ifndef int_fast24_t
#ifdef __CCI__
typedef signed long int int_fast24_t;
#define INT_FAST24_MIN (-2147483648L)
#define INT_FAST24_MAX (2147483647L)
#else
typedef signed short long int int_fast24_t;
#define INT_FAST24_MIN (-8388608L)
#define INT_FAST24_MAX (8388607L)
#endif
#define int_fast24_t int_fast24_t
#endif

#ifndef int_fast32_t
typedef signed long int int_fast32_t;
#define int_fast32_t int_fast32_t
#define INT_FAST32_MIN (-2147483648L)
#define INT_FAST32_MAX (2147483647L)
#endif

#ifndef uint_fast8_t
typedef unsigned char uint_fast8_t;
#define uint_fast8_t uint_fast8_t
#define UINT_FAST8_MAX (255)
#endif

#ifndef uint_fast16_t
typedef unsigned int uint_fast16_t;
#define uint_fast16_t uint_fast16_t
#define UINT_FAST16_MAX (65535UL)
#endif

#ifndef uint_fast24_t
#ifdef __CCI__
typedef unsigned long int uint_fast24_t;
#define UINT_FAST24_MAX (4294967295UL)
#else
typedef unsigned short long int uint_fast24_t;
#define UINT_FAST24_MAX (16777215UL)
#endif
#define uint_fast24_t uint_fast24_t
#endif

#ifndef uint_fast32_t
typedef unsigned long int uint_fast32_t;
#define uint_fast32_t uint_fast32_t
#define UINT_FAST32_MAX (4294967295UL)
#endif

#ifndef intmax_t
typedef int32_t intmax_t;
#define intmax_t intmax_t
#endif

#ifndef uintmax_t
typedef uint32_t uintmax_t;
#define uintmax_t uintmax_t
#endif

#ifndef intptr_t
typedef int16_t	intptr_t;
#define intptr_t intptr_t
#endif

#ifndef uintptr_t
typedef uint16_t uintptr_t;
#define uintptr_t uintptr_t
#endif

#ifndef intmax_t
typedef int32_t intmax_t
#define intmax_t intmax_t
#endif

#ifndef uintmax_t
typedef uint32_t uintmax_t
#define uintmax_t uintmax_t
#endif

#if defined(EEPROM_CS_TRIS) || defined(SPIFLASH_CS_TRIS)
	void SaveAppConfig(void);
#else
	#define SaveAppConfig()
#endif
