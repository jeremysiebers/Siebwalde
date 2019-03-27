EESchema Schematic File Version 4
LIBS:PIC32MZ_piggyback2-cache
EELAYER 26 0
EELAYER END
$Descr A4 11693 8268
encoding utf-8
Sheet 1 1
Title ""
Date ""
Rev ""
Comp ""
Comment1 ""
Comment2 ""
Comment3 ""
Comment4 ""
$EndDescr
$Comp
L Connector_Generic:Conn_02x13_Odd_Even J1
U 1 1 5C9A99C1
P 2350 1850
F 0 "J1" H 2400 2667 50  0000 C CNN
F 1 "Conn_02x13_Odd_Even" H 2400 2576 50  0000 C CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_2x13_P2.54mm_Vertical" H 2350 1850 50  0001 C CNN
F 3 "~" H 2350 1850 50  0001 C CNN
	1    2350 1850
	1    0    0    -1  
$EndComp
$Comp
L Connector:Conn_01x13_Male J3-2
U 1 1 5C9A9A55
P 7750 4450
F 0 "J3-2" H 7856 5228 50  0000 C CNN
F 1 "Conn_01x13_Male" H 7856 5137 50  0000 C CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_1x13_P2.54mm_Vertical" H 7750 4450 50  0001 C CNN
F 3 "~" H 7750 4450 50  0001 C CNN
	1    7750 4450
	1    0    0    -1  
$EndComp
$Comp
L Connector:Conn_01x13_Male J3-1
U 1 1 5C9A9A99
P 7650 4450
F 0 "J3-1" H 7800 3750 50  0000 R CNN
F 1 "Conn_01x13_Male" H 8000 3700 50  0000 R CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_1x13_P2.54mm_Vertical" H 7650 4450 50  0001 C CNN
F 3 "~" H 7650 4450 50  0001 C CNN
	1    7650 4450
	-1   0    0    -1  
$EndComp
Entry Wire Line
	900  1150 1000 1250
Wire Wire Line
	2150 1250 1000 1250
Text Label 1500 1250 0    50   ~ 0
VCC
Entry Wire Line
	900  1250 1000 1350
Entry Wire Line
	900  1350 1000 1450
Entry Wire Line
	900  1450 1000 1550
Entry Wire Line
	900  1550 1000 1650
Entry Wire Line
	900  1650 1000 1750
Entry Wire Line
	900  1750 1000 1850
Entry Wire Line
	900  1850 1000 1950
Entry Wire Line
	900  1950 1000 2050
Entry Wire Line
	900  2050 1000 2150
Entry Wire Line
	900  2150 1000 2250
Entry Wire Line
	900  2250 1000 2350
Entry Wire Line
	900  2350 1000 2450
Wire Wire Line
	2150 1350 1000 1350
Wire Wire Line
	1000 1450 2150 1450
Wire Wire Line
	2150 1550 1000 1550
Wire Wire Line
	1000 1650 2150 1650
Wire Wire Line
	2150 1750 1000 1750
Wire Wire Line
	1000 1850 2150 1850
Wire Wire Line
	2150 1950 1000 1950
Wire Wire Line
	1000 2050 2150 2050
Wire Wire Line
	2150 2150 1000 2150
Wire Wire Line
	1000 2250 2150 2250
Wire Wire Line
	2150 2350 1000 2350
Wire Wire Line
	1000 2450 2150 2450
Entry Bus Bus
	3500 3050 3600 3150
Entry Wire Line
	3400 1250 3500 1350
Entry Wire Line
	3400 1450 3500 1550
Entry Wire Line
	3400 1350 3500 1450
Entry Wire Line
	3400 1550 3500 1650
Entry Wire Line
	3400 1650 3500 1750
Entry Wire Line
	3400 1750 3500 1850
Entry Wire Line
	3400 1850 3500 1950
Entry Wire Line
	3400 1950 3500 2050
Entry Wire Line
	3400 2050 3500 2150
Entry Wire Line
	3400 2150 3500 2250
Entry Wire Line
	3400 2250 3500 2350
Entry Wire Line
	3400 2350 3500 2450
Entry Wire Line
	3400 2450 3500 2550
Wire Wire Line
	3400 1250 2650 1250
Wire Wire Line
	2650 1350 3400 1350
Wire Wire Line
	3400 1450 2650 1450
Wire Wire Line
	2650 1550 3400 1550
Wire Wire Line
	3400 1650 2650 1650
Wire Wire Line
	2650 1750 3400 1750
Wire Wire Line
	3400 1850 2650 1850
Wire Wire Line
	2650 1950 3400 1950
Wire Wire Line
	3400 2050 2650 2050
Wire Wire Line
	2650 2150 3400 2150
Wire Wire Line
	3400 2250 2650 2250
Wire Wire Line
	2650 2350 3400 2350
Wire Wire Line
	3400 2450 2650 2450
