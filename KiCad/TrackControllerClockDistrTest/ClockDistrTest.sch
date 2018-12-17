EESchema Schematic File Version 4
LIBS:ClockDistrTest-cache
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
L ClockDistrTest-rescue:Conn_01x02_Male-ClockDistrTest-rescue J1
U 1 1 5AD59A7C
P 950 900
F 0 "J1" H 950 1000 50  0000 C CNN
F 1 "Conn_01x02_Male" H 950 700 50  0000 C CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_1x02_P2.54mm_Vertical" H 950 900 50  0001 C CNN
F 3 "" H 950 900 50  0001 C CNN
	1    950  900 
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:LM7805_TO220-ClockDistrTest-rescue U3
U 1 1 5AD59A99
P 2950 900
F 0 "U3" H 2800 1025 50  0000 C CNN
F 1 "LM7805_TO220" H 2950 1025 50  0000 L CNN
F 2 "Package_TO_SOT_THT:TO-220-3_Horizontal_TabDown" H 2950 1125 50  0001 C CIN
F 3 "" H 2950 850 50  0001 C CNN
	1    2950 900 
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:GND-ClockDistrTest-rescue #PWR012
U 1 1 5AD59D67
P 2950 1650
F 0 "#PWR012" H 2950 1400 50  0001 C CNN
F 1 "GND" H 2950 1500 50  0000 C CNN
F 2 "" H 2950 1650 50  0001 C CNN
F 3 "" H 2950 1650 50  0001 C CNN
	1    2950 1650
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:+5V-ClockDistrTest-rescue #PWR014
U 1 1 5AD59DB0
P 3850 750
F 0 "#PWR014" H 3850 600 50  0001 C CNN
F 1 "+5V" H 3850 890 50  0000 C CNN
F 2 "" H 3850 750 50  0001 C CNN
F 3 "" H 3850 750 50  0001 C CNN
	1    3850 750 
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:SG-531-ClockDistrTest-rescue X1
U 1 1 5AD5A042
P 2550 2400
F 0 "X1" H 2350 2650 50  0000 L CNN
F 1 "SG-531" H 2600 2150 50  0000 L CNN
F 2 "Oscillator:Oscillator_SeikoEpson_SG-8002DC" H 3000 2050 50  0001 C CNN
F 3 "" H 2450 2400 50  0001 C CNN
	1    2550 2400
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:NB3N511-ClockDistrTest-rescue U2
U 1 1 5AD5DBD1
P 2550 4050
F 0 "U2" H 2650 4750 60  0000 L CNN
F 1 "NB3N511" H 2650 4650 60  0000 L CNN
F 2 "Package_SO:SOIC-8_3.9x4.9mm_P1.27mm" H 2600 3300 60  0001 C CNN
F 3 "http://www.onsemi.com/pub/Collateral/NB3N511-D.PDF" H 2600 3200 60  0001 C CNN
	1    2550 4050
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:C-ClockDistrTest-rescue C7
U 1 1 5AD5E473
P 1950 2250
F 0 "C7" H 1975 2350 50  0000 L CNN
F 1 "0.1u" H 1975 2150 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 1988 2100 50  0001 C CNN
F 3 "" H 1950 2250 50  0001 C CNN
	1    1950 2250
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:C-ClockDistrTest-rescue C13
U 1 1 5AD5E538
P 3550 1200
F 0 "C13" H 3575 1300 50  0000 L CNN
F 1 "0.1u" H 3575 1100 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 3588 1050 50  0001 C CNN
F 3 "" H 3550 1200 50  0001 C CNN
	1    3550 1200
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:C-ClockDistrTest-rescue C14
U 1 1 5AD5E56A
P 3850 1200
F 0 "C14" H 3875 1300 50  0000 L CNN
F 1 "22u" H 3875 1100 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 3888 1050 50  0001 C CNN
F 3 "" H 3850 1200 50  0001 C CNN
	1    3850 1200
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:C-ClockDistrTest-rescue C9
U 1 1 5AD5E591
P 2350 1200
F 0 "C9" H 2375 1300 50  0000 L CNN
F 1 "0.1u" H 2375 1100 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 2388 1050 50  0001 C CNN
F 3 "" H 2350 1200 50  0001 C CNN
	1    2350 1200
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:C-ClockDistrTest-rescue C8
U 1 1 5AD5E5C7
P 2100 1200
F 0 "C8" H 2125 1300 50  0000 L CNN
F 1 "22u" H 2125 1100 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 2138 1050 50  0001 C CNN
F 3 "" H 2100 1200 50  0001 C CNN
	1    2100 1200
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:C-ClockDistrTest-rescue C6
U 1 1 5AD5E750
P 1650 2250
F 0 "C6" H 1675 2350 50  0000 L CNN
F 1 "22u" H 1675 2150 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 1688 2100 50  0001 C CNN
F 3 "" H 1650 2250 50  0001 C CNN
	1    1650 2250
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:C-ClockDistrTest-rescue C12
U 1 1 5AD5E784
P 3350 3500
F 0 "C12" H 3375 3600 50  0000 L CNN
F 1 "22u" H 3375 3400 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 3388 3350 50  0001 C CNN
F 3 "" H 3350 3500 50  0001 C CNN
	1    3350 3500
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:+5V-ClockDistrTest-rescue #PWR03
U 1 1 5AD5ECA0
P 1100 1900
F 0 "#PWR03" H 1100 1750 50  0001 C CNN
F 1 "+5V" H 1100 2040 50  0000 C CNN
F 2 "" H 1100 1900 50  0001 C CNN
F 3 "" H 1100 1900 50  0001 C CNN
	1    1100 1900
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:GND-ClockDistrTest-rescue #PWR010
U 1 1 5AD5ED68
P 2550 2800
F 0 "#PWR010" H 2550 2550 50  0001 C CNN
F 1 "GND" H 2550 2650 50  0000 C CNN
F 2 "" H 2550 2800 50  0001 C CNN
F 3 "" H 2550 2800 50  0001 C CNN
	1    2550 2800
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:GND-ClockDistrTest-rescue #PWR011
U 1 1 5AD5ED94
P 2550 4800
F 0 "#PWR011" H 2550 4550 50  0001 C CNN
F 1 "GND" H 2550 4650 50  0000 C CNN
F 2 "" H 2550 4800 50  0001 C CNN
F 3 "" H 2550 4800 50  0001 C CNN
	1    2550 4800
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:+5V-ClockDistrTest-rescue #PWR015
U 1 1 5AD5EE99
P 4200 3100
F 0 "#PWR015" H 4200 2950 50  0001 C CNN
F 1 "+5V" H 4200 3240 50  0000 C CNN
F 2 "" H 4200 3100 50  0001 C CNN
F 3 "" H 4200 3100 50  0001 C CNN
	1    4200 3100
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:Crystal-ClockDistrTest-rescue Y1
U 1 1 5AD5F521
P 1150 3700
F 0 "Y1" H 1150 3850 50  0000 C CNN
F 1 "Crystal" H 1150 3550 50  0000 C CNN
F 2 "Crystal:Crystal_HC49-U_Vertical" H 1150 3700 50  0001 C CNN
F 3 "" H 1150 3700 50  0001 C CNN
	1    1150 3700
	0    1    1    0   
