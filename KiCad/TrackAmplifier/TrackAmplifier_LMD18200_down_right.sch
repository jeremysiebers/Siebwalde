EESchema Schematic File Version 4
LIBS:TrackAmplifier-cache
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
L TrackAmplifier-rescue:DB15_Male_MountingHoles J1
U 1 1 5ABE3833
P 10400 5250
F 0 "J1" H 10400 6200 50  0000 C CNN
F 1 "DB15_Male_MountingHoles" H 10400 6125 50  0000 C CNN
F 2 "Connector_Dsub:DSUB-15_Male_Horizontal_P2.77x2.84mm_EdgePinOffset4.94mm_Housed_MountingHolesOffset7.48mm" H 10400 5250 50  0001 C CNN
F 3 "" H 10400 5250 50  0001 C CNN
	1    10400 5250
	1    0    0    1   
$EndComp
Wire Wire Line
	10100 4550 9250 4550
Wire Wire Line
	9250 4550 9250 4750
Wire Wire Line
	10100 5950 9250 5950
Wire Wire Line
	10100 4650 9200 4650
Wire Wire Line
	9200 5850 10100 5850
$Comp
L TrackAmplifier-rescue:C C7
U 1 1 5ABE3B27
P 1950 1050
F 0 "C7" H 1975 1150 50  0000 L CNN
F 1 "100n" H 1975 950 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 1988 900 50  0001 C CNN
F 3 "" H 1950 1050 50  0001 C CNN
	1    1950 1050
	-1   0    0    1   
$EndComp
$Comp
L Interface_UART:ST485EBDR U1
U 1 1 5AE19840
P 1850 2400
F 0 "U1" H 1600 2750 50  0000 C CNN
F 1 "ST485EBDR" H 1950 2750 50  0000 L CNN
F 2 "Package_SO:SOIC-8_3.9x4.9mm_P1.27mm" H 1850 1500 50  0001 C CNN
F 3 "http://www.st.com/resource/en/datasheet/st485eb.pdf" H 1850 2450 50  0001 C CNN
	1    1850 2400
	-1   0    0    -1  
$EndComp
$Comp
L Regulator_Linear:LM78M05_TO252 U3
U 1 1 5AE1A916
P 2350 800
F 0 "U3" H 2200 925 50  0000 C CNN
F 1 "LM78M05_TO252" H 2350 925 50  0000 L CNN
F 2 "Package_TO_SOT_SMD:TO-252-2" H 2350 1025 50  0001 C CIN
F 3 "http://www.fairchildsemi.com/ds/LM/LM78M05.pdf" H 2350 750 50  0001 C CNN
	1    2350 800 
	1    0    0    -1  
$EndComp
$Comp
L TrackAmplifier-rescue:C C5
U 1 1 5AE1AF47
P 1650 1050
F 0 "C5" H 1675 1150 50  0000 L CNN
F 1 "22u" H 1675 950 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 1688 900 50  0001 C CNN
F 3 "" H 1650 1050 50  0001 C CNN
	1    1650 1050
	-1   0    0    1   
$EndComp
$Comp
L TrackAmplifier-rescue:C C10
U 1 1 5AE1AF69
P 2900 1050
F 0 "C10" H 2925 1150 50  0000 L CNN
F 1 "100n" H 2925 950 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 2938 900 50  0001 C CNN
F 3 "" H 2900 1050 50  0001 C CNN
	1    2900 1050
	-1   0    0    1   
$EndComp
$Comp
L TrackAmplifier-rescue:C C13
U 1 1 5AE1AFA5
P 3200 1050
F 0 "C13" H 3225 1150 50  0000 L CNN
F 1 "22u" H 3225 950 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 3238 900 50  0001 C CNN
F 3 "" H 3200 1050 50  0001 C CNN
	1    3200 1050
	-1   0    0    1   
$EndComp
Wire Wire Line
	1650 800  1650 900 
Wire Wire Line
	1650 800  1950 800 
Wire Wire Line
	1950 900  1950 800 
Connection ~ 1950 800 
Wire Wire Line
	1950 800  2050 800 
$Comp
L TrackAmplifier-rescue:GND #PWR019
U 1 1 5AE1B528
P 3750 1350
F 0 "#PWR019" H 3750 1100 50  0001 C CNN
F 1 "GND" H 3750 1200 50  0000 C CNN
F 2 "" H 3750 1350 50  0001 C CNN
F 3 "" H 3750 1350 50  0001 C CNN
	1    3750 1350
	-1   0    0    -1  
$EndComp
Wire Wire Line
	1650 1200 1650 1300
Wire Wire Line
	1650 1300 1950 1300
Wire Wire Line
	1950 1300 1950 1200
Wire Wire Line
	1950 1300 2350 1300
Wire Wire Line
	2350 1300 2350 1100
Connection ~ 1950 1300
Wire Wire Line
	2350 1300 2900 1300
Wire Wire Line
	2900 1300 2900 1200
Connection ~ 2350 1300
Wire Wire Line
	2900 1300 3200 1300
Wire Wire Line
	3200 1300 3200 1200
Connection ~ 2900 1300
Wire Wire Line
	2650 800  2900 800 
Wire Wire Line
	2900 800  2900 900 
Wire Wire Line
	2900 800  3200 800 
Wire Wire Line
	3200 800  3200 900 
Connection ~ 2900 800 
$Comp
L Device:CP C16
U 1 1 5AE1CE9B
P 3500 1050
F 0 "C16" H 3550 950 50  0000 L CNN
F 1 "100u" H 3550 1150 50  0000 L CNN
F 2 "Capacitor_THT:CP_Radial_D6.3mm_P2.50mm" H 3538 900 50  0001 C CNN
F 3 "~" H 3500 1050 50  0001 C CNN
	1    3500 1050
	1    0    0    -1  
$EndComp
Wire Wire Line
	3200 800  3500 800 
Wire Wire Line
	3500 800  3500 900 
Connection ~ 3200 800 
Wire Wire Line
	3500 1200 3500 1300
Wire Wire Line
	3500 1300 3200 1300
Connection ~ 3200 1300
$Comp
L power:+5V #PWR018
U 1 1 5AE1D9C6
P 3750 750
F 0 "#PWR018" H 3750 600 50  0001 C CNN
F 1 "+5V" H 3750 890 50  0000 C CNN
F 2 "" H 3750 750 50  0001 C CNN
F 3 "" H 3750 750 50  0001 C CNN
	1    3750 750 
	1    0    0    -1  
$EndComp
Wire Wire Line
	3500 800  3750 800 
Connection ~ 3500 800 
Connection ~ 1650 800 
Wire Wire Line
	3750 750  3750 800 
Wire Wire Line
	3750 1350 3750 1300
Wire Wire Line
	3750 1300 3500 1300
Connection ~ 3500 1300
$Comp
L TrackAmplifier-rescue:C C3
U 1 1 5AE23D01
P 1000 2450
F 0 "C3" H 1025 2550 50  0000 L CNN
F 1 "100n" H 1025 2350 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 1038 2300 50  0001 C CNN
F 3 "" H 1000 2450 50  0001 C CNN
	1    1000 2450
	-1   0    0    1   
$EndComp
$Comp
L TrackAmplifier-rescue:C C1
U 1 1 5AE23D39
P 750 2450
F 0 "C1" H 775 2550 50  0000 L CNN
F 1 "22u" H 775 2350 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 788 2300 50  0001 C CNN
F 3 "" H 750 2450 50  0001 C CNN
	1    750  2450
	-1   0    0    1   
$EndComp
$Comp
L power:+5V #PWR01
U 1 1 5AE29A91
P 750 1850
F 0 "#PWR01" H 750 1700 50  0001 C CNN
F 1 "+5V" H 750 1990 50  0000 C CNN
F 2 "" H 750 1850 50  0001 C CNN
F 3 "" H 750 1850 50  0001 C CNN
	1    750  1850
	1    0    0    -1  
$EndComp
$Comp
L TrackAmplifier-rescue:GND #PWR02
U 1 1 5AE29AB8
P 750 2700
F 0 "#PWR02" H 750 2450 50  0001 C CNN
F 1 "GND" H 750 2550 50  0000 C CNN
F 2 "" H 750 2700 50  0001 C CNN
F 3 "" H 750 2700 50  0001 C CNN
	1    750  2700
	-1   0    0    -1  
$EndComp
$Comp
L Device:R R1
U 1 1 5AE3F1D3
P 750 2050
F 0 "R1" V 830 2050 50  0000 C CNN
F 1 "10E" V 750 2050 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 680 2050 50  0001 C CNN
F 3 "~" H 750 2050 50  0001 C CNN
	1    750  2050
	-1   0    0    1   
$EndComp
Wire Wire Line
	750  1850 750  1900
Wire Wire Line
	750  2200 750  2250
Wire Wire Line
	750  2600 750  2650
Wire Wire Line
	750  2250 1000 2250
Wire Wire Line
	1000 2250 1000 2300
Connection ~ 750  2250
Wire Wire Line
	750  2250 750  2300
Wire Wire Line
	750  2650 1000 2650
Wire Wire Line
	1000 2650 1000 2600
Connection ~ 750  2650
Wire Wire Line
	750  2650 750  2700
Wire Wire Line
	1850 2000 1850 1950
Wire Wire Line
	1850 1950 1000 1950
Wire Wire Line
	1000 1950 1000 2250
Connection ~ 1000 2250
Wire Wire Line
	1850 2900 1850 2950
Wire Wire Line
	1850 2950 1000 2950
Wire Wire Line
	1000 2950 1000 2650
Connection ~ 1000 2650
Text GLabel 1400 2300 0    50   Input ~ 0
Reset_B
Text GLabel 1400 2600 0    50   Input ~ 0
Reset_A
Wire Wire Line
	1400 2300 1450 2300
Wire Wire Line
	1400 2600 1450 2600
Wire Wire Line
	1850 2950 2350 2950
Wire Wire Line
	2350 2950 2350 2600
Wire Wire Line
	2350 2500 2250 2500
Connection ~ 1850 2950
Wire Wire Line
	2250 2400 2350 2400
Wire Wire Line
	2350 2400 2350 2500
Connection ~ 2350 2500
Wire Wire Line
	2250 2600 2350 2600
Connection ~ 2350 2600
Wire Wire Line
	2350 2600 2350 2500
$Comp
L Interface_UART:ST485EBDR U2
U 1 1 5AE58D1D
P 1850 3700
F 0 "U2" H 1600 4050 50  0000 C CNN
F 1 "ST485EBDR" H 1950 4050 50  0000 L CNN
F 2 "Package_SO:SOIC-8_3.9x4.9mm_P1.27mm" H 1850 2800 50  0001 C CNN
F 3 "http://www.st.com/resource/en/datasheet/st485eb.pdf" H 1850 3750 50  0001 C CNN
	1    1850 3700
	-1   0    0    -1  
$EndComp
$Comp
L TrackAmplifier-rescue:C C4
U 1 1 5AE58D24
P 1000 3750
F 0 "C4" H 1025 3850 50  0000 L CNN
F 1 "100n" H 1025 3650 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 1038 3600 50  0001 C CNN
F 3 "" H 1000 3750 50  0001 C CNN
	1    1000 3750
	-1   0    0    1   
$EndComp
$Comp
L TrackAmplifier-rescue:C C2
U 1 1 5AE58D2B
P 750 3750
F 0 "C2" H 775 3850 50  0000 L CNN
F 1 "22u" H 775 3650 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 788 3600 50  0001 C CNN
F 3 "" H 750 3750 50  0001 C CNN
	1    750  3750
	-1   0    0    1   