Text Label 1500 1350 0    50   ~ 0
RE5
Text Label 1500 1450 0    50   ~ 0
RE7
Text Label 1500 1550 0    50   ~ 0
RC2
Text Label 1500 1650 0    50   ~ 0
RC4
Text Label 1500 1750 0    50   ~ 0
RG7
Text Label 1500 1850 0    50   ~ 0
MCLR
Text Label 1500 1950 0    50   ~ 0
RA0
Text Label 1500 2050 0    50   ~ 0
RE9
Text Label 1500 2150 0    50   ~ 0
RB4
Text Label 1500 2250 0    50   ~ 0
RB2
Text Label 1500 2350 0    50   ~ 0
RB0
Text Label 1500 2450 0    50   ~ 0
VCC
Text Label 2900 1250 0    50   ~ 0
GND
Text Label 2900 1350 0    50   ~ 0
RG15
Text Label 2900 1450 0    50   ~ 0
RE6
Text Label 2900 1550 0    50   ~ 0
RC1
Text Label 2900 1650 0    50   ~ 0
RC3
Text Label 2900 1750 0    50   ~ 0
RG6
Text Label 2900 1850 0    50   ~ 0
RG8
Text Label 2900 1950 0    50   ~ 0
RG9
Text Label 2900 2050 0    50   ~ 0
RE8
Text Label 2900 2150 0    50   ~ 0
RB5
Text Label 2900 2250 0    50   ~ 0
RB3
Text Label 2900 2350 0    50   ~ 0
RB1
Text Label 2900 2450 0    50   ~ 0
GND
$Comp
L Connector_Generic:Conn_02x13_Odd_Even J2
U 1 1 5C9ADD85
P 5400 1850
F 0 "J2" H 5450 2667 50  0000 C CNN
F 1 "Conn_02x13_Odd_Even" H 5450 2576 50  0000 C CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_2x13_P2.54mm_Vertical" H 5400 1850 50  0001 C CNN
F 3 "~" H 5400 1850 50  0001 C CNN
	1    5400 1850
	1    0    0    -1  
$EndComp
Entry Wire Line
	3950 1150 4050 1250
Wire Wire Line
	5200 1250 4050 1250
Text Label 4550 1250 0    50   ~ 0
RD1
Entry Wire Line
	3950 1250 4050 1350
Entry Wire Line
	3950 1350 4050 1450
Entry Wire Line
	3950 1450 4050 1550
Entry Wire Line
	3950 1650 4050 1750
Entry Wire Line
	3950 1750 4050 1850
Entry Wire Line
	3950 1850 4050 1950
Entry Wire Line
	3950 1950 4050 2050
Entry Wire Line
	3950 2050 4050 2150
Entry Wire Line
	3950 2150 4050 2250
Entry Wire Line
	3950 2250 4050 2350
Entry Wire Line
	3950 2350 4050 2450
Wire Wire Line
	5200 1350 4050 1350
Wire Wire Line
	4050 1450 5200 1450
Wire Wire Line
	5200 1550 4050 1550
Wire Wire Line
	5200 1750 4050 1750
Wire Wire Line
	4050 1850 5200 1850
Wire Wire Line
	5200 1950 4050 1950
Wire Wire Line
	4050 2050 5200 2050
Wire Wire Line
	5200 2150 4050 2150
Wire Wire Line
	4050 2250 5200 2250
Wire Wire Line
	5200 2350 4050 2350
Wire Wire Line
	4050 2450 5200 2450
Entry Bus Bus
	6550 3050 6650 3150
Entry Wire Line
	6450 1250 6550 1350
Entry Wire Line
	6450 1450 6550 1550
Entry Wire Line
	6450 1350 6550 1450
Entry Wire Line
	6450 1550 6550 1650
Entry Wire Line
	6450 1650 6550 1750
Entry Wire Line
	6450 1750 6550 1850
Entry Wire Line
	6450 1850 6550 1950
Entry Wire Line
	6450 1950 6550 2050
Entry Wire Line
	6450 2050 6550 2150
Entry Wire Line
	6450 2150 6550 2250
Entry Wire Line
	6450 2250 6550 2350
Entry Wire Line
	6450 2350 6550 2450
Entry Wire Line
	6450 2450 6550 2550
Wire Wire Line
	6450 1250 5700 1250
Wire Wire Line
	5700 1350 6450 1350
Wire Wire Line
	6450 1450 5700 1450
Wire Wire Line
	5700 1550 6450 1550
Wire Wire Line
	6450 1650 5700 1650
Wire Wire Line
	5700 1750 6450 1750
Wire Wire Line
	6450 1850 5700 1850
Wire Wire Line
	5700 1950 6450 1950
Wire Wire Line
	6450 2050 5700 2050
Wire Wire Line
	5700 2150 6450 2150
Wire Wire Line
	6450 2250 5700 2250
Wire Wire Line
	5700 2350 6450 2350
Wire Wire Line
	6450 2450 5700 2450