$EndComp
$Comp
L ClockDistrTest-rescue:C-ClockDistrTest-rescue C1
U 1 1 5AD5F60E
P 850 4100
F 0 "C1" H 875 4200 50  0000 L CNN
F 1 "12p" H 875 4000 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 888 3950 50  0001 C CNN
F 3 "" H 850 4100 50  0001 C CNN
	1    850  4100
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:C-ClockDistrTest-rescue C3
U 1 1 5AD5F685
P 1350 4100
F 0 "C3" H 1375 4200 50  0000 L CNN
F 1 "12p" H 1375 4000 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 1388 3950 50  0001 C CNN
F 3 "" H 1350 4100 50  0001 C CNN
	1    1350 4100
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:GND-ClockDistrTest-rescue #PWR04
U 1 1 5AD5F876
P 1150 4350
F 0 "#PWR04" H 1150 4100 50  0001 C CNN
F 1 "GND" H 1150 4200 50  0000 C CNN
F 2 "" H 1150 4350 50  0001 C CNN
F 3 "" H 1150 4350 50  0001 C CNN
	1    1150 4350
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:4049-ClockDistrTest-rescue U1
U 3 1 5AD601E3
P 3800 5600
F 0 "U1" H 3800 5650 50  0000 C CNN
F 1 "4049" H 3800 5550 50  0000 C CNN
F 2 "Package_DIP:DIP-16_W7.62mm" H 3800 5600 50  0001 C CNN
F 3 "" H 3800 5600 50  0001 C CNN
	3    3800 5600
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:4049-ClockDistrTest-rescue U1
U 4 1 5AD60358
P 3800 6050
F 0 "U1" H 3800 6100 50  0000 C CNN
F 1 "4049" H 3800 6000 50  0000 C CNN
F 2 "Package_DIP:DIP-16_W7.62mm" H 3800 6050 50  0001 C CNN
F 3 "" H 3800 6050 50  0001 C CNN
	4    3800 6050
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:4049-ClockDistrTest-rescue U1
U 5 1 5AD6040E
P 3800 6500
F 0 "U1" H 3800 6550 50  0000 C CNN
F 1 "4049" H 3800 6450 50  0000 C CNN
F 2 "Package_DIP:DIP-16_W7.62mm" H 3800 6500 50  0001 C CNN
F 3 "" H 3800 6500 50  0001 C CNN
	5    3800 6500
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:4049-ClockDistrTest-rescue U1
U 6 1 5AD6045D
P 3800 6950
F 0 "U1" H 3800 7000 50  0000 C CNN
F 1 "4049" H 3800 6900 50  0000 C CNN
F 2 "Package_DIP:DIP-16_W7.62mm" H 3800 6950 50  0001 C CNN
F 3 "" H 3800 6950 50  0001 C CNN
	6    3800 6950
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:4049-ClockDistrTest-rescue U1
U 2 1 5AD604BF
P 3000 5600
F 0 "U1" H 3000 5650 50  0000 C CNN
F 1 "4049" H 3000 5550 50  0000 C CNN
F 2 "Package_DIP:DIP-16_W7.62mm" H 3000 5600 50  0001 C CNN
F 3 "" H 3000 5600 50  0001 C CNN
	2    3000 5600
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:4049-ClockDistrTest-rescue U1
U 1 1 5AD60510
P 1950 5600
F 0 "U1" H 1950 5650 50  0000 C CNN
F 1 "4049" H 1950 5550 50  0000 C CNN
F 2 "Package_DIP:DIP-16_W7.62mm" H 1950 5600 50  0001 C CNN
F 3 "" H 1950 5600 50  0001 C CNN
	1    1950 5600
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:4049-ClockDistrTest-rescue U1
U 7 1 5AD60C43
P 850 6050
F 0 "U1" H 850 6100 50  0000 C CNN
F 1 "4049" H 850 6000 50  0000 C CNN
F 2 "Package_DIP:DIP-16_W7.62mm" H 850 6050 50  0001 C CNN
F 3 "" H 850 6050 50  0001 C CNN
	7    850  6050
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:GND-ClockDistrTest-rescue #PWR02
U 1 1 5AD60E78
P 850 6650
F 0 "#PWR02" H 850 6400 50  0001 C CNN
F 1 "GND" H 850 6500 50  0000 C CNN
F 2 "" H 850 6650 50  0001 C CNN
F 3 "" H 850 6650 50  0001 C CNN
	1    850  6650
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:+5V-ClockDistrTest-rescue #PWR01
U 1 1 5AD60EC2
P 850 4900
F 0 "#PWR01" H 850 4750 50  0001 C CNN
F 1 "+5V" H 850 5040 50  0000 C CNN
F 2 "" H 850 4900 50  0001 C CNN
F 3 "" H 850 4900 50  0001 C CNN
	1    850  4900
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:Crystal-ClockDistrTest-rescue Y2
U 1 1 5AD6137B
P 1900 6150
F 0 "Y2" H 1900 6300 50  0000 C CNN
F 1 "Crystal" H 1900 6000 50  0000 C CNN
F 2 "Crystal:Crystal_HC49-U_Vertical" H 1900 6150 50  0001 C CNN
F 3 "" H 1900 6150 50  0001 C CNN
	1    1900 6150
	-1   0    0    1   
$EndComp
$Comp
L ClockDistrTest-rescue:C-ClockDistrTest-rescue C5
U 1 1 5AD61382
P 1550 6350
F 0 "C5" H 1575 6450 50  0000 L CNN
F 1 "10pF" H 1575 6250 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 1588 6200 50  0001 C CNN
F 3 "" H 1550 6350 50  0001 C CNN
	1    1550 6350
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:C-ClockDistrTest-rescue C10
U 1 1 5AD61389
P 2350 6350
F 0 "C10" H 2375 6450 50  0000 L CNN
F 1 "10pF" H 2375 6250 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 2388 6200 50  0001 C CNN
F 3 "" H 2350 6350 50  0001 C CNN
	1    2350 6350
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:GND-ClockDistrTest-rescue #PWR06
U 1 1 5AD61390
P 1550 6650
F 0 "#PWR06" H 1550 6400 50  0001 C CNN
F 1 "GND" H 1550 6500 50  0000 C CNN
F 2 "" H 1550 6650 50  0001 C CNN
F 3 "" H 1550 6650 50  0001 C CNN
	1    1550 6650
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:R-ClockDistrTest-rescue R3
U 1 1 5AD61931
P 1900 5300
F 0 "R3" V 1980 5300 50  0000 C CNN
F 1 "1M" V 1900 5300 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 1830 5300 50  0001 C CNN
F 3 "" H 1900 5300 50  0001 C CNN
	1    1900 5300
	0    1    1    0   
$EndComp
$Comp
L ClockDistrTest-rescue:R-ClockDistrTest-rescue R4
U 1 1 5AD6210A
P 2350 5950
F 0 "R4" V 2430 5950 50  0000 C CNN
F 1 "270" V 2350 5950 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 2280 5950 50  0001 C CNN
F 3 "" H 2350 5950 50  0001 C CNN
	1    2350 5950
	-1   0    0    1   
$EndComp
$Comp
L ClockDistrTest-rescue:GND-ClockDistrTest-rescue #PWR09
U 1 1 5AD6272B
P 2350 6650
F 0 "#PWR09" H 2350 6400 50  0001 C CNN
F 1 "GND" H 2350 6500 50  0000 C CNN
F 2 "" H 2350 6650 50  0001 C CNN
F 3 "" H 2350 6650 50  0001 C CNN
	1    2350 6650
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:Conn_01x02_Male-ClockDistrTest-rescue J4
U 1 1 5AD6809E
P 4500 3850
F 0 "J4" H 4500 3950 50  0000 C CNN
F 1 "Conn_01x02_Male" H 4350 3650 50  0000 C CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_1x02_P2.54mm_Vertical" H 4500 3850 50  0001 C CNN
F 3 "" H 4500 3850 50  0001 C CNN
	1    4500 3850
	0    1    1    0   
$EndComp
$Comp
L ClockDistrTest-rescue:Conn_01x02_Male-ClockDistrTest-rescue J2
U 1 1 5AD68320
P 3600 2200
F 0 "J2" H 3600 2300 50  0000 C CNN
F 1 "Conn_01x02_Male" H 3450 2000 50  0000 C CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_1x02_P2.54mm_Vertical" H 3600 2200 50  0001 C CNN
F 3 "" H 3600 2200 50  0001 C CNN
	1    3600 2200
	0    1    1    0   
$EndComp
Connection ~ 2350 900 
Wire Wire Line
	2950 1200 2950 1500
Wire Wire Line
	1350 1500 2100 1500
Connection ~ 2100 1500
Connection ~ 2350 1500
Connection ~ 2950 1500
Connection ~ 3550 1500
Wire Wire Line
	3250 900  3550 900 
Connection ~ 3550 900 
Connection ~ 3850 900 
Wire Wire Line
	1150 1000 1350 1000
Wire Wire Line
	1350 1000 1350 1500
Wire Wire Line
	2100 1050 2100 900 
Wire Wire Line
	2350 1050 2350 900 
Wire Wire Line
	2100 1500 2100 1350
Wire Wire Line
	2350 1500 2350 1350
Wire Wire Line
	3550 1500 3550 1350
Wire Wire Line
	3550 1050 3550 900 
Wire Wire Line
	3850 750  3850 900 
Wire Wire Line
	3850 1500 3850 1350
Connection ~ 3850 1500
Wire Wire Line
	2550 2700 2550 2800
Wire Wire Line
	2550 4650 2550 4800
Wire Wire Line
	850  4250 1150 4250
Wire Wire Line
	1150 4250 1150 4350
Connection ~ 1150 4250
Wire Wire Line
	850  3950 850  3450
Wire Wire Line
	850  3450 1150 3450
Wire Wire Line
	1150 3450 1150 3550
Wire Wire Line
	1150 3850 1150 3950
Wire Wire Line
	1150 3950 1350 3950
Connection ~ 1350 3950
Wire Wire Line
	2050 3850 1400 3850
Wire Wire Line
	1400 3850 1400 3450
Connection ~ 1150 3450
Wire Wire Line
	2050 4250 1700 4250
Wire Wire Line
	2200 2050 2200 2400
Wire Wire Line
	2200 2400 2250 2400
Wire Wire Line
	2200 2050 2550 2050
Wire Wire Line
	850  6550 850  6650
Wire Wire Line
	1550 6500 1550 6650
Wire Wire Line
	1550 5300 1550 5600
Wire Wire Line
	1550 6150 1750 6150
Wire Wire Line
	2350 6100 2350 6150
Connection ~ 2350 6150
Wire Wire Line
	1550 5300 1750 5300
Connection ~ 1550 6150
Wire Wire Line
	1650 5600 1550 5600
Connection ~ 1550 5600
Wire Wire Line
	2350 6500 2350 6650