$EndComp
$Comp
L power:+5V #PWR03
U 1 1 5AE58D32
P 750 3150
F 0 "#PWR03" H 750 3000 50  0001 C CNN
F 1 "+5V" H 750 3290 50  0000 C CNN
F 2 "" H 750 3150 50  0001 C CNN
F 3 "" H 750 3150 50  0001 C CNN
	1    750  3150
	1    0    0    -1  
$EndComp
$Comp
L TrackAmplifier-rescue:GND #PWR04
U 1 1 5AE58D38
P 750 4000
F 0 "#PWR04" H 750 3750 50  0001 C CNN
F 1 "GND" H 750 3850 50  0000 C CNN
F 2 "" H 750 4000 50  0001 C CNN
F 3 "" H 750 4000 50  0001 C CNN
	1    750  4000
	-1   0    0    -1  
$EndComp
$Comp
L Device:R R2
U 1 1 5AE58D3E
P 750 3350
F 0 "R2" V 830 3350 50  0000 C CNN
F 1 "10E" V 750 3350 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 680 3350 50  0001 C CNN
F 3 "~" H 750 3350 50  0001 C CNN
	1    750  3350
	-1   0    0    1   
$EndComp
Wire Wire Line
	750  3150 750  3200
Wire Wire Line
	750  3500 750  3550
Wire Wire Line
	750  3900 750  3950
Wire Wire Line
	750  3550 1000 3550
Wire Wire Line
	1000 3550 1000 3600
Connection ~ 750  3550
Wire Wire Line
	750  3550 750  3600
Wire Wire Line
	750  3950 1000 3950
Wire Wire Line
	1000 3950 1000 3900
Connection ~ 750  3950
Wire Wire Line
	750  3950 750  4000
Wire Wire Line
	1850 3300 1850 3250
Wire Wire Line
	1850 3250 1000 3250
Wire Wire Line
	1000 3250 1000 3550
Connection ~ 1000 3550
Wire Wire Line
	1850 4200 1850 4250
Wire Wire Line
	1850 4250 1000 4250
Connection ~ 1000 3950
Text GLabel 1400 3600 0    50   Input ~ 0
Sync_B
Text GLabel 1400 3900 0    50   Input ~ 0
Sync_A
Wire Wire Line
	1400 3600 1450 3600
Wire Wire Line
	1400 3900 1450 3900
Wire Wire Line
	1850 4250 2350 4250
Wire Wire Line
	2350 4250 2350 3900
Wire Wire Line
	2350 3800 2250 3800
Connection ~ 1850 4250
Wire Wire Line
	2250 3700 2350 3700
Wire Wire Line
	2350 3700 2350 3800
Connection ~ 2350 3800
Wire Wire Line
	2250 3900 2350 3900
Connection ~ 2350 3900
Wire Wire Line
	2350 3900 2350 3800
$Comp
L Interface_UART:ST485EBDR U6
U 1 1 5AE5B3FC
P 4050 5000
F 0 "U6" H 3800 5350 50  0000 C CNN
F 1 "ST485EBDR" H 4150 5350 50  0000 L CNN
F 2 "Package_SO:SOIC-8_3.9x4.9mm_P1.27mm" H 4050 4100 50  0001 C CNN
F 3 "http://www.st.com/resource/en/datasheet/st485eb.pdf" H 4050 5050 50  0001 C CNN
	1    4050 5000
	-1   0    0    -1  
$EndComp
$Comp
L TrackAmplifier-rescue:C C15
U 1 1 5AE5B403
P 3200 5050
F 0 "C15" H 3225 5150 50  0000 L CNN
F 1 "100n" H 3225 4950 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 3238 4900 50  0001 C CNN
F 3 "" H 3200 5050 50  0001 C CNN
	1    3200 5050
	-1   0    0    1   
$EndComp
$Comp
L TrackAmplifier-rescue:C C12
U 1 1 5AE5B40A
P 2950 5050
F 0 "C12" H 2975 5150 50  0000 L CNN
F 1 "22u" H 2975 4950 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 2988 4900 50  0001 C CNN
F 3 "" H 2950 5050 50  0001 C CNN
	1    2950 5050
	-1   0    0    1   
$EndComp
$Comp
L power:+5V #PWR015
U 1 1 5AE5B411
P 2950 4450
F 0 "#PWR015" H 2950 4300 50  0001 C CNN
F 1 "+5V" H 2950 4590 50  0000 C CNN
F 2 "" H 2950 4450 50  0001 C CNN
F 3 "" H 2950 4450 50  0001 C CNN
	1    2950 4450
	1    0    0    -1  
$EndComp
$Comp
L TrackAmplifier-rescue:GND #PWR016
U 1 1 5AE5B417
P 2950 5300
F 0 "#PWR016" H 2950 5050 50  0001 C CNN
F 1 "GND" H 2950 5150 50  0000 C CNN
F 2 "" H 2950 5300 50  0001 C CNN
F 3 "" H 2950 5300 50  0001 C CNN
	1    2950 5300
	-1   0    0    -1  
$EndComp
$Comp
L Device:R R10
U 1 1 5AE5B41D
P 2950 4650
F 0 "R10" V 3030 4650 50  0000 C CNN
F 1 "10E" V 2950 4650 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 2880 4650 50  0001 C CNN
F 3 "~" H 2950 4650 50  0001 C CNN
	1    2950 4650
	-1   0    0    1   
$EndComp
Wire Wire Line
	2950 4450 2950 4500
Wire Wire Line
	2950 4800 2950 4850
Wire Wire Line
	2950 5200 2950 5250
Wire Wire Line
	2950 4850 3200 4850
Wire Wire Line
	3200 4850 3200 4900
Connection ~ 2950 4850
Wire Wire Line
	2950 4850 2950 4900
Wire Wire Line
	2950 5250 3200 5250
Wire Wire Line
	3200 5250 3200 5200
Connection ~ 2950 5250
Wire Wire Line
	2950 5250 2950 5300
Wire Wire Line
	4050 4600 4050 4550
Wire Wire Line
	4050 4550 3200 4550
Wire Wire Line
	3200 4550 3200 4850
Connection ~ 3200 4850
Wire Wire Line
	4050 5500 4050 5550
Wire Wire Line
	4050 5550 3200 5550
Wire Wire Line
	3200 5550 3200 5250
Connection ~ 3200 5250
Text GLabel 3600 4900 0    50   Input ~ 0
RX_B
Text GLabel 3600 5200 0    50   Input ~ 0
RX_A
Wire Wire Line
	3600 4900 3650 4900
Wire Wire Line
	3600 5200 3650 5200
Wire Wire Line
	4050 5550 4550 5550
Wire Wire Line
	4550 5550 4550 5200
Wire Wire Line
	4550 5100 4450 5100
Connection ~ 4050 5550
Wire Wire Line
	4450 5000 4550 5000
Wire Wire Line
	4550 5000 4550 5100
Connection ~ 4550 5100
Wire Wire Line
	4450 5200 4550 5200
Connection ~ 4550 5200
Wire Wire Line
	4550 5200 4550 5100
$Comp
L Interface_UART:ST485EBDR U5
U 1 1 5AE5F184
P 4050 3650
F 0 "U5" H 3800 4000 50  0000 C CNN
F 1 "ST485EBDR" H 4150 4000 50  0000 L CNN
F 2 "Package_SO:SOIC-8_3.9x4.9mm_P1.27mm" H 4050 2750 50  0001 C CNN
F 3 "http://www.st.com/resource/en/datasheet/st485eb.pdf" H 4050 3700 50  0001 C CNN
	1    4050 3650
	-1   0    0    -1  
$EndComp
$Comp
L TrackAmplifier-rescue:C C14
U 1 1 5AE5F18B
P 3200 3700
F 0 "C14" H 3225 3800 50  0000 L CNN
F 1 "100n" H 3225 3600 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 3238 3550 50  0001 C CNN
F 3 "" H 3200 3700 50  0001 C CNN
	1    3200 3700
	-1   0    0    1   
$EndComp
$Comp
L TrackAmplifier-rescue:C C11
U 1 1 5AE5F192
P 2950 3700
F 0 "C11" H 2975 3800 50  0000 L CNN
F 1 "22u" H 2975 3600 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 2988 3550 50  0001 C CNN
F 3 "" H 2950 3700 50  0001 C CNN
	1    2950 3700
	-1   0    0    1   
$EndComp
$Comp
L power:+5V #PWR013
U 1 1 5AE5F199
P 2950 3100
F 0 "#PWR013" H 2950 2950 50  0001 C CNN
F 1 "+5V" H 2950 3240 50  0000 C CNN
F 2 "" H 2950 3100 50  0001 C CNN
F 3 "" H 2950 3100 50  0001 C CNN
	1    2950 3100
	1    0    0    -1  
$EndComp
$Comp
L TrackAmplifier-rescue:GND #PWR014
U 1 1 5AE5F19F
P 2950 3950
F 0 "#PWR014" H 2950 3700 50  0001 C CNN
F 1 "GND" H 2950 3800 50  0000 C CNN
F 2 "" H 2950 3950 50  0001 C CNN
F 3 "" H 2950 3950 50  0001 C CNN
	1    2950 3950
	-1   0    0    -1  
$EndComp
$Comp
L Device:R R9
U 1 1 5AE5F1A5
P 2950 3300
F 0 "R9" V 3030 3300 50  0000 C CNN
F 1 "10E" V 2950 3300 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 2880 3300 50  0001 C CNN
F 3 "~" H 2950 3300 50  0001 C CNN
	1    2950 3300
	-1   0    0    1   
$EndComp
Wire Wire Line
	2950 3100 2950 3150
Wire Wire Line
	2950 3450 2950 3500
Wire Wire Line
	2950 3850 2950 3900
Wire Wire Line
	2950 3500 3200 3500
Wire Wire Line
	3200 3500 3200 3550
Connection ~ 2950 3500
Wire Wire Line
	2950 3500 2950 3550
Wire Wire Line
	2950 3900 3200 3900
Wire Wire Line
	3200 3900 3200 3850
Connection ~ 2950 3900
Wire Wire Line
	2950 3900 2950 3950
Wire Wire Line
	4050 3200 3200 3200
Wire Wire Line
	3200 3200 3200 3500
Connection ~ 3200 3500
Wire Wire Line
	4050 4150 4050 4200
Wire Wire Line
	4050 4200 3200 4200
Wire Wire Line
	3200 4200 3200 3900
Connection ~ 3200 3900
Text GLabel 3600 3550 0    50   Output ~ 0
TX_B
Text GLabel 3600 3850 0    50   Output ~ 0
TX_A
Wire Wire Line
	3600 3550 3650 3550
Wire Wire Line
	3600 3850 3650 3850
Text GLabel 9800 4950 0    50   Output ~ 0
Reset_B
Text GLabel 9800 4850 0    50   Output ~ 0
Reset_A
Text GLabel 9800 5150 0    50   Output ~ 0
Sync_B
Text GLabel 9800 5050 0    50   Output ~ 0
Sync_A
Text GLabel 9800 5350 0    50   Output ~ 0
RX_B
Text GLabel 9800 5250 0    50   Output ~ 0
RX_A
Text GLabel 9800 5450 0    50   Input ~ 0
TX_B
Text GLabel 9800 5550 0    50   Input ~ 0
TX_A
$Comp
L Driver_Motor:LMD18200 U4
U 1 1 5AE92F98
P 2600 6800
F 0 "U4" H 2800 7400 50  0000 R CNN
F 1 "LMD18200" H 3100 7500 50  0000 R CNN
F 2 "Package_TO_SOT_THT:TO-220-11_Horizontal_TabDown_StaggeredType1_Py2.54mm" H 2600 5900 50  0001 C CNN
F 3 "http://www.ti.com/lit/ds/symlink/lmd18200.pdf" H 2500 6800 50  0001 C CNN
	1    2600 6800
	1    0    0    -1  
