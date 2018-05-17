#
# Generated Makefile - do not edit!
#
# Edit the Makefile in the project folder instead (../Makefile). Each target
# has a -pre and a -post target defined where you can add customized code.
#
# This makefile implements configuration specific macros and targets.


# Include project Makefile
ifeq "${IGNORE_LOCAL}" "TRUE"
# do not include local makefile. User is passing all local related variables already
else
include Makefile
# Include makefile containing local settings
ifeq "$(wildcard nbproject/Makefile-local-default.mk)" "nbproject/Makefile-local-default.mk"
include nbproject/Makefile-local-default.mk
endif
endif

# Environment
MKDIR=gnumkdir -p
RM=rm -f 
MV=mv 
CP=cp 

# Macros
CND_CONF=default
ifeq ($(TYPE_IMAGE), DEBUG_RUN)
IMAGE_TYPE=debug
OUTPUT_SUFFIX=cof
DEBUGGABLE_SUFFIX=cof
FINAL_IMAGE=dist/${CND_CONF}/${IMAGE_TYPE}/TrackController3.X.${IMAGE_TYPE}.${OUTPUT_SUFFIX}
else
IMAGE_TYPE=production
OUTPUT_SUFFIX=hex
DEBUGGABLE_SUFFIX=cof
FINAL_IMAGE=dist/${CND_CONF}/${IMAGE_TYPE}/TrackController3.X.${IMAGE_TYPE}.${OUTPUT_SUFFIX}
endif

ifeq ($(COMPARE_BUILD), true)
COMPARISON_BUILD=
else
COMPARISON_BUILD=
endif

ifdef SUB_IMAGE_ADDRESS

else
SUB_IMAGE_ADDRESS_COMMAND=
endif

# Object Directory
OBJECTDIR=build/${CND_CONF}/${IMAGE_TYPE}

# Distribution Directory
DISTDIR=dist/${CND_CONF}/${IMAGE_TYPE}

# Source Files Quoted if spaced
SOURCEFILES_QUOTED_IF_SPACED=I2c2/i2c1ack.c I2c2/i2c1clos.c I2c2/i2c1dtrd.c I2c2/i2c1eeap.c I2c2/i2c1eebw.c I2c2/i2c1eecr.c I2c2/i2c1eepw.c I2c2/i2c1eerr.c I2c2/i2c1eesr.c I2c2/i2c1gets.c I2c2/i2c1idle.c I2c2/i2c1nack.c I2c2/i2c1open.c I2c2/i2c1puts.c I2c2/i2c1read.c I2c2/i2c1rstr.c I2c2/i2c1stop.c I2c2/i2c1strt.c I2c2/i2c1writ.c I2c2/i2c2ack.c I2c2/i2c2clos.c I2c2/i2c2dtrd.c I2c2/i2c2eeap.c I2c2/i2c2eebw.c I2c2/i2c2eecr.c I2c2/i2c2eepw.c I2c2/i2c2eerr.c I2c2/i2c2eesr.c I2c2/i2c2gets.c I2c2/i2c2idle.c I2c2/i2c2nack.c I2c2/i2c2open.c I2c2/i2c2puts.c I2c2/i2c2read.c I2c2/i2c2rstr.c I2c2/i2c2stop.c I2c2/i2c2strt.c I2c2/i2c2writ.c Microchip/Common/TimeDelay.c Microchip/Common/timer.c Microchip/PIC18Salloc/salloc.c Microchip/TCPIPStack/Announce.c Microchip/TCPIPStack/ARCFOUR.c Microchip/TCPIPStack/ARP.c Microchip/TCPIPStack/AutoIP.c Microchip/TCPIPStack/BerkeleyAPI.c Microchip/TCPIPStack/BigInt.c Microchip/TCPIPStack/Delay.c Microchip/TCPIPStack/DHCP.c Microchip/TCPIPStack/DHCPs.c Microchip/TCPIPStack/DNS.c Microchip/TCPIPStack/DNSs.c Microchip/TCPIPStack/DynDNS.c Microchip/TCPIPStack/ETH97J60.c Microchip/TCPIPStack/FileSystem.c Microchip/TCPIPStack/FTP.c Microchip/TCPIPStack/Hashes.c Microchip/TCPIPStack/Helpers.c Microchip/TCPIPStack/HTTP.c Microchip/TCPIPStack/HTTP2.c Microchip/TCPIPStack/ICMP.c Microchip/TCPIPStack/IP.c Microchip/TCPIPStack/LCDBlocking.c Microchip/TCPIPStack/MPFS.c Microchip/TCPIPStack/MPFS2.c Microchip/TCPIPStack/NBNS.c Microchip/TCPIPStack/Random.c Microchip/TCPIPStack/Reboot.c Microchip/TCPIPStack/RSA.c Microchip/TCPIPStack/SMTP.c Microchip/TCPIPStack/SNMP.c Microchip/TCPIPStack/SNMPv3.c Microchip/TCPIPStack/SNMPv3USM.c Microchip/TCPIPStack/SNTP.c Microchip/TCPIPStack/SPIEEPROM.c Microchip/TCPIPStack/SPIFlash.c Microchip/TCPIPStack/SPIRAM.c Microchip/TCPIPStack/SSL.c Microchip/TCPIPStack/StackTsk.c Microchip/TCPIPStack/TCP.c Microchip/TCPIPStack/TCPPerformanceTest.c Microchip/TCPIPStack/Telnet.c Microchip/TCPIPStack/TFTPc.c Microchip/TCPIPStack/Tick.c Microchip/TCPIPStack/UART.c Microchip/TCPIPStack/UART2TCPBridge.c Microchip/TCPIPStack/UDP.c Microchip/TCPIPStack/UDPPerformanceTest.c modbus/General.c modbus/Init.c modbus/Interrupts.c modbus/PetitModbus.c modbus/PetitModbusPort.c modbus/PetitRegConstant.c Command_Machine.c Diagnostic_ret.c Shift_Register.c Var_Out.c FiddleYard.c eusart1.c eusart2.c State_Machine.c

