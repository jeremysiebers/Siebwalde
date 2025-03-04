EESchema Schematic File Version 4
LIBS:MCP6N11_piggy_back-cache
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
L Amplifier_Instrumentation:INA326 U1
U 1 1 5BB5B05C
P 5450 3450
F 0 "U1" H 5600 3575 50  0000 L CNN
F 1 "MCP6N11" H 5600 3325 50  0000 L CNN
F 2 "Package_SO:SOIC-8_3.9x4.9mm_P1.27mm" H 5450 3450 50  0001 L CNN
F 3 "http://www.ti.com/lit/ds/symlink/ina326.pdf" H 5550 3450 50  0001 C CNN
	1    5450 3450
	1    0    0    -1  
$EndComp
$Comp
L Device:R R1
U 1 1 5BB5B0F0
P 3350 3250
F 0 "R1" V 3430 3250 50  0000 C CNN
F 1 "9k10" V 3350 3250 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 3280 3250 50  0001 C CNN
F 3 "~" H 3350 3250 50  0001 C CNN
	1    3350 3250
	0    1    1    0   
$EndComp
$Comp
L Device:R R2
U 1 1 5BB5B123
P 3350 3650
F 0 "R2" V 3430 3650 50  0000 C CNN
F 1 "9k10" V 3350 3650 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 3280 3650 50  0001 C CNN
F 3 "~" H 3350 3650 50  0001 C CNN
	1    3350 3650
	0    1    1    0   
$EndComp
$Comp
L Device:R R6
U 1 1 5BB5B19E
P 4950 3900
F 0 "R6" V 5030 3900 50  0000 C CNN
F 1 "1k" V 4950 3900 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 4880 3900 50  0001 C CNN
F 3 "~" H 4950 3900 50  0001 C CNN
	1    4950 3900
	-1   0    0    1   
$EndComp
$Comp
L Device:R R5
U 1 1 5BB5B1E7
P 4750 3900
F 0 "R5" V 4830 3900 50  0000 C CNN
F 1 "1k" V 4750 3900 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 4680 3900 50  0001 C CNN
F 3 "~" H 4750 3900 50  0001 C CNN
	1    4750 3900
	-1   0    0    1   
$EndComp
Wire Wire Line
	4950 3250 4950 3750
Wire Wire Line
	5150 3250 4950 3250
Connection ~ 4950 3250
$Comp
L power:+5V #PWR01
U 1 1 5BB5B4C1
P 4450 2300
F 0 "#PWR01" H 4450 2150 50  0001 C CNN
F 1 "+5V" H 4450 2440 50  0000 C CNN
F 2 "" H 4450 2300 50  0001 C CNN
F 3 "" H 4450 2300 50  0001 C CNN
	1    4450 2300
	1    0    0    -1  
$EndComp
$Comp
L power:GND #PWR02
U 1 1 5BB5B52C
P 4950 4150
F 0 "#PWR02" H 4950 3900 50  0001 C CNN
F 1 "GND" H 4950 4000 50  0000 C CNN
F 2 "" H 4950 4150 50  0001 C CNN
F 3 "" H 4950 4150 50  0001 C CNN
	1    4950 4150
	1    0    0    -1  
$EndComp
$Comp
L Device:R R4
U 1 1 5BB5B5C4
P 4750 2450
F 0 "R4" V 4830 2450 50  0000 C CNN
F 1 "10E" V 4750 2450 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 4680 2450 50  0001 C CNN
F 3 "~" H 4750 2450 50  0001 C CNN
	1    4750 2450
	0    1    1    0   
$EndComp
$Comp
L Device:C C4
U 1 1 5BB5B6BC
P 5250 2700
F 0 "C4" H 5275 2800 50  0000 L CNN
F 1 "2.2u" H 5275 2600 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 5288 2550 50  0001 C CNN
F 3 "~" H 5250 2700 50  0001 C CNN
	1    5250 2700
	1    0    0    -1  