$EndComp
Text GLabel 6000 6950 2    50   Output ~ 0
PWMO1
Text GLabel 6000 7150 2    50   Output ~ 0
PWMO2
Text GLabel 9800 5650 0    50   Input ~ 0
PWMO1
Text GLabel 9800 5750 0    50   Input ~ 0
PWMO2
Wire Wire Line
	9800 5750 10100 5750
Wire Wire Line
	4050 3200 4050 3250
Wire Wire Line
	4050 3200 4550 3200
Wire Wire Line
	4550 3200 4550 3650
Wire Wire Line
	4550 3650 4450 3650
Connection ~ 4050 3200
$Comp
L MCU_Microchip_PIC16:PIC16F18854-SO U9
U 1 1 5AEBDEF2
P 7850 2350
F 0 "U9" H 7500 3100 50  0000 C CNN
F 1 "PIC16F18854-SO" H 8250 3100 50  0000 C CNN
F 2 "Package_SO:SOIC-28W_7.5x17.9mm_P1.27mm" H 7850 1250 50  0001 C CNN
F 3 "http://ww1.microchip.com/downloads/en/DeviceDoc/PIC16(L)F18854%20Data%20Sheet_DS40001826C.pdf" H 8150 1750 50  0001 C CNN
	1    7850 2350
	1    0    0    -1  
$EndComp
$Comp
L TrackAmplifier-rescue:GND #PWR034
U 1 1 5AEBDFF7
P 7850 3300
F 0 "#PWR034" H 7850 3050 50  0001 C CNN
F 1 "GND" H 7850 3150 50  0000 C CNN
F 2 "" H 7850 3300 50  0001 C CNN
F 3 "" H 7850 3300 50  0001 C CNN
	1    7850 3300
	-1   0    0    -1  
$EndComp
$Comp
L TrackAmplifier-rescue:C C27
U 1 1 5AEC3F8E
P 7450 1150
F 0 "C27" H 7475 1250 50  0000 L CNN
F 1 "100n" H 7475 1050 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 7488 1000 50  0001 C CNN
F 3 "" H 7450 1150 50  0001 C CNN
	1    7450 1150
	-1   0    0    1   
$EndComp
$Comp
L TrackAmplifier-rescue:C C26
U 1 1 5AEC3F95
P 7200 1150
F 0 "C26" H 7225 1250 50  0000 L CNN
F 1 "22u" H 7225 1050 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 7238 1000 50  0001 C CNN
F 3 "" H 7200 1150 50  0001 C CNN
	1    7200 1150
	-1   0    0    1   
$EndComp
$Comp
L power:+5V #PWR029
U 1 1 5AEC3F9C
P 6800 900
F 0 "#PWR029" H 6800 750 50  0001 C CNN
F 1 "+5V" H 6800 1040 50  0000 C CNN
F 2 "" H 6800 900 50  0001 C CNN
F 3 "" H 6800 900 50  0001 C CNN
	1    6800 900 
	1    0    0    -1  
$EndComp
$Comp
L TrackAmplifier-rescue:GND #PWR031
U 1 1 5AEC3FA2
P 7200 1400
F 0 "#PWR031" H 7200 1150 50  0001 C CNN
F 1 "GND" H 7200 1250 50  0000 C CNN
F 2 "" H 7200 1400 50  0001 C CNN
F 3 "" H 7200 1400 50  0001 C CNN
	1    7200 1400
	-1   0    0    -1  
$EndComp
$Comp
L Device:R R22
U 1 1 5AEC3FA8
P 7000 950
F 0 "R22" V 7080 950 50  0000 C CNN
F 1 "10E" V 7000 950 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 6930 950 50  0001 C CNN
F 3 "~" H 7000 950 50  0001 C CNN
	1    7000 950 
	0    -1   -1   0   
$EndComp
Wire Wire Line
	7200 1300 7200 1350
Wire Wire Line
	7200 950  7450 950 
Wire Wire Line
	7450 950  7450 1000
Wire Wire Line
	7200 950  7200 1000
Wire Wire Line
	7200 1350 7450 1350
Wire Wire Line
	7450 1350 7450 1300
Connection ~ 7200 1350
Wire Wire Line
	7200 1350 7200 1400
Wire Wire Line
	6800 900  6800 950 
Wire Wire Line
	6800 950  6850 950 
Wire Wire Line
	7150 950  7200 950 
Connection ~ 7200 950 
Wire Wire Line
	7450 950  7850 950 
Wire Wire Line
	7850 950  7850 1550
Connection ~ 7450 950 
Text GLabel 4550 4900 2    50   Output ~ 0
RX
Wire Wire Line
	4550 4900 4450 4900
Text GLabel 4550 3850 2    50   Input ~ 0
TX
Wire Wire Line
	4550 3850 4450 3850
Text GLabel 4950 3750 2    50   Input ~ 0
TX_ENA
Text GLabel 7050 2950 0    50   Input ~ 0
RX
Text GLabel 7050 2750 0    50   Output ~ 0
TX
Text GLabel 7050 2850 0    50   Output ~ 0
TX_ENA
Text GLabel 2350 3600 2    50   Output ~ 0
SYNC
Text GLabel 3100 2000 2    50   Input ~ 0
RST
Wire Wire Line
	2250 3600 2350 3600
$Comp
L Connector_Generic:Conn_01x06 J2
U 1 1 5AF2B4EA
P 10800 2300
F 0 "J2" H 10800 2600 50  0000 C CNN
F 1 "Conn_01x06" H 10800 1900 50  0000 C CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_1x06_P2.54mm_Horizontal" H 10800 2300 50  0001 C CNN
F 3 "~" H 10800 2300 50  0001 C CNN
	1    10800 2300
	1    0    0    -1  
$EndComp
Text GLabel 10500 2100 0    50   Output ~ 0
VPP
$Comp
L power:+5V #PWR044
U 1 1 5AF2B95F
P 10000 2150
F 0 "#PWR044" H 10000 2000 50  0001 C CNN
F 1 "+5V" H 10000 2290 50  0000 C CNN
F 2 "" H 10000 2150 50  0001 C CNN
F 3 "" H 10000 2150 50  0001 C CNN
	1    10000 2150
	1    0    0    -1  
$EndComp
$Comp
L TrackAmplifier-rescue:GND #PWR045
U 1 1 5AF2B9A6
P 10000 2350
F 0 "#PWR045" H 10000 2100 50  0001 C CNN
F 1 "GND" H 10000 2200 50  0000 C CNN
F 2 "" H 10000 2350 50  0001 C CNN
F 3 "" H 10000 2350 50  0001 C CNN
	1    10000 2350
	-1   0    0    -1  
$EndComp
Wire Wire Line
	10000 2150 10000 2200
Wire Wire Line
	10000 2200 10600 2200
Wire Wire Line
	10600 2100 10500 2100
Wire Wire Line
	10000 2350 10000 2300
Wire Wire Line
	10000 2300 10600 2300
Text GLabel 10500 2400 0    50   Output ~ 0
ICSPDAT
Text GLabel 10500 2500 0    50   Output ~ 0
ICSPCLK
Wire Wire Line
	10600 2400 10500 2400
Wire Wire Line
	10600 2500 10500 2500
Text GLabel 8550 1750 2    50   Input ~ 0
ICSPDAT
Text GLabel 8550 1850 2    50   Input ~ 0
ICSPCLK
Wire Wire Line
	8350 1750 8550 1750
Wire Wire Line
	8550 1850 8350 1850
Text GLabel 10600 3350 2    50   Input ~ 0
VPP
Text GLabel 10600 3650 2    50   Output ~ 0
RST
$Comp
L TrackAmplifier-rescue:C C9
U 1 1 5AF826A4
P 2350 5900
F 0 "C9" H 2375 6000 50  0000 L CNN
F 1 "100n" H 2375 5800 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 2388 5750 50  0001 C CNN
F 3 "" H 2350 5900 50  0001 C CNN
	1    2350 5900
	-1   0    0    1   
$EndComp
$Comp
L TrackAmplifier-rescue:C C8
U 1 1 5AF82746
P 2100 5900
F 0 "C8" H 2125 6000 50  0000 L CNN
F 1 "22u" H 2125 5800 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 2138 5750 50  0001 C CNN
F 3 "" H 2100 5900 50  0001 C CNN
	1    2100 5900
	-1   0    0    1   
$EndComp
$Comp
L Device:CP C6
U 1 1 5AF827EA
P 1850 5900
F 0 "C6" H 1900 5800 50  0000 L CNN
F 1 "22u" H 1600 5950 50  0000 L CNN
F 2 "Capacitor_THT:CP_Axial_L21.0mm_D8.0mm_P28.00mm_Horizontal" H 1888 5750 50  0001 C CNN
F 3 "~" H 1850 5900 50  0001 C CNN
	1    1850 5900
	1    0    0    -1  
$EndComp
Wire Wire Line
	2100 5750 2100 5700
Wire Wire Line
	2100 5700 1850 5700
Wire Wire Line
	1850 5700 1850 5750
Wire Wire Line
	2350 5750 2350 5700
Wire Wire Line
	2350 5700 2100 5700
Connection ~ 2100 5700
Wire Wire Line
	1850 6050 1850 6100
Wire Wire Line
	1850 6100 2100 6100
Wire Wire Line
	2100 6100 2100 6050
Wire Wire Line
	2100 6100 2350 6100
Wire Wire Line
	2350 6100 2350 6050
Connection ~ 2100 6100
$Comp
L TrackAmplifier-rescue:GND #PWR011
U 1 1 5AFD78B7
P 2600 7500
F 0 "#PWR011" H 2600 7250 50  0001 C CNN
F 1 "GND" H 2600 7350 50  0000 C CNN
F 2 "" H 2600 7500 50  0001 C CNN
F 3 "" H 2600 7500 50  0001 C CNN
	1    2600 7500
	-1   0    0    -1  
$EndComp
Wire Wire Line
	2350 5700 2600 5700
Wire Wire Line
	2600 5700 2600 6200
Connection ~ 2350 5700
$Comp
L TrackAmplifier-rescue:C C17
U 1 1 5B006269
P 4100 6800
F 0 "C17" H 4125 6900 50  0000 L CNN
F 1 "10n" H 4125 6700 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 4138 6650 50  0001 C CNN
F 3 "" H 4100 6800 50  0001 C CNN
	1    4100 6800
	-1   0    0    1   
$EndComp
$Comp
L TrackAmplifier-rescue:C C18
U 1 1 5B0062DB
P 4100 7200
F 0 "C18" H 4125 7300 50  0000 L CNN
F 1 "10n" H 4125 7100 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 4138 7050 50  0001 C CNN
F 3 "" H 4100 7200 50  0001 C CNN
	1    4100 7200
	-1   0    0    1   
$EndComp
Wire Wire Line
	4100 6950 3750 6950
Wire Wire Line
	3750 6900 3750 6950
Wire Wire Line
	4100 7050 3750 7050
Wire Wire Line
	3750 7050 3750 7100
Wire Wire Line
	3750 7400 4100 7400
Wire Wire Line
	4100 7400 4100 7350
Wire Wire Line
	3750 7200 3750 7400
Wire Wire Line
	3750 6600 3750 6800
Wire Wire Line
	4100 6650 4100 6600
Wire Wire Line
	4100 6600 3750 6600
Wire Wire Line
	7850 3150 7850 3300