Wire Wire Line
	2350 6150 2050 6150
Wire Wire Line
	2350 5800 2350 5600
Wire Wire Line
	2350 5300 2050 5300
Wire Wire Line
	2250 5600 2350 5600
Connection ~ 2350 5600
Wire Wire Line
	3300 5600 3400 5600
Wire Wire Line
	3500 6050 3400 6050
Wire Wire Line
	3400 5600 3400 6050
Connection ~ 3400 5600
Wire Wire Line
	3400 6500 3500 6500
Connection ~ 3400 6050
Wire Wire Line
	3400 6950 3500 6950
Connection ~ 3400 6500
Wire Wire Line
	2850 2400 3500 2400
Wire Wire Line
	3050 4050 4400 4050
Wire Wire Line
	4500 4150 4500 4050
Connection ~ 4850 4150
$Comp
L ClockDistrTest-rescue:+5V-ClockDistrTest-rescue #PWR016
U 1 1 5AD69C34
P 5500 850
F 0 "#PWR016" H 5500 700 50  0001 C CNN
F 1 "+5V" H 5500 990 50  0000 C CNN
F 2 "" H 5500 850 50  0001 C CNN
F 3 "" H 5500 850 50  0001 C CNN
	1    5500 850 
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:GND-ClockDistrTest-rescue #PWR017
U 1 1 5AD69C99
P 5500 3300
F 0 "#PWR017" H 5500 3050 50  0001 C CNN
F 1 "GND" H 5500 3150 50  0000 C CNN
F 2 "" H 5500 3300 50  0001 C CNN
F 3 "" H 5500 3300 50  0001 C CNN
	1    5500 3300
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:C-ClockDistrTest-rescue C11
U 1 1 5AD6B20B
P 3150 3500
F 0 "C11" H 3175 3600 50  0000 L CNN
F 1 "0.1u" H 3175 3400 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 3188 3350 50  0001 C CNN
F 3 "" H 3150 3500 50  0001 C CNN
	1    3150 3500
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:Conn_01x02_Male-ClockDistrTest-rescue J3
U 1 1 5AD6C687
P 4450 6750
F 0 "J3" H 4450 6850 50  0000 C CNN
F 1 "Conn_01x02_Male" V 4750 7100 50  0000 C CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_1x02_P2.54mm_Vertical" H 4450 6750 50  0001 C CNN
F 3 "" H 4450 6750 50  0001 C CNN
	1    4450 6750
	0    1    1    0   
$EndComp
Wire Wire Line
	4100 6950 4350 6950
Wire Wire Line
	4850 6950 4450 6950
Wire Wire Line
	2100 900  2350 900 
Wire Wire Line
	2350 900  2650 900 
Wire Wire Line
	2100 1500 2350 1500
Wire Wire Line
	2350 1500 2950 1500
Wire Wire Line
	2950 1500 2950 1650
Wire Wire Line
	2950 1500 3550 1500
Wire Wire Line
	3550 1500 3850 1500
Wire Wire Line
	3550 900  3850 900 
Wire Wire Line
	3850 900  3850 1050
Wire Wire Line
	1150 4250 1350 4250
Wire Wire Line
	1350 3950 2050 3950
Wire Wire Line
	1150 3450 1400 3450
Wire Wire Line
	2550 2050 2550 2100
Wire Wire Line
	2350 6150 2350 6200
Wire Wire Line
	1550 6150 1550 6200
Wire Wire Line
	1550 5600 1550 6150
Wire Wire Line
	2350 5600 2350 5300
Wire Wire Line
	2350 5600 2700 5600
Wire Wire Line
	3400 5600 3500 5600
Wire Wire Line
	3400 6050 3400 6500
Wire Wire Line
	3400 6500 3400 6950
Wire Wire Line
	4850 4150 4500 4150
$Comp
L Interface_UART:MAX14783E U4
U 1 1 5AD6BC12
P 5500 2600
F 0 "U4" H 5600 3200 50  0000 C CNN
F 1 "MAX14783E" H 5750 3100 50  0000 C CNN
F 2 "Package_SO:SOIC-8_3.9x4.9mm_P1.27mm" H 5500 1900 50  0001 C CNN
F 3 "https://datasheets.maximintegrated.com/en/ds/MAX14783E.pdf" H 5500 2650 50  0001 C CNN
	1    5500 2600
	1    0    0    -1  
$EndComp
Wire Wire Line
	5100 2600 5050 2600
Connection ~ 5050 2600
Wire Wire Line
	5050 2600 5050 2700
Wire Wire Line
	5100 2700 5050 2700
Wire Wire Line
	5100 2800 4850 2800
Wire Wire Line
	5500 3200 5500 3300
$Comp
L Interface_UART:MAX14783E U9
U 1 1 5AD7A80E
P 9750 2600
F 0 "U9" H 9850 3200 50  0000 C CNN
F 1 "MAX14783E" H 10000 3100 50  0000 C CNN
F 2 "Package_SO:SOIC-8_3.9x4.9mm_P1.27mm" H 9750 1900 50  0001 C CNN
F 3 "https://datasheets.maximintegrated.com/en/ds/MAX14783E.pdf" H 9750 2650 50  0001 C CNN
	1    9750 2600
	-1   0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:GND-ClockDistrTest-rescue #PWR038
U 1 1 5AD7DA78
P 9750 3350
F 0 "#PWR038" H 9750 3100 50  0001 C CNN
F 1 "GND" H 9750 3200 50  0000 C CNN
F 2 "" H 9750 3350 50  0001 C CNN
F 3 "" H 9750 3350 50  0001 C CNN
	1    9750 3350
	1    0    0    -1  
$EndComp
Wire Wire Line
	9750 3200 9750 3350
$Comp
L ClockDistrTest-rescue:Conn_01x02_Male-ClockDistrTest-rescue J22
U 1 1 5AD872BC
P 10600 2200
F 0 "J22" H 10600 2300 50  0000 C CNN
F 1 "Conn_01x02_Male" H 10450 2000 50  0000 C CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_1x02_P2.54mm_Vertical" H 10600 2200 50  0001 C CNN
F 3 "" H 10600 2200 50  0001 C CNN
	1    10600 2200
	0    1    1    0   
$EndComp
Wire Wire Line
	10150 2500 10500 2500
Wire Wire Line
	10500 2500 10500 2400
$Comp
L ClockDistrTest-rescue:GND-ClockDistrTest-rescue #PWR043
U 1 1 5AD8DD65
P 10600 3350
F 0 "#PWR043" H 10600 3100 50  0001 C CNN
F 1 "GND" H 10600 3200 50  0000 C CNN
F 2 "" H 10600 3350 50  0001 C CNN
F 3 "" H 10600 3350 50  0001 C CNN
	1    10600 3350
	1    0    0    -1  
$EndComp
Wire Wire Line
	10600 2400 10600 3350
Wire Wire Line
	10150 2600 10250 2600
$Comp
L ClockDistrTest-rescue:GND-ClockDistrTest-rescue #PWR042
U 1 1 5AD97D53
P 10350 3350
F 0 "#PWR042" H 10350 3100 50  0001 C CNN
F 1 "GND" H 10350 3200 50  0000 C CNN
F 2 "" H 10350 3350 50  0001 C CNN
F 3 "" H 10350 3350 50  0001 C CNN
	1    10350 3350
	1    0    0    -1  
$EndComp
Wire Wire Line
	10150 2700 10250 2700
Wire Wire Line
	10250 2700 10250 2600
Connection ~ 10250 2600
Wire Wire Line
	10250 2600 10350 2600
$Comp
L ClockDistrTest-rescue:R-ClockDistrTest-rescue R11
U 1 1 5ADA21C1
P 6150 2650
F 0 "R11" V 6230 2650 50  0000 C CNN
F 1 "120E" V 6150 2650 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 6080 2650 50  0001 C CNN
F 3 "" H 6150 2650 50  0001 C CNN
	1    6150 2650
	-1   0    0    1   
$EndComp
$Comp
L ClockDistrTest-rescue:R-ClockDistrTest-rescue R10
U 1 1 5ADA2300
P 6150 2250
F 0 "R10" V 6230 2250 50  0000 C CNN
F 1 "1K" V 6150 2250 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 6080 2250 50  0001 C CNN
F 3 "" H 6150 2250 50  0001 C CNN
	1    6150 2250
	-1   0    0    1   
$EndComp
$Comp
L ClockDistrTest-rescue:R-ClockDistrTest-rescue R12
U 1 1 5ADA2370
P 6150 3050
F 0 "R12" V 6230 3050 50  0000 C CNN
F 1 "1K" V 6150 3050 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 6080 3050 50  0001 C CNN
F 3 "" H 6150 3050 50  0001 C CNN
	1    6150 3050
	-1   0    0    1   