Text Label 4550 1350 0    50   ~ 0
RD3
Text Label 4550 1450 0    50   ~ 0
RD12
Text Label 4550 1550 0    50   ~ 0
RD4
Text Label 4550 1750 0    50   ~ 0
RF1
Text Label 4550 1850 0    50   ~ 0
RG0
Text Label 4550 1950 0    50   ~ 0
RA7
Text Label 4550 2050 0    50   ~ 0
RE1
Text Label 4550 2150 0    50   ~ 0
RG12
Text Label 4550 2250 0    50   ~ 0
RE2
Text Label 4550 2350 0    50   ~ 0
RE4
Text Label 4550 2450 0    50   ~ 0
VCC
Text Label 5950 1250 0    50   ~ 0
GND
Text Label 5950 1350 0    50   ~ 0
RD2
Text Label 5950 1450 0    50   ~ 0
RD13
Text Label 5950 1550 0    50   ~ 0
RD5
Text Label 5950 1650 0    50   ~ 0
RD7
Text Label 5950 1750 0    50   ~ 0
RF0
Text Label 5950 1850 0    50   ~ 0
RG1
Text Label 5950 1950 0    50   ~ 0
RA6
Text Label 5950 2050 0    50   ~ 0
RE0
Text Label 5950 2150 0    50   ~ 0
RG14
Text Label 5950 2250 0    50   ~ 0
RG13
Text Label 5950 2350 0    50   ~ 0
RE3
Text Label 5950 2450 0    50   ~ 0
GND
$Comp
L Connector_Generic:Conn_02x13_Odd_Even J3
U 1 1 5C9AF336
P 2350 4450
F 0 "J3" H 2400 5267 50  0000 C CNN
F 1 "Conn_02x13_Odd_Even" H 2400 5176 50  0000 C CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_2x13_P2.54mm_Vertical" H 2350 4450 50  0001 C CNN
F 3 "~" H 2350 4450 50  0001 C CNN
	1    2350 4450
	1    0    0    -1  
$EndComp
Entry Wire Line
	900  3750 1000 3850
Wire Wire Line
	2150 3850 1000 3850
Text Label 1500 3850 0    50   ~ 0
TXN
Entry Wire Line
	900  3850 1000 3950
Entry Wire Line
	900  3950 1000 4050
Entry Wire Line
	900  4050 1000 4150
Entry Wire Line
	900  4150 1000 4250
Entry Wire Line
	900  4250 1000 4350
Entry Wire Line
	900  4350 1000 4450
Entry Wire Line
	900  4450 1000 4550
Entry Wire Line
	900  4550 1000 4650
Entry Wire Line
	900  4650 1000 4750
Entry Wire Line
	900  4750 1000 4850
Entry Wire Line
	900  4850 1000 4950
Entry Wire Line
	900  4950 1000 5050
Wire Wire Line
	2150 3950 1000 3950
Wire Wire Line
	1000 4050 2150 4050
Wire Wire Line
	2150 4150 1000 4150
Wire Wire Line
	1000 4250 2150 4250
Wire Wire Line
	2150 4350 1000 4350
Wire Wire Line
	1000 4450 2150 4450
Wire Wire Line
	2150 4550 1000 4550
Wire Wire Line
	1000 4650 2150 4650
Wire Wire Line
	2150 4750 1000 4750
Wire Wire Line
	1000 4850 2150 4850
Wire Wire Line
	2150 4950 1000 4950
Wire Wire Line
	1000 5050 2150 5050
Entry Bus Bus
	3500 5650 3600 5750
Entry Wire Line
	3400 3850 3500 3950
Entry Wire Line
	3400 4050 3500 4150
Entry Wire Line
	3400 3950 3500 4050
Entry Wire Line
	3400 4150 3500 4250
Entry Wire Line
	3400 4250 3500 4350
Entry Wire Line
	3400 4350 3500 4450
Entry Wire Line
	3400 4450 3500 4550
Entry Wire Line
	3400 4550 3500 4650
Entry Wire Line
	3400 4650 3500 4750
Entry Wire Line
	3400 4750 3500 4850
Entry Wire Line
	3400 4850 3500 4950
Entry Wire Line
	3400 4950 3500 5050
Entry Wire Line
	3400 5050 3500 5150
Wire Wire Line
	3400 3850 2650 3850
Wire Wire Line
	2650 3950 3400 3950
Wire Wire Line
	3400 4050 2650 4050
Wire Wire Line
	2650 4150 3400 4150
Wire Wire Line
	3400 4250 2650 4250
Wire Wire Line
	2650 4350 3400 4350
Wire Wire Line
	3400 4450 2650 4450
Wire Wire Line
	2650 4550 3400 4550
Wire Wire Line
	3400 4650 2650 4650
Wire Wire Line
	2650 4750 3400 4750
Wire Wire Line
	3400 4850 2650 4850
Wire Wire Line
	2650 4950 3400 4950
Wire Wire Line
	3400 5050 2650 5050