Text GLabel 1600 6600 0    50   Input ~ 0
PWM
Text GLabel 1550 6800 0    50   Input ~ 0
DIR
Text GLabel 1100 7000 0    50   Input ~ 0
BRK
Wire Wire Line
	1600 6600 1700 6600
Wire Wire Line
	2000 6800 1900 6800
Wire Wire Line
	2000 7000 1250 7000
Text GLabel 8500 2650 2    50   Input ~ 0
TEMP
Text GLabel 8500 2750 2    50   Output ~ 0
PWM
Text GLabel 8500 2950 2    50   Output ~ 0
DIR
Text GLabel 8500 2850 2    50   Output ~ 0
BRK
Text GLabel 7050 2650 0    50   Input ~ 0
SYNC
$Comp
L Device:R R6
U 1 1 5B19F40D
P 1700 7200
F 0 "R6" V 1780 7200 50  0000 C CNN
F 1 "4K7" V 1700 7200 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 1630 7200 50  0001 C CNN
F 3 "~" H 1700 7200 50  0001 C CNN
	1    1700 7200
	-1   0    0    1   
$EndComp
Wire Wire Line
	1700 7050 1700 6600
Connection ~ 1700 6600
Wire Wire Line
	1700 6600 2000 6600
$Comp
L TrackAmplifier-rescue:GND #PWR08
U 1 1 5B1CE88A
P 1700 7450
F 0 "#PWR08" H 1700 7200 50  0001 C CNN
F 1 "GND" H 1700 7300 50  0000 C CNN
F 2 "" H 1700 7450 50  0001 C CNN
F 3 "" H 1700 7450 50  0001 C CNN
	1    1700 7450
	-1   0    0    -1  
$EndComp
Wire Wire Line
	1700 7350 1700 7400
$Comp
L Device:R R4
U 1 1 5B1DAA4F
P 1250 6700
F 0 "R4" V 1330 6700 50  0000 C CNN
F 1 "4K7" V 1250 6700 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 1180 6700 50  0001 C CNN
F 3 "~" H 1250 6700 50  0001 C CNN
	1    1250 6700
	-1   0    0    1   
$EndComp
$Comp
L power:+5V #PWR06
U 1 1 5B1DAAB9
P 1250 6450
F 0 "#PWR06" H 1250 6300 50  0001 C CNN
F 1 "+5V" H 1250 6590 50  0000 C CNN
F 2 "" H 1250 6450 50  0001 C CNN
F 3 "" H 1250 6450 50  0001 C CNN
	1    1250 6450
	1    0    0    -1  
$EndComp
Wire Wire Line
	1250 6450 1250 6550
Wire Wire Line
	1250 6850 1250 7000
Connection ~ 1250 7000
Wire Wire Line
	1250 7000 1100 7000
$Comp
L Device:R R7
U 1 1 5B2635CD
P 1900 7200
F 0 "R7" V 1980 7200 50  0000 C CNN
F 1 "4K7" V 1900 7200 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 1830 7200 50  0001 C CNN
F 3 "~" H 1900 7200 50  0001 C CNN
	1    1900 7200
	-1   0    0    1   
$EndComp
Wire Wire Line
	1900 6800 1900 7050
Connection ~ 1900 6800
Wire Wire Line
	1900 6800 1550 6800
Wire Wire Line
	1700 7400 1900 7400
Wire Wire Line
	1900 7400 1900 7350
Connection ~ 1700 7400
Wire Wire Line
	1700 7400 1700 7450
Text Notes 700  7450 0    50   ~ 0
Default Drivers Open\nBRAKE = H\nPWM = L\nDIR = X
$Comp
L Device:R R14
U 1 1 5B2BF8D7
P 4850 3950
F 0 "R14" V 4930 3950 50  0000 C CNN
F 1 "4K7" V 4850 3950 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 4780 3950 50  0001 C CNN
F 3 "~" H 4850 3950 50  0001 C CNN
	1    4850 3950
	-1   0    0    1   
$EndComp
$Comp
L TrackAmplifier-rescue:GND #PWR022
U 1 1 5B2BF955
P 4850 4150
F 0 "#PWR022" H 4850 3900 50  0001 C CNN
F 1 "GND" H 4850 4000 50  0000 C CNN
F 2 "" H 4850 4150 50  0001 C CNN
F 3 "" H 4850 4150 50  0001 C CNN
	1    4850 4150
	-1   0    0    -1  
$EndComp
Wire Wire Line
	4850 3800 4850 3750
Connection ~ 4850 3750
Wire Wire Line
	4850 3750 4450 3750
Wire Wire Line
	4850 4100 4850 4150
$Comp
L Device:R R32
U 1 1 5B2F5C72
P 9850 3300
F 0 "R32" V 9930 3300 50  0000 C CNN
F 1 "10K" V 9850 3300 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 9780 3300 50  0001 C CNN
F 3 "~" H 9850 3300 50  0001 C CNN
	1    9850 3300
	1    0    0    -1  
$EndComp
$Comp
L power:+5V #PWR041
U 1 1 5B2F5CDC
P 9850 3100
F 0 "#PWR041" H 9850 2950 50  0001 C CNN
F 1 "+5V" H 9850 3240 50  0000 C CNN
F 2 "" H 9850 3100 50  0001 C CNN
F 3 "" H 9850 3100 50  0001 C CNN
	1    9850 3100
	1    0    0    -1  
$EndComp
$Comp
L TrackAmplifier-rescue:C C31
U 1 1 5B3110C1
P 9850 3700
F 0 "C31" H 9875 3800 50  0000 L CNN
F 1 "100n" H 9875 3600 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 9888 3550 50  0001 C CNN
F 3 "" H 9850 3700 50  0001 C CNN
	1    9850 3700
	-1   0    0    1   
$EndComp
$Comp
L TrackAmplifier-rescue:GND #PWR042
U 1 1 5B31EDF2
P 9850 3950
F 0 "#PWR042" H 9850 3700 50  0001 C CNN
F 1 "GND" H 9850 3800 50  0000 C CNN
F 2 "" H 9850 3950 50  0001 C CNN
F 3 "" H 9850 3950 50  0001 C CNN
	1    9850 3950
	-1   0    0    -1  
$EndComp
$Comp
L Transistor_FET:2N7002 Q1
U 1 1 5B357979
P 2900 2300
F 0 "Q1" H 3100 2375 50  0000 L CNN
F 1 "2N7002" H 3100 2300 50  0000 L CNN
F 2 "Package_TO_SOT_SMD:SOT-23" H 3100 2225 50  0001 L CIN
F 3 "https://www.fairchildsemi.com/datasheets/2N/2N7002.pdf" H 2900 2300 50  0001 L CNN
	1    2900 2300
	1    0    0    -1  
$EndComp
$Comp
L Device:R R8
U 1 1 5B357D23
P 2450 2300
F 0 "R8" V 2530 2300 50  0000 C CNN
F 1 "100E" V 2450 2300 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 2380 2300 50  0001 C CNN
F 3 "~" H 2450 2300 50  0001 C CNN
	1    2450 2300
	0    -1   -1   0   
$EndComp
Wire Wire Line
	2250 2300 2300 2300
Wire Wire Line
	2600 2300 2650 2300
$Comp
L TrackAmplifier-rescue:GND #PWR012
U 1 1 5B3BB8BC
P 3000 2600
F 0 "#PWR012" H 3000 2350 50  0001 C CNN
F 1 "GND" H 3000 2450 50  0000 C CNN
F 2 "" H 3000 2600 50  0001 C CNN
F 3 "" H 3000 2600 50  0001 C CNN
	1    3000 2600
	-1   0    0    -1  
$EndComp
Wire Wire Line
	3000 2600 3000 2500
Wire Wire Line
	3100 2000 3000 2000
Wire Wire Line
	3000 2000 3000 2100
$Comp
L Device:D D7
U 1 1 5B3F6022
P 10100 3500
F 0 "D7" H 10100 3600 50  0000 C CNN
F 1 "1n4148" H 10100 3400 50  0000 C CNN
F 2 "Diode_SMD:D_1206_3216Metric" H 10100 3500 50  0001 C CNN
F 3 "~" H 10100 3500 50  0001 C CNN
	1    10100 3500
	-1   0    0    1   
$EndComp
Text GLabel 10600 3500 2    50   Output ~ 0
~MCLR
Text GLabel 6800 2550 0    50   Input ~ 0
~MCLR
Wire Wire Line
	6800 2550 7350 2550
Wire Wire Line
	10600 3350 10450 3350
Wire Wire Line
	10450 3350 10450 3500
Wire Wire Line
	10450 3500 10600 3500
Text GLabel 9800 4750 0    50   Input ~ 0
ID_R
Text GLabel 9900 1400 3    50   Output ~ 0
ID_R
Wire Wire Line
	9850 3850 9850 3900
Wire Wire Line
	9850 3100 9850 3150
Wire Wire Line
	9850 3450 9850 3500
Wire Wire Line
	10600 3650 10450 3650
Wire Wire Line
	10450 3650 10450 3500
Connection ~ 10450 3500
Wire Wire Line
	10250 3500 10450 3500
Wire Wire Line
	9850 3500 9950 3500
Connection ~ 9850 3500
Wire Wire Line
	9850 3500 9850 3550
Text Notes 10050 4050 0    50   ~ 0
~MCLR~ Vil = 0.2V Vih = 0.7V\nLVP is possible, HVP only\nwhen "RESET" transceiver \ninput A < B 
Text Notes 2150 1900 0    50   ~ 0
Disconnected receiver inputs\nor idle bus will have RO high.\nInvert for uController reset
Wire Wire Line
	9800 5650 10100 5650
Wire Wire Line
	9800 4750 10100 4750
Wire Wire Line
	10100 5450 9800 5450
Wire Wire Line
	9800 5550 10100 5550
$Comp
L power:GNDD #PWR046
U 1 1 5B57F88C
P 10700 4450
F 0 "#PWR046" H 10700 4200 50  0001 C CNN
F 1 "GNDD" H 10700 4325 50  0000 C CNN
F 2 "" H 10700 4450 50  0001 C CNN
F 3 "" H 10700 4450 50  0001 C CNN
	1    10700 4450
	1    0    0    -1  
$EndComp
Wire Wire Line
	10400 4350 10400 4250
Wire Wire Line
	10400 4250 10700 4250
Wire Wire Line
	10700 4250 10700 4450
$Comp
L Amplifier_Instrumentation:INA326 U7
U 1 1 5B58EFF0
P 4950 1750
F 0 "U7" H 5100 1875 50  0000 L CNN
F 1 "INA326" H 5100 1625 50  0000 L CNN
F 2 "Package_SO:MSOP-8_3x3mm_P0.65mm" H 4950 1750 50  0001 L CNN
F 3 "http://www.ti.com/lit/ds/symlink/ina326.pdf" H 5050 1750 50  0001 C CNN
	1    4950 1750
	1    0    0    -1  
$EndComp
$Comp
L Device:EMI_Filter_CommonMode FL1
U 1 1 5B58F25D
P 5700 7050
F 0 "FL1" H 5700 7225 50  0000 C CNN
F 1 "EMI_Filter_CMode" H 5800 7350 50  0000 C CNN
F 2 "Inductor_THT:Choke_EPCOS_B82722-A2302-N1-22.3x22.7mm" V 5700 7090 50  0001 C CNN
F 3 "~" V 5700 7090 50  0000 C CNN
	1    5700 7050
	1    0    0    -1  
$EndComp
Wire Wire Line
	5900 6950 6000 6950
Wire Wire Line
	6000 7150 5900 7150