$EndComp
$Comp
L ClockDistrTest-rescue:+5V-ClockDistrTest-rescue #PWR023
U 1 1 5ADA23EE
P 6150 1950
F 0 "#PWR023" H 6150 1800 50  0001 C CNN
F 1 "+5V" H 6150 2090 50  0000 C CNN
F 2 "" H 6150 1950 50  0001 C CNN
F 3 "" H 6150 1950 50  0001 C CNN
	1    6150 1950
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:GND-ClockDistrTest-rescue #PWR024
U 1 1 5ADA244B
P 6150 3300
F 0 "#PWR024" H 6150 3050 50  0001 C CNN
F 1 "GND" H 6150 3150 50  0000 C CNN
F 2 "" H 6150 3300 50  0001 C CNN
F 3 "" H 6150 3300 50  0001 C CNN
	1    6150 3300
	1    0    0    -1  
$EndComp
Wire Wire Line
	6150 3300 6150 3200
Wire Wire Line
	6150 2900 6150 2800
Wire Wire Line
	5900 2800 6150 2800
Connection ~ 6150 2800
Wire Wire Line
	5900 2500 6150 2500
Wire Wire Line
	6150 2400 6150 2500
Connection ~ 6150 2500
Wire Wire Line
	6150 2100 6150 1950
$Comp
L ClockDistrTest-rescue:R-ClockDistrTest-rescue R16
U 1 1 5ADC3E4F
P 9100 2650
F 0 "R16" V 9180 2650 50  0000 C CNN
F 1 "120E" V 9100 2650 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 9030 2650 50  0001 C CNN
F 3 "" H 9100 2650 50  0001 C CNN
	1    9100 2650
	-1   0    0    1   
$EndComp
$Comp
L ClockDistrTest-rescue:R-ClockDistrTest-rescue R15
U 1 1 5ADC3E56
P 9100 2250
F 0 "R15" V 9180 2250 50  0000 C CNN
F 1 "1K" V 9100 2250 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 9030 2250 50  0001 C CNN
F 3 "" H 9100 2250 50  0001 C CNN
	1    9100 2250
	-1   0    0    1   
$EndComp
$Comp
L ClockDistrTest-rescue:R-ClockDistrTest-rescue R17
U 1 1 5ADC3E5D
P 9100 3050
F 0 "R17" V 9180 3050 50  0000 C CNN
F 1 "1K" V 9100 3050 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 9030 3050 50  0001 C CNN
F 3 "" H 9100 3050 50  0001 C CNN
	1    9100 3050
	-1   0    0    1   
$EndComp
$Comp
L ClockDistrTest-rescue:+5V-ClockDistrTest-rescue #PWR033
U 1 1 5ADC3E64
P 9100 1950
F 0 "#PWR033" H 9100 1800 50  0001 C CNN
F 1 "+5V" H 9100 2090 50  0000 C CNN
F 2 "" H 9100 1950 50  0001 C CNN
F 3 "" H 9100 1950 50  0001 C CNN
	1    9100 1950
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:GND-ClockDistrTest-rescue #PWR034
U 1 1 5ADC3E6A
P 9100 3300
F 0 "#PWR034" H 9100 3050 50  0001 C CNN
F 1 "GND" H 9100 3150 50  0000 C CNN
F 2 "" H 9100 3300 50  0001 C CNN
F 3 "" H 9100 3300 50  0001 C CNN
	1    9100 3300
	1    0    0    -1  
$EndComp
Wire Wire Line
	9100 3300 9100 3200
Wire Wire Line
	9100 2900 9100 2800
Wire Wire Line
	9100 2400 9100 2500
Wire Wire Line
	9100 2100 9100 1950
Wire Wire Line
	9350 2500 9100 2500
Connection ~ 9100 2500
Wire Wire Line
	9350 2800 9100 2800
Connection ~ 9100 2800
$Comp
L Interface_UART:MAX3284E U7
U 1 1 5ADDCA8A
P 9150 4300
F 0 "U7" H 8850 4650 50  0000 L CNN
F 1 "MAX3284E" H 9300 4650 50  0000 L CNN
F 2 "Package_TO_SOT_SMD:SOT-23-6" H 9150 3600 50  0001 C CNN
F 3 "https://datasheets.maximintegrated.com/en/ds/MAX3280E-MAX3284E.pdf" H 9140 4260 50  0001 C CNN
	1    9150 4300
	1    0    0    -1  
$EndComp
Text Label 5950 2500 0    50   ~ 0
RS422-B
$Comp
L Interface_UART:MAX3284E U5
U 1 1 5ADE610B
P 6950 4300
F 0 "U5" H 6650 4650 50  0000 L CNN
F 1 "MAX3284E" H 7100 4650 50  0000 L CNN
F 2 "Package_TO_SOT_SMD:SOT-23-6" H 6950 3600 50  0001 C CNN
F 3 "https://datasheets.maximintegrated.com/en/ds/MAX3280E-MAX3284E.pdf" H 6940 4260 50  0001 C CNN
	1    6950 4300
	1    0    0    -1  
$EndComp
$Comp
L Interface_UART:MAX3284E U6
U 1 1 5ADE62DD
P 6950 5750
F 0 "U6" H 6650 6100 50  0000 L CNN
F 1 "MAX3284E" H 7100 6100 50  0000 L CNN
F 2 "Package_TO_SOT_SMD:SOT-23-6" H 6950 5050 50  0001 C CNN
F 3 "https://datasheets.maximintegrated.com/en/ds/MAX3280E-MAX3284E.pdf" H 6940 5710 50  0001 C CNN
	1    6950 5750
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:+5V-ClockDistrTest-rescue #PWR018
U 1 1 5ADE664A
P 5500 3700
F 0 "#PWR018" H 5500 3550 50  0001 C CNN
F 1 "+5V" H 5500 3840 50  0000 C CNN
F 2 "" H 5500 3700 50  0001 C CNN
F 3 "" H 5500 3700 50  0001 C CNN
	1    5500 3700
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:+5V-ClockDistrTest-rescue #PWR019
U 1 1 5ADE671C
P 5500 5150
F 0 "#PWR019" H 5500 5000 50  0001 C CNN
F 1 "+5V" H 5500 5290 50  0000 C CNN
F 2 "" H 5500 5150 50  0001 C CNN
F 3 "" H 5500 5150 50  0001 C CNN
	1    5500 5150
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:GND-ClockDistrTest-rescue #PWR025
U 1 1 5ADE681C
P 6950 4750
F 0 "#PWR025" H 6950 4500 50  0001 C CNN
F 1 "GND" H 6950 4600 50  0000 C CNN
F 2 "" H 6950 4750 50  0001 C CNN
F 3 "" H 6950 4750 50  0001 C CNN
	1    6950 4750
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:GND-ClockDistrTest-rescue #PWR035
U 1 1 5ADE6885
P 9150 4750
F 0 "#PWR035" H 9150 4500 50  0001 C CNN
F 1 "GND" H 9150 4600 50  0000 C CNN
F 2 "" H 9150 4750 50  0001 C CNN
F 3 "" H 9150 4750 50  0001 C CNN
	1    9150 4750
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:GND-ClockDistrTest-rescue #PWR026
U 1 1 5ADE68EE
P 6950 6200
F 0 "#PWR026" H 6950 5950 50  0001 C CNN
F 1 "GND" H 6950 6050 50  0000 C CNN
F 2 "" H 6950 6200 50  0001 C CNN
F 3 "" H 6950 6200 50  0001 C CNN
	1    6950 6200
	1    0    0    -1  
$EndComp
Wire Wire Line
	7050 3900 7050 3750
Wire Wire Line
	7050 3750 6850 3750
Connection ~ 6850 3750
Wire Wire Line
	6850 3750 6850 3900
Wire Wire Line
	6950 4700 6950 4750
Wire Wire Line
	9150 4700 9150 4750
Wire Wire Line
	7050 5350 7050 5200
Wire Wire Line
	7050 5200 6850 5200
Wire Wire Line
	6850 5200 6850 5350
Wire Wire Line
	6950 6150 6950 6200
$Comp
L ClockDistrTest-rescue:Conn_01x02_Male-ClockDistrTest-rescue J15
U 1 1 5AE14EB4
P 7800 4100
F 0 "J15" H 7800 4200 50  0000 C CNN
F 1 "Conn_01x02_Male" H 7650 3900 50  0000 C CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_1x02_P2.54mm_Vertical" H 7800 4100 50  0001 C CNN
F 3 "" H 7800 4100 50  0001 C CNN
	1    7800 4100
	0    1    1    0   
$EndComp
$Comp
L ClockDistrTest-rescue:GND-ClockDistrTest-rescue #PWR030
U 1 1 5AE15614
P 7800 4750
F 0 "#PWR030" H 7800 4500 50  0001 C CNN
F 1 "GND" H 7800 4600 50  0000 C CNN
F 2 "" H 7800 4750 50  0001 C CNN
F 3 "" H 7800 4750 50  0001 C CNN
	1    7800 4750
	1    0    0    -1  
$EndComp
Wire Wire Line
	7700 4300 7350 4300
Wire Wire Line
	7800 4300 7800 4750