Text Label 1500 3950 0    50   ~ 0
RXN
Text Label 1500 4050 0    50   ~ 0
LED1
Text Label 1500 4150 0    50   ~ 0
RB6
Text Label 1500 4250 0    50   ~ 0
RA9
Text Label 1500 4350 0    50   ~ 0
RB8
Text Label 1500 4450 0    50   ~ 0
RB10
Text Label 1500 4550 0    50   ~ 0
RF13
Text Label 1500 4650 0    50   ~ 0
RB12
Text Label 1500 4750 0    50   ~ 0
RB14
Text Label 1500 4850 0    50   ~ 0
RD14
Text Label 1500 4950 0    50   ~ 0
RF4
Text Label 1500 5050 0    50   ~ 0
VCC
Text Label 2900 3850 0    50   ~ 0
TXP
Text Label 2900 3950 0    50   ~ 0
RXP
Text Label 2900 4050 0    50   ~ 0
LED2
Text Label 2900 4150 0    50   ~ 0
RB7
Text Label 2900 4250 0    50   ~ 0
RA10
Text Label 2900 4350 0    50   ~ 0
RB9
Text Label 2900 4450 0    50   ~ 0
RB11
Text Label 2900 4550 0    50   ~ 0
RA1
Text Label 2900 4650 0    50   ~ 0
RF12
Text Label 2900 4750 0    50   ~ 0
RB13
Text Label 2900 4850 0    50   ~ 0
RB15
Text Label 2900 4950 0    50   ~ 0
RF5
Text Label 2900 5050 0    50   ~ 0
GND
$Comp
L Connector_Generic:Conn_02x13_Odd_Even J4
U 1 1 5C9B2BF6
P 5400 4450
F 0 "J4" H 5450 5267 50  0000 C CNN
F 1 "Conn_02x13_Odd_Even" H 5450 5176 50  0000 C CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_2x13_P2.54mm_Vertical" H 5400 4450 50  0001 C CNN
F 3 "~" H 5400 4450 50  0001 C CNN
	1    5400 4450
	1    0    0    -1  
$EndComp
Entry Wire Line
	3950 3750 4050 3850
Wire Wire Line
	5200 3850 4050 3850
Text Label 4550 3850 0    50   ~ 0
VCC
Entry Wire Line
	3950 3950 4050 4050
Entry Wire Line
	3950 4050 4050 4150
Entry Wire Line
	3950 4250 4050 4350
Entry Wire Line
	3950 4350 4050 4450
Entry Wire Line
	3950 4450 4050 4550
Entry Wire Line
	3950 4550 4050 4650
Entry Wire Line
	3950 4750 4050 4850
Entry Wire Line
	3950 4850 4050 4950
Entry Wire Line
	3950 4950 4050 5050
Wire Wire Line
	4050 4050 5200 4050
Wire Wire Line
	5200 4150 4050 4150
Wire Wire Line
	5200 4350 4050 4350
Wire Wire Line
	4050 4450 5200 4450
Wire Wire Line
	5200 4550 4050 4550
Wire Wire Line
	4050 4650 5200 4650
Wire Wire Line
	4050 4850 5200 4850
Wire Wire Line
	5200 4950 4050 4950
Wire Wire Line
	4050 5050 5200 5050
Entry Bus Bus
	6550 5650 6650 5750
Entry Wire Line
	6450 3850 6550 3950
Entry Wire Line
	6450 4050 6550 4150
Entry Wire Line
	6450 4250 6550 4350
Entry Wire Line
	6450 4350 6550 4450
Entry Wire Line
	6450 4450 6550 4550
Entry Wire Line
	6450 4550 6550 4650
Entry Wire Line
	6450 4650 6550 4750
Entry Wire Line
	6450 4750 6550 4850
Entry Wire Line
	6450 4850 6550 4950
Entry Wire Line
	6450 5050 6550 5150
Wire Wire Line
	6450 3850 5700 3850
Wire Wire Line
	6450 4050 5700 4050
Wire Wire Line
	6450 4250 5700 4250
Wire Wire Line
	5700 4350 6450 4350
Wire Wire Line
	6450 4450 5700 4450
Wire Wire Line
	5700 4550 6450 4550
Wire Wire Line
	6450 4650 5700 4650
Wire Wire Line
	5700 4750 6450 4750
Wire Wire Line
	6450 4850 5700 4850
Wire Wire Line
	6450 5050 5700 5050
Text Label 4550 4050 0    50   ~ 0
USB-D_P
Text Label 4550 4150 0    50   ~ 0
USB-VBUS
Text Label 4550 4350 0    50   ~ 0
MCU_MISO
Text Label 4550 4450 0    50   ~ 0
RA2
Text Label 4550 4550 0    50   ~ 0
RA4
Text Label 4550 4650 0    50   ~ 0
RA14
Text Label 4550 4850 0    50   ~ 0
RD10
Text Label 4550 4950 0    50   ~ 0
RD0
Text Label 4550 5050 0    50   ~ 0
VCC
Text Label 5950 3850 0    50   ~ 0
GND
Text Label 5950 4050 0    50   ~ 0
USB-D_N
Text Label 5950 4250 0    50   ~ 0
RF3
Text Label 5950 4350 0    50   ~ 0
MCU_SCK
Text Label 5950 4450 0    50   ~ 0
MCU_MOSI
Text Label 5950 4550 0    50   ~ 0
RA3
Text Label 5950 4650 0    50   ~ 0
RA5
Text Label 5950 4750 0    50   ~ 0
RA15
Text Label 5950 4850 0    50   ~ 0
RD9
Text Label 5950 5050 0    50   ~ 0
GND
NoConn ~ 5200 1650
Connection ~ 3950 3150
Connection ~ 3950 5750
$Comp
L Connector:Conn_01x13_Male J1-2
U 1 1 5C9E15B2
P 7800 1850
F 0 "J1-2" H 7906 2628 50  0000 C CNN
F 1 "Conn_01x13_Male" H 7906 2537 50  0000 C CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_1x13_P2.54mm_Vertical" H 7800 1850 50  0001 C CNN
F 3 "~" H 7800 1850 50  0001 C CNN
	1    7800 1850
	1    0    0    -1  