Wire Wire Line
	4100 6600 4300 6600
Wire Wire Line
	4300 6600 4300 6950
Connection ~ 4100 6600
Wire Wire Line
	4100 7400 4300 7400
Wire Wire Line
	4300 7400 4300 7150
Connection ~ 4100 7400
Wire Wire Line
	3200 6800 3750 6800
Wire Wire Line
	3200 6900 3750 6900
Wire Wire Line
	3200 7100 3750 7100
Wire Wire Line
	3200 7200 3750 7200
Text GLabel 3450 6600 2    50   Output ~ 0
CSEN
Wire Wire Line
	3450 6600 3200 6600
$Comp
L power:+5V #PWR043
U 1 1 5B63D46A
P 9900 750
F 0 "#PWR043" H 9900 600 50  0001 C CNN
F 1 "+5V" H 9900 890 50  0000 C CNN
F 2 "" H 9900 750 50  0001 C CNN
F 3 "" H 9900 750 50  0001 C CNN
	1    9900 750 
	1    0    0    -1  
$EndComp
$Comp
L Device:R R33
U 1 1 5B63D4D3
P 9900 950
F 0 "R33" V 9980 950 50  0000 C CNN
F 1 "1K" V 9900 950 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 9830 950 50  0001 C CNN
F 3 "~" H 9900 950 50  0001 C CNN
	1    9900 950 
	1    0    0    -1  
$EndComp
Wire Wire Line
	9900 750  9900 800 
Wire Wire Line
	9900 1100 9900 1150
$Comp
L TrackAmplifier-rescue:C C30
U 1 1 5B65E3B2
P 9700 1350
F 0 "C30" H 9725 1450 50  0000 L CNN
F 1 "22u" H 9725 1250 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 9738 1200 50  0001 C CNN
F 3 "" H 9700 1350 50  0001 C CNN
	1    9700 1350
	-1   0    0    1   
$EndComp
$Comp
L TrackAmplifier-rescue:GND #PWR040
U 1 1 5B66EA4C
P 9700 1550
F 0 "#PWR040" H 9700 1300 50  0001 C CNN
F 1 "GND" H 9700 1400 50  0000 C CNN
F 2 "" H 9700 1550 50  0001 C CNN
F 3 "" H 9700 1550 50  0001 C CNN
	1    9700 1550
	-1   0    0    -1  
$EndComp
Connection ~ 9900 1150
Text Notes 10000 1250 0    50   ~ 0
Resistor on backplane \nwill determine ID voltage
$Comp
L Device:R R30
U 1 1 5B6A0D19
P 9450 1150
F 0 "R30" V 9530 1150 50  0000 C CNN
F 1 "1K" V 9450 1150 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 9380 1150 50  0001 C CNN
F 3 "~" H 9450 1150 50  0001 C CNN
	1    9450 1150
	0    1    1    0   
$EndComp
$Comp
L Device:D D5
U 1 1 5B6B1C9C
P 9200 950
F 0 "D5" H 9200 1050 50  0000 C CNN
F 1 "1n4148" H 9200 850 50  0000 C CNN
F 2 "Diode_SMD:D_1206_3216Metric" H 9200 950 50  0001 C CNN
F 3 "~" H 9200 950 50  0001 C CNN
	1    9200 950 
	0    1    1    0   
$EndComp
$Comp
L Device:D D6
U 1 1 5B6E441C
P 9200 1350
F 0 "D6" H 9200 1450 50  0000 C CNN
F 1 "1n4148" H 9200 1250 50  0000 C CNN
F 2 "Diode_SMD:D_1206_3216Metric" H 9200 1350 50  0001 C CNN
F 3 "~" H 9200 1350 50  0001 C CNN
	1    9200 1350
	0    1    1    0   
$EndComp
$Comp
L TrackAmplifier-rescue:GND #PWR037
U 1 1 5B6E449A
P 9200 1550
F 0 "#PWR037" H 9200 1300 50  0001 C CNN
F 1 "GND" H 9200 1400 50  0000 C CNN
F 2 "" H 9200 1550 50  0001 C CNN
F 3 "" H 9200 1550 50  0001 C CNN
	1    9200 1550
	-1   0    0    -1  
$EndComp
$Comp
L power:+5V #PWR036
U 1 1 5B6E450D
P 9200 750
F 0 "#PWR036" H 9200 600 50  0001 C CNN
F 1 "+5V" H 9200 890 50  0000 C CNN
F 2 "" H 9200 750 50  0001 C CNN
F 3 "" H 9200 750 50  0001 C CNN
	1    9200 750 
	1    0    0    -1  
$EndComp
Wire Wire Line
	9200 750  9200 800 
Wire Wire Line
	9200 1100 9200 1150
Wire Wire Line
	9200 1550 9200 1500
Wire Wire Line
	9300 1150 9200 1150
Connection ~ 9200 1150
Wire Wire Line
	9200 1150 9200 1200
Text GLabel 9000 1150 0    50   Output ~ 0
ID
Wire Wire Line
	9000 1150 9200 1150
Text GLabel 7200 2150 0    50   Input ~ 0
ID
$Comp
L TrackAmplifier-rescue:C C20
U 1 1 5B7814F9
P 4750 1000
F 0 "C20" H 4775 1100 50  0000 L CNN
F 1 "100n" H 4775 900 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 4788 850 50  0001 C CNN
F 3 "" H 4750 1000 50  0001 C CNN
	1    4750 1000
	-1   0    0    1   
$EndComp
$Comp
L TrackAmplifier-rescue:C C19
U 1 1 5B781500
P 4500 1000
F 0 "C19" H 4525 1100 50  0000 L CNN
F 1 "22u" H 4525 900 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 4538 850 50  0001 C CNN
F 3 "" H 4500 1000 50  0001 C CNN
	1    4500 1000
	-1   0    0    1   
$EndComp
$Comp
L power:+5V #PWR020
U 1 1 5B781507
P 4100 750
F 0 "#PWR020" H 4100 600 50  0001 C CNN
F 1 "+5V" H 4100 890 50  0000 C CNN
F 2 "" H 4100 750 50  0001 C CNN
F 3 "" H 4100 750 50  0001 C CNN
	1    4100 750 
	1    0    0    -1  
$EndComp
$Comp
L TrackAmplifier-rescue:GND #PWR021
U 1 1 5B78150D
P 4500 1250
F 0 "#PWR021" H 4500 1000 50  0001 C CNN
F 1 "GND" H 4500 1100 50  0000 C CNN
F 2 "" H 4500 1250 50  0001 C CNN
F 3 "" H 4500 1250 50  0001 C CNN
	1    4500 1250
	-1   0    0    -1  
$EndComp
$Comp
L Device:R R12
U 1 1 5B781513
P 4300 800
F 0 "R12" V 4380 800 50  0000 C CNN
F 1 "10E" V 4300 800 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 4230 800 50  0001 C CNN
F 3 "~" H 4300 800 50  0001 C CNN
	1    4300 800 
	0    -1   -1   0   
$EndComp
Wire Wire Line
	4500 1150 4500 1200
Wire Wire Line
	4500 800  4750 800 
Wire Wire Line
	4750 800  4750 850 
Wire Wire Line
	4500 800  4500 850 
Wire Wire Line
	4500 1200 4750 1200
Wire Wire Line
	4750 1200 4750 1150
Connection ~ 4500 1200
Wire Wire Line
	4500 1200 4500 1250
Wire Wire Line
	4100 750  4100 800 
Wire Wire Line
	4100 800  4150 800 
Wire Wire Line
	4450 800  4500 800 
Connection ~ 4500 800 
Wire Wire Line
	4950 1450 4950 800 
Wire Wire Line
	4950 800  4750 800 
Connection ~ 4750 800 
$Comp
L TrackAmplifier-rescue:GND #PWR023
U 1 1 5B7A7F2A
P 4950 2100
F 0 "#PWR023" H 4950 1850 50  0001 C CNN
F 1 "GND" H 4950 1950 50  0000 C CNN
F 2 "" H 4950 2100 50  0001 C CNN
F 3 "" H 4950 2100 50  0001 C CNN
	1    4950 2100
	-1   0    0    -1  
$EndComp
Wire Wire Line
	4950 2050 4950 2100
$Comp
L Device:R R13
U 1 1 5B7BBAAE
P 4500 1750
F 0 "R13" V 4580 1750 50  0000 C CNN
F 1 "10E" V 4500 1750 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 4430 1750 50  0001 C CNN
F 3 "~" H 4500 1750 50  0001 C CNN
	1    4500 1750
	1    0    0    -1  
$EndComp
$Comp
L Device:R R15
U 1 1 5B7BBBFA
P 5050 2450
F 0 "R15" V 5130 2450 50  0000 C CNN
F 1 "10E" V 5050 2450 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 4980 2450 50  0001 C CNN
F 3 "~" H 5050 2450 50  0001 C CNN
	1    5050 2450
	1    0    0    -1  
$EndComp
Wire Wire Line
	4500 1600 4600 1600
Wire Wire Line
	4600 1600 4600 1650
Wire Wire Line
	4600 1650 4650 1650
Wire Wire Line
	4500 1900 4600 1900
Wire Wire Line
	4600 1900 4600 1850
Wire Wire Line
	4600 1850 4650 1850
Wire Wire Line
	5050 2050 5050 2250
Text GLabel 4450 1550 0    50   Input ~ 0
PWM1
Text GLabel 4450 1950 0    50   Input ~ 0
PWM2
Wire Wire Line
	4450 1550 4650 1550
Wire Wire Line
	4450 1950 4650 1950
$Comp
L Device:R R18
U 1 1 5B860263
P 5600 1750
F 0 "R18" V 5680 1750 50  0000 C CNN
F 1 "100E" V 5600 1750 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 5530 1750 50  0001 C CNN
F 3 "~" H 5600 1750 50  0001 C CNN
	1    5600 1750
	0    1    1    0   
$EndComp
$Comp
L TrackAmplifier-rescue:C C22
U 1 1 5B860337
P 5900 2000
F 0 "C22" H 5925 2100 50  0000 L CNN
F 1 "1u" H 5925 1900 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 5938 1850 50  0001 C CNN
F 3 "" H 5900 2000 50  0001 C CNN
	1    5900 2000
	-1   0    0    1   
$EndComp
$Comp
L TrackAmplifier-rescue:C C21
U 1 1 5B86044C
P 5350 2450
F 0 "C21" H 5375 2550 50  0000 L CNN
F 1 "100n" H 5375 2350 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 5388 2300 50  0001 C CNN
F 3 "" H 5350 2450 50  0001 C CNN
	1    5350 2450
	-1   0    0    1   
$EndComp
Wire Wire Line
	5350 2300 5350 2250
Wire Wire Line
	5350 2250 5050 2250
Connection ~ 5050 2250
Wire Wire Line
	5050 2250 5050 2300
Wire Wire Line
	5050 2600 5050 2650
Wire Wire Line
	5050 2650 5350 2650
Wire Wire Line
	5350 2650 5350 2600
$Comp
L TrackAmplifier-rescue:GND #PWR026
U 1 1 5B88BA38
P 5900 2250
F 0 "#PWR026" H 5900 2000 50  0001 C CNN
F 1 "GND" H 5900 2100 50  0000 C CNN
F 2 "" H 5900 2250 50  0001 C CNN
F 3 "" H 5900 2250 50  0001 C CNN
	1    5900 2250
	-1   0    0    -1  
$EndComp
Wire Wire Line
	5900 2150 5900 2250
Wire Wire Line
	5350 1750 5450 1750
Wire Wire Line
	5750 1750 5900 1750
Wire Wire Line
	5900 1750 5900 1850