# Object Files Quoted if spaced
OBJECTFILES_QUOTED_IF_SPACED=${OBJECTDIR}/I2c2/i2c1ack.o ${OBJECTDIR}/I2c2/i2c1clos.o ${OBJECTDIR}/I2c2/i2c1dtrd.o ${OBJECTDIR}/I2c2/i2c1eeap.o ${OBJECTDIR}/I2c2/i2c1eebw.o ${OBJECTDIR}/I2c2/i2c1eecr.o ${OBJECTDIR}/I2c2/i2c1eepw.o ${OBJECTDIR}/I2c2/i2c1eerr.o ${OBJECTDIR}/I2c2/i2c1eesr.o ${OBJECTDIR}/I2c2/i2c1gets.o ${OBJECTDIR}/I2c2/i2c1idle.o ${OBJECTDIR}/I2c2/i2c1nack.o ${OBJECTDIR}/I2c2/i2c1open.o ${OBJECTDIR}/I2c2/i2c1puts.o ${OBJECTDIR}/I2c2/i2c1read.o ${OBJECTDIR}/I2c2/i2c1rstr.o ${OBJECTDIR}/I2c2/i2c1stop.o ${OBJECTDIR}/I2c2/i2c1strt.o ${OBJECTDIR}/I2c2/i2c1writ.o ${OBJECTDIR}/I2c2/i2c2ack.o ${OBJECTDIR}/I2c2/i2c2clos.o ${OBJECTDIR}/I2c2/i2c2dtrd.o ${OBJECTDIR}/I2c2/i2c2eeap.o ${OBJECTDIR}/I2c2/i2c2eebw.o ${OBJECTDIR}/I2c2/i2c2eecr.o ${OBJECTDIR}/I2c2/i2c2eepw.o ${OBJECTDIR}/I2c2/i2c2eerr.o ${OBJECTDIR}/I2c2/i2c2eesr.o ${OBJECTDIR}/I2c2/i2c2gets.o ${OBJECTDIR}/I2c2/i2c2idle.o ${OBJECTDIR}/I2c2/i2c2nack.o ${OBJECTDIR}/I2c2/i2c2open.o ${OBJECTDIR}/I2c2/i2c2puts.o ${OBJECTDIR}/I2c2/i2c2read.o ${OBJECTDIR}/I2c2/i2c2rstr.o ${OBJECTDIR}/I2c2/i2c2stop.o ${OBJECTDIR}/I2c2/i2c2strt.o ${OBJECTDIR}/I2c2/i2c2writ.o ${OBJECTDIR}/Microchip/Common/TimeDelay.o ${OBJECTDIR}/Microchip/Common/timer.o ${OBJECTDIR}/Microchip/PIC18Salloc/salloc.o ${OBJECTDIR}/Microchip/TCPIPStack/Announce.o ${OBJECTDIR}/Microchip/TCPIPStack/ARCFOUR.o ${OBJECTDIR}/Microchip/TCPIPStack/ARP.o ${OBJECTDIR}/Microchip/TCPIPStack/AutoIP.o ${OBJECTDIR}/Microchip/TCPIPStack/BerkeleyAPI.o ${OBJECTDIR}/Microchip/TCPIPStack/BigInt.o ${OBJECTDIR}/Microchip/TCPIPStack/Delay.o ${OBJECTDIR}/Microchip/TCPIPStack/DHCP.o ${OBJECTDIR}/Microchip/TCPIPStack/DHCPs.o ${OBJECTDIR}/Microchip/TCPIPStack/DNS.o ${OBJECTDIR}/Microchip/TCPIPStack/DNSs.o ${OBJECTDIR}/Microchip/TCPIPStack/DynDNS.o ${OBJECTDIR}/Microchip/TCPIPStack/ETH97J60.o ${OBJECTDIR}/Microchip/TCPIPStack/FileSystem.o ${OBJECTDIR}/Microchip/TCPIPStack/FTP.o ${OBJECTDIR}/Microchip/TCPIPStack/Hashes.o ${OBJECTDIR}/Microchip/TCPIPStack/Helpers.o ${OBJECTDIR}/Microchip/TCPIPStack/HTTP.o ${OBJECTDIR}/Microchip/TCPIPStack/HTTP2.o ${OBJECTDIR}/Microchip/TCPIPStack/ICMP.o ${OBJECTDIR}/Microchip/TCPIPStack/IP.o ${OBJECTDIR}/Microchip/TCPIPStack/LCDBlocking.o ${OBJECTDIR}/Microchip/TCPIPStack/MPFS.o ${OBJECTDIR}/Microchip/TCPIPStack/MPFS2.o ${OBJECTDIR}/Microchip/TCPIPStack/NBNS.o ${OBJECTDIR}/Microchip/TCPIPStack/Random.o ${OBJECTDIR}/Microchip/TCPIPStack/Reboot.o ${OBJECTDIR}/Microchip/TCPIPStack/RSA.o ${OBJECTDIR}/Microchip/TCPIPStack/SMTP.o ${OBJECTDIR}/Microchip/TCPIPStack/SNMP.o ${OBJECTDIR}/Microchip/TCPIPStack/SNMPv3.o ${OBJECTDIR}/Microchip/TCPIPStack/SNMPv3USM.o ${OBJECTDIR}/Microchip/TCPIPStack/SNTP.o ${OBJECTDIR}/Microchip/TCPIPStack/SPIEEPROM.o ${OBJECTDIR}/Microchip/TCPIPStack/SPIFlash.o ${OBJECTDIR}/Microchip/TCPIPStack/SPIRAM.o ${OBJECTDIR}/Microchip/TCPIPStack/SSL.o ${OBJECTDIR}/Microchip/TCPIPStack/StackTsk.o ${OBJECTDIR}/Microchip/TCPIPStack/TCP.o ${OBJECTDIR}/Microchip/TCPIPStack/TCPPerformanceTest.o ${OBJECTDIR}/Microchip/TCPIPStack/Telnet.o ${OBJECTDIR}/Microchip/TCPIPStack/TFTPc.o ${OBJECTDIR}/Microchip/TCPIPStack/Tick.o ${OBJECTDIR}/Microchip/TCPIPStack/UART.o ${OBJECTDIR}/Microchip/TCPIPStack/UART2TCPBridge.o ${OBJECTDIR}/Microchip/TCPIPStack/UDP.o ${OBJECTDIR}/Microchip/TCPIPStack/UDPPerformanceTest.o ${OBJECTDIR}/modbus/General.o ${OBJECTDIR}/modbus/Init.o ${OBJECTDIR}/modbus/Interrupts.o ${OBJECTDIR}/modbus/PetitModbus.o ${OBJECTDIR}/modbus/PetitModbusPort.o ${OBJECTDIR}/modbus/PetitRegConstant.o ${OBJECTDIR}/Command_Machine.o ${OBJECTDIR}/Diagnostic_ret.o ${OBJECTDIR}/Shift_Register.o ${OBJECTDIR}/Var_Out.o ${OBJECTDIR}/FiddleYard.o ${OBJECTDIR}/eusart1.o ${OBJECTDIR}/eusart2.o ${OBJECTDIR}/State_Machine.o
POSSIBLE_DEPFILES=${OBJECTDIR}/I2c2/i2c1ack.o.d ${OBJECTDIR}/I2c2/i2c1clos.o.d ${OBJECTDIR}/I2c2/i2c1dtrd.o.d ${OBJECTDIR}/I2c2/i2c1eeap.o.d ${OBJECTDIR}/I2c2/i2c1eebw.o.d ${OBJECTDIR}/I2c2/i2c1eecr.o.d ${OBJECTDIR}/I2c2/i2c1eepw.o.d ${OBJECTDIR}/I2c2/i2c1eerr.o.d ${OBJECTDIR}/I2c2/i2c1eesr.o.d ${OBJECTDIR}/I2c2/i2c1gets.o.d ${OBJECTDIR}/I2c2/i2c1idle.o.d ${OBJECTDIR}/I2c2/i2c1nack.o.d ${OBJECTDIR}/I2c2/i2c1open.o.d ${OBJECTDIR}/I2c2/i2c1puts.o.d ${OBJECTDIR}/I2c2/i2c1read.o.d ${OBJECTDIR}/I2c2/i2c1rstr.o.d ${OBJECTDIR}/I2c2/i2c1stop.o.d ${OBJECTDIR}/I2c2/i2c1strt.o.d ${OBJECTDIR}/I2c2/i2c1writ.o.d ${OBJECTDIR}/I2c2/i2c2ack.o.d ${OBJECTDIR}/I2c2/i2c2clos.o.d ${OBJECTDIR}/I2c2/i2c2dtrd.o.d ${OBJECTDIR}/I2c2/i2c2eeap.o.d ${OBJECTDIR}/I2c2/i2c2eebw.o.d ${OBJECTDIR}/I2c2/i2c2eecr.o.d ${OBJECTDIR}/I2c2/i2c2eepw.o.d ${OBJECTDIR}/I2c2/i2c2eerr.o.d ${OBJECTDIR}/I2c2/i2c2eesr.o.d ${OBJECTDIR}/I2c2/i2c2gets.o.d ${OBJECTDIR}/I2c2/i2c2idle.o.d ${OBJECTDIR}/I2c2/i2c2nack.o.d ${OBJECTDIR}/I2c2/i2c2open.o.d ${OBJECTDIR}/I2c2/i2c2puts.o.d ${OBJECTDIR}/I2c2/i2c2read.o.d ${OBJECTDIR}/I2c2/i2c2rstr.o.d ${OBJECTDIR}/I2c2/i2c2stop.o.d ${OBJECTDIR}/I2c2/i2c2strt.o.d ${OBJECTDIR}/I2c2/i2c2writ.o.d ${OBJECTDIR}/Microchip/Common/TimeDelay.o.d ${OBJECTDIR}/Microchip/Common/timer.o.d ${OBJECTDIR}/Microchip/PIC18Salloc/salloc.o.d ${OBJECTDIR}/Microchip/TCPIPStack/Announce.o.d ${OBJECTDIR}/Microchip/TCPIPStack/ARCFOUR.o.d ${OBJECTDIR}/Microchip/TCPIPStack/ARP.o.d ${OBJECTDIR}/Microchip/TCPIPStack/AutoIP.o.d ${OBJECTDIR}/Microchip/TCPIPStack/BerkeleyAPI.o.d ${OBJECTDIR}/Microchip/TCPIPStack/BigInt.o.d ${OBJECTDIR}/Microchip/TCPIPStack/Delay.o.d ${OBJECTDIR}/Microchip/TCPIPStack/DHCP.o.d ${OBJECTDIR}/Microchip/TCPIPStack/DHCPs.o.d ${OBJECTDIR}/Microchip/TCPIPStack/DNS.o.d ${OBJECTDIR}/Microchip/TCPIPStack/DNSs.o.d ${OBJECTDIR}/Microchip/TCPIPStack/DynDNS.o.d ${OBJECTDIR}/Microchip/TCPIPStack/ETH97J60.o.d ${OBJECTDIR}/Microchip/TCPIPStack/FileSystem.o.d ${OBJECTDIR}/Microchip/TCPIPStack/FTP.o.d ${OBJECTDIR}/Microchip/TCPIPStack/Hashes.o.d ${OBJECTDIR}/Microchip/TCPIPStack/Helpers.o.d ${OBJECTDIR}/Microchip/TCPIPStack/HTTP.o.d ${OBJECTDIR}/Microchip/TCPIPStack/HTTP2.o.d ${OBJECTDIR}/Microchip/TCPIPStack/ICMP.o.d ${OBJECTDIR}/Microchip/TCPIPStack/IP.o.d ${OBJECTDIR}/Microchip/TCPIPStack/LCDBlocking.o.d ${OBJECTDIR}/Microchip/TCPIPStack/MPFS.o.d ${OBJECTDIR}/Microchip/TCPIPStack/MPFS2.o.d ${OBJECTDIR}/Microchip/TCPIPStack/NBNS.o.d ${OBJECTDIR}/Microchip/TCPIPStack/Random.o.d ${OBJECTDIR}/Microchip/TCPIPStack/Reboot.o.d ${OBJECTDIR}/Microchip/TCPIPStack/RSA.o.d ${OBJECTDIR}/Microchip/TCPIPStack/SMTP.o.d ${OBJECTDIR}/Microchip/TCPIPStack/SNMP.o.d ${OBJECTDIR}/Microchip/TCPIPStack/SNMPv3.o.d ${OBJECTDIR}/Microchip/TCPIPStack/SNMPv3USM.o.d ${OBJECTDIR}/Microchip/TCPIPStack/SNTP.o.d ${OBJECTDIR}/Microchip/TCPIPStack/SPIEEPROM.o.d ${OBJECTDIR}/Microchip/TCPIPStack/SPIFlash.o.d ${OBJECTDIR}/Microchip/TCPIPStack/SPIRAM.o.d ${OBJECTDIR}/Microchip/TCPIPStack/SSL.o.d ${OBJECTDIR}/Microchip/TCPIPStack/StackTsk.o.d ${OBJECTDIR}/Microchip/TCPIPStack/TCP.o.d ${OBJECTDIR}/Microchip/TCPIPStack/TCPPerformanceTest.o.d ${OBJECTDIR}/Microchip/TCPIPStack/Telnet.o.d ${OBJECTDIR}/Microchip/TCPIPStack/TFTPc.o.d ${OBJECTDIR}/Microchip/TCPIPStack/Tick.o.d ${OBJECTDIR}/Microchip/TCPIPStack/UART.o.d ${OBJECTDIR}/Microchip/TCPIPStack/UART2TCPBridge.o.d ${OBJECTDIR}/Microchip/TCPIPStack/UDP.o.d ${OBJECTDIR}/Microchip/TCPIPStack/UDPPerformanceTest.o.d ${OBJECTDIR}/modbus/General.o.d ${OBJECTDIR}/modbus/Init.o.d ${OBJECTDIR}/modbus/Interrupts.o.d ${OBJECTDIR}/modbus/PetitModbus.o.d ${OBJECTDIR}/modbus/PetitModbusPort.o.d ${OBJECTDIR}/modbus/PetitRegConstant.o.d ${OBJECTDIR}/Command_Machine.o.d ${OBJECTDIR}/Diagnostic_ret.o.d ${OBJECTDIR}/Shift_Register.o.d ${OBJECTDIR}/Var_Out.o.d ${OBJECTDIR}/FiddleYard.o.d ${OBJECTDIR}/eusart1.o.d ${OBJECTDIR}/eusart2.o.d ${OBJECTDIR}/State_Machine.o.d