$EndComp
$Comp
L Connector:Conn_01x13_Male J1-1
U 1 1 5C9E15B9
P 7700 1850
F 0 "J1-1" H 7850 1150 50  0000 R CNN
F 1 "Conn_01x13_Male" H 8050 1100 50  0000 R CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_1x13_P2.54mm_Vertical" H 7700 1850 50  0001 C CNN
F 3 "~" H 7700 1850 50  0001 C CNN
	1    7700 1850
	-1   0    0    -1  
$EndComp
$Comp
L Connector:Conn_01x13_Male J2-2
U 1 1 5C9E71A0
P 10050 1850
F 0 "J2-2" H 10156 2628 50  0000 C CNN
F 1 "Conn_01x13_Male" H 10156 2537 50  0000 C CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_1x13_P2.54mm_Vertical" H 10050 1850 50  0001 C CNN
F 3 "~" H 10050 1850 50  0001 C CNN
	1    10050 1850
	1    0    0    -1  
$EndComp
$Comp
L Connector:Conn_01x13_Male J2-1
U 1 1 5C9E71A7
P 9950 1850
F 0 "J2-1" H 10100 1150 50  0000 R CNN
F 1 "Conn_01x13_Male" H 10300 1100 50  0000 R CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_1x13_P2.54mm_Vertical" H 9950 1850 50  0001 C CNN
F 3 "~" H 9950 1850 50  0001 C CNN
	1    9950 1850
	-1   0    0    -1  
$EndComp
$Comp
L Connector:Conn_01x13_Male J4-2
U 1 1 5C9ECD98
P 10000 4450
F 0 "J4-2" H 10106 5228 50  0000 C CNN
F 1 "Conn_01x13_Male" H 10106 5137 50  0000 C CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_1x13_P2.54mm_Vertical" H 10000 4450 50  0001 C CNN
F 3 "~" H 10000 4450 50  0001 C CNN
	1    10000 4450
	1    0    0    -1  
$EndComp
$Comp
L Connector:Conn_01x13_Male J4-1
U 1 1 5C9ECD9F
P 9900 4450
F 0 "J4-1" H 10050 3750 50  0000 R CNN
F 1 "Conn_01x13_Male" H 10250 3700 50  0000 R CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_1x13_P2.54mm_Vertical" H 9900 4450 50  0001 C CNN
F 3 "~" H 9900 4450 50  0001 C CNN
	1    9900 4450
	-1   0    0    -1  
$EndComp
Wire Bus Line
	11000 850  11050 850 
Connection ~ 11000 3150
Connection ~ 900  3150
Connection ~ 8800 3150
Wire Bus Line
	8800 3150 11000 3150
Entry Wire Line
	6550 1250 6650 1350
Entry Wire Line
	6550 1350 6650 1450
Entry Wire Line
	6550 1450 6650 1550
Entry Wire Line
	6550 1550 6650 1650
Entry Wire Line
	6550 1650 6650 1750
Entry Wire Line
	6550 1750 6650 1850
Entry Wire Line
	6550 1850 6650 1950
Entry Wire Line
	6550 1950 6650 2050
Entry Wire Line
	6550 2050 6650 2150
Entry Wire Line
	6550 2150 6650 2250
Entry Wire Line
	6550 2250 6650 2350
Entry Wire Line
	6550 2350 6650 2450
Entry Wire Line
	6550 1150 6650 1250
Entry Wire Line
	8700 1250 8800 1350
Entry Wire Line
	8700 1350 8800 1450
Entry Wire Line
	8700 1450 8800 1550
Entry Wire Line
	8700 1550 8800 1650
Entry Wire Line
	8700 1650 8800 1750
Entry Wire Line
	8700 1750 8800 1850
Entry Wire Line
	8700 1850 8800 1950
Entry Wire Line
	8700 1950 8800 2050
Entry Wire Line
	8700 2050 8800 2150
Entry Wire Line
	8700 2150 8800 2250
Entry Wire Line
	8700 2250 8800 2350
Entry Wire Line
	8700 2350 8800 2450
Entry Wire Line
	8800 1150 8900 1250
Entry Wire Line
	8800 1250 8900 1350
Entry Wire Line
	8800 1350 8900 1450
Entry Wire Line
	8800 1450 8900 1550
Entry Wire Line
	8800 1550 8900 1650
Entry Wire Line
	8800 1650 8900 1750
Entry Wire Line
	8800 1750 8900 1850
Entry Wire Line
	8800 1850 8900 1950
Entry Wire Line
	8800 1950 8900 2050
Entry Wire Line
	8800 2050 8900 2150
Entry Wire Line
	8800 2150 8900 2250
Entry Wire Line
	8800 2250 8900 2350
Entry Wire Line
	8800 2350 8900 2450
Entry Wire Line
	8800 3750 8900 3850
Entry Wire Line
	8800 3850 8900 3950
Entry Wire Line
	8800 3950 8900 4050
Entry Wire Line
	8800 4050 8900 4150
Entry Wire Line
	8800 4150 8900 4250
Entry Wire Line
	8800 4250 8900 4350
Entry Wire Line
	8800 4350 8900 4450
Entry Wire Line
	8800 4450 8900 4550