$Comp
L TrackAmplifier-rescue:GND #PWR024
U 1 1 5B8CE6A0
P 5350 2700
F 0 "#PWR024" H 5350 2450 50  0001 C CNN
F 1 "GND" H 5350 2550 50  0000 C CNN
F 2 "" H 5350 2700 50  0001 C CNN
F 3 "" H 5350 2700 50  0001 C CNN
	1    5350 2700
	-1   0    0    -1  
$EndComp
Wire Wire Line
	5350 2650 5350 2700
Connection ~ 5350 2650
Text GLabel 5950 1750 2    50   Output ~ 0
BMF
Wire Wire Line
	5900 1750 5950 1750
Connection ~ 5900 1750
Text GLabel 7200 1950 0    50   Input ~ 0
BMF
Text GLabel 4350 6600 2    50   Output ~ 0
PWM1
Text GLabel 4350 7400 2    50   Output ~ 0
PWM2
Connection ~ 4300 6600
Connection ~ 4300 7400
Wire Wire Line
	4300 6600 4350 6600
Wire Wire Line
	4300 7400 4350 7400
$Comp
L Device:R R17
U 1 1 5BA29CE0
P 5100 6950
F 0 "R17" V 5180 6950 50  0000 C CNN
F 1 "0.2E" V 5100 6950 50  0000 C CNN
F 2 "Resistor_THT:R_Axial_DIN0411_L9.9mm_D3.6mm_P12.70mm_Horizontal" V 5030 6950 50  0001 C CNN
F 3 "~" H 5100 6950 50  0001 C CNN
	1    5100 6950
	0    -1   -1   0   
$EndComp
Wire Wire Line
	5250 6950 5300 6950
$Comp
L Device:R R16
U 1 1 5BA58FC8
P 5100 6750
F 0 "R16" V 5180 6750 50  0000 C CNN
F 1 "0.2E" V 5100 6750 50  0000 C CNN
F 2 "Resistor_THT:R_Axial_DIN0411_L9.9mm_D3.6mm_P12.70mm_Horizontal" V 5030 6750 50  0001 C CNN
F 3 "~" H 5100 6750 50  0001 C CNN
	1    5100 6750
	0    -1   -1   0   
$EndComp
Wire Wire Line
	4950 6750 4900 6750
Wire Wire Line
	4900 6750 4900 6950
Connection ~ 4900 6950
Wire Wire Line
	4900 6950 4950 6950
Wire Wire Line
	5250 6750 5300 6750
Wire Wire Line
	5300 6750 5300 6950
Connection ~ 5300 6950
Wire Wire Line
	5300 6950 5500 6950
Text GLabel 4900 6550 1    50   Output ~ 0
OCC1
Text GLabel 5300 6550 1    50   Output ~ 0
OCC2
Wire Wire Line
	4900 6550 4900 6750
Connection ~ 4900 6750
Wire Wire Line
	5300 6550 5300 6750
Connection ~ 5300 6750
$Comp
L Amplifier_Instrumentation:INA326 U8
U 1 1 5BAED109
P 6550 4200
F 0 "U8" H 6700 4325 50  0000 L CNN
F 1 "INA326" H 6700 4075 50  0000 L CNN
F 2 "Package_SO:MSOP-8_3x3mm_P0.65mm" H 6550 4200 50  0001 L CNN
F 3 "http://www.ti.com/lit/ds/symlink/ina326.pdf" H 6650 4200 50  0001 C CNN
	1    6550 4200
	1    0    0    -1  
$EndComp
$Comp
L TrackAmplifier-rescue:C C24
U 1 1 5BAED110
P 6350 3450
F 0 "C24" H 6375 3550 50  0000 L CNN
F 1 "100n" H 6375 3350 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 6388 3300 50  0001 C CNN
F 3 "" H 6350 3450 50  0001 C CNN
	1    6350 3450
	-1   0    0    1   
$EndComp
$Comp
L TrackAmplifier-rescue:C C23
U 1 1 5BAED117
P 6100 3450
F 0 "C23" H 6125 3550 50  0000 L CNN
F 1 "22u" H 6125 3350 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 6138 3300 50  0001 C CNN
F 3 "" H 6100 3450 50  0001 C CNN
	1    6100 3450
	-1   0    0    1   
$EndComp
$Comp
L power:+5V #PWR025
U 1 1 5BAED11E
P 5700 3200
F 0 "#PWR025" H 5700 3050 50  0001 C CNN
F 1 "+5V" H 5700 3340 50  0000 C CNN
F 2 "" H 5700 3200 50  0001 C CNN
F 3 "" H 5700 3200 50  0001 C CNN
	1    5700 3200
	1    0    0    -1  
$EndComp
$Comp
L TrackAmplifier-rescue:GND #PWR027
U 1 1 5BAED124
P 6100 3700
F 0 "#PWR027" H 6100 3450 50  0001 C CNN
F 1 "GND" H 6100 3550 50  0000 C CNN
F 2 "" H 6100 3700 50  0001 C CNN
F 3 "" H 6100 3700 50  0001 C CNN
	1    6100 3700
	-1   0    0    -1  
$EndComp
$Comp
L Device:R R19
U 1 1 5BAED12A
P 5900 3250
F 0 "R19" V 5980 3250 50  0000 C CNN
F 1 "10E" V 5900 3250 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 5830 3250 50  0001 C CNN
F 3 "~" H 5900 3250 50  0001 C CNN
	1    5900 3250
	0    -1   -1   0   
$EndComp
Wire Wire Line
	6100 3600 6100 3650
Wire Wire Line
	6100 3250 6350 3250
Wire Wire Line
	6350 3250 6350 3300
Wire Wire Line
	6100 3250 6100 3300
Wire Wire Line
	6100 3650 6350 3650
Wire Wire Line
	6350 3650 6350 3600
Connection ~ 6100 3650
Wire Wire Line
	6100 3650 6100 3700
Wire Wire Line
	5700 3200 5700 3250
Wire Wire Line
	5700 3250 5750 3250
Wire Wire Line
	6050 3250 6100 3250
Connection ~ 6100 3250
Wire Wire Line
	6550 3900 6550 3250
Wire Wire Line
	6550 3250 6350 3250
Connection ~ 6350 3250
$Comp
L TrackAmplifier-rescue:GND #PWR028
U 1 1 5BAED140
P 6550 4550
F 0 "#PWR028" H 6550 4300 50  0001 C CNN
F 1 "GND" H 6550 4400 50  0000 C CNN
F 2 "" H 6550 4550 50  0001 C CNN
F 3 "" H 6550 4550 50  0001 C CNN
	1    6550 4550
	-1   0    0    -1  
$EndComp
Wire Wire Line
	6550 4500 6550 4550
$Comp
L Device:R R20
U 1 1 5BAED147
P 6100 4200
F 0 "R20" V 6180 4200 50  0000 C CNN
F 1 "10E" V 6100 4200 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 6030 4200 50  0001 C CNN
F 3 "~" H 6100 4200 50  0001 C CNN
	1    6100 4200
	1    0    0    -1  
$EndComp
$Comp
L Device:R R21
U 1 1 5BAED14E
P 6650 4900
F 0 "R21" V 6730 4900 50  0000 C CNN
F 1 "10E" V 6650 4900 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 6580 4900 50  0001 C CNN
F 3 "~" H 6650 4900 50  0001 C CNN
	1    6650 4900
	1    0    0    -1  
$EndComp
Wire Wire Line
	6100 4050 6200 4050
Wire Wire Line
	6200 4050 6200 4100
Wire Wire Line
	6200 4100 6250 4100
Wire Wire Line
	6100 4350 6200 4350
Wire Wire Line
	6200 4350 6200 4300
Wire Wire Line
	6200 4300 6250 4300
Wire Wire Line
	6650 4500 6650 4700
Text GLabel 6050 4000 0    50   Input ~ 0
OCC1
Text GLabel 6050 4400 0    50   Input ~ 0
OCC2
Wire Wire Line
	6050 4000 6250 4000
Wire Wire Line
	6050 4400 6250 4400
$Comp
L Device:R R23
U 1 1 5BAED160
P 7200 4200
F 0 "R23" V 7280 4200 50  0000 C CNN
F 1 "100E" V 7200 4200 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 7130 4200 50  0001 C CNN
F 3 "~" H 7200 4200 50  0001 C CNN
	1    7200 4200
	0    1    1    0   
$EndComp
$Comp
L TrackAmplifier-rescue:C C28
U 1 1 5BAED167
P 7500 4450
F 0 "C28" H 7525 4550 50  0000 L CNN
F 1 "1u" H 7525 4350 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 7538 4300 50  0001 C CNN
F 3 "" H 7500 4450 50  0001 C CNN
	1    7500 4450
	-1   0    0    1   
$EndComp
$Comp
L TrackAmplifier-rescue:C C25
U 1 1 5BAED16E
P 6950 4900
F 0 "C25" H 6975 5000 50  0000 L CNN
F 1 "100n" H 6975 4800 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 6988 4750 50  0001 C CNN
F 3 "" H 6950 4900 50  0001 C CNN
	1    6950 4900
	-1   0    0    1   
$EndComp
Wire Wire Line
	6950 4750 6950 4700
Wire Wire Line
	6950 4700 6650 4700
Connection ~ 6650 4700
Wire Wire Line
	6650 4700 6650 4750
Wire Wire Line
	6650 5050 6650 5100
Wire Wire Line
	6650 5100 6950 5100
Wire Wire Line
	6950 5100 6950 5050
$Comp
L TrackAmplifier-rescue:GND #PWR032
U 1 1 5BAED17C
P 7500 4700
F 0 "#PWR032" H 7500 4450 50  0001 C CNN
F 1 "GND" H 7500 4550 50  0000 C CNN
F 2 "" H 7500 4700 50  0001 C CNN
F 3 "" H 7500 4700 50  0001 C CNN
	1    7500 4700
	-1   0    0    -1  
$EndComp
Wire Wire Line
	7500 4600 7500 4700
Wire Wire Line
	6950 4200 7050 4200
Wire Wire Line
	7350 4200 7500 4200
Wire Wire Line
	7500 4200 7500 4300
$Comp
L TrackAmplifier-rescue:GND #PWR030
U 1 1 5BAED186
P 6950 5150
F 0 "#PWR030" H 6950 4900 50  0001 C CNN
F 1 "GND" H 6950 5000 50  0000 C CNN
F 2 "" H 6950 5150 50  0001 C CNN
F 3 "" H 6950 5150 50  0001 C CNN
	1    6950 5150
	-1   0    0    -1  
$EndComp
Wire Wire Line
	6950 5100 6950 5150
Connection ~ 6950 5100
Text GLabel 7550 4200 2    50   Output ~ 0
OCC
Wire Wire Line
	7500 4200 7550 4200
Connection ~ 7500 4200
Text GLabel 7200 2050 0    50   Input ~ 0
OCC
Wire Wire Line
	9900 1150 9900 1400
Wire Wire Line
	4300 6950 4900 6950
Wire Wire Line
	4300 7150 5500 7150
Text GLabel 7550 5550 0    50   Input ~ 0
CSEN
$Comp
L Device:R R24
U 1 1 5BEC31FD
P 7600 5750
F 0 "R24" V 7680 5750 50  0000 C CNN
F 1 "3K3" V 7600 5750 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 7530 5750 50  0001 C CNN
F 3 "~" H 7600 5750 50  0001 C CNN
	1    7600 5750
	-1   0    0    1   