$Comp
L ClockDistrTest-rescue:Conn_01x02_Male-ClockDistrTest-rescue J20
U 1 1 5AE20A26
P 9950 4100
F 0 "J20" H 9950 4200 50  0000 C CNN
F 1 "Conn_01x02_Male" H 9800 3900 50  0000 C CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_1x02_P2.54mm_Vertical" H 9950 4100 50  0001 C CNN
F 3 "" H 9950 4100 50  0001 C CNN
	1    9950 4100
	0    1    1    0   
$EndComp
$Comp
L ClockDistrTest-rescue:GND-ClockDistrTest-rescue #PWR039
U 1 1 5AE20A9E
P 9950 4750
F 0 "#PWR039" H 9950 4500 50  0001 C CNN
F 1 "GND" H 9950 4600 50  0000 C CNN
F 2 "" H 9950 4750 50  0001 C CNN
F 3 "" H 9950 4750 50  0001 C CNN
	1    9950 4750
	1    0    0    -1  
$EndComp
Wire Wire Line
	9950 4750 9950 4300
Wire Wire Line
	9850 4300 9550 4300
$Comp
L ClockDistrTest-rescue:Conn_01x02_Male-ClockDistrTest-rescue J14
U 1 1 5AE2C540
P 7750 5550
F 0 "J14" H 7750 5650 50  0000 C CNN
F 1 "Conn_01x02_Male" H 7600 5350 50  0000 C CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_1x02_P2.54mm_Vertical" H 7750 5550 50  0001 C CNN
F 3 "" H 7750 5550 50  0001 C CNN
	1    7750 5550
	0    1    1    0   
$EndComp
Wire Wire Line
	7650 5750 7350 5750
$Comp
L ClockDistrTest-rescue:GND-ClockDistrTest-rescue #PWR029
U 1 1 5AE3232D
P 7750 6200
F 0 "#PWR029" H 7750 5950 50  0001 C CNN
F 1 "GND" H 7750 6050 50  0000 C CNN
F 2 "" H 7750 6200 50  0001 C CNN
F 3 "" H 7750 6200 50  0001 C CNN
	1    7750 6200
	1    0    0    -1  
$EndComp
Wire Wire Line
	7750 6200 7750 5750
Connection ~ 4850 2800
Wire Wire Line
	4850 2800 4850 4150
$Comp
L Interface_UART:MAX3284E U8
U 1 1 5AE8B94C
P 9150 5750
F 0 "U8" H 8850 6100 50  0000 L CNN
F 1 "MAX3284E" H 9300 6100 50  0000 L CNN
F 2 "Package_TO_SOT_SMD:SOT-23-6" H 9150 5050 50  0001 C CNN
F 3 "https://datasheets.maximintegrated.com/en/ds/MAX3280E-MAX3284E.pdf" H 9140 5710 50  0001 C CNN
	1    9150 5750
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:GND-ClockDistrTest-rescue #PWR036
U 1 1 5AE8B959
P 9150 6200
F 0 "#PWR036" H 9150 5950 50  0001 C CNN
F 1 "GND" H 9150 6050 50  0000 C CNN
F 2 "" H 9150 6200 50  0001 C CNN
F 3 "" H 9150 6200 50  0001 C CNN
	1    9150 6200
	1    0    0    -1  
$EndComp
Wire Wire Line
	9150 6150 9150 6200
$Comp
L ClockDistrTest-rescue:Conn_01x02_Male-ClockDistrTest-rescue J21
U 1 1 5AE8B965
P 9950 5550
F 0 "J21" H 9950 5650 50  0000 C CNN
F 1 "Conn_01x02_Male" H 9800 5350 50  0000 C CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_1x02_P2.54mm_Vertical" H 9950 5550 50  0001 C CNN
F 3 "" H 9950 5550 50  0001 C CNN
	1    9950 5550
	0    1    1    0   
$EndComp
Wire Wire Line
	9850 5750 9550 5750
$Comp
L ClockDistrTest-rescue:GND-ClockDistrTest-rescue #PWR040
U 1 1 5AE8B96D
P 9950 6200
F 0 "#PWR040" H 9950 5950 50  0001 C CNN
F 1 "GND" H 9950 6050 50  0000 C CNN
F 2 "" H 9950 6200 50  0001 C CNN
F 3 "" H 9950 6200 50  0001 C CNN
	1    9950 6200
	1    0    0    -1  
$EndComp
Wire Wire Line
	9950 6200 9950 5750
$Comp
L ClockDistrTest-rescue:C-ClockDistrTest-rescue C2
U 1 1 5AE92EEC
P 1150 5600
F 0 "C2" H 1175 5700 50  0000 L CNN
F 1 "0.1u" H 1175 5500 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 1188 5450 50  0001 C CNN
F 3 "" H 1150 5600 50  0001 C CNN
	1    1150 5600
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:C-ClockDistrTest-rescue C4
U 1 1 5AE92F74
P 1350 5600
F 0 "C4" H 1375 5700 50  0000 L CNN
F 1 "22u" H 1375 5500 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 1388 5450 50  0001 C CNN
F 3 "" H 1350 5600 50  0001 C CNN
	1    1350 5600
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:C-ClockDistrTest-rescue C15
U 1 1 5AE92FFC
P 5700 1500
F 0 "C15" H 5725 1600 50  0000 L CNN
F 1 "0.1u" H 5725 1400 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 5738 1350 50  0001 C CNN
F 3 "" H 5700 1500 50  0001 C CNN
	1    5700 1500
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:C-ClockDistrTest-rescue C16
U 1 1 5AE93086
P 5900 1500
F 0 "C16" H 5925 1600 50  0000 L CNN
F 1 "22u" H 5925 1400 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 5938 1350 50  0001 C CNN
F 3 "" H 5900 1500 50  0001 C CNN
	1    5900 1500
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:C-ClockDistrTest-rescue C19
U 1 1 5AE9310E
P 6200 3950
F 0 "C19" H 6225 4050 50  0000 L CNN
F 1 "0.1u" H 6225 3850 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 6238 3800 50  0001 C CNN
F 3 "" H 6200 3950 50  0001 C CNN
	1    6200 3950
	1    0    0    -1  
$EndComp
$Comp
L pspice:DIODE D1
U 1 1 5AEE6930
P 1700 900
F 0 "D1" H 1700 1165 50  0000 C CNN
F 1 "DIODE" H 1700 1074 50  0000 C CNN
F 2 "Diode_THT:D_DO-15_P10.16mm_Horizontal" H 1700 900 50  0001 C CNN
F 3 "" H 1700 900 50  0001 C CNN
	1    1700 900 
	1    0    0    -1  
$EndComp
Wire Wire Line
	1500 900  1150 900 
Wire Wire Line
	1900 900  2100 900 
Connection ~ 2100 900 
$Comp
L ClockDistrTest-rescue:R-ClockDistrTest-rescue R6
U 1 1 5AEFC266
P 4150 900
F 0 "R6" V 4230 900 50  0000 C CNN
F 1 "300" V 4150 900 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 4080 900 50  0001 C CNN
F 3 "" H 4150 900 50  0001 C CNN
	1    4150 900 
	0    -1   -1   0   
$EndComp
$Comp
L Device:LED D2
U 1 1 5AF0580B
P 4300 1200
F 0 "D2" V 4338 1083 50  0000 R CNN
F 1 "LED" V 4247 1083 50  0000 R CNN
F 2 "LED_THT:LED_D5.0mm" H 4300 1200 50  0001 C CNN
F 3 "~" H 4300 1200 50  0001 C CNN
	1    4300 1200
	0    -1   -1   0   
$EndComp
Wire Wire Line
	4300 900  4300 1050
Wire Wire Line
	4300 1350 4300 1500
Wire Wire Line
	6150 2500 6400 2500
Wire Wire Line
	6150 2800 6400 2800
$Comp
L ClockDistrTest-rescue:Conn_01x02_Male-ClockDistrTest-rescue J8
U 1 1 5AD969A5
P 6600 2700
F 0 "J8" H 6700 2800 50  0000 C CNN
F 1 "Conn_01x02_Male" V 6700 2200 50  0000 C CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_1x02_P2.54mm_Vertical" H 6600 2700 50  0001 C CNN
F 3 "" H 6600 2700 50  0001 C CNN
	1    6600 2700
	-1   0    0    1   
$EndComp
Wire Wire Line
	6400 2500 6400 2600
Wire Wire Line
	6400 2700 6400 2800
$Comp
L ClockDistrTest-rescue:Conn_01x02_Male-ClockDistrTest-rescue J19
U 1 1 5ADAA57B
P 8650 2700
F 0 "J19" H 8650 2800 50  0000 C CNN
F 1 "Conn_01x02_Male" V 8700 2250 50  0000 C CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_1x02_P2.54mm_Vertical" H 8650 2700 50  0001 C CNN
F 3 "" H 8650 2700 50  0001 C CNN
	1    8650 2700
	1    0    0    1   
$EndComp
Wire Wire Line
	8850 2600 8850 2500
Wire Wire Line
	8850 2500 9100 2500
Wire Wire Line
	8850 2700 8850 2800
Wire Wire Line
	8850 2800 9100 2800