# Object Files
OBJECTFILES=${OBJECTDIR}/I2c2/i2c1ack.o ${OBJECTDIR}/I2c2/i2c1clos.o ${OBJECTDIR}/I2c2/i2c1dtrd.o ${OBJECTDIR}/I2c2/i2c1eeap.o ${OBJECTDIR}/I2c2/i2c1eebw.o ${OBJECTDIR}/I2c2/i2c1eecr.o ${OBJECTDIR}/I2c2/i2c1eepw.o ${OBJECTDIR}/I2c2/i2c1eerr.o ${OBJECTDIR}/I2c2/i2c1eesr.o ${OBJECTDIR}/I2c2/i2c1gets.o ${OBJECTDIR}/I2c2/i2c1idle.o ${OBJECTDIR}/I2c2/i2c1nack.o ${OBJECTDIR}/I2c2/i2c1open.o ${OBJECTDIR}/I2c2/i2c1puts.o ${OBJECTDIR}/I2c2/i2c1read.o ${OBJECTDIR}/I2c2/i2c1rstr.o ${OBJECTDIR}/I2c2/i2c1stop.o ${OBJECTDIR}/I2c2/i2c1strt.o ${OBJECTDIR}/I2c2/i2c1writ.o ${OBJECTDIR}/I2c2/i2c2ack.o ${OBJECTDIR}/I2c2/i2c2clos.o ${OBJECTDIR}/I2c2/i2c2dtrd.o ${OBJECTDIR}/I2c2/i2c2eeap.o ${OBJECTDIR}/I2c2/i2c2eebw.o ${OBJECTDIR}/I2c2/i2c2eecr.o ${OBJECTDIR}/I2c2/i2c2eepw.o ${OBJECTDIR}/I2c2/i2c2eerr.o ${OBJECTDIR}/I2c2/i2c2eesr.o ${OBJECTDIR}/I2c2/i2c2gets.o ${OBJECTDIR}/I2c2/i2c2idle.o ${OBJECTDIR}/I2c2/i2c2nack.o ${OBJECTDIR}/I2c2/i2c2open.o ${OBJECTDIR}/I2c2/i2c2puts.o ${OBJECTDIR}/I2c2/i2c2read.o ${OBJECTDIR}/I2c2/i2c2rstr.o ${OBJECTDIR}/I2c2/i2c2stop.o ${OBJECTDIR}/I2c2/i2c2strt.o ${OBJECTDIR}/I2c2/i2c2writ.o ${OBJECTDIR}/Microchip/Common/TimeDelay.o ${OBJECTDIR}/Microchip/Common/timer.o ${OBJECTDIR}/Microchip/PIC18Salloc/salloc.o ${OBJECTDIR}/Microchip/TCPIPStack/Announce.o ${OBJECTDIR}/Microchip/TCPIPStack/ARCFOUR.o ${OBJECTDIR}/Microchip/TCPIPStack/ARP.o ${OBJECTDIR}/Microchip/TCPIPStack/AutoIP.o ${OBJECTDIR}/Microchip/TCPIPStack/BerkeleyAPI.o ${OBJECTDIR}/Microchip/TCPIPStack/BigInt.o ${OBJECTDIR}/Microchip/TCPIPStack/Delay.o ${OBJECTDIR}/Microchip/TCPIPStack/DHCP.o ${OBJECTDIR}/Microchip/TCPIPStack/DHCPs.o ${OBJECTDIR}/Microchip/TCPIPStack/DNS.o ${OBJECTDIR}/Microchip/TCPIPStack/DNSs.o ${OBJECTDIR}/Microchip/TCPIPStack/DynDNS.o ${OBJECTDIR}/Microchip/TCPIPStack/ETH97J60.o ${OBJECTDIR}/Microchip/TCPIPStack/FileSystem.o ${OBJECTDIR}/Microchip/TCPIPStack/FTP.o ${OBJECTDIR}/Microchip/TCPIPStack/Hashes.o ${OBJECTDIR}/Microchip/TCPIPStack/Helpers.o ${OBJECTDIR}/Microchip/TCPIPStack/HTTP.o ${OBJECTDIR}/Microchip/TCPIPStack/HTTP2.o ${OBJECTDIR}/Microchip/TCPIPStack/ICMP.o ${OBJECTDIR}/Microchip/TCPIPStack/IP.o ${OBJECTDIR}/Microchip/TCPIPStack/LCDBlocking.o ${OBJECTDIR}/Microchip/TCPIPStack/MPFS.o ${OBJECTDIR}/Microchip/TCPIPStack/MPFS2.o ${OBJECTDIR}/Microchip/TCPIPStack/NBNS.o ${OBJECTDIR}/Microchip/TCPIPStack/Random.o ${OBJECTDIR}/Microchip/TCPIPStack/Reboot.o ${OBJECTDIR}/Microchip/TCPIPStack/RSA.o ${OBJECTDIR}/Microchip/TCPIPStack/SMTP.o ${OBJECTDIR}/Microchip/TCPIPStack/SNMP.o ${OBJECTDIR}/Microchip/TCPIPStack/SNMPv3.o ${OBJECTDIR}/Microchip/TCPIPStack/SNMPv3USM.o ${OBJECTDIR}/Microchip/TCPIPStack/SNTP.o ${OBJECTDIR}/Microchip/TCPIPStack/SPIEEPROM.o ${OBJECTDIR}/Microchip/TCPIPStack/SPIFlash.o ${OBJECTDIR}/Microchip/TCPIPStack/SPIRAM.o ${OBJECTDIR}/Microchip/TCPIPStack/SSL.o ${OBJECTDIR}/Microchip/TCPIPStack/StackTsk.o ${OBJECTDIR}/Microchip/TCPIPStack/TCP.o ${OBJECTDIR}/Microchip/TCPIPStack/TCPPerformanceTest.o ${OBJECTDIR}/Microchip/TCPIPStack/Telnet.o ${OBJECTDIR}/Microchip/TCPIPStack/TFTPc.o ${OBJECTDIR}/Microchip/TCPIPStack/Tick.o ${OBJECTDIR}/Microchip/TCPIPStack/UART.o ${OBJECTDIR}/Microchip/TCPIPStack/UART2TCPBridge.o ${OBJECTDIR}/Microchip/TCPIPStack/UDP.o ${OBJECTDIR}/Microchip/TCPIPStack/UDPPerformanceTest.o ${OBJECTDIR}/modbus/General.o ${OBJECTDIR}/modbus/Init.o ${OBJECTDIR}/modbus/Interrupts.o ${OBJECTDIR}/modbus/PetitModbus.o ${OBJECTDIR}/modbus/PetitModbusPort.o ${OBJECTDIR}/modbus/PetitRegConstant.o ${OBJECTDIR}/Command_Machine.o ${OBJECTDIR}/Diagnostic_ret.o ${OBJECTDIR}/Shift_Register.o ${OBJECTDIR}/Var_Out.o ${OBJECTDIR}/FiddleYard.o ${OBJECTDIR}/eusart1.o ${OBJECTDIR}/eusart2.o ${OBJECTDIR}/State_Machine.o

# Source Files
SOURCEFILES=I2c2/i2c1ack.c I2c2/i2c1clos.c I2c2/i2c1dtrd.c I2c2/i2c1eeap.c I2c2/i2c1eebw.c I2c2/i2c1eecr.c I2c2/i2c1eepw.c I2c2/i2c1eerr.c I2c2/i2c1eesr.c I2c2/i2c1gets.c I2c2/i2c1idle.c I2c2/i2c1nack.c I2c2/i2c1open.c I2c2/i2c1puts.c I2c2/i2c1read.c I2c2/i2c1rstr.c I2c2/i2c1stop.c I2c2/i2c1strt.c I2c2/i2c1writ.c I2c2/i2c2ack.c I2c2/i2c2clos.c I2c2/i2c2dtrd.c I2c2/i2c2eeap.c I2c2/i2c2eebw.c I2c2/i2c2eecr.c I2c2/i2c2eepw.c I2c2/i2c2eerr.c I2c2/i2c2eesr.c I2c2/i2c2gets.c I2c2/i2c2idle.c I2c2/i2c2nack.c I2c2/i2c2open.c I2c2/i2c2puts.c I2c2/i2c2read.c I2c2/i2c2rstr.c I2c2/i2c2stop.c I2c2/i2c2strt.c I2c2/i2c2writ.c Microchip/Common/TimeDelay.c Microchip/Common/timer.c Microchip/PIC18Salloc/salloc.c Microchip/TCPIPStack/Announce.c Microchip/TCPIPStack/ARCFOUR.c Microchip/TCPIPStack/ARP.c Microchip/TCPIPStack/AutoIP.c Microchip/TCPIPStack/BerkeleyAPI.c Microchip/TCPIPStack/BigInt.c Microchip/TCPIPStack/Delay.c Microchip/TCPIPStack/DHCP.c Microchip/TCPIPStack/DHCPs.c Microchip/TCPIPStack/DNS.c Microchip/TCPIPStack/DNSs.c Microchip/TCPIPStack/DynDNS.c Microchip/TCPIPStack/ETH97J60.c Microchip/TCPIPStack/FileSystem.c Microchip/TCPIPStack/FTP.c Microchip/TCPIPStack/Hashes.c Microchip/TCPIPStack/Helpers.c Microchip/TCPIPStack/HTTP.c Microchip/TCPIPStack/HTTP2.c Microchip/TCPIPStack/ICMP.c Microchip/TCPIPStack/IP.c Microchip/TCPIPStack/LCDBlocking.c Microchip/TCPIPStack/MPFS.c Microchip/TCPIPStack/MPFS2.c Microchip/TCPIPStack/NBNS.c Microchip/TCPIPStack/Random.c Microchip/TCPIPStack/Reboot.c Microchip/TCPIPStack/RSA.c Microchip/TCPIPStack/SMTP.c Microchip/TCPIPStack/SNMP.c Microchip/TCPIPStack/SNMPv3.c Microchip/TCPIPStack/SNMPv3USM.c Microchip/TCPIPStack/SNTP.c Microchip/TCPIPStack/SPIEEPROM.c Microchip/TCPIPStack/SPIFlash.c Microchip/TCPIPStack/SPIRAM.c Microchip/TCPIPStack/SSL.c Microchip/TCPIPStack/StackTsk.c Microchip/TCPIPStack/TCP.c Microchip/TCPIPStack/TCPPerformanceTest.c Microchip/TCPIPStack/Telnet.c Microchip/TCPIPStack/TFTPc.c Microchip/TCPIPStack/Tick.c Microchip/TCPIPStack/UART.c Microchip/TCPIPStack/UART2TCPBridge.c Microchip/TCPIPStack/UDP.c Microchip/TCPIPStack/UDPPerformanceTest.c modbus/General.c modbus/Init.c modbus/Interrupts.c modbus/PetitModbus.c modbus/PetitModbusPort.c modbus/PetitRegConstant.c Command_Machine.c Diagnostic_ret.c Shift_Register.c Var_Out.c FiddleYard.c eusart1.c eusart2.c State_Machine.c