$EndComp
$Comp
L TrackAmplifier-rescue:GND #PWR033
U 1 1 5BEC3561
P 7600 6000
F 0 "#PWR033" H 7600 5750 50  0001 C CNN
F 1 "GND" H 7600 5850 50  0000 C CNN
F 2 "" H 7600 6000 50  0001 C CNN
F 3 "" H 7600 6000 50  0001 C CNN
	1    7600 6000
	-1   0    0    -1  
$EndComp
Wire Wire Line
	7600 5900 7600 6000
Wire Wire Line
	7550 5550 7600 5550
Wire Wire Line
	7600 5550 7600 5600
$Comp
L Device:R R25
U 1 1 5BF9A128
P 7900 5550
F 0 "R25" V 7980 5550 50  0000 C CNN
F 1 "1K" V 7900 5550 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 7830 5550 50  0001 C CNN
F 3 "~" H 7900 5550 50  0001 C CNN
	1    7900 5550
	0    -1   -1   0   
$EndComp
$Comp
L TrackAmplifier-rescue:C C29
U 1 1 5BFB89D8
P 8150 5750
F 0 "C29" H 8175 5850 50  0000 L CNN
F 1 "22u" H 8175 5650 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 8188 5600 50  0001 C CNN
F 3 "" H 8150 5750 50  0001 C CNN
	1    8150 5750
	-1   0    0    1   
$EndComp
$Comp
L TrackAmplifier-rescue:GND #PWR035
U 1 1 5BFB8A8C
P 8150 6000
F 0 "#PWR035" H 8150 5750 50  0001 C CNN
F 1 "GND" H 8150 5850 50  0000 C CNN
F 2 "" H 8150 6000 50  0001 C CNN
F 3 "" H 8150 6000 50  0001 C CNN
	1    8150 6000
	-1   0    0    -1  
$EndComp
Wire Wire Line
	8150 5900 8150 6000
Wire Wire Line
	7600 5550 7750 5550
Connection ~ 7600 5550
Wire Wire Line
	8050 5550 8150 5550
Wire Wire Line
	8150 5550 8150 5600
Text GLabel 8250 5550 2    50   Output ~ 0
CSEN_FLT
Wire Wire Line
	8250 5550 8150 5550
Connection ~ 8150 5550
Text GLabel 8500 2450 2    50   Input ~ 0
CSEN_FLT
$Comp
L Device:R R26
U 1 1 5C055174
P 8700 1950
F 0 "R26" V 8750 2150 50  0000 C CNN
F 1 "330E" V 8700 1950 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 8630 1950 50  0001 C CNN
F 3 "~" H 8700 1950 50  0001 C CNN
	1    8700 1950
	0    -1   -1   0   
$EndComp
$Comp
L Device:LED D1
U 1 1 5C0558FC
P 9150 1950
F 0 "D1" H 8900 1900 50  0000 C CNN
F 1 "LED" H 9300 1900 50  0000 C CNN
F 2 "LED_SMD:LED_1206_3216Metric" H 9150 1950 50  0001 C CNN
F 3 "~" H 9150 1950 50  0001 C CNN
	1    9150 1950
	-1   0    0    1   
$EndComp
$Comp
L Device:R R27
U 1 1 5C136087
P 8700 2050
F 0 "R27" V 8750 2250 50  0000 C CNN
F 1 "330E" V 8700 2050 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 8630 2050 50  0001 C CNN
F 3 "~" H 8700 2050 50  0001 C CNN
	1    8700 2050
	0    -1   -1   0   
$EndComp
$Comp
L Device:R R28
U 1 1 5C136135
P 8700 2150
F 0 "R28" V 8750 2350 50  0000 C CNN
F 1 "330E" V 8700 2150 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 8630 2150 50  0001 C CNN
F 3 "~" H 8700 2150 50  0001 C CNN
	1    8700 2150
	0    -1   -1   0   
$EndComp
$Comp
L Device:LED D2
U 1 1 5C1363EF
P 9150 2050
F 0 "D2" H 8900 2000 50  0000 C CNN
F 1 "LED" H 9300 2000 50  0000 C CNN
F 2 "LED_SMD:LED_1206_3216Metric" H 9150 2050 50  0001 C CNN
F 3 "~" H 9150 2050 50  0001 C CNN
	1    9150 2050
	-1   0    0    1   
$EndComp
$Comp
L Device:LED D3
U 1 1 5C136953
P 9150 2150
F 0 "D3" H 8900 2100 50  0000 C CNN
F 1 "LED" H 9300 2100 50  0000 C CNN
F 2 "LED_SMD:LED_1206_3216Metric" H 9150 2150 50  0001 C CNN
F 3 "~" H 9150 2150 50  0001 C CNN
	1    9150 2150
	-1   0    0    1   
$EndComp
Wire Wire Line
	8850 1950 9000 1950
Wire Wire Line
	9000 2050 8850 2050
Wire Wire Line
	8850 2150 9000 2150
$Comp
L TrackAmplifier-rescue:GND #PWR039
U 1 1 5C1FAD94
P 9500 2450
F 0 "#PWR039" H 9500 2200 50  0001 C CNN
F 1 "GND" H 9500 2300 50  0000 C CNN
F 2 "" H 9500 2450 50  0001 C CNN
F 3 "" H 9500 2450 50  0001 C CNN
	1    9500 2450
	-1   0    0    -1  
$EndComp
Wire Wire Line
	9300 1950 9500 1950
Wire Wire Line
	9500 1950 9500 2050
Wire Wire Line
	9300 2050 9500 2050
Connection ~ 9500 2050
Wire Wire Line
	9500 2050 9500 2150
Wire Wire Line
	9300 2150 9500 2150
Connection ~ 9500 2150
$Comp
L TrackAmplifier-rescue:+15V #PWR07
U 1 1 5AE1A953
P 1650 750
F 0 "#PWR07" H 1650 600 50  0001 C CNN
F 1 "+15V" H 1650 890 50  0000 C CNN
F 2 "" H 1650 750 50  0001 C CNN
F 3 "" H 1650 750 50  0001 C CNN
	1    1650 750 
	-1   0    0    -1  
$EndComp
Wire Wire Line
	1650 750  1650 800 
Text GLabel 900  800  0    50   Input ~ 0
Vbus_flt+
Text Notes 7350 6450 0    50   ~ 0
377uA/A --> 4A meauring range\n5V / (377^-6 x 4) -->\n3K3
Wire Wire Line
	2600 7400 2600 7500
$Comp
L Device:R R31
U 1 1 5C33103C
P 9550 3700
F 0 "R31" V 9630 3700 50  0000 C CNN
F 1 "100K" V 9550 3700 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 9480 3700 50  0001 C CNN
F 3 "~" H 9550 3700 50  0001 C CNN
	1    9550 3700
	1    0    0    -1  
$EndComp
Wire Wire Line
	9550 3550 9550 3500
Wire Wire Line
	9550 3500 9850 3500
Wire Wire Line
	9550 3850 9550 3900
Wire Wire Line
	9550 3900 9850 3900
Connection ~ 9850 3900
Wire Wire Line
	9850 3900 9850 3950
Wire Wire Line
	9600 1150 9700 1150
Wire Wire Line
	9700 1150 9700 1200
Wire Wire Line
	9700 1150 9900 1150
Connection ~ 9700 1150
Wire Wire Line
	4850 3750 4950 3750
$Comp
L Device:R R29
U 1 1 5C599BC7
P 8700 2350
F 0 "R29" V 8750 2550 50  0000 C CNN
F 1 "330E" V 8700 2350 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 8630 2350 50  0001 C CNN
F 3 "~" H 8700 2350 50  0001 C CNN
	1    8700 2350
	0    -1   -1   0   
$EndComp
$Comp
L Device:LED D4
U 1 1 5C5DCFB1
P 9150 2250
F 0 "D4" H 8900 2200 50  0000 C CNN
F 1 "LED" H 9300 2200 50  0000 C CNN
F 2 "LED_SMD:LED_1206_3216Metric" H 9150 2250 50  0001 C CNN
F 3 "~" H 9150 2250 50  0001 C CNN
	1    9150 2250
	-1   0    0    1   
$EndComp
$Comp
L Device:R R5
U 1 1 5AEE904B
P 1450 800
F 0 "R5" V 1530 800 50  0000 C CNN
F 1 "10E" V 1450 800 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 1380 800 50  0001 C CNN
F 3 "~" H 1450 800 50  0001 C CNN
	1    1450 800 
	0    -1   -1   0   
$EndComp
$Comp
L Device:R R3
U 1 1 5AEE9174
P 1100 800
F 0 "R3" V 1180 800 50  0000 C CNN
F 1 "10E" V 1100 800 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 1030 800 50  0001 C CNN
F 3 "~" H 1100 800 50  0001 C CNN
	1    1100 800 
	0    -1   -1   0   
$EndComp
Wire Wire Line
	900  800  950  800 
Wire Wire Line
	1250 800  1300 800 
Wire Wire Line
	1600 800  1650 800 
Text Notes 7150 7100 0    50   ~ 0
length pcb = 104.7mm\nwidth pcb = 52mm\nheart conn to far pcb edge = 35mm\n\nConnector heart distance = 75mm\nConnector plate = 39.3mm\nSpace between connector plates = 35.4mm
$Comp
L Mechanical:Mounting_Hole MK1
U 1 1 5AED960F
P 5500 7500
F 0 "MK1" H 5600 7546 50  0000 L CNN
F 1 "Mounting_Hole" H 5600 7455 50  0000 L CNN
F 2 "MountingHole:MountingHole_3.7mm" H 5500 7500 50  0001 C CNN
F 3 "" H 5500 7500 50  0001 C CNN
	1    5500 7500
	1    0    0    -1  
$EndComp
Wire Wire Line
	9800 4850 10100 4850
Wire Wire Line
	9800 4950 10100 4950
Wire Wire Line
	9800 5050 10100 5050
Wire Wire Line
	9800 5150 10100 5150
Wire Wire Line
	9800 5250 10100 5250
Wire Wire Line
	9800 5350 10100 5350
Text GLabel 1700 5700 0    50   Input ~ 0
Vbus_flt
Wire Wire Line
	1700 5700 1850 5700
Connection ~ 1850 5700
Wire Wire Line
	9500 2150 9500 2250
Wire Wire Line
	8500 2950 8350 2950
Wire Wire Line
	8350 2850 8500 2850
Wire Wire Line
	8500 2750 8350 2750
Wire Wire Line
	8350 2650 8500 2650
Wire Wire Line
	8550 1950 8350 1950
Wire Wire Line
	8350 2050 8550 2050
Wire Wire Line
	8550 2150 8350 2150
Wire Wire Line
	8500 2450 8350 2450
Wire Wire Line
	7050 2650 7350 2650
Wire Wire Line
	7200 1950 7350 1950
Wire Wire Line
	7350 2050 7200 2050
Wire Wire Line
	7200 2150 7350 2150
$Comp
L Reference_Voltage:REF3025 U10
U 1 1 5B4E4160
P 6350 1150
F 0 "U10" H 6120 1196 50  0000 R CNN
F 1 "REF3025" H 6120 1105 50  0000 R CIN
F 2 "Package_TO_SOT_SMD:SOT-23" H 6350 700 50  0001 C CIN
F 3 "http://www.ti.com/lit/ds/symlink/ref3033.pdf" H 6450 800 50  0001 C CIN
	1    6350 1150
	1    0    0    -1  
$EndComp
$Comp
L power:+5V #PWR09
U 1 1 5B4E45AA
P 5200 750
F 0 "#PWR09" H 5200 600 50  0001 C CNN
F 1 "+5V" H 5200 890 50  0000 C CNN
F 2 "" H 5200 750 50  0001 C CNN
F 3 "" H 5200 750 50  0001 C CNN
	1    5200 750 
	1    0    0    -1  