$Comp
L ClockDistrTest-rescue:Conn_01x02_Male-ClockDistrTest-rescue J9
U 1 1 5AE5F9F1
P 6650 2700
F 0 "J9" H 6700 2800 50  0000 C CNN
F 1 "Conn_01x02_Male" V 6700 2250 50  0000 C CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_1x02_P2.54mm_Vertical" H 6650 2700 50  0001 C CNN
F 3 "" H 6650 2700 50  0001 C CNN
	1    6650 2700
	1    0    0    1   
$EndComp
$Comp
L ClockDistrTest-rescue:Conn_01x02_Male-ClockDistrTest-rescue J10
U 1 1 5AE60DB3
P 7100 2700
F 0 "J10" H 7200 2800 50  0000 C CNN
F 1 "Conn_01x02_Male" V 7200 2200 50  0000 C CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_1x02_P2.54mm_Vertical" H 7100 2700 50  0001 C CNN
F 3 "" H 7100 2700 50  0001 C CNN
	1    7100 2700
	-1   0    0    1   
$EndComp
$Comp
L ClockDistrTest-rescue:Conn_01x02_Male-ClockDistrTest-rescue J11
U 1 1 5AE60DBA
P 7150 2700
F 0 "J11" H 7200 2800 50  0000 C CNN
F 1 "Conn_01x02_Male" V 7200 2250 50  0000 C CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_1x02_P2.54mm_Vertical" H 7150 2700 50  0001 C CNN
F 3 "" H 7150 2700 50  0001 C CNN
	1    7150 2700
	1    0    0    1   
$EndComp
$Comp
L ClockDistrTest-rescue:Conn_01x02_Male-ClockDistrTest-rescue J12
U 1 1 5AE69BC0
P 7600 2700
F 0 "J12" H 7700 2800 50  0000 C CNN
F 1 "Conn_01x02_Male" V 7700 2200 50  0000 C CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_1x02_P2.54mm_Vertical" H 7600 2700 50  0001 C CNN
F 3 "" H 7600 2700 50  0001 C CNN
	1    7600 2700
	-1   0    0    1   
$EndComp
$Comp
L ClockDistrTest-rescue:Conn_01x02_Male-ClockDistrTest-rescue J13
U 1 1 5AE69BC7
P 7650 2700
F 0 "J13" H 7700 2800 50  0000 C CNN
F 1 "Conn_01x02_Male" V 7700 2250 50  0000 C CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_1x02_P2.54mm_Vertical" H 7650 2700 50  0001 C CNN
F 3 "" H 7650 2700 50  0001 C CNN
	1    7650 2700
	1    0    0    1   
$EndComp
Wire Wire Line
	6900 2600 6850 2600
Wire Wire Line
	6850 2700 6900 2700
Wire Wire Line
	7350 2600 7400 2600
Wire Wire Line
	7400 2700 7350 2700
$Comp
L ClockDistrTest-rescue:Conn_01x02_Male-ClockDistrTest-rescue J16
U 1 1 5AEAA2FB
P 8100 2700
F 0 "J16" H 8200 2800 50  0000 C CNN
F 1 "Conn_01x02_Male" V 8200 2200 50  0000 C CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_1x02_P2.54mm_Vertical" H 8100 2700 50  0001 C CNN
F 3 "" H 8100 2700 50  0001 C CNN
	1    8100 2700
	-1   0    0    1   
$EndComp
$Comp
L ClockDistrTest-rescue:Conn_01x02_Male-ClockDistrTest-rescue J17
U 1 1 5AEAA302
P 8150 2700
F 0 "J17" H 8200 2800 50  0000 C CNN
F 1 "Conn_01x02_Male" V 8200 2250 50  0000 C CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_1x02_P2.54mm_Vertical" H 8150 2700 50  0001 C CNN
F 3 "" H 8150 2700 50  0001 C CNN
	1    8150 2700
	1    0    0    1   
$EndComp
$Comp
L ClockDistrTest-rescue:Conn_01x02_Male-ClockDistrTest-rescue J18
U 1 1 5AEB35EC
P 8600 2700
F 0 "J18" H 8700 2800 50  0000 C CNN
F 1 "Conn_01x02_Male" V 8700 2200 50  0000 C CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_1x02_P2.54mm_Vertical" H 8600 2700 50  0001 C CNN
F 3 "" H 8600 2700 50  0001 C CNN
	1    8600 2700
	-1   0    0    1   
$EndComp
Wire Wire Line
	7850 2600 7900 2600
Wire Wire Line
	7850 2700 7900 2700
Wire Wire Line
	8350 2600 8400 2600
Wire Wire Line
	8350 2700 8400 2700
Text GLabel 6900 2800 3    50   Input ~ 0
A1
Text GLabel 6900 2500 1    50   Input ~ 0
B1
Wire Wire Line
	6900 2700 6900 2800
Connection ~ 6900 2700
Wire Wire Line
	6900 2500 6900 2600
Connection ~ 6900 2600
Text GLabel 7400 2500 1    50   Input ~ 0
B2
Text GLabel 7400 2800 3    50   Input ~ 0
A2
Text GLabel 7900 2500 1    50   Input ~ 0
B3
Text GLabel 7900 2800 3    50   Input ~ 0
A3
Text GLabel 8400 2500 1    50   Input ~ 0
B4
Text GLabel 8400 2800 3    50   Input ~ 0
A4
Wire Wire Line
	7400 2500 7400 2600
Connection ~ 7400 2600
Wire Wire Line
	7400 2800 7400 2700
Connection ~ 7400 2700
Wire Wire Line
	7900 2800 7900 2700
Connection ~ 7900 2700
Wire Wire Line
	7900 2500 7900 2600
Connection ~ 7900 2600
Wire Wire Line
	8400 2500 8400 2600
Connection ~ 8400 2600
Wire Wire Line
	8400 2800 8400 2700
Connection ~ 8400 2700
Text GLabel 6450 4200 0    50   Input ~ 0
A1
Text GLabel 6450 4400 0    50   Input ~ 0
B1
Text GLabel 6450 5650 0    50   Input ~ 0
A2
Text GLabel 6450 5850 0    50   Input ~ 0
B2
Text GLabel 8650 4200 0    50   Input ~ 0
A3
Text GLabel 8650 4400 0    50   Input ~ 0
B3
Text GLabel 8650 5650 0    50   Input ~ 0
A4
Text GLabel 8650 5850 0    50   Input ~ 0
B4
Wire Wire Line
	6450 4200 6550 4200
Wire Wire Line
	6550 4400 6450 4400
Wire Wire Line
	8650 4200 8750 4200
Wire Wire Line
	8650 4400 8750 4400
Wire Wire Line
	8650 5650 8750 5650
Wire Wire Line
	8650 5850 8750 5850
Wire Wire Line
	6450 5650 6550 5650
Wire Wire Line
	6450 5850 6550 5850
Text Label 5950 2900 0    50   ~ 0
RS422-A
Wire Wire Line
	3850 1500 4300 1500
Wire Wire Line
	3850 900  4000 900 
$Comp
L ClockDistrTest-rescue:R-ClockDistrTest-rescue R2
U 1 1 5AD94BFF
P 1300 2050
F 0 "R2" V 1380 2050 50  0000 C CNN
F 1 "10E" V 1300 2050 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 1230 2050 50  0001 C CNN
F 3 "" H 1300 2050 50  0001 C CNN
	1    1300 2050
	0    -1   -1   0   
$EndComp
Wire Wire Line
	1100 1900 1100 2050
Wire Wire Line
	1100 2050 1150 2050
Connection ~ 2200 2050
Wire Wire Line
	1950 2100 1950 2050
Wire Wire Line
	1950 2050 2200 2050
$Comp
L ClockDistrTest-rescue:GND-ClockDistrTest-rescue #PWR08
U 1 1 5ADACE0D
P 1950 2800
F 0 "#PWR08" H 1950 2550 50  0001 C CNN
F 1 "GND" H 1950 2650 50  0000 C CNN
F 2 "" H 1950 2800 50  0001 C CNN
F 3 "" H 1950 2800 50  0001 C CNN
	1    1950 2800
	1    0    0    -1  
$EndComp
Wire Wire Line
	1950 2800 1950 2400
Wire Wire Line
	1450 2050 1650 2050
Wire Wire Line
	1650 2050 1650 2100
Wire Wire Line
	1650 2050 1950 2050
Connection ~ 1650 2050
Connection ~ 1950 2050
$Comp
L ClockDistrTest-rescue:GND-ClockDistrTest-rescue #PWR07
U 1 1 5ADCEEDB
P 1650 2800
F 0 "#PWR07" H 1650 2550 50  0001 C CNN
F 1 "GND" H 1650 2650 50  0000 C CNN
F 2 "" H 1650 2800 50  0001 C CNN
F 3 "" H 1650 2800 50  0001 C CNN
	1    1650 2800
	1    0    0    -1  
$EndComp
Wire Wire Line
	1650 2800 1650 2400
Wire Wire Line
	3600 2400 4850 2400
Wire Wire Line
	4850 2400 4850 2800