$EndComp
$Comp
L Device:C C3
U 1 1 5BB5B70D
P 5000 2700
F 0 "C3" H 5025 2800 50  0000 L CNN
F 1 "100n" H 5025 2600 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 5038 2550 50  0001 C CNN
F 3 "~" H 5000 2700 50  0001 C CNN
	1    5000 2700
	1    0    0    -1  
$EndComp
$Comp
L power:GND #PWR03
U 1 1 5BB5B901
P 5000 3000
F 0 "#PWR03" H 5000 2750 50  0001 C CNN
F 1 "GND" H 5000 2850 50  0000 C CNN
F 2 "" H 5000 3000 50  0001 C CNN
F 3 "" H 5000 3000 50  0001 C CNN
	1    5000 3000
	1    0    0    -1  
$EndComp
Wire Wire Line
	4750 4050 4950 4050
Wire Wire Line
	4950 4050 4950 4150
Connection ~ 4950 4050
$Comp
L power:GND #PWR04
U 1 1 5BB5BB8B
P 5450 4150
F 0 "#PWR04" H 5450 3900 50  0001 C CNN
F 1 "GND" H 5450 4000 50  0000 C CNN
F 2 "" H 5450 4150 50  0001 C CNN
F 3 "" H 5450 4150 50  0001 C CNN
	1    5450 4150
	1    0    0    -1  
$EndComp
$Comp
L Device:R R8
U 1 1 5BB5BC99
P 6150 4050
F 0 "R8" V 6230 4050 50  0000 C CNN
F 1 "100k" V 6150 4050 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 6080 4050 50  0001 C CNN
F 3 "~" H 6150 4050 50  0001 C CNN
	1    6150 4050
	-1   0    0    1   
$EndComp
$Comp
L Device:R R7
U 1 1 5BB5BCC5
P 6150 3700
F 0 "R7" V 6230 3700 50  0000 C CNN
F 1 "1k" V 6150 3700 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 6080 3700 50  0001 C CNN
F 3 "~" H 6150 3700 50  0001 C CNN
	1    6150 3700
	-1   0    0    1   
$EndComp
$Comp
L Device:R R3
U 1 1 5BB5BD01
P 4550 3000
F 0 "R3" V 4630 3000 50  0000 C CNN
F 1 "4k75" V 4550 3000 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 4480 3000 50  0001 C CNN
F 3 "~" H 4550 3000 50  0001 C CNN
	1    4550 3000
	-1   0    0    1   
$EndComp
$Comp
L Connector:Conn_01x02_Male J4
U 1 1 5BB5C1A3
P 6950 2850
F 0 "J4" H 6950 2950 50  0000 C CNN
F 1 "Conn_01x02_Male" H 6800 2650 50  0000 C CNN
F 2 "Connector_PinHeader_2.00mm:PinHeader_1x02_P2.00mm_Vertical" H 6950 2850 50  0001 C CNN
F 3 "~" H 6950 2850 50  0001 C CNN
	1    6950 2850
	-1   0    0    1   
$EndComp
$Comp
L Connector:Conn_01x02_Male J5
U 1 1 5BB5C20E
P 6950 3550
F 0 "J5" H 6950 3650 50  0000 C CNN
F 1 "Conn_01x02_Male" H 6950 3350 50  0000 C CNN
F 2 "Connector_PinHeader_2.00mm:PinHeader_1x02_P2.00mm_Vertical" H 6950 3550 50  0001 C CNN
F 3 "~" H 6950 3550 50  0001 C CNN
	1    6950 3550
	-1   0    0    1   
$EndComp
$Comp
L Connector:Conn_01x02_Male J1
U 1 1 5BB5C279
P 2900 3400
F 0 "J1" H 2900 3500 50  0000 C CNN
F 1 "Conn_01x02_Male" H 2650 3200 50  0000 C CNN
F 2 "Connector_PinHeader_2.00mm:PinHeader_1x02_P2.00mm_Vertical" H 2900 3400 50  0001 C CNN
F 3 "~" H 2900 3400 50  0001 C CNN
	1    2900 3400
	1    0    0    -1  