Entry Wire Line
	8800 4550 8900 4650
Entry Wire Line
	8800 4650 8900 4750
Entry Wire Line
	8800 4750 8900 4850
Entry Wire Line
	8800 4850 8900 4950
Entry Wire Line
	8800 4950 8900 5050
Entry Wire Line
	6550 3850 6650 3950
Entry Wire Line
	6550 3950 6650 4050
Entry Wire Line
	6550 4050 6650 4150
Entry Wire Line
	6550 4150 6650 4250
Entry Wire Line
	6550 4250 6650 4350
Entry Wire Line
	6550 4350 6650 4450
Entry Wire Line
	6550 4450 6650 4550
Entry Wire Line
	6550 4550 6650 4650
Entry Wire Line
	6550 4650 6650 4750
Entry Wire Line
	6550 4750 6650 4850
Entry Wire Line
	6550 4850 6650 4950
Entry Wire Line
	6550 4950 6650 5050
Entry Wire Line
	6550 3750 6650 3850
Entry Wire Line
	8700 2450 8800 2550
Entry Wire Line
	10900 1250 11000 1350
Entry Wire Line
	10900 1350 11000 1450
Entry Wire Line
	10900 1450 11000 1550
Entry Wire Line
	10900 1550 11000 1650
Entry Wire Line
	10900 1650 11000 1750
Entry Wire Line
	10900 1750 11000 1850
Entry Wire Line
	10900 1850 11000 1950
Entry Wire Line
	10900 1950 11000 2050
Entry Wire Line
	10900 2050 11000 2150
Entry Wire Line
	10900 2150 11000 2250
Entry Wire Line
	10900 2250 11000 2350
Entry Wire Line
	10900 2350 11000 2450
Entry Wire Line
	10900 2450 11000 2550
Entry Wire Line
	10900 3850 11000 3950
Entry Wire Line
	10900 3950 11000 4050
Entry Wire Line
	10900 4050 11000 4150
Entry Wire Line
	10900 4150 11000 4250
Entry Wire Line
	10900 4250 11000 4350
Entry Wire Line
	10900 4350 11000 4450
Entry Wire Line
	10900 4450 11000 4550
Entry Wire Line
	10900 4550 11000 4650
Entry Wire Line
	10900 4650 11000 4750
Entry Wire Line
	10900 4750 11000 4850
Entry Wire Line
	10900 4850 11000 4950
Entry Wire Line
	10900 4950 11000 5050
Entry Wire Line
	10900 5050 11000 5150
Entry Wire Line
	8700 3850 8800 3950
Entry Wire Line
	8700 3950 8800 4050
Entry Wire Line
	8700 4050 8800 4150
Entry Wire Line
	8700 4150 8800 4250
Entry Wire Line
	8700 4250 8800 4350
Entry Wire Line
	8700 4350 8800 4450
Entry Wire Line
	8700 4450 8800 4550
Entry Wire Line
	8700 4550 8800 4650
Entry Wire Line
	8700 4650 8800 4750
Entry Wire Line
	8700 4750 8800 4850
Entry Wire Line
	8700 4850 8800 4950
Entry Wire Line
	8700 4950 8800 5050
Entry Wire Line
	8700 5050 8800 5150
Wire Wire Line
	6650 1250 7500 1250
Wire Wire Line
	7500 1350 6650 1350
Wire Wire Line
	6650 1450 7500 1450
Wire Wire Line
	7500 1550 6650 1550
Wire Wire Line
	6650 1650 7500 1650
Wire Wire Line
	7500 1750 6650 1750
Wire Wire Line
	6650 1850 7500 1850
Wire Wire Line
	7500 1950 6650 1950
Wire Wire Line
	6650 2050 7500 2050
Wire Wire Line
	7500 2150 6650 2150
Wire Wire Line
	6650 2250 7500 2250
Wire Wire Line
	7500 2350 6650 2350
Wire Wire Line
	6650 2450 7500 2450
Wire Wire Line
	8000 1250 8700 1250
Wire Wire Line
	8700 1350 8000 1350
Wire Wire Line
	8000 1450 8700 1450
Wire Wire Line
	8700 1550 8000 1550
Wire Wire Line
	8000 1650 8700 1650
Wire Wire Line
	8700 1750 8000 1750
Wire Wire Line
	8000 1850 8700 1850
Wire Wire Line
	8700 1950 8000 1950
Wire Wire Line
	8000 2050 8700 2050
Wire Wire Line
	8700 2150 8000 2150
Wire Wire Line
	8000 2250 8700 2250
Wire Wire Line
	8700 2350 8000 2350
Wire Wire Line
	8000 2450 8700 2450
Wire Wire Line
	8900 1250 9750 1250
Wire Wire Line
	9750 1350 8900 1350
Wire Wire Line
	8900 1450 9750 1450
Wire Wire Line
	9750 1550 8900 1550
Wire Wire Line
	8900 1650 9750 1650
Wire Wire Line
	9750 1750 8900 1750
Wire Wire Line
	8900 1850 9750 1850
Wire Wire Line
	9750 1950 8900 1950
Wire Wire Line
	8900 2050 9750 2050
Wire Wire Line
	9750 2150 8900 2150
Wire Wire Line
	8900 2250 9750 2250