CFLAGS=
ASFLAGS=
LDLIBSOPTIONS=

############# Tool locations ##########################################
# If you copy a project from one host to another, the path where the  #
# compiler is installed may be different.                             #
# If you open this project with MPLAB X in the new host, this         #
# makefile will be regenerated and the paths will be corrected.       #
#######################################################################
# fixDeps replaces a bunch of sed/cat/printf statements that slow down the build
FIXDEPS=fixDeps

.build-conf:  ${BUILD_SUBPROJECTS}
ifneq ($(INFORMATION_MESSAGE), )
	@echo $(INFORMATION_MESSAGE)
endif
	${MAKE}  -f nbproject/Makefile-default.mk dist/${CND_CONF}/${IMAGE_TYPE}/TrackController3.X.${IMAGE_TYPE}.${OUTPUT_SUFFIX}

MP_PROCESSOR_OPTION=18F97J60
MP_PROCESSOR_OPTION_LD=18f97j60
MP_LINKER_DEBUG_OPTION=  -u_DEBUGSTACK
# ------------------------------------------------------------------------------------
# Rules for buildStep: assemble
ifeq ($(TYPE_IMAGE), DEBUG_RUN)
else
endif

# ------------------------------------------------------------------------------------
# Rules for buildStep: compile
ifeq ($(TYPE_IMAGE), DEBUG_RUN)
${OBJECTDIR}/I2c2/i2c1ack.o: I2c2/i2c1ack.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1ack.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1ack.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1ack.o   I2c2/i2c1ack.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1ack.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1ack.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1clos.o: I2c2/i2c1clos.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1clos.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1clos.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1clos.o   I2c2/i2c1clos.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1clos.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1clos.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1dtrd.o: I2c2/i2c1dtrd.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1dtrd.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1dtrd.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1dtrd.o   I2c2/i2c1dtrd.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1dtrd.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1dtrd.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1eeap.o: I2c2/i2c1eeap.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1eeap.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1eeap.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1eeap.o   I2c2/i2c1eeap.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1eeap.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1eeap.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1eebw.o: I2c2/i2c1eebw.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1eebw.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1eebw.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1eebw.o   I2c2/i2c1eebw.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1eebw.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1eebw.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1eecr.o: I2c2/i2c1eecr.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1eecr.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1eecr.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1eecr.o   I2c2/i2c1eecr.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1eecr.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1eecr.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1eepw.o: I2c2/i2c1eepw.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1eepw.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1eepw.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1eepw.o   I2c2/i2c1eepw.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1eepw.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1eepw.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1eerr.o: I2c2/i2c1eerr.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1eerr.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1eerr.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1eerr.o   I2c2/i2c1eerr.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1eerr.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1eerr.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1eesr.o: I2c2/i2c1eesr.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1eesr.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1eesr.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1eesr.o   I2c2/i2c1eesr.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1eesr.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1eesr.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1gets.o: I2c2/i2c1gets.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1gets.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1gets.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1gets.o   I2c2/i2c1gets.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1gets.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1gets.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1idle.o: I2c2/i2c1idle.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1idle.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1idle.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1idle.o   I2c2/i2c1idle.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1idle.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1idle.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1nack.o: I2c2/i2c1nack.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1nack.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1nack.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1nack.o   I2c2/i2c1nack.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1nack.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1nack.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1open.o: I2c2/i2c1open.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1open.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1open.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1open.o   I2c2/i2c1open.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1open.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1open.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1puts.o: I2c2/i2c1puts.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1puts.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1puts.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1puts.o   I2c2/i2c1puts.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1puts.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1puts.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1read.o: I2c2/i2c1read.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1read.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1read.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1read.o   I2c2/i2c1read.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1read.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1read.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1rstr.o: I2c2/i2c1rstr.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1rstr.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1rstr.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1rstr.o   I2c2/i2c1rstr.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1rstr.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1rstr.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1stop.o: I2c2/i2c1stop.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1stop.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1stop.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1stop.o   I2c2/i2c1stop.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1stop.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1stop.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1strt.o: I2c2/i2c1strt.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1strt.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1strt.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1strt.o   I2c2/i2c1strt.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1strt.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1strt.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1writ.o: I2c2/i2c1writ.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1writ.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1writ.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1writ.o   I2c2/i2c1writ.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1writ.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1writ.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2ack.o: I2c2/i2c2ack.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2ack.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2ack.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2ack.o   I2c2/i2c2ack.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2ack.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2ack.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2clos.o: I2c2/i2c2clos.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2clos.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2clos.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2clos.o   I2c2/i2c2clos.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2clos.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2clos.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2dtrd.o: I2c2/i2c2dtrd.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2dtrd.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2dtrd.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2dtrd.o   I2c2/i2c2dtrd.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2dtrd.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2dtrd.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2eeap.o: I2c2/i2c2eeap.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2eeap.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2eeap.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2eeap.o   I2c2/i2c2eeap.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2eeap.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2eeap.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2eebw.o: I2c2/i2c2eebw.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2eebw.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2eebw.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2eebw.o   I2c2/i2c2eebw.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2eebw.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2eebw.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2eecr.o: I2c2/i2c2eecr.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2eecr.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2eecr.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2eecr.o   I2c2/i2c2eecr.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2eecr.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2eecr.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2eepw.o: I2c2/i2c2eepw.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2eepw.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2eepw.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2eepw.o   I2c2/i2c2eepw.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2eepw.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2eepw.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2eerr.o: I2c2/i2c2eerr.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2eerr.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2eerr.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2eerr.o   I2c2/i2c2eerr.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2eerr.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2eerr.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2eesr.o: I2c2/i2c2eesr.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2eesr.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2eesr.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2eesr.o   I2c2/i2c2eesr.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2eesr.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2eesr.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2gets.o: I2c2/i2c2gets.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2gets.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2gets.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2gets.o   I2c2/i2c2gets.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2gets.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2gets.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2idle.o: I2c2/i2c2idle.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2idle.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2idle.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2idle.o   I2c2/i2c2idle.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2idle.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2idle.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2nack.o: I2c2/i2c2nack.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2nack.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2nack.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2nack.o   I2c2/i2c2nack.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2nack.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2nack.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2open.o: I2c2/i2c2open.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2open.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2open.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2open.o   I2c2/i2c2open.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2open.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2open.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2puts.o: I2c2/i2c2puts.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2puts.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2puts.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2puts.o   I2c2/i2c2puts.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2puts.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2puts.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2read.o: I2c2/i2c2read.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2read.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2read.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2read.o   I2c2/i2c2read.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2read.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2read.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2rstr.o: I2c2/i2c2rstr.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2rstr.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2rstr.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2rstr.o   I2c2/i2c2rstr.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2rstr.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2rstr.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2stop.o: I2c2/i2c2stop.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2stop.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2stop.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2stop.o   I2c2/i2c2stop.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2stop.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2stop.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2strt.o: I2c2/i2c2strt.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2strt.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2strt.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2strt.o   I2c2/i2c2strt.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2strt.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2strt.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2writ.o: I2c2/i2c2writ.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2writ.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2writ.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2writ.o   I2c2/i2c2writ.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2writ.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2writ.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/Common/TimeDelay.o: Microchip/Common/TimeDelay.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/Common" 
	@${RM} ${OBJECTDIR}/Microchip/Common/TimeDelay.o.d 
	@${RM} ${OBJECTDIR}/Microchip/Common/TimeDelay.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/Common/TimeDelay.o   Microchip/Common/TimeDelay.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/Common/TimeDelay.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/Common/TimeDelay.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/Common/timer.o: Microchip/Common/timer.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/Common" 
	@${RM} ${OBJECTDIR}/Microchip/Common/timer.o.d 
	@${RM} ${OBJECTDIR}/Microchip/Common/timer.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/Common/timer.o   Microchip/Common/timer.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/Common/timer.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/Common/timer.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/PIC18Salloc/salloc.o: Microchip/PIC18Salloc/salloc.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/PIC18Salloc" 
	@${RM} ${OBJECTDIR}/Microchip/PIC18Salloc/salloc.o.d 
	@${RM} ${OBJECTDIR}/Microchip/PIC18Salloc/salloc.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/PIC18Salloc/salloc.o   Microchip/PIC18Salloc/salloc.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/PIC18Salloc/salloc.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/PIC18Salloc/salloc.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/Announce.o: Microchip/TCPIPStack/Announce.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Announce.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Announce.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/Announce.o   Microchip/TCPIPStack/Announce.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/Announce.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/Announce.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/ARCFOUR.o: Microchip/TCPIPStack/ARCFOUR.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/ARCFOUR.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/ARCFOUR.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/ARCFOUR.o   Microchip/TCPIPStack/ARCFOUR.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/ARCFOUR.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/ARCFOUR.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/ARP.o: Microchip/TCPIPStack/ARP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/ARP.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/ARP.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/ARP.o   Microchip/TCPIPStack/ARP.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/ARP.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/ARP.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/AutoIP.o: Microchip/TCPIPStack/AutoIP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/AutoIP.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/AutoIP.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/AutoIP.o   Microchip/TCPIPStack/AutoIP.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/AutoIP.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/AutoIP.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/BerkeleyAPI.o: Microchip/TCPIPStack/BerkeleyAPI.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/BerkeleyAPI.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/BerkeleyAPI.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/BerkeleyAPI.o   Microchip/TCPIPStack/BerkeleyAPI.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/BerkeleyAPI.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/BerkeleyAPI.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/BigInt.o: Microchip/TCPIPStack/BigInt.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/BigInt.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/BigInt.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/BigInt.o   Microchip/TCPIPStack/BigInt.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/BigInt.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/BigInt.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/Delay.o: Microchip/TCPIPStack/Delay.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Delay.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Delay.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/Delay.o   Microchip/TCPIPStack/Delay.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/Delay.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/Delay.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/DHCP.o: Microchip/TCPIPStack/DHCP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/DHCP.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/DHCP.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/DHCP.o   Microchip/TCPIPStack/DHCP.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/DHCP.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/DHCP.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/DHCPs.o: Microchip/TCPIPStack/DHCPs.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/DHCPs.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/DHCPs.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/DHCPs.o   Microchip/TCPIPStack/DHCPs.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/DHCPs.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/DHCPs.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/DNS.o: Microchip/TCPIPStack/DNS.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/DNS.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/DNS.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/DNS.o   Microchip/TCPIPStack/DNS.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/DNS.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/DNS.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/DNSs.o: Microchip/TCPIPStack/DNSs.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/DNSs.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/DNSs.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/DNSs.o   Microchip/TCPIPStack/DNSs.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/DNSs.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/DNSs.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/DynDNS.o: Microchip/TCPIPStack/DynDNS.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/DynDNS.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/DynDNS.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/DynDNS.o   Microchip/TCPIPStack/DynDNS.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/DynDNS.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/DynDNS.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/ETH97J60.o: Microchip/TCPIPStack/ETH97J60.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/ETH97J60.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/ETH97J60.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/ETH97J60.o   Microchip/TCPIPStack/ETH97J60.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/ETH97J60.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/ETH97J60.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/FileSystem.o: Microchip/TCPIPStack/FileSystem.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/FileSystem.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/FileSystem.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/FileSystem.o   Microchip/TCPIPStack/FileSystem.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/FileSystem.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/FileSystem.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/FTP.o: Microchip/TCPIPStack/FTP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/FTP.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/FTP.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/FTP.o   Microchip/TCPIPStack/FTP.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/FTP.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/FTP.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/Hashes.o: Microchip/TCPIPStack/Hashes.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Hashes.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Hashes.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/Hashes.o   Microchip/TCPIPStack/Hashes.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/Hashes.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/Hashes.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/Helpers.o: Microchip/TCPIPStack/Helpers.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Helpers.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Helpers.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/Helpers.o   Microchip/TCPIPStack/Helpers.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/Helpers.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/Helpers.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/HTTP.o: Microchip/TCPIPStack/HTTP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/HTTP.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/HTTP.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/HTTP.o   Microchip/TCPIPStack/HTTP.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/HTTP.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/HTTP.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/HTTP2.o: Microchip/TCPIPStack/HTTP2.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/HTTP2.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/HTTP2.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/HTTP2.o   Microchip/TCPIPStack/HTTP2.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/HTTP2.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/HTTP2.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/ICMP.o: Microchip/TCPIPStack/ICMP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/ICMP.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/ICMP.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/ICMP.o   Microchip/TCPIPStack/ICMP.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/ICMP.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/ICMP.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/IP.o: Microchip/TCPIPStack/IP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/IP.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/IP.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/IP.o   Microchip/TCPIPStack/IP.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/IP.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/IP.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/LCDBlocking.o: Microchip/TCPIPStack/LCDBlocking.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/LCDBlocking.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/LCDBlocking.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/LCDBlocking.o   Microchip/TCPIPStack/LCDBlocking.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/LCDBlocking.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/LCDBlocking.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/MPFS.o: Microchip/TCPIPStack/MPFS.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/MPFS.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/MPFS.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/MPFS.o   Microchip/TCPIPStack/MPFS.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/MPFS.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/MPFS.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/MPFS2.o: Microchip/TCPIPStack/MPFS2.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/MPFS2.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/MPFS2.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/MPFS2.o   Microchip/TCPIPStack/MPFS2.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/MPFS2.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/MPFS2.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/NBNS.o: Microchip/TCPIPStack/NBNS.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/NBNS.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/NBNS.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/NBNS.o   Microchip/TCPIPStack/NBNS.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/NBNS.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/NBNS.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/Random.o: Microchip/TCPIPStack/Random.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Random.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Random.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/Random.o   Microchip/TCPIPStack/Random.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/Random.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/Random.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/Reboot.o: Microchip/TCPIPStack/Reboot.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Reboot.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Reboot.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/Reboot.o   Microchip/TCPIPStack/Reboot.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/Reboot.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/Reboot.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/RSA.o: Microchip/TCPIPStack/RSA.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/RSA.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/RSA.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/RSA.o   Microchip/TCPIPStack/RSA.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/RSA.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/RSA.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/SMTP.o: Microchip/TCPIPStack/SMTP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SMTP.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SMTP.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/SMTP.o   Microchip/TCPIPStack/SMTP.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/SMTP.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/SMTP.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/SNMP.o: Microchip/TCPIPStack/SNMP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SNMP.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SNMP.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/SNMP.o   Microchip/TCPIPStack/SNMP.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/SNMP.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/SNMP.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/SNMPv3.o: Microchip/TCPIPStack/SNMPv3.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SNMPv3.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SNMPv3.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/SNMPv3.o   Microchip/TCPIPStack/SNMPv3.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/SNMPv3.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/SNMPv3.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/SNMPv3USM.o: Microchip/TCPIPStack/SNMPv3USM.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SNMPv3USM.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SNMPv3USM.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/SNMPv3USM.o   Microchip/TCPIPStack/SNMPv3USM.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/SNMPv3USM.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/SNMPv3USM.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/SNTP.o: Microchip/TCPIPStack/SNTP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SNTP.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SNTP.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/SNTP.o   Microchip/TCPIPStack/SNTP.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/SNTP.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/SNTP.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/SPIEEPROM.o: Microchip/TCPIPStack/SPIEEPROM.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SPIEEPROM.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SPIEEPROM.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/SPIEEPROM.o   Microchip/TCPIPStack/SPIEEPROM.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/SPIEEPROM.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/SPIEEPROM.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/SPIFlash.o: Microchip/TCPIPStack/SPIFlash.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SPIFlash.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SPIFlash.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/SPIFlash.o   Microchip/TCPIPStack/SPIFlash.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/SPIFlash.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/SPIFlash.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/SPIRAM.o: Microchip/TCPIPStack/SPIRAM.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SPIRAM.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SPIRAM.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/SPIRAM.o   Microchip/TCPIPStack/SPIRAM.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/SPIRAM.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/SPIRAM.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/SSL.o: Microchip/TCPIPStack/SSL.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SSL.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SSL.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/SSL.o   Microchip/TCPIPStack/SSL.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/SSL.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/SSL.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/StackTsk.o: Microchip/TCPIPStack/StackTsk.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/StackTsk.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/StackTsk.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/StackTsk.o   Microchip/TCPIPStack/StackTsk.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/StackTsk.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/StackTsk.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/TCP.o: Microchip/TCPIPStack/TCP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/TCP.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/TCP.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/TCP.o   Microchip/TCPIPStack/TCP.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/TCP.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/TCP.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/TCPPerformanceTest.o: Microchip/TCPIPStack/TCPPerformanceTest.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/TCPPerformanceTest.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/TCPPerformanceTest.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/TCPPerformanceTest.o   Microchip/TCPIPStack/TCPPerformanceTest.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/TCPPerformanceTest.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/TCPPerformanceTest.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/Telnet.o: Microchip/TCPIPStack/Telnet.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Telnet.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Telnet.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/Telnet.o   Microchip/TCPIPStack/Telnet.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/Telnet.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/Telnet.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/TFTPc.o: Microchip/TCPIPStack/TFTPc.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/TFTPc.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/TFTPc.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/TFTPc.o   Microchip/TCPIPStack/TFTPc.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/TFTPc.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/TFTPc.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/Tick.o: Microchip/TCPIPStack/Tick.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Tick.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Tick.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/Tick.o   Microchip/TCPIPStack/Tick.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/Tick.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/Tick.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/UART.o: Microchip/TCPIPStack/UART.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/UART.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/UART.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/UART.o   Microchip/TCPIPStack/UART.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/UART.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/UART.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/UART2TCPBridge.o: Microchip/TCPIPStack/UART2TCPBridge.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/UART2TCPBridge.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/UART2TCPBridge.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/UART2TCPBridge.o   Microchip/TCPIPStack/UART2TCPBridge.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/UART2TCPBridge.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/UART2TCPBridge.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/UDP.o: Microchip/TCPIPStack/UDP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/UDP.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/UDP.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/UDP.o   Microchip/TCPIPStack/UDP.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/UDP.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/UDP.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/UDPPerformanceTest.o: Microchip/TCPIPStack/UDPPerformanceTest.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/UDPPerformanceTest.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/UDPPerformanceTest.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/UDPPerformanceTest.o   Microchip/TCPIPStack/UDPPerformanceTest.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/UDPPerformanceTest.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/UDPPerformanceTest.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/modbus/General.o: modbus/General.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus" 
	@${RM} ${OBJECTDIR}/modbus/General.o.d 
	@${RM} ${OBJECTDIR}/modbus/General.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/modbus/General.o   modbus/General.c 
	@${DEP_GEN} -d ${OBJECTDIR}/modbus/General.o 
	@${FIXDEPS} "${OBJECTDIR}/modbus/General.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/modbus/Init.o: modbus/Init.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus" 
	@${RM} ${OBJECTDIR}/modbus/Init.o.d 
	@${RM} ${OBJECTDIR}/modbus/Init.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/modbus/Init.o   modbus/Init.c 
	@${DEP_GEN} -d ${OBJECTDIR}/modbus/Init.o 
	@${FIXDEPS} "${OBJECTDIR}/modbus/Init.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/modbus/Interrupts.o: modbus/Interrupts.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus" 
	@${RM} ${OBJECTDIR}/modbus/Interrupts.o.d 
	@${RM} ${OBJECTDIR}/modbus/Interrupts.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/modbus/Interrupts.o   modbus/Interrupts.c 
	@${DEP_GEN} -d ${OBJECTDIR}/modbus/Interrupts.o 
	@${FIXDEPS} "${OBJECTDIR}/modbus/Interrupts.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/modbus/PetitModbus.o: modbus/PetitModbus.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus" 
	@${RM} ${OBJECTDIR}/modbus/PetitModbus.o.d 
	@${RM} ${OBJECTDIR}/modbus/PetitModbus.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/modbus/PetitModbus.o   modbus/PetitModbus.c 
	@${DEP_GEN} -d ${OBJECTDIR}/modbus/PetitModbus.o 
	@${FIXDEPS} "${OBJECTDIR}/modbus/PetitModbus.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/modbus/PetitModbusPort.o: modbus/PetitModbusPort.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus" 
	@${RM} ${OBJECTDIR}/modbus/PetitModbusPort.o.d 
	@${RM} ${OBJECTDIR}/modbus/PetitModbusPort.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/modbus/PetitModbusPort.o   modbus/PetitModbusPort.c 
	@${DEP_GEN} -d ${OBJECTDIR}/modbus/PetitModbusPort.o 
	@${FIXDEPS} "${OBJECTDIR}/modbus/PetitModbusPort.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/modbus/PetitRegConstant.o: modbus/PetitRegConstant.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus" 
	@${RM} ${OBJECTDIR}/modbus/PetitRegConstant.o.d 
	@${RM} ${OBJECTDIR}/modbus/PetitRegConstant.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/modbus/PetitRegConstant.o   modbus/PetitRegConstant.c 
	@${DEP_GEN} -d ${OBJECTDIR}/modbus/PetitRegConstant.o 
	@${FIXDEPS} "${OBJECTDIR}/modbus/PetitRegConstant.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Command_Machine.o: Command_Machine.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/Command_Machine.o.d 
	@${RM} ${OBJECTDIR}/Command_Machine.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Command_Machine.o   Command_Machine.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Command_Machine.o 
	@${FIXDEPS} "${OBJECTDIR}/Command_Machine.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Diagnostic_ret.o: Diagnostic_ret.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/Diagnostic_ret.o.d 
	@${RM} ${OBJECTDIR}/Diagnostic_ret.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Diagnostic_ret.o   Diagnostic_ret.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Diagnostic_ret.o 
	@${FIXDEPS} "${OBJECTDIR}/Diagnostic_ret.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Shift_Register.o: Shift_Register.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/Shift_Register.o.d 
	@${RM} ${OBJECTDIR}/Shift_Register.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Shift_Register.o   Shift_Register.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Shift_Register.o 
	@${FIXDEPS} "${OBJECTDIR}/Shift_Register.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Var_Out.o: Var_Out.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/Var_Out.o.d 
	@${RM} ${OBJECTDIR}/Var_Out.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Var_Out.o   Var_Out.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Var_Out.o 
	@${FIXDEPS} "${OBJECTDIR}/Var_Out.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/FiddleYard.o: FiddleYard.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/FiddleYard.o.d 
	@${RM} ${OBJECTDIR}/FiddleYard.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/FiddleYard.o   FiddleYard.c 
	@${DEP_GEN} -d ${OBJECTDIR}/FiddleYard.o 
	@${FIXDEPS} "${OBJECTDIR}/FiddleYard.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/eusart1.o: eusart1.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/eusart1.o.d 
	@${RM} ${OBJECTDIR}/eusart1.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/eusart1.o   eusart1.c 
	@${DEP_GEN} -d ${OBJECTDIR}/eusart1.o 
	@${FIXDEPS} "${OBJECTDIR}/eusart1.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/eusart2.o: eusart2.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/eusart2.o.d 
	@${RM} ${OBJECTDIR}/eusart2.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/eusart2.o   eusart2.c 
	@${DEP_GEN} -d ${OBJECTDIR}/eusart2.o 
	@${FIXDEPS} "${OBJECTDIR}/eusart2.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/State_Machine.o: State_Machine.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/State_Machine.o.d 
	@${RM} ${OBJECTDIR}/State_Machine.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -D__DEBUG -D__MPLAB_DEBUGGER_ICD3=1 -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/State_Machine.o   State_Machine.c 
	@${DEP_GEN} -d ${OBJECTDIR}/State_Machine.o 
	@${FIXDEPS} "${OBJECTDIR}/State_Machine.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