$EndComp
$Comp
L Connector:Conn_01x01_Male J2
U 1 1 5BB5C41E
P 5200 4450
F 0 "J2" H 5200 4550 50  0000 C CNN
F 1 "Conn_01x01_Male" V 5100 4450 50  0000 C CNN
F 2 "Connector_PinHeader_1.00mm:PinHeader_1x01_P1.00mm_Vertical" H 5200 4450 50  0001 C CNN
F 3 "~" H 5200 4450 50  0001 C CNN
	1    5200 4450
	0    -1   -1   0   
$EndComp
$Comp
L power:GND #PWR07
U 1 1 5BB5D241
P 6650 3650
F 0 "#PWR07" H 6650 3400 50  0001 C CNN
F 1 "GND" H 6650 3500 50  0000 C CNN
F 2 "" H 6650 3650 50  0001 C CNN
F 3 "" H 6650 3650 50  0001 C CNN
	1    6650 3650
	1    0    0    -1  
$EndComp
$Comp
L power:GND #PWR06
U 1 1 5BB5D26C
P 6650 2950
F 0 "#PWR06" H 6650 2700 50  0001 C CNN
F 1 "GND" H 6650 2800 50  0000 C CNN
F 2 "" H 6650 2950 50  0001 C CNN
F 3 "" H 6650 2950 50  0001 C CNN
	1    6650 2950
	1    0    0    -1  
$EndComp
$Comp
L power:+5V #PWR05
U 1 1 5BB5D297
P 6650 2650
F 0 "#PWR05" H 6650 2500 50  0001 C CNN
F 1 "+5V" H 6650 2790 50  0000 C CNN
F 2 "" H 6650 2650 50  0001 C CNN
F 3 "" H 6650 2650 50  0001 C CNN
	1    6650 2650
	1    0    0    -1  
$EndComp
Wire Wire Line
	6750 2750 6650 2750
Wire Wire Line
	6650 2750 6650 2650
Wire Wire Line
	6650 2950 6650 2850
Wire Wire Line
	6650 2850 6750 2850
Wire Wire Line
	6750 3550 6650 3550
Wire Wire Line
	6650 3550 6650 3650
Wire Wire Line
	3100 3250 3100 3400
Wire Wire Line
	3100 3500 3100 3650
Wire Wire Line
	5050 3350 5150 3350
Wire Wire Line
	6150 4200 6150 4250
Wire Wire Line
	6150 4250 5550 4250
Wire Wire Line
	5550 4250 5550 4000
Text Notes 6200 3750 0    50   ~ 0
Rf
Text Notes 6200 4100 0    50   ~ 0
Rg
Text Notes 5100 4700 0    50   ~ 0
Vref
Wire Wire Line
	3100 3250 3200 3250
Wire Wire Line
	3100 3650 3200 3650
Wire Wire Line
	4750 3750 4750 3650
Connection ~ 4750 3650
Wire Wire Line
	4750 3650 5150 3650
$Comp
L Device:D D1
U 1 1 5BB653CD
P 3900 3000
F 0 "D1" H 3900 3100 50  0000 C CNN
F 1 "1n4148" H 3900 2900 50  0000 C CNN
F 2 "Diode_SMD:D_SOD-123F" H 3900 3000 50  0001 C CNN
F 3 "~" H 3900 3000 50  0001 C CNN
	1    3900 3000
	0    1    1    0   
$EndComp
$Comp
L Device:D D2
U 1 1 5BB6543B
P 4250 3000
F 0 "D2" H 4250 3100 50  0000 C CNN
F 1 "1n4148" H 4250 2900 50  0000 C CNN
F 2 "Diode_SMD:D_SOD-123F" H 4250 3000 50  0001 C CNN
F 3 "~" H 4250 3000 50  0001 C CNN
	1    4250 3000
	0    1    1    0   