$EndComp
$Comp
L Device:R R34
U 1 1 5B4E4661
P 5350 850
F 0 "R34" V 5430 850 50  0000 C CNN
F 1 "10E" V 5350 850 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 5280 850 50  0001 C CNN
F 3 "~" H 5350 850 50  0001 C CNN
	1    5350 850 
	0    -1   -1   0   
$EndComp
$Comp
L TrackAmplifier-rescue:C C32
U 1 1 5B4E484C
P 5500 1100
F 0 "C32" H 5525 1200 50  0000 L CNN
F 1 "22u" H 5525 1000 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 5538 950 50  0001 C CNN
F 3 "" H 5500 1100 50  0001 C CNN
	1    5500 1100
	-1   0    0    1   
$EndComp
$Comp
L TrackAmplifier-rescue:C C33
U 1 1 5B4E4A3A
P 5750 1100
F 0 "C33" H 5775 1200 50  0000 L CNN
F 1 "100n" H 5775 1000 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 5788 950 50  0001 C CNN
F 3 "" H 5750 1100 50  0001 C CNN
	1    5750 1100
	-1   0    0    1   
$EndComp
Wire Wire Line
	5500 850  5500 950 
Wire Wire Line
	5750 950  5750 850 
Wire Wire Line
	5750 850  5500 850 
Connection ~ 5500 850 
Wire Wire Line
	5200 750  5200 850 
Wire Wire Line
	5750 1250 5500 1250
$Comp
L TrackAmplifier-rescue:GND #PWR047
U 1 1 5B56DA8F
P 5500 1450
F 0 "#PWR047" H 5500 1200 50  0001 C CNN
F 1 "GND" H 5500 1300 50  0000 C CNN
F 2 "" H 5500 1450 50  0001 C CNN
F 3 "" H 5500 1450 50  0001 C CNN
	1    5500 1450
	-1   0    0    -1  
$EndComp
Wire Wire Line
	5500 1450 5500 1250
Connection ~ 5500 1250
Wire Wire Line
	6250 850  5750 850 
Connection ~ 5750 850 
Wire Wire Line
	6250 1450 5500 1450
Connection ~ 5500 1450
Wire Wire Line
	7350 1750 7300 1750
Wire Wire Line
	6750 1750 6750 1150
Wire Wire Line
	6750 1150 6650 1150
Wire Wire Line
	7350 1850 7300 1850
Wire Wire Line
	7300 1850 7300 1750
Connection ~ 7300 1750
Wire Wire Line
	7300 1750 6750 1750
Text GLabel 6700 1750 0    50   Output ~ 0
REF
Wire Wire Line
	6700 1750 6750 1750
Connection ~ 6750 1750
Text GLabel 5500 2250 2    50   Input ~ 0
REF
Wire Wire Line
	5500 2250 5350 2250
Connection ~ 5350 2250
Text GLabel 7100 4700 2    50   Input ~ 0
REF
Wire Wire Line
	7100 4700 6950 4700
Connection ~ 6950 4700
Wire Wire Line
	9700 1550 9700 1500
NoConn ~ 7350 2250
NoConn ~ 7350 2350
NoConn ~ 7350 2450
NoConn ~ 4450 3550
NoConn ~ 10600 2600
Wire Wire Line
	7050 2850 7350 2850
$Comp
L Device:R R35
U 1 1 5B8B9949
P 8700 2250
F 0 "R35" V 8650 2050 50  0000 C CNN
F 1 "330E" V 8700 2250 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 8630 2250 50  0001 C CNN
F 3 "~" H 8700 2250 50  0001 C CNN
	1    8700 2250
	0    1    1    0   
$EndComp
$Comp
L Device:LED D8
U 1 1 5B8B9950
P 9150 2350
F 0 "D8" H 8900 2300 50  0000 C CNN
F 1 "LED" H 9300 2300 50  0000 C CNN
F 2 "LED_SMD:LED_1206_3216Metric" H 9150 2350 50  0001 C CNN
F 3 "~" H 9150 2350 50  0001 C CNN
	1    9150 2350
	-1   0    0    1   
$EndComp
Wire Wire Line
	7050 2750 7350 2750
Wire Wire Line
	7050 2950 7350 2950
Wire Wire Line
	8350 2250 8550 2250
Wire Wire Line
	8550 2350 8350 2350
Wire Wire Line
	8850 2250 9000 2250
Wire Wire Line
	9000 2350 8850 2350
Wire Wire Line
	9300 2250 9500 2250
Connection ~ 9500 2250
Wire Wire Line
	9500 2250 9500 2350
Wire Wire Line
	9300 2350 9500 2350
Connection ~ 9500 2350
Wire Wire Line
	9500 2350 9500 2450
Text Notes 1200 8100 0    50   ~ 0
http://nl.farnell.com/epcos/b82722a2202n001/filter-common-mode-choke-2-2mh/dp/1223076?ost=B82722A2202N001&scope=partnumberlookahead&exaMfpn=true&searchref=searchlookahead&ddkey=http%3Anl-NL%2FElement14_Netherlands%2Fw%2Fsearch
Wire Wire Line
	1850 4350 1000 4350
$Comp
L TrackAmplifier-rescue:C C35
U 1 1 5AEECD74
P 1000 5150
F 0 "C35" H 1025 5250 50  0000 L CNN
F 1 "100n" H 1025 5050 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 1038 5000 50  0001 C CNN
F 3 "" H 1000 5150 50  0001 C CNN
	1    1000 5150
	-1   0    0    1   
$EndComp
$Comp
L TrackAmplifier-rescue:C C34
U 1 1 5AEECD7B
P 750 5150
F 0 "C34" H 775 5250 50  0000 L CNN
F 1 "22u" H 775 5050 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 788 5000 50  0001 C CNN
F 3 "" H 750 5150 50  0001 C CNN
	1    750  5150
	-1   0    0    1   
$EndComp
$Comp
L power:+5V #PWR0101
U 1 1 5AEECD82
P 750 4550
F 0 "#PWR0101" H 750 4400 50  0001 C CNN
F 1 "+5V" H 750 4690 50  0000 C CNN
F 2 "" H 750 4550 50  0001 C CNN
F 3 "" H 750 4550 50  0001 C CNN
	1    750  4550
	1    0    0    -1  
$EndComp
$Comp
L TrackAmplifier-rescue:GND #PWR0102
U 1 1 5AEECD88
P 750 5400
F 0 "#PWR0102" H 750 5150 50  0001 C CNN
F 1 "GND" H 750 5250 50  0000 C CNN
F 2 "" H 750 5400 50  0001 C CNN
F 3 "" H 750 5400 50  0001 C CNN
	1    750  5400
	-1   0    0    -1  
$EndComp
$Comp
L Device:R R11
U 1 1 5AEECD8E
P 750 4750
F 0 "R11" V 830 4750 50  0000 C CNN
F 1 "10E" V 750 4750 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 680 4750 50  0001 C CNN
F 3 "~" H 750 4750 50  0001 C CNN
	1    750  4750
	-1   0    0    1   
$EndComp
Wire Wire Line
	750  4550 750  4600
Wire Wire Line
	750  4900 750  4950
Wire Wire Line
	750  5300 750  5350
Wire Wire Line
	750  4950 1000 4950
Wire Wire Line
	1000 4950 1000 5000
Connection ~ 750  4950
Wire Wire Line
	750  4950 750  5000
Wire Wire Line
	750  5350 1000 5350
Wire Wire Line
	1000 5350 1000 5300
Connection ~ 750  5350
Wire Wire Line
	750  5350 750  5400
Connection ~ 1000 4950
Connection ~ 1000 5350
Wire Wire Line
	1000 3950 1000 4350
$Comp
L Sensor_Temperature:LM35-LP U11
U 1 1 5AEEC2F8
P 1500 5200
F 0 "U11" H 1250 5450 50  0000 C CNN
F 1 "LM35-LP" H 1550 5450 50  0000 L CNN
F 2 "Package_TO_SOT_THT:TO-92" H 1550 4950 50  0001 L CNN
F 3 "http://www.ti.com/lit/ds/symlink/lm35.pdf" H 1500 5200 50  0001 C CNN
	1    1500 5200
	1    0    0    -1  
$EndComp
Wire Wire Line
	1500 5500 1500 5600
Wire Wire Line
	1500 5600 1000 5600
Wire Wire Line
	1000 5600 1000 5350
Wire Wire Line
	1500 4900 1500 4800
Wire Wire Line
	1500 4800 1000 4800
Wire Wire Line
	1000 4800 1000 4950
Text GLabel 2050 5200 2    50   Output ~ 0
TEMP
Wire Wire Line
	2050 5200 1900 5200
NoConn ~ 3200 6500
$Comp
L Device:EMI_Filter_CommonMode FL2
U 1 1 5AFC22EA
P 8800 4650
F 0 "FL2" H 8800 4825 50  0000 C CNN
F 1 "EMI_Filter_CMode" H 8900 4950 50  0000 C CNN
F 2 "Inductor_THT:Choke_EPCOS_B82722-A2302-N1-22.3x22.7mm" V 8800 4690 50  0001 C CNN
F 3 "~" V 8800 4690 50  0000 C CNN
	1    8800 4650
	-1   0    0    -1  
$EndComp
Text GLabel 8450 4750 0    50   Output ~ 0
Vbus_flt+
Text GLabel 8450 4550 0    50   Output ~ 0
GND
Text GLabel 900  1300 0    50   Input ~ 0
GND
Wire Wire Line
	900  1300 1650 1300
Connection ~ 1650 1300
Wire Wire Line
	9200 4650 9200 5850
Wire Wire Line
	9000 4750 9250 4750
Connection ~ 9250 4750
Wire Wire Line
	9250 4750 9250 5950
Wire Wire Line
	9000 4550 9200 4550
Wire Wire Line
	9200 4550 9200 4650
Connection ~ 9200 4650
Wire Wire Line
	8450 4550 8600 4550
Wire Wire Line
	8600 4750 8450 4750
$Comp
L Device:R R36
U 1 1 5B1A1FDD
P 2650 2550
F 0 "R36" V 2730 2550 50  0000 C CNN
F 1 "10K" V 2650 2550 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 2580 2550 50  0001 C CNN
F 3 "~" H 2650 2550 50  0001 C CNN
	1    2650 2550
	1    0    0    -1  
$EndComp
Wire Wire Line
	2650 2400 2650 2300
Connection ~ 2650 2300
Wire Wire Line
	2650 2300 2700 2300
$Comp
L TrackAmplifier-rescue:GND #PWR05
U 1 1 5B1F6A68
P 2650 2750
F 0 "#PWR05" H 2650 2500 50  0001 C CNN
F 1 "GND" H 2650 2600 50  0000 C CNN
F 2 "" H 2650 2750 50  0001 C CNN
F 3 "" H 2650 2750 50  0001 C CNN
	1    2650 2750
	-1   0    0    -1  
$EndComp
Wire Wire Line
	2650 2700 2650 2750
$Comp
L TrackAmplifier-rescue:GND #PWR0103
U 1 1 5B2227F0
P 1850 6200
F 0 "#PWR0103" H 1850 5950 50  0001 C CNN
F 1 "GND" H 1850 6050 50  0000 C CNN
F 2 "" H 1850 6200 50  0001 C CNN
F 3 "" H 1850 6200 50  0001 C CNN
	1    1850 6200
	-1   0    0    -1  
$EndComp
Wire Wire Line
	1850 6200 1850 6100
Connection ~ 1850 6100
$EndSCHEMATC