$Comp
L ClockDistrTest-rescue:R-ClockDistrTest-rescue R1
U 1 1 5ADEA583
P 850 5200
F 0 "R1" V 930 5200 50  0000 C CNN
F 1 "10E" V 850 5200 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 780 5200 50  0001 C CNN
F 3 "" H 850 5200 50  0001 C CNN
	1    850  5200
	1    0    0    -1  
$EndComp
Wire Wire Line
	2550 3350 2550 3150
Wire Wire Line
	2550 3150 3150 3150
Wire Wire Line
	3150 3350 3150 3150
Connection ~ 3150 3150
Wire Wire Line
	3150 3150 3350 3150
Wire Wire Line
	3350 3350 3350 3150
Connection ~ 3350 3150
Wire Wire Line
	4200 3150 4200 3100
$Comp
L ClockDistrTest-rescue:GND-ClockDistrTest-rescue #PWR013
U 1 1 5AE104AC
P 3350 3750
F 0 "#PWR013" H 3350 3500 50  0001 C CNN
F 1 "GND" H 3350 3600 50  0000 C CNN
F 2 "" H 3350 3750 50  0001 C CNN
F 3 "" H 3350 3750 50  0001 C CNN
	1    3350 3750
	1    0    0    -1  
$EndComp
Wire Wire Line
	3150 3650 3150 3700
Wire Wire Line
	3150 3700 3350 3700
Wire Wire Line
	3350 3700 3350 3750
Wire Wire Line
	3350 3650 3350 3700
Connection ~ 3350 3700
Wire Wire Line
	1700 3150 2550 3150
Wire Wire Line
	1700 3150 1700 4250
Connection ~ 2550 3150
Wire Wire Line
	850  5550 850  5400
Wire Wire Line
	850  5050 850  4900
Wire Wire Line
	850  5400 1150 5400
Wire Wire Line
	1150 5400 1150 5450
Connection ~ 850  5400
Wire Wire Line
	850  5400 850  5350
Wire Wire Line
	1150 5400 1350 5400
Wire Wire Line
	1350 5400 1350 5450
Connection ~ 1150 5400
$Comp
L ClockDistrTest-rescue:GND-ClockDistrTest-rescue #PWR05
U 1 1 5AE7A4BB
P 1150 6650
F 0 "#PWR05" H 1150 6400 50  0001 C CNN
F 1 "GND" H 1150 6500 50  0000 C CNN
F 2 "" H 1150 6650 50  0001 C CNN
F 3 "" H 1150 6650 50  0001 C CNN
	1    1150 6650
	1    0    0    -1  
$EndComp
Wire Wire Line
	1150 5750 1150 5800
Wire Wire Line
	1350 5750 1350 5800
Wire Wire Line
	1350 5800 1150 5800
Connection ~ 1150 5800
Wire Wire Line
	1150 5800 1150 6650
$Comp
L ClockDistrTest-rescue:GND-ClockDistrTest-rescue #PWR020
U 1 1 5AEB7000
P 5900 1750
F 0 "#PWR020" H 5900 1500 50  0001 C CNN
F 1 "GND" H 5900 1600 50  0000 C CNN
F 2 "" H 5900 1750 50  0001 C CNN
F 3 "" H 5900 1750 50  0001 C CNN
	1    5900 1750
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:R-ClockDistrTest-rescue R7
U 1 1 5AEB72E5
P 5500 1100
F 0 "R7" V 5580 1100 50  0000 C CNN
F 1 "10E" V 5500 1100 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 5430 1100 50  0001 C CNN
F 3 "" H 5500 1100 50  0001 C CNN
	1    5500 1100
	-1   0    0    1   
$EndComp
Wire Wire Line
	5500 1250 5500 1300
Wire Wire Line
	5500 850  5500 950 
Wire Wire Line
	5700 1350 5700 1300
Wire Wire Line
	5700 1300 5500 1300
Connection ~ 5500 1300
Wire Wire Line
	5500 1300 5500 1750
Wire Wire Line
	5700 1650 5700 1700
Wire Wire Line
	5700 1700 5900 1700
Wire Wire Line
	5900 1700 5900 1750
Wire Wire Line
	5900 1650 5900 1700
Connection ~ 5900 1700
Wire Wire Line
	5700 1300 5900 1300
Wire Wire Line
	5900 1300 5900 1350
Connection ~ 5700 1300
Wire Wire Line
	5050 1750 5500 1750
Connection ~ 5500 1750
Wire Wire Line
	5500 1750 5500 2100
$Comp
L ClockDistrTest-rescue:R-ClockDistrTest-rescue R8
U 1 1 5AF1FFE1
P 5750 3750
F 0 "R8" V 5830 3750 50  0000 C CNN
F 1 "10E" V 5750 3750 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 5680 3750 50  0001 C CNN
F 3 "" H 5750 3750 50  0001 C CNN
	1    5750 3750
	0    -1   -1   0   
$EndComp
$Comp
L ClockDistrTest-rescue:C-ClockDistrTest-rescue C17
U 1 1 5AF200CF
P 6000 3950
F 0 "C17" H 6025 4050 50  0000 L CNN
F 1 "22u" H 6025 3850 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 6038 3800 50  0001 C CNN
F 3 "" H 6000 3950 50  0001 C CNN
	1    6000 3950
	1    0    0    -1  
$EndComp
Wire Wire Line
	6000 3800 6000 3750
Wire Wire Line
	6000 3750 6200 3750
Wire Wire Line
	6200 3800 6200 3750
Connection ~ 6200 3750
Wire Wire Line
	6200 3750 6850 3750
$Comp
L ClockDistrTest-rescue:GND-ClockDistrTest-rescue #PWR021
U 1 1 5AF4A831
P 6000 4750
F 0 "#PWR021" H 6000 4500 50  0001 C CNN
F 1 "GND" H 6000 4600 50  0000 C CNN
F 2 "" H 6000 4750 50  0001 C CNN
F 3 "" H 6000 4750 50  0001 C CNN
	1    6000 4750
	1    0    0    -1  
$EndComp
Wire Wire Line
	6000 4100 6000 4150
Wire Wire Line
	6200 4100 6200 4150
Wire Wire Line
	6200 4150 6000 4150
Connection ~ 6000 4150
Wire Wire Line
	6000 4150 6000 4750
Wire Wire Line
	5500 3700 5500 3750
Wire Wire Line
	5500 3750 5600 3750
Wire Wire Line
	5900 3750 6000 3750
Connection ~ 6000 3750
$Comp
L ClockDistrTest-rescue:C-ClockDistrTest-rescue C20
U 1 1 5B008A66
P 6200 5400
F 0 "C20" H 6225 5500 50  0000 L CNN
F 1 "0.1u" H 6225 5300 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 6238 5250 50  0001 C CNN
F 3 "" H 6200 5400 50  0001 C CNN
	1    6200 5400
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:R-ClockDistrTest-rescue R9
U 1 1 5B008A6D
P 5750 5200
F 0 "R9" V 5830 5200 50  0000 C CNN
F 1 "10E" V 5750 5200 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 5680 5200 50  0001 C CNN
F 3 "" H 5750 5200 50  0001 C CNN
	1    5750 5200
	0    -1   -1   0   
$EndComp
$Comp
L ClockDistrTest-rescue:C-ClockDistrTest-rescue C18
U 1 1 5B008A74
P 6000 5400
F 0 "C18" H 6025 5500 50  0000 L CNN
F 1 "22u" H 6025 5300 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 6038 5250 50  0001 C CNN
F 3 "" H 6000 5400 50  0001 C CNN
	1    6000 5400
	1    0    0    -1  
$EndComp
Wire Wire Line
	6000 5250 6000 5200
Wire Wire Line
	6000 5200 6200 5200
Wire Wire Line
	6200 5250 6200 5200
Connection ~ 6200 5200
Wire Wire Line
	6200 5200 6850 5200
$Comp
L ClockDistrTest-rescue:GND-ClockDistrTest-rescue #PWR022
U 1 1 5B008A80
P 6000 6200
F 0 "#PWR022" H 6000 5950 50  0001 C CNN
F 1 "GND" H 6000 6050 50  0000 C CNN
F 2 "" H 6000 6200 50  0001 C CNN
F 3 "" H 6000 6200 50  0001 C CNN
	1    6000 6200
	1    0    0    -1  
$EndComp
Wire Wire Line
	6000 5550 6000 5600
Wire Wire Line
	6200 5550 6200 5600
Wire Wire Line
	6200 5600 6000 5600
Connection ~ 6000 5600
Wire Wire Line
	6000 5600 6000 6200
Wire Wire Line
	5500 5200 5600 5200
Wire Wire Line
	5900 5200 6000 5200
Connection ~ 6000 5200
Connection ~ 6850 5200
Wire Wire Line
	5500 5150 5500 5200