Wire Wire Line
	9750 2350 8900 2350
Wire Wire Line
	8900 2450 9750 2450
Wire Wire Line
	10250 1250 10900 1250
Wire Wire Line
	10900 1350 10250 1350
Wire Wire Line
	10250 1450 10900 1450
Wire Wire Line
	10900 1550 10250 1550
Wire Wire Line
	10250 1650 10900 1650
Wire Wire Line
	10900 1750 10250 1750
Wire Wire Line
	10250 1850 10900 1850
Wire Wire Line
	10900 1950 10250 1950
Wire Wire Line
	10250 2050 10900 2050
Wire Wire Line
	10900 2150 10250 2150
Wire Wire Line
	10250 2250 10900 2250
Wire Wire Line
	10900 2350 10250 2350
Wire Wire Line
	10250 2450 10900 2450
Wire Wire Line
	10900 3850 10200 3850
Wire Wire Line
	10200 3950 10900 3950
Wire Wire Line
	10900 4050 10200 4050
Wire Wire Line
	10200 4150 10900 4150
Wire Wire Line
	10900 4250 10200 4250
Wire Wire Line
	10200 4350 10900 4350
Wire Wire Line
	10900 4450 10200 4450
Wire Wire Line
	10200 4550 10900 4550
Wire Wire Line
	10900 4650 10200 4650
Wire Wire Line
	10200 4750 10900 4750
Wire Wire Line
	10900 4850 10200 4850
Wire Wire Line
	10200 4950 10900 4950
Wire Wire Line
	10900 5050 10200 5050
Wire Wire Line
	9700 3850 8900 3850
Wire Wire Line
	8900 3950 9700 3950
Wire Wire Line
	9700 4050 8900 4050
Wire Wire Line
	8900 4150 9700 4150
Wire Wire Line
	9700 4250 8900 4250
Wire Wire Line
	8900 4350 9700 4350
Wire Wire Line
	9700 4450 8900 4450
Wire Wire Line
	8900 4550 9700 4550
Wire Wire Line
	9700 4650 8900 4650
Wire Wire Line
	8900 4750 9700 4750
Wire Wire Line
	9700 4850 8900 4850
Wire Wire Line
	8900 4950 9700 4950
Wire Wire Line
	9700 5050 8900 5050
Wire Wire Line
	8700 3850 7950 3850
Wire Wire Line
	7950 3950 8700 3950
Wire Wire Line
	8700 4050 7950 4050
Wire Wire Line
	7950 4150 8700 4150
Wire Wire Line
	8700 4250 7950 4250
Wire Wire Line
	7950 4350 8700 4350
Wire Wire Line
	8700 4450 7950 4450
Wire Wire Line
	7950 4550 8700 4550
Wire Wire Line
	8700 4650 7950 4650
Wire Wire Line
	7950 4750 8700 4750
Wire Wire Line
	8700 4850 7950 4850
Wire Wire Line
	7950 4950 8700 4950
Wire Wire Line
	8700 5050 7950 5050
Wire Wire Line
	6650 5050 7450 5050
Wire Wire Line
	7450 4950 6650 4950
Wire Wire Line
	6650 4850 7450 4850
Wire Wire Line
	7450 4750 6650 4750
Wire Wire Line
	6650 4650 7450 4650
Wire Wire Line
	7450 4550 6650 4550
Wire Wire Line
	6650 4450 7450 4450
Wire Wire Line
	7450 4350 6650 4350
Wire Wire Line
	6650 4250 7450 4250
Wire Wire Line
	7450 4150 6650 4150
Wire Wire Line
	6650 4050 7450 4050
Wire Wire Line
	7450 3950 6650 3950
Wire Wire Line
	6650 3850 7450 3850
Text Label 7050 4650 0    50   ~ 0
VCC
Text Label 9250 4050 0    50   ~ 0
VCC
Text Label 10450 3950 0    50   ~ 0
VCC
Text Label 10450 4350 0    50   ~ 0
VCC
Text Label 9300 1650 0    50   ~ 0
VCC
Text Label 9300 2450 0    50   ~ 0
VCC
Text Label 10450 1750 0    50   ~ 0
VCC
Text Notes 6950 3350 0    50   ~ 0
Jx-1 = outside dubbel row connector\nJx-2 = inside dubbel row connector
Text Label 7050 1250 0    50   ~ 0
VCC
Text Label 7050 1750 0    50   ~ 0
VCC
Text Label 7050 4550 0    50   ~ 0
GND
Text Label 10450 4050 0    50   ~ 0
GND
Text Label 9250 4350 0    50   ~ 0
GND
Text Label 9250 4550 0    50   ~ 0
GND
Text Label 10450 1650 0    50   ~ 0
GND
Text Label 10450 2250 0    50   ~ 0
GND
Text Label 9300 1950 0    50   ~ 0
GND
Text Label 8200 1350 0    50   ~ 0
GND
Text Label 8200 1650 0    50   ~ 0
GND
Text Label 7050 1550 0    50   ~ 0
GND
Text Label 7050 4450 0    50   ~ 0
MCLR
Text Label 9300 1550 0    50   ~ 0
RB7
Text Label 9300 2050 0    50   ~ 0
RB6
Wire Bus Line
	3950 5750 6850 5750