else
${OBJECTDIR}/I2c2/i2c1ack.o: I2c2/i2c1ack.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1ack.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1ack.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1ack.o   I2c2/i2c1ack.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1ack.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1ack.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1clos.o: I2c2/i2c1clos.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1clos.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1clos.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1clos.o   I2c2/i2c1clos.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1clos.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1clos.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1dtrd.o: I2c2/i2c1dtrd.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1dtrd.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1dtrd.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1dtrd.o   I2c2/i2c1dtrd.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1dtrd.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1dtrd.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1eeap.o: I2c2/i2c1eeap.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1eeap.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1eeap.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1eeap.o   I2c2/i2c1eeap.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1eeap.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1eeap.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1eebw.o: I2c2/i2c1eebw.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1eebw.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1eebw.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1eebw.o   I2c2/i2c1eebw.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1eebw.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1eebw.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1eecr.o: I2c2/i2c1eecr.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1eecr.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1eecr.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1eecr.o   I2c2/i2c1eecr.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1eecr.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1eecr.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1eepw.o: I2c2/i2c1eepw.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1eepw.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1eepw.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1eepw.o   I2c2/i2c1eepw.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1eepw.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1eepw.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1eerr.o: I2c2/i2c1eerr.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1eerr.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1eerr.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1eerr.o   I2c2/i2c1eerr.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1eerr.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1eerr.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1eesr.o: I2c2/i2c1eesr.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1eesr.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1eesr.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1eesr.o   I2c2/i2c1eesr.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1eesr.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1eesr.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1gets.o: I2c2/i2c1gets.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1gets.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1gets.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1gets.o   I2c2/i2c1gets.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1gets.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1gets.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1idle.o: I2c2/i2c1idle.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1idle.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1idle.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1idle.o   I2c2/i2c1idle.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1idle.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1idle.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1nack.o: I2c2/i2c1nack.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1nack.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1nack.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1nack.o   I2c2/i2c1nack.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1nack.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1nack.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1open.o: I2c2/i2c1open.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1open.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1open.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1open.o   I2c2/i2c1open.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1open.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1open.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1puts.o: I2c2/i2c1puts.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1puts.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1puts.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1puts.o   I2c2/i2c1puts.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1puts.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1puts.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1read.o: I2c2/i2c1read.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1read.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1read.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1read.o   I2c2/i2c1read.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1read.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1read.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1rstr.o: I2c2/i2c1rstr.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1rstr.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1rstr.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1rstr.o   I2c2/i2c1rstr.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1rstr.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1rstr.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1stop.o: I2c2/i2c1stop.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1stop.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1stop.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1stop.o   I2c2/i2c1stop.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1stop.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1stop.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1strt.o: I2c2/i2c1strt.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1strt.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1strt.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1strt.o   I2c2/i2c1strt.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1strt.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1strt.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c1writ.o: I2c2/i2c1writ.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c1writ.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c1writ.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c1writ.o   I2c2/i2c1writ.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c1writ.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c1writ.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2ack.o: I2c2/i2c2ack.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2ack.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2ack.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2ack.o   I2c2/i2c2ack.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2ack.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2ack.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2clos.o: I2c2/i2c2clos.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2clos.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2clos.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2clos.o   I2c2/i2c2clos.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2clos.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2clos.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2dtrd.o: I2c2/i2c2dtrd.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2dtrd.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2dtrd.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2dtrd.o   I2c2/i2c2dtrd.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2dtrd.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2dtrd.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2eeap.o: I2c2/i2c2eeap.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2eeap.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2eeap.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2eeap.o   I2c2/i2c2eeap.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2eeap.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2eeap.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2eebw.o: I2c2/i2c2eebw.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2eebw.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2eebw.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2eebw.o   I2c2/i2c2eebw.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2eebw.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2eebw.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2eecr.o: I2c2/i2c2eecr.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2eecr.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2eecr.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2eecr.o   I2c2/i2c2eecr.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2eecr.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2eecr.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2eepw.o: I2c2/i2c2eepw.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2eepw.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2eepw.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2eepw.o   I2c2/i2c2eepw.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2eepw.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2eepw.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2eerr.o: I2c2/i2c2eerr.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2eerr.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2eerr.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2eerr.o   I2c2/i2c2eerr.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2eerr.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2eerr.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2eesr.o: I2c2/i2c2eesr.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2eesr.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2eesr.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2eesr.o   I2c2/i2c2eesr.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2eesr.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2eesr.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2gets.o: I2c2/i2c2gets.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2gets.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2gets.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2gets.o   I2c2/i2c2gets.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2gets.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2gets.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2idle.o: I2c2/i2c2idle.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2idle.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2idle.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2idle.o   I2c2/i2c2idle.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2idle.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2idle.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2nack.o: I2c2/i2c2nack.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2nack.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2nack.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2nack.o   I2c2/i2c2nack.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2nack.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2nack.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2open.o: I2c2/i2c2open.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2open.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2open.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2open.o   I2c2/i2c2open.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2open.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2open.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2puts.o: I2c2/i2c2puts.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2puts.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2puts.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2puts.o   I2c2/i2c2puts.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2puts.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2puts.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2read.o: I2c2/i2c2read.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2read.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2read.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2read.o   I2c2/i2c2read.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2read.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2read.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2rstr.o: I2c2/i2c2rstr.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2rstr.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2rstr.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2rstr.o   I2c2/i2c2rstr.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2rstr.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2rstr.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2stop.o: I2c2/i2c2stop.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2stop.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2stop.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2stop.o   I2c2/i2c2stop.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2stop.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2stop.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2strt.o: I2c2/i2c2strt.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2strt.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2strt.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2strt.o   I2c2/i2c2strt.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2strt.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2strt.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/I2c2/i2c2writ.o: I2c2/i2c2writ.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/I2c2" 
	@${RM} ${OBJECTDIR}/I2c2/i2c2writ.o.d 
	@${RM} ${OBJECTDIR}/I2c2/i2c2writ.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/I2c2/i2c2writ.o   I2c2/i2c2writ.c 
	@${DEP_GEN} -d ${OBJECTDIR}/I2c2/i2c2writ.o 
	@${FIXDEPS} "${OBJECTDIR}/I2c2/i2c2writ.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/Common/TimeDelay.o: Microchip/Common/TimeDelay.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/Common" 
	@${RM} ${OBJECTDIR}/Microchip/Common/TimeDelay.o.d 
	@${RM} ${OBJECTDIR}/Microchip/Common/TimeDelay.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/Common/TimeDelay.o   Microchip/Common/TimeDelay.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/Common/TimeDelay.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/Common/TimeDelay.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/Common/timer.o: Microchip/Common/timer.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/Common" 
	@${RM} ${OBJECTDIR}/Microchip/Common/timer.o.d 
	@${RM} ${OBJECTDIR}/Microchip/Common/timer.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/Common/timer.o   Microchip/Common/timer.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/Common/timer.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/Common/timer.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/PIC18Salloc/salloc.o: Microchip/PIC18Salloc/salloc.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/PIC18Salloc" 
	@${RM} ${OBJECTDIR}/Microchip/PIC18Salloc/salloc.o.d 
	@${RM} ${OBJECTDIR}/Microchip/PIC18Salloc/salloc.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/PIC18Salloc/salloc.o   Microchip/PIC18Salloc/salloc.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/PIC18Salloc/salloc.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/PIC18Salloc/salloc.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/Announce.o: Microchip/TCPIPStack/Announce.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Announce.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Announce.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/Announce.o   Microchip/TCPIPStack/Announce.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/Announce.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/Announce.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/ARCFOUR.o: Microchip/TCPIPStack/ARCFOUR.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/ARCFOUR.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/ARCFOUR.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/ARCFOUR.o   Microchip/TCPIPStack/ARCFOUR.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/ARCFOUR.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/ARCFOUR.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/ARP.o: Microchip/TCPIPStack/ARP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/ARP.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/ARP.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/ARP.o   Microchip/TCPIPStack/ARP.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/ARP.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/ARP.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/AutoIP.o: Microchip/TCPIPStack/AutoIP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/AutoIP.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/AutoIP.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/AutoIP.o   Microchip/TCPIPStack/AutoIP.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/AutoIP.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/AutoIP.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/BerkeleyAPI.o: Microchip/TCPIPStack/BerkeleyAPI.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/BerkeleyAPI.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/BerkeleyAPI.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/BerkeleyAPI.o   Microchip/TCPIPStack/BerkeleyAPI.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/BerkeleyAPI.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/BerkeleyAPI.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/BigInt.o: Microchip/TCPIPStack/BigInt.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/BigInt.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/BigInt.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/BigInt.o   Microchip/TCPIPStack/BigInt.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/BigInt.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/BigInt.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/Delay.o: Microchip/TCPIPStack/Delay.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Delay.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Delay.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/Delay.o   Microchip/TCPIPStack/Delay.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/Delay.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/Delay.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/DHCP.o: Microchip/TCPIPStack/DHCP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/DHCP.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/DHCP.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/DHCP.o   Microchip/TCPIPStack/DHCP.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/DHCP.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/DHCP.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/DHCPs.o: Microchip/TCPIPStack/DHCPs.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/DHCPs.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/DHCPs.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/DHCPs.o   Microchip/TCPIPStack/DHCPs.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/DHCPs.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/DHCPs.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/DNS.o: Microchip/TCPIPStack/DNS.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/DNS.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/DNS.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/DNS.o   Microchip/TCPIPStack/DNS.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/DNS.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/DNS.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/DNSs.o: Microchip/TCPIPStack/DNSs.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/DNSs.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/DNSs.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/DNSs.o   Microchip/TCPIPStack/DNSs.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/DNSs.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/DNSs.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/DynDNS.o: Microchip/TCPIPStack/DynDNS.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/DynDNS.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/DynDNS.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/DynDNS.o   Microchip/TCPIPStack/DynDNS.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/DynDNS.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/DynDNS.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/ETH97J60.o: Microchip/TCPIPStack/ETH97J60.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/ETH97J60.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/ETH97J60.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/ETH97J60.o   Microchip/TCPIPStack/ETH97J60.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/ETH97J60.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/ETH97J60.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/FileSystem.o: Microchip/TCPIPStack/FileSystem.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/FileSystem.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/FileSystem.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/FileSystem.o   Microchip/TCPIPStack/FileSystem.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/FileSystem.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/FileSystem.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/FTP.o: Microchip/TCPIPStack/FTP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/FTP.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/FTP.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/FTP.o   Microchip/TCPIPStack/FTP.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/FTP.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/FTP.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/Hashes.o: Microchip/TCPIPStack/Hashes.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Hashes.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Hashes.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/Hashes.o   Microchip/TCPIPStack/Hashes.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/Hashes.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/Hashes.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/Helpers.o: Microchip/TCPIPStack/Helpers.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Helpers.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Helpers.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/Helpers.o   Microchip/TCPIPStack/Helpers.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/Helpers.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/Helpers.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/HTTP.o: Microchip/TCPIPStack/HTTP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/HTTP.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/HTTP.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/HTTP.o   Microchip/TCPIPStack/HTTP.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/HTTP.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/HTTP.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/HTTP2.o: Microchip/TCPIPStack/HTTP2.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/HTTP2.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/HTTP2.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/HTTP2.o   Microchip/TCPIPStack/HTTP2.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/HTTP2.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/HTTP2.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/ICMP.o: Microchip/TCPIPStack/ICMP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/ICMP.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/ICMP.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/ICMP.o   Microchip/TCPIPStack/ICMP.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/ICMP.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/ICMP.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/IP.o: Microchip/TCPIPStack/IP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/IP.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/IP.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/IP.o   Microchip/TCPIPStack/IP.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/IP.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/IP.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/LCDBlocking.o: Microchip/TCPIPStack/LCDBlocking.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/LCDBlocking.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/LCDBlocking.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/LCDBlocking.o   Microchip/TCPIPStack/LCDBlocking.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/LCDBlocking.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/LCDBlocking.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/MPFS.o: Microchip/TCPIPStack/MPFS.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/MPFS.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/MPFS.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/MPFS.o   Microchip/TCPIPStack/MPFS.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/MPFS.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/MPFS.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/MPFS2.o: Microchip/TCPIPStack/MPFS2.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/MPFS2.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/MPFS2.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/MPFS2.o   Microchip/TCPIPStack/MPFS2.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/MPFS2.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/MPFS2.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/NBNS.o: Microchip/TCPIPStack/NBNS.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/NBNS.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/NBNS.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/NBNS.o   Microchip/TCPIPStack/NBNS.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/NBNS.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/NBNS.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/Random.o: Microchip/TCPIPStack/Random.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Random.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Random.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/Random.o   Microchip/TCPIPStack/Random.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/Random.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/Random.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/Reboot.o: Microchip/TCPIPStack/Reboot.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Reboot.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Reboot.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/Reboot.o   Microchip/TCPIPStack/Reboot.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/Reboot.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/Reboot.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/RSA.o: Microchip/TCPIPStack/RSA.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/RSA.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/RSA.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/RSA.o   Microchip/TCPIPStack/RSA.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/RSA.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/RSA.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/SMTP.o: Microchip/TCPIPStack/SMTP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SMTP.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SMTP.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/SMTP.o   Microchip/TCPIPStack/SMTP.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/SMTP.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/SMTP.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/SNMP.o: Microchip/TCPIPStack/SNMP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SNMP.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SNMP.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/SNMP.o   Microchip/TCPIPStack/SNMP.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/SNMP.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/SNMP.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/SNMPv3.o: Microchip/TCPIPStack/SNMPv3.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SNMPv3.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SNMPv3.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/SNMPv3.o   Microchip/TCPIPStack/SNMPv3.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/SNMPv3.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/SNMPv3.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/SNMPv3USM.o: Microchip/TCPIPStack/SNMPv3USM.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SNMPv3USM.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SNMPv3USM.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/SNMPv3USM.o   Microchip/TCPIPStack/SNMPv3USM.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/SNMPv3USM.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/SNMPv3USM.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/SNTP.o: Microchip/TCPIPStack/SNTP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SNTP.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SNTP.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/SNTP.o   Microchip/TCPIPStack/SNTP.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/SNTP.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/SNTP.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/SPIEEPROM.o: Microchip/TCPIPStack/SPIEEPROM.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SPIEEPROM.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SPIEEPROM.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/SPIEEPROM.o   Microchip/TCPIPStack/SPIEEPROM.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/SPIEEPROM.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/SPIEEPROM.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/SPIFlash.o: Microchip/TCPIPStack/SPIFlash.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SPIFlash.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SPIFlash.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/SPIFlash.o   Microchip/TCPIPStack/SPIFlash.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/SPIFlash.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/SPIFlash.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/SPIRAM.o: Microchip/TCPIPStack/SPIRAM.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SPIRAM.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SPIRAM.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/SPIRAM.o   Microchip/TCPIPStack/SPIRAM.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/SPIRAM.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/SPIRAM.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/SSL.o: Microchip/TCPIPStack/SSL.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SSL.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/SSL.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/SSL.o   Microchip/TCPIPStack/SSL.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/SSL.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/SSL.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/StackTsk.o: Microchip/TCPIPStack/StackTsk.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/StackTsk.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/StackTsk.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/StackTsk.o   Microchip/TCPIPStack/StackTsk.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/StackTsk.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/StackTsk.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/TCP.o: Microchip/TCPIPStack/TCP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/TCP.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/TCP.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/TCP.o   Microchip/TCPIPStack/TCP.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/TCP.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/TCP.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/TCPPerformanceTest.o: Microchip/TCPIPStack/TCPPerformanceTest.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/TCPPerformanceTest.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/TCPPerformanceTest.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/TCPPerformanceTest.o   Microchip/TCPIPStack/TCPPerformanceTest.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/TCPPerformanceTest.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/TCPPerformanceTest.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/Telnet.o: Microchip/TCPIPStack/Telnet.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Telnet.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Telnet.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/Telnet.o   Microchip/TCPIPStack/Telnet.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/Telnet.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/Telnet.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/TFTPc.o: Microchip/TCPIPStack/TFTPc.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/TFTPc.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/TFTPc.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/TFTPc.o   Microchip/TCPIPStack/TFTPc.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/TFTPc.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/TFTPc.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/Tick.o: Microchip/TCPIPStack/Tick.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Tick.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/Tick.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/Tick.o   Microchip/TCPIPStack/Tick.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/Tick.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/Tick.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/UART.o: Microchip/TCPIPStack/UART.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/UART.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/UART.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/UART.o   Microchip/TCPIPStack/UART.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/UART.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/UART.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/UART2TCPBridge.o: Microchip/TCPIPStack/UART2TCPBridge.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/UART2TCPBridge.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/UART2TCPBridge.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/UART2TCPBridge.o   Microchip/TCPIPStack/UART2TCPBridge.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/UART2TCPBridge.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/UART2TCPBridge.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/UDP.o: Microchip/TCPIPStack/UDP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/UDP.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/UDP.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/UDP.o   Microchip/TCPIPStack/UDP.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/UDP.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/UDP.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Microchip/TCPIPStack/UDPPerformanceTest.o: Microchip/TCPIPStack/UDPPerformanceTest.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Microchip/TCPIPStack" 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/UDPPerformanceTest.o.d 
	@${RM} ${OBJECTDIR}/Microchip/TCPIPStack/UDPPerformanceTest.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Microchip/TCPIPStack/UDPPerformanceTest.o   Microchip/TCPIPStack/UDPPerformanceTest.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Microchip/TCPIPStack/UDPPerformanceTest.o 
	@${FIXDEPS} "${OBJECTDIR}/Microchip/TCPIPStack/UDPPerformanceTest.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/modbus/General.o: modbus/General.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus" 
	@${RM} ${OBJECTDIR}/modbus/General.o.d 
	@${RM} ${OBJECTDIR}/modbus/General.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/modbus/General.o   modbus/General.c 
	@${DEP_GEN} -d ${OBJECTDIR}/modbus/General.o 
	@${FIXDEPS} "${OBJECTDIR}/modbus/General.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/modbus/Init.o: modbus/Init.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus" 
	@${RM} ${OBJECTDIR}/modbus/Init.o.d 
	@${RM} ${OBJECTDIR}/modbus/Init.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/modbus/Init.o   modbus/Init.c 
	@${DEP_GEN} -d ${OBJECTDIR}/modbus/Init.o 
	@${FIXDEPS} "${OBJECTDIR}/modbus/Init.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/modbus/Interrupts.o: modbus/Interrupts.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus" 
	@${RM} ${OBJECTDIR}/modbus/Interrupts.o.d 
	@${RM} ${OBJECTDIR}/modbus/Interrupts.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/modbus/Interrupts.o   modbus/Interrupts.c 
	@${DEP_GEN} -d ${OBJECTDIR}/modbus/Interrupts.o 
	@${FIXDEPS} "${OBJECTDIR}/modbus/Interrupts.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/modbus/PetitModbus.o: modbus/PetitModbus.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus" 
	@${RM} ${OBJECTDIR}/modbus/PetitModbus.o.d 
	@${RM} ${OBJECTDIR}/modbus/PetitModbus.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/modbus/PetitModbus.o   modbus/PetitModbus.c 
	@${DEP_GEN} -d ${OBJECTDIR}/modbus/PetitModbus.o 
	@${FIXDEPS} "${OBJECTDIR}/modbus/PetitModbus.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/modbus/PetitModbusPort.o: modbus/PetitModbusPort.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus" 
	@${RM} ${OBJECTDIR}/modbus/PetitModbusPort.o.d 
	@${RM} ${OBJECTDIR}/modbus/PetitModbusPort.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/modbus/PetitModbusPort.o   modbus/PetitModbusPort.c 
	@${DEP_GEN} -d ${OBJECTDIR}/modbus/PetitModbusPort.o 
	@${FIXDEPS} "${OBJECTDIR}/modbus/PetitModbusPort.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/modbus/PetitRegConstant.o: modbus/PetitRegConstant.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus" 
	@${RM} ${OBJECTDIR}/modbus/PetitRegConstant.o.d 
	@${RM} ${OBJECTDIR}/modbus/PetitRegConstant.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/modbus/PetitRegConstant.o   modbus/PetitRegConstant.c 
	@${DEP_GEN} -d ${OBJECTDIR}/modbus/PetitRegConstant.o 
	@${FIXDEPS} "${OBJECTDIR}/modbus/PetitRegConstant.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Command_Machine.o: Command_Machine.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/Command_Machine.o.d 
	@${RM} ${OBJECTDIR}/Command_Machine.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Command_Machine.o   Command_Machine.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Command_Machine.o 
	@${FIXDEPS} "${OBJECTDIR}/Command_Machine.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Diagnostic_ret.o: Diagnostic_ret.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/Diagnostic_ret.o.d 
	@${RM} ${OBJECTDIR}/Diagnostic_ret.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Diagnostic_ret.o   Diagnostic_ret.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Diagnostic_ret.o 
	@${FIXDEPS} "${OBJECTDIR}/Diagnostic_ret.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Shift_Register.o: Shift_Register.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/Shift_Register.o.d 
	@${RM} ${OBJECTDIR}/Shift_Register.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Shift_Register.o   Shift_Register.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Shift_Register.o 
	@${FIXDEPS} "${OBJECTDIR}/Shift_Register.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/Var_Out.o: Var_Out.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/Var_Out.o.d 
	@${RM} ${OBJECTDIR}/Var_Out.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/Var_Out.o   Var_Out.c 
	@${DEP_GEN} -d ${OBJECTDIR}/Var_Out.o 
	@${FIXDEPS} "${OBJECTDIR}/Var_Out.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/FiddleYard.o: FiddleYard.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/FiddleYard.o.d 
	@${RM} ${OBJECTDIR}/FiddleYard.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/FiddleYard.o   FiddleYard.c 
	@${DEP_GEN} -d ${OBJECTDIR}/FiddleYard.o 
	@${FIXDEPS} "${OBJECTDIR}/FiddleYard.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/eusart1.o: eusart1.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/eusart1.o.d 
	@${RM} ${OBJECTDIR}/eusart1.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/eusart1.o   eusart1.c 
	@${DEP_GEN} -d ${OBJECTDIR}/eusart1.o 
	@${FIXDEPS} "${OBJECTDIR}/eusart1.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/eusart2.o: eusart2.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/eusart2.o.d 
	@${RM} ${OBJECTDIR}/eusart2.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/eusart2.o   eusart2.c 
	@${DEP_GEN} -d ${OBJECTDIR}/eusart2.o 
	@${FIXDEPS} "${OBJECTDIR}/eusart2.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