$Comp
L ClockDistrTest-rescue:+5V-ClockDistrTest-rescue #PWR027
U 1 1 5B057ABF
P 7700 3700
F 0 "#PWR027" H 7700 3550 50  0001 C CNN
F 1 "+5V" H 7700 3840 50  0000 C CNN
F 2 "" H 7700 3700 50  0001 C CNN
F 3 "" H 7700 3700 50  0001 C CNN
	1    7700 3700
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:C-ClockDistrTest-rescue C23
U 1 1 5B057AC5
P 8400 3950
F 0 "C23" H 8425 4050 50  0000 L CNN
F 1 "0.1u" H 8425 3850 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 8438 3800 50  0001 C CNN
F 3 "" H 8400 3950 50  0001 C CNN
	1    8400 3950
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:R-ClockDistrTest-rescue R13
U 1 1 5B057ACC
P 7950 3750
F 0 "R13" V 8030 3750 50  0000 C CNN
F 1 "10E" V 7950 3750 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 7880 3750 50  0001 C CNN
F 3 "" H 7950 3750 50  0001 C CNN
	1    7950 3750
	0    -1   -1   0   
$EndComp
$Comp
L ClockDistrTest-rescue:C-ClockDistrTest-rescue C21
U 1 1 5B057AD3
P 8200 3950
F 0 "C21" H 8225 4050 50  0000 L CNN
F 1 "22u" H 8225 3850 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 8238 3800 50  0001 C CNN
F 3 "" H 8200 3950 50  0001 C CNN
	1    8200 3950
	1    0    0    -1  
$EndComp
Wire Wire Line
	8200 3800 8200 3750
Wire Wire Line
	8200 3750 8400 3750
Wire Wire Line
	8400 3800 8400 3750
Connection ~ 8400 3750
Wire Wire Line
	8400 3750 9050 3750
$Comp
L ClockDistrTest-rescue:GND-ClockDistrTest-rescue #PWR031
U 1 1 5B057ADF
P 8200 4750
F 0 "#PWR031" H 8200 4500 50  0001 C CNN
F 1 "GND" H 8200 4600 50  0000 C CNN
F 2 "" H 8200 4750 50  0001 C CNN
F 3 "" H 8200 4750 50  0001 C CNN
	1    8200 4750
	1    0    0    -1  
$EndComp
Wire Wire Line
	8200 4100 8200 4150
Wire Wire Line
	8400 4100 8400 4150
Wire Wire Line
	8400 4150 8200 4150
Connection ~ 8200 4150
Wire Wire Line
	8200 4150 8200 4750
Wire Wire Line
	7700 3750 7800 3750
Wire Wire Line
	8100 3750 8200 3750
Connection ~ 8200 3750
Wire Wire Line
	7700 3700 7700 3750
$Comp
L ClockDistrTest-rescue:+5V-ClockDistrTest-rescue #PWR028
U 1 1 5B067D62
P 7700 5150
F 0 "#PWR028" H 7700 5000 50  0001 C CNN
F 1 "+5V" H 7700 5290 50  0000 C CNN
F 2 "" H 7700 5150 50  0001 C CNN
F 3 "" H 7700 5150 50  0001 C CNN
	1    7700 5150
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:C-ClockDistrTest-rescue C24
U 1 1 5B067D68
P 8400 5400
F 0 "C24" H 8425 5500 50  0000 L CNN
F 1 "0.1u" H 8425 5300 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 8438 5250 50  0001 C CNN
F 3 "" H 8400 5400 50  0001 C CNN
	1    8400 5400
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:R-ClockDistrTest-rescue R14
U 1 1 5B067D6F
P 7950 5200
F 0 "R14" V 8030 5200 50  0000 C CNN
F 1 "10E" V 7950 5200 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 7880 5200 50  0001 C CNN
F 3 "" H 7950 5200 50  0001 C CNN
	1    7950 5200
	0    -1   -1   0   
$EndComp
$Comp
L ClockDistrTest-rescue:C-ClockDistrTest-rescue C22
U 1 1 5B067D76
P 8200 5400
F 0 "C22" H 8225 5500 50  0000 L CNN
F 1 "22u" H 8225 5300 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 8238 5250 50  0001 C CNN
F 3 "" H 8200 5400 50  0001 C CNN
	1    8200 5400
	1    0    0    -1  
$EndComp
Wire Wire Line
	8200 5250 8200 5200
Wire Wire Line
	8200 5200 8400 5200
Wire Wire Line
	8400 5250 8400 5200
Connection ~ 8400 5200
Wire Wire Line
	8400 5200 9050 5200
$Comp
L ClockDistrTest-rescue:GND-ClockDistrTest-rescue #PWR032
U 1 1 5B067D82
P 8200 6200
F 0 "#PWR032" H 8200 5950 50  0001 C CNN
F 1 "GND" H 8200 6050 50  0000 C CNN
F 2 "" H 8200 6200 50  0001 C CNN
F 3 "" H 8200 6200 50  0001 C CNN
	1    8200 6200
	1    0    0    -1  
$EndComp
Wire Wire Line
	8200 5550 8200 5600
Wire Wire Line
	8400 5550 8400 5600
Wire Wire Line
	8400 5600 8200 5600
Connection ~ 8200 5600
Wire Wire Line
	8200 5600 8200 6200
Wire Wire Line
	7700 5200 7800 5200
Wire Wire Line
	8100 5200 8200 5200
Connection ~ 8200 5200
Wire Wire Line
	7700 5150 7700 5200
Wire Wire Line
	9050 3750 9050 3900
Wire Wire Line
	9050 3750 9250 3750
Wire Wire Line
	9250 3750 9250 3900
Connection ~ 9050 3750
Wire Wire Line
	9050 5200 9050 5350
Wire Wire Line
	9250 5350 9250 5200
Wire Wire Line
	9250 5200 9050 5200
Connection ~ 9050 5200
Wire Wire Line
	5050 1750 5050 2600
$Comp
L ClockDistrTest-rescue:+5V-ClockDistrTest-rescue #PWR037
U 1 1 5B0E405B
P 9750 850
F 0 "#PWR037" H 9750 700 50  0001 C CNN
F 1 "+5V" H 9750 990 50  0000 C CNN
F 2 "" H 9750 850 50  0001 C CNN
F 3 "" H 9750 850 50  0001 C CNN
	1    9750 850 
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:C-ClockDistrTest-rescue C25
U 1 1 5B0E4061
P 9950 1500
F 0 "C25" H 9975 1600 50  0000 L CNN
F 1 "0.1u" H 9975 1400 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 9988 1350 50  0001 C CNN
F 3 "" H 9950 1500 50  0001 C CNN
	1    9950 1500
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:C-ClockDistrTest-rescue C26
U 1 1 5B0E4068
P 10150 1500
F 0 "C26" H 10175 1600 50  0000 L CNN
F 1 "22u" H 10175 1400 50  0000 L CNN
F 2 "Capacitor_SMD:C_1206_3216Metric" H 10188 1350 50  0001 C CNN
F 3 "" H 10150 1500 50  0001 C CNN
	1    10150 1500
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:GND-ClockDistrTest-rescue #PWR041
U 1 1 5B0E406F
P 10150 1750
F 0 "#PWR041" H 10150 1500 50  0001 C CNN
F 1 "GND" H 10150 1600 50  0000 C CNN
F 2 "" H 10150 1750 50  0001 C CNN
F 3 "" H 10150 1750 50  0001 C CNN
	1    10150 1750
	1    0    0    -1  
$EndComp
$Comp
L ClockDistrTest-rescue:R-ClockDistrTest-rescue R18
U 1 1 5B0E4075
P 9750 1100
F 0 "R18" V 9830 1100 50  0000 C CNN
F 1 "10E" V 9750 1100 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 9680 1100 50  0001 C CNN
F 3 "" H 9750 1100 50  0001 C CNN
	1    9750 1100
	-1   0    0    1   
$EndComp
Wire Wire Line
	9750 1250 9750 1300
Wire Wire Line
	9750 850  9750 950 
Wire Wire Line
	9950 1350 9950 1300
Wire Wire Line
	9950 1300 9750 1300
Connection ~ 9750 1300
Wire Wire Line
	9950 1650 9950 1700
Wire Wire Line
	9950 1700 10150 1700
Wire Wire Line
	10150 1700 10150 1750
Wire Wire Line
	10150 1650 10150 1700
Connection ~ 10150 1700
Wire Wire Line
	9950 1300 10150 1300
Wire Wire Line
	10150 1300 10150 1350
Connection ~ 9950 1300
Wire Wire Line
	9750 1300 9750 2100
Wire Wire Line
	10350 2600 10350 3350
$Comp
L ClockDistrTest-rescue:R-ClockDistrTest-rescue R5
U 1 1 5B12FE2D
P 3600 3150
F 0 "R5" V 3680 3150 50  0000 C CNN
F 1 "10E" V 3600 3150 50  0000 C CNN
F 2 "Resistor_SMD:R_1206_3216Metric" V 3530 3150 50  0001 C CNN
F 3 "" H 3600 3150 50  0001 C CNN
	1    3600 3150
	0    -1   -1   0   
$EndComp
Wire Wire Line
	3350 3150 3450 3150
Wire Wire Line
	3750 3150 4200 3150
Wire Wire Line
	4850 4150 4850 6950
$EndSCHEMATC