Wire Bus Line
	900  3150 3950 3150
Wire Bus Line
	900  5750 3950 5750
Wire Bus Line
	3950 3150 8800 3150
Wire Bus Line
	3950 3450 3950 5750
Wire Bus Line
	900  850  900  3150
Wire Bus Line
	3500 850  3500 3050
Wire Bus Line
	3950 850  3950 3150
Wire Bus Line
	900  3150 900  5750
Wire Bus Line
	3500 3450 3500 5650
Wire Bus Line
	6550 850  6550 3050
Wire Bus Line
	8800 850  8800 3150
Wire Bus Line
	6550 3450 6550 5650
Wire Bus Line
	11000 850  11000 3150
Wire Bus Line
	11000 3150 11000 5750
Wire Bus Line
	8800 3150 8800 5750
Text Label 8200 1250 0    50   ~ 0
TXN
Text Label 9300 2350 0    50   ~ 0
RXN
Text Label 7050 1350 0    50   ~ 0
TXP
Text Label 10450 2350 0    50   ~ 0
RXP
Text Label 7050 2450 0    50   ~ 0
RB0
Text Label 7050 2350 0    50   ~ 0
RB2
Text Label 7050 2250 0    50   ~ 0
RB4
Text Label 7050 2150 0    50   ~ 0
RE9
Text Label 7050 2050 0    50   ~ 0
RA0
Text Label 7050 1950 0    50   ~ 0
RG7
Text Label 7050 1850 0    50   ~ 0
RC4
Text Label 7050 1650 0    50   ~ 0
RC2
Text Label 7050 1450 0    50   ~ 0
RE7
Text Label 8200 2450 0    50   ~ 0
RB1
Text Label 8200 2350 0    50   ~ 0
RB3
Text Label 8200 2250 0    50   ~ 0
RB5
Text Label 8200 2150 0    50   ~ 0
RE8
Text Label 8200 2050 0    50   ~ 0
RG9
Text Label 8200 1950 0    50   ~ 0
RG8
Text Label 8200 1850 0    50   ~ 0
RG6
Text Label 8200 1750 0    50   ~ 0
RC3
Text Label 8200 1550 0    50   ~ 0
RC1
Text Label 8200 1450 0    50   ~ 0
VCC
Text Label 9250 4250 0    50   ~ 0
LED1
Text Label 10450 4250 0    50   ~ 0
LED2
Text Label 7050 5050 0    50   ~ 0
RF4
Text Label 7050 4950 0    50   ~ 0
RD14
Text Label 7050 4850 0    50   ~ 0
RB14
Text Label 7050 4750 0    50   ~ 0
RB12
Text Label 7050 4350 0    50   ~ 0
RB10
Text Label 7050 4250 0    50   ~ 0
RB8
Text Label 7050 4150 0    50   ~ 0
RA9
Text Label 8150 5050 0    50   ~ 0
RF5
Text Label 8150 4950 0    50   ~ 0
RB15
Text Label 8150 4850 0    50   ~ 0
RB13
Text Label 8150 4750 0    50   ~ 0
RF12
Text Label 8150 4650 0    50   ~ 0
RA1
Text Label 8150 4550 0    50   ~ 0
RB11
Text Label 8150 4450 0    50   ~ 0
RB9
Text Label 8150 4350 0    50   ~ 0
RA10
Text Label 9250 5050 0    50   ~ 0
RD0
Text Label 9250 4950 0    50   ~ 0
RD10
Text Label 9250 4850 0    50   ~ 0
RA14
Text Label 9250 4750 0    50   ~ 0
RA4
Text Label 9250 4450 0    50   ~ 0
RA2
Text Label 9250 4150 0    50   ~ 0
MCU_MISO
Text Label 10450 5050 0    50   ~ 0
RD9
Text Label 10450 4950 0    50   ~ 0
RA15
Text Label 10450 4850 0    50   ~ 0
RA5
Text Label 10450 4750 0    50   ~ 0
RA3
Text Label 10450 4650 0    50   ~ 0
MCU_MOSI
Text Label 10450 4550 0    50   ~ 0
MCU_SCK
Text Label 10450 4450 0    50   ~ 0
RF3
Text Label 9300 1250 0    50   ~ 0
RD1
Text Label 9300 1350 0    50   ~ 0
RD3
Text Label 9300 1450 0    50   ~ 0
RD12
Text Label 9300 1750 0    50   ~ 0
RD4
Text Label 9300 1850 0    50   ~ 0
RF1
Text Label 9300 2150 0    50   ~ 0
RG12
Text Label 9300 2250 0    50   ~ 0
RE2
Text Label 10450 1250 0    50   ~ 0
RD2
Text Label 10450 1350 0    50   ~ 0
RD13
Text Label 10450 1550 0    50   ~ 0
RD5
Text Label 10450 1850 0    50   ~ 0
RD7
Text Label 10450 1950 0    50   ~ 0
RF0
Text Label 10450 2050 0    50   ~ 0
RE0
Text Label 10450 2150 0    50   ~ 0
RG14
Text Label 10450 2450 0    50   ~ 0
RE3
Text Label 7050 3950 0    50   ~ 0
LED1
Text Label 7050 4050 0    50   ~ 0
LED2
$EndSCHEMATC