$EndComp
Wire Wire Line
	3900 3150 3900 3250
Connection ~ 3900 3250
Wire Wire Line
	3900 3250 4950 3250
Wire Wire Line
	4250 3150 4250 3650
Connection ~ 4250 3650
Wire Wire Line
	4250 3650 4750 3650
Wire Wire Line
	4250 2750 4250 2850
Wire Wire Line
	4250 2750 3900 2750
Wire Wire Line
	3900 2750 3900 2850
Wire Wire Line
	4250 2450 4250 2750
Wire Wire Line
	4250 2450 4450 2450
Connection ~ 4250 2750
Wire Wire Line
	4450 2300 4450 2450
Connection ~ 4450 2450
Wire Wire Line
	4450 2450 4550 2450
Wire Wire Line
	4550 2850 4550 2450
Wire Wire Line
	4550 3550 4550 3150
Wire Wire Line
	4600 2450 4550 2450
Connection ~ 4550 2450
Wire Wire Line
	4900 2450 5000 2450
Wire Wire Line
	5000 2450 5000 2550
Wire Wire Line
	5000 2450 5250 2450
Wire Wire Line
	5250 2450 5250 2550
Connection ~ 5000 2450
Wire Wire Line
	5450 2450 5250 2450
Wire Wire Line
	5450 2450 5450 3150
Connection ~ 5250 2450
Wire Wire Line
	5250 2850 5000 2850
Wire Wire Line
	5000 2850 5000 3000
Connection ~ 5000 2850
$Comp
L Device:C C2
U 1 1 5BB762F2
P 3350 3900
F 0 "C2" H 3375 4000 50  0000 L CNN
F 1 "22p" H 3375 3800 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 3388 3750 50  0001 C CNN
F 3 "~" H 3350 3900 50  0001 C CNN
	1    3350 3900
	0    1    1    0   
$EndComp
$Comp
L Device:C C1
U 1 1 5BB76361
P 3350 2950
F 0 "C1" H 3375 3050 50  0000 L CNN
F 1 "22p" H 3375 2850 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 3388 2800 50  0001 C CNN
F 3 "~" H 3350 2950 50  0001 C CNN
	1    3350 2950
	0    1    1    0   
$EndComp
Wire Wire Line
	3200 2950 3100 2950
Wire Wire Line
	3100 2950 3100 3250
Connection ~ 3100 3250
Wire Wire Line
	3200 3900 3100 3900
Wire Wire Line
	3100 3900 3100 3650
Connection ~ 3100 3650
Wire Wire Line
	3500 2950 3600 2950
Wire Wire Line
	3600 2950 3600 3250
Wire Wire Line
	3600 3250 3500 3250
Wire Wire Line
	3600 3250 3900 3250
Connection ~ 3600 3250
Wire Wire Line
	3500 3650 3600 3650
Wire Wire Line
	3500 3900 3600 3900
Wire Wire Line
	3600 3900 3600 3650
Connection ~ 3600 3650
Wire Wire Line
	3600 3650 4250 3650
Wire Wire Line
	5450 3750 5450 4150
Wire Wire Line
	5200 4000 5550 4000
Connection ~ 5550 4000
Wire Wire Line
	5550 4000 5550 3750
Wire Wire Line
	4550 3550 5150 3550
Wire Wire Line
	5200 4000 5200 4250
Text Notes 5850 4350 0    50   ~ 0
see page 9, Gmin = 1 and is 1 when Rf = 0 Ohm and Rg = open
Wire Wire Line
	5850 3450 6150 3450
Wire Wire Line
	6150 3550 6150 3450
Connection ~ 6150 3450
Wire Wire Line
	6150 3450 6750 3450
Wire Wire Line
	6150 3850 6150 3900
Wire Wire Line
	5050 3850 6150 3850
Wire Wire Line
	5050 3350 5050 3850
Connection ~ 6150 3850
$EndSCHEMATC