${OBJECTDIR}/State_Machine.o: State_Machine.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/State_Machine.o.d 
	@${RM} ${OBJECTDIR}/State_Machine.o 
	${MP_CC} $(MP_EXTRA_CC_PRE) -p$(MP_PROCESSOR_OPTION) -I"." -I"Microchip/Include/TCPIP Stack" -I"Microchip/Include/PIC18Salloc" -I"Microchip/Include" -I"Microchip/TCPIPStack" -I"Microchip" -I"I2c2" -ml -oa- -o-  -I ${MP_CC_DIR}\\..\\h  -fo ${OBJECTDIR}/State_Machine.o   State_Machine.c 
	@${DEP_GEN} -d ${OBJECTDIR}/State_Machine.o 
	@${FIXDEPS} "${OBJECTDIR}/State_Machine.o.d" $(SILENT) -rsi ${MP_CC_DIR}../ -c18 
	
endif

# ------------------------------------------------------------------------------------
# Rules for buildStep: link
ifeq ($(TYPE_IMAGE), DEBUG_RUN)
dist/${CND_CONF}/${IMAGE_TYPE}/TrackController3.X.${IMAGE_TYPE}.${OUTPUT_SUFFIX}: ${OBJECTFILES}  nbproject/Makefile-${CND_CONF}.mk    
	@${MKDIR} dist/${CND_CONF}/${IMAGE_TYPE} 
	${MP_LD} $(MP_EXTRA_LD_PRE)   -p$(MP_PROCESSOR_OPTION_LD)  -w -x -u_DEBUG -m"$(BINDIR_)$(TARGETBASE).map" -w -l"../../../../../../MCC18/lib" -l"."  -z__MPLAB_BUILD=1  -u_CRUNTIME -z__MPLAB_DEBUG=1 -z__MPLAB_DEBUGGER_ICD3=1 $(MP_LINKER_DEBUG_OPTION) -l ${MP_CC_DIR}\\..\\lib  -o dist/${CND_CONF}/${IMAGE_TYPE}/TrackController3.X.${IMAGE_TYPE}.${OUTPUT_SUFFIX}  ${OBJECTFILES_QUOTED_IF_SPACED}   
else
dist/${CND_CONF}/${IMAGE_TYPE}/TrackController3.X.${IMAGE_TYPE}.${OUTPUT_SUFFIX}: ${OBJECTFILES}  nbproject/Makefile-${CND_CONF}.mk   
	@${MKDIR} dist/${CND_CONF}/${IMAGE_TYPE} 
	${MP_LD} $(MP_EXTRA_LD_PRE)   -p$(MP_PROCESSOR_OPTION_LD)  -w  -m"$(BINDIR_)$(TARGETBASE).map" -w -l"../../../../../../MCC18/lib" -l"."  -z__MPLAB_BUILD=1  -u_CRUNTIME -l ${MP_CC_DIR}\\..\\lib  -o dist/${CND_CONF}/${IMAGE_TYPE}/TrackController3.X.${IMAGE_TYPE}.${DEBUGGABLE_SUFFIX}  ${OBJECTFILES_QUOTED_IF_SPACED}   
endif


# Subprojects
.build-subprojects:


# Subprojects
.clean-subprojects:

# Clean Targets
.clean-conf: ${CLEAN_SUBPROJECTS}
	${RM} -r build/default
	${RM} -r dist/default

# Enable dependency checking
.dep.inc: .depcheck-impl

DEPFILES=$(shell mplabwildcard ${POSSIBLE_DEPFILES})
ifneq (${DEPFILES},)
include ${DEPFILES}
endif
