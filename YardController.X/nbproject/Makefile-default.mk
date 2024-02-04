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
OUTPUT_SUFFIX=elf
DEBUGGABLE_SUFFIX=elf
FINAL_IMAGE=${DISTDIR}/YardController.X.${IMAGE_TYPE}.${OUTPUT_SUFFIX}
else
IMAGE_TYPE=production
OUTPUT_SUFFIX=hex
DEBUGGABLE_SUFFIX=elf
FINAL_IMAGE=${DISTDIR}/YardController.X.${IMAGE_TYPE}.${OUTPUT_SUFFIX}
endif

ifeq ($(COMPARE_BUILD), true)
COMPARISON_BUILD=-mafrlcsj
else
COMPARISON_BUILD=
endif

# Object Directory
OBJECTDIR=build/${CND_CONF}/${IMAGE_TYPE}

# Distribution Directory
DISTDIR=dist/${CND_CONF}/${IMAGE_TYPE}

# Source Files Quoted if spaced
SOURCEFILES_QUOTED_IF_SPACED=communication.c mcc_generated_files/TCPIPLibrary/icmp.c mcc_generated_files/TCPIPLibrary/log_console.c mcc_generated_files/TCPIPLibrary/log.c mcc_generated_files/TCPIPLibrary/rtcc.c mcc_generated_files/TCPIPLibrary/ip_database.c mcc_generated_files/TCPIPLibrary/network.c mcc_generated_files/TCPIPLibrary/lfsr.c mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.c mcc_generated_files/TCPIPLibrary/log_syslog.c mcc_generated_files/TCPIPLibrary/ipv4.c mcc_generated_files/TCPIPLibrary/udpv4.c mcc_generated_files/TCPIPLibrary/arpv4.c mcc_generated_files/TCPIPLibrary/ETHxxJ6x_driver.c mcc_generated_files/TCPIPLibrary/mac_address.c mcc_generated_files/mcc.c mcc_generated_files/pin_manager.c mcc_generated_files/device_config.c mcc_generated_files/tmr0.c mcc_generated_files/interrupt_manager.c mcc_generated_files/tmr1.c main.c debounce.c bus.c firedep.c milisecond_counter.c setocc.c rand.c pathway.c

# Object Files Quoted if spaced
OBJECTFILES_QUOTED_IF_SPACED=${OBJECTDIR}/communication.p1 ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/icmp.p1 ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_console.p1 ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log.p1 ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/rtcc.p1 ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ip_database.p1 ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/network.p1 ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/lfsr.p1 ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.p1 ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_syslog.p1 ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ipv4.p1 ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4.p1 ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/arpv4.p1 ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ETHxxJ6x_driver.p1 ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/mac_address.p1 ${OBJECTDIR}/mcc_generated_files/mcc.p1 ${OBJECTDIR}/mcc_generated_files/pin_manager.p1 ${OBJECTDIR}/mcc_generated_files/device_config.p1 ${OBJECTDIR}/mcc_generated_files/tmr0.p1 ${OBJECTDIR}/mcc_generated_files/interrupt_manager.p1 ${OBJECTDIR}/mcc_generated_files/tmr1.p1 ${OBJECTDIR}/main.p1 ${OBJECTDIR}/debounce.p1 ${OBJECTDIR}/bus.p1 ${OBJECTDIR}/firedep.p1 ${OBJECTDIR}/milisecond_counter.p1 ${OBJECTDIR}/setocc.p1 ${OBJECTDIR}/rand.p1 ${OBJECTDIR}/pathway.p1
POSSIBLE_DEPFILES=${OBJECTDIR}/communication.p1.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/icmp.p1.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_console.p1.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log.p1.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/rtcc.p1.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ip_database.p1.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/network.p1.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/lfsr.p1.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.p1.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_syslog.p1.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ipv4.p1.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4.p1.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/arpv4.p1.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ETHxxJ6x_driver.p1.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/mac_address.p1.d ${OBJECTDIR}/mcc_generated_files/mcc.p1.d ${OBJECTDIR}/mcc_generated_files/pin_manager.p1.d ${OBJECTDIR}/mcc_generated_files/device_config.p1.d ${OBJECTDIR}/mcc_generated_files/tmr0.p1.d ${OBJECTDIR}/mcc_generated_files/interrupt_manager.p1.d ${OBJECTDIR}/mcc_generated_files/tmr1.p1.d ${OBJECTDIR}/main.p1.d ${OBJECTDIR}/debounce.p1.d ${OBJECTDIR}/bus.p1.d ${OBJECTDIR}/firedep.p1.d ${OBJECTDIR}/milisecond_counter.p1.d ${OBJECTDIR}/setocc.p1.d ${OBJECTDIR}/rand.p1.d ${OBJECTDIR}/pathway.p1.d

# Object Files
OBJECTFILES=${OBJECTDIR}/communication.p1 ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/icmp.p1 ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_console.p1 ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log.p1 ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/rtcc.p1 ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ip_database.p1 ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/network.p1 ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/lfsr.p1 ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.p1 ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_syslog.p1 ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ipv4.p1 ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4.p1 ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/arpv4.p1 ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ETHxxJ6x_driver.p1 ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/mac_address.p1 ${OBJECTDIR}/mcc_generated_files/mcc.p1 ${OBJECTDIR}/mcc_generated_files/pin_manager.p1 ${OBJECTDIR}/mcc_generated_files/device_config.p1 ${OBJECTDIR}/mcc_generated_files/tmr0.p1 ${OBJECTDIR}/mcc_generated_files/interrupt_manager.p1 ${OBJECTDIR}/mcc_generated_files/tmr1.p1 ${OBJECTDIR}/main.p1 ${OBJECTDIR}/debounce.p1 ${OBJECTDIR}/bus.p1 ${OBJECTDIR}/firedep.p1 ${OBJECTDIR}/milisecond_counter.p1 ${OBJECTDIR}/setocc.p1 ${OBJECTDIR}/rand.p1 ${OBJECTDIR}/pathway.p1

# Source Files
SOURCEFILES=communication.c mcc_generated_files/TCPIPLibrary/icmp.c mcc_generated_files/TCPIPLibrary/log_console.c mcc_generated_files/TCPIPLibrary/log.c mcc_generated_files/TCPIPLibrary/rtcc.c mcc_generated_files/TCPIPLibrary/ip_database.c mcc_generated_files/TCPIPLibrary/network.c mcc_generated_files/TCPIPLibrary/lfsr.c mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.c mcc_generated_files/TCPIPLibrary/log_syslog.c mcc_generated_files/TCPIPLibrary/ipv4.c mcc_generated_files/TCPIPLibrary/udpv4.c mcc_generated_files/TCPIPLibrary/arpv4.c mcc_generated_files/TCPIPLibrary/ETHxxJ6x_driver.c mcc_generated_files/TCPIPLibrary/mac_address.c mcc_generated_files/mcc.c mcc_generated_files/pin_manager.c mcc_generated_files/device_config.c mcc_generated_files/tmr0.c mcc_generated_files/interrupt_manager.c mcc_generated_files/tmr1.c main.c debounce.c bus.c firedep.c milisecond_counter.c setocc.c rand.c pathway.c



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
	${MAKE}  -f nbproject/Makefile-default.mk ${DISTDIR}/YardController.X.${IMAGE_TYPE}.${OUTPUT_SUFFIX}

MP_PROCESSOR_OPTION=18F97J60
# ------------------------------------------------------------------------------------
# Rules for buildStep: compile
ifeq ($(TYPE_IMAGE), DEBUG_RUN)
${OBJECTDIR}/communication.p1: communication.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/communication.p1.d 
	@${RM} ${OBJECTDIR}/communication.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c  -D__DEBUG=1  -mdebugger=pickit3   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/communication.p1 communication.c 
	@-${MV} ${OBJECTDIR}/communication.d ${OBJECTDIR}/communication.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/communication.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/icmp.p1: mcc_generated_files/TCPIPLibrary/icmp.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files/TCPIPLibrary" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/icmp.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/icmp.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c  -D__DEBUG=1  -mdebugger=pickit3   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/icmp.p1 mcc_generated_files/TCPIPLibrary/icmp.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/icmp.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/icmp.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/icmp.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_console.p1: mcc_generated_files/TCPIPLibrary/log_console.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files/TCPIPLibrary" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_console.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_console.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c  -D__DEBUG=1  -mdebugger=pickit3   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_console.p1 mcc_generated_files/TCPIPLibrary/log_console.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_console.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_console.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_console.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log.p1: mcc_generated_files/TCPIPLibrary/log.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files/TCPIPLibrary" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c  -D__DEBUG=1  -mdebugger=pickit3   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log.p1 mcc_generated_files/TCPIPLibrary/log.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/rtcc.p1: mcc_generated_files/TCPIPLibrary/rtcc.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files/TCPIPLibrary" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/rtcc.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/rtcc.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c  -D__DEBUG=1  -mdebugger=pickit3   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/rtcc.p1 mcc_generated_files/TCPIPLibrary/rtcc.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/rtcc.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/rtcc.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/rtcc.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ip_database.p1: mcc_generated_files/TCPIPLibrary/ip_database.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files/TCPIPLibrary" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ip_database.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ip_database.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c  -D__DEBUG=1  -mdebugger=pickit3   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ip_database.p1 mcc_generated_files/TCPIPLibrary/ip_database.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ip_database.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ip_database.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ip_database.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/network.p1: mcc_generated_files/TCPIPLibrary/network.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files/TCPIPLibrary" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/network.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/network.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c  -D__DEBUG=1  -mdebugger=pickit3   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/network.p1 mcc_generated_files/TCPIPLibrary/network.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/network.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/network.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/network.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/lfsr.p1: mcc_generated_files/TCPIPLibrary/lfsr.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files/TCPIPLibrary" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/lfsr.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/lfsr.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c  -D__DEBUG=1  -mdebugger=pickit3   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/lfsr.p1 mcc_generated_files/TCPIPLibrary/lfsr.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/lfsr.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/lfsr.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/lfsr.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.p1: mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files/TCPIPLibrary" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c  -D__DEBUG=1  -mdebugger=pickit3   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.p1 mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_syslog.p1: mcc_generated_files/TCPIPLibrary/log_syslog.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files/TCPIPLibrary" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_syslog.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_syslog.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c  -D__DEBUG=1  -mdebugger=pickit3   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_syslog.p1 mcc_generated_files/TCPIPLibrary/log_syslog.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_syslog.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_syslog.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_syslog.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ipv4.p1: mcc_generated_files/TCPIPLibrary/ipv4.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files/TCPIPLibrary" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ipv4.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ipv4.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c  -D__DEBUG=1  -mdebugger=pickit3   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ipv4.p1 mcc_generated_files/TCPIPLibrary/ipv4.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ipv4.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ipv4.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ipv4.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4.p1: mcc_generated_files/TCPIPLibrary/udpv4.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files/TCPIPLibrary" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c  -D__DEBUG=1  -mdebugger=pickit3   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4.p1 mcc_generated_files/TCPIPLibrary/udpv4.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/arpv4.p1: mcc_generated_files/TCPIPLibrary/arpv4.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files/TCPIPLibrary" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/arpv4.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/arpv4.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c  -D__DEBUG=1  -mdebugger=pickit3   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/arpv4.p1 mcc_generated_files/TCPIPLibrary/arpv4.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/arpv4.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/arpv4.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/arpv4.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ETHxxJ6x_driver.p1: mcc_generated_files/TCPIPLibrary/ETHxxJ6x_driver.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files/TCPIPLibrary" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ETHxxJ6x_driver.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ETHxxJ6x_driver.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c  -D__DEBUG=1  -mdebugger=pickit3   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ETHxxJ6x_driver.p1 mcc_generated_files/TCPIPLibrary/ETHxxJ6x_driver.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ETHxxJ6x_driver.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ETHxxJ6x_driver.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ETHxxJ6x_driver.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/mac_address.p1: mcc_generated_files/TCPIPLibrary/mac_address.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files/TCPIPLibrary" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/mac_address.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/mac_address.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c  -D__DEBUG=1  -mdebugger=pickit3   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/mac_address.p1 mcc_generated_files/TCPIPLibrary/mac_address.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/mac_address.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/mac_address.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/mac_address.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/mcc.p1: mcc_generated_files/mcc.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/mcc.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/mcc.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c  -D__DEBUG=1  -mdebugger=pickit3   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/mcc.p1 mcc_generated_files/mcc.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/mcc.d ${OBJECTDIR}/mcc_generated_files/mcc.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/mcc.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/pin_manager.p1: mcc_generated_files/pin_manager.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/pin_manager.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/pin_manager.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c  -D__DEBUG=1  -mdebugger=pickit3   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/pin_manager.p1 mcc_generated_files/pin_manager.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/pin_manager.d ${OBJECTDIR}/mcc_generated_files/pin_manager.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/pin_manager.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/device_config.p1: mcc_generated_files/device_config.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/device_config.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/device_config.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c  -D__DEBUG=1  -mdebugger=pickit3   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/device_config.p1 mcc_generated_files/device_config.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/device_config.d ${OBJECTDIR}/mcc_generated_files/device_config.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/device_config.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/tmr0.p1: mcc_generated_files/tmr0.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/tmr0.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/tmr0.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c  -D__DEBUG=1  -mdebugger=pickit3   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/tmr0.p1 mcc_generated_files/tmr0.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/tmr0.d ${OBJECTDIR}/mcc_generated_files/tmr0.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/tmr0.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/interrupt_manager.p1: mcc_generated_files/interrupt_manager.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/interrupt_manager.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/interrupt_manager.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c  -D__DEBUG=1  -mdebugger=pickit3   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/interrupt_manager.p1 mcc_generated_files/interrupt_manager.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/interrupt_manager.d ${OBJECTDIR}/mcc_generated_files/interrupt_manager.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/interrupt_manager.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/tmr1.p1: mcc_generated_files/tmr1.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/tmr1.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/tmr1.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c  -D__DEBUG=1  -mdebugger=pickit3   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/tmr1.p1 mcc_generated_files/tmr1.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/tmr1.d ${OBJECTDIR}/mcc_generated_files/tmr1.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/tmr1.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/main.p1: main.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/main.p1.d 
	@${RM} ${OBJECTDIR}/main.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c  -D__DEBUG=1  -mdebugger=pickit3   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/main.p1 main.c 
	@-${MV} ${OBJECTDIR}/main.d ${OBJECTDIR}/main.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/main.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/debounce.p1: debounce.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/debounce.p1.d 
	@${RM} ${OBJECTDIR}/debounce.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c  -D__DEBUG=1  -mdebugger=pickit3   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/debounce.p1 debounce.c 
	@-${MV} ${OBJECTDIR}/debounce.d ${OBJECTDIR}/debounce.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/debounce.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/bus.p1: bus.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/bus.p1.d 
	@${RM} ${OBJECTDIR}/bus.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c  -D__DEBUG=1  -mdebugger=pickit3   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/bus.p1 bus.c 
	@-${MV} ${OBJECTDIR}/bus.d ${OBJECTDIR}/bus.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/bus.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/firedep.p1: firedep.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/firedep.p1.d 
	@${RM} ${OBJECTDIR}/firedep.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c  -D__DEBUG=1  -mdebugger=pickit3   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/firedep.p1 firedep.c 
	@-${MV} ${OBJECTDIR}/firedep.d ${OBJECTDIR}/firedep.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/firedep.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/milisecond_counter.p1: milisecond_counter.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/milisecond_counter.p1.d 
	@${RM} ${OBJECTDIR}/milisecond_counter.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c  -D__DEBUG=1  -mdebugger=pickit3   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/milisecond_counter.p1 milisecond_counter.c 
	@-${MV} ${OBJECTDIR}/milisecond_counter.d ${OBJECTDIR}/milisecond_counter.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/milisecond_counter.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/setocc.p1: setocc.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/setocc.p1.d 
	@${RM} ${OBJECTDIR}/setocc.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c  -D__DEBUG=1  -mdebugger=pickit3   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/setocc.p1 setocc.c 
	@-${MV} ${OBJECTDIR}/setocc.d ${OBJECTDIR}/setocc.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/setocc.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/rand.p1: rand.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/rand.p1.d 
	@${RM} ${OBJECTDIR}/rand.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c  -D__DEBUG=1  -mdebugger=pickit3   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/rand.p1 rand.c 
	@-${MV} ${OBJECTDIR}/rand.d ${OBJECTDIR}/rand.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/rand.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/pathway.p1: pathway.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/pathway.p1.d 
	@${RM} ${OBJECTDIR}/pathway.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c  -D__DEBUG=1  -mdebugger=pickit3   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/pathway.p1 pathway.c 
	@-${MV} ${OBJECTDIR}/pathway.d ${OBJECTDIR}/pathway.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/pathway.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
else
${OBJECTDIR}/communication.p1: communication.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/communication.p1.d 
	@${RM} ${OBJECTDIR}/communication.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/communication.p1 communication.c 
	@-${MV} ${OBJECTDIR}/communication.d ${OBJECTDIR}/communication.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/communication.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/icmp.p1: mcc_generated_files/TCPIPLibrary/icmp.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files/TCPIPLibrary" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/icmp.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/icmp.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/icmp.p1 mcc_generated_files/TCPIPLibrary/icmp.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/icmp.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/icmp.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/icmp.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_console.p1: mcc_generated_files/TCPIPLibrary/log_console.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files/TCPIPLibrary" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_console.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_console.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_console.p1 mcc_generated_files/TCPIPLibrary/log_console.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_console.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_console.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_console.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log.p1: mcc_generated_files/TCPIPLibrary/log.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files/TCPIPLibrary" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log.p1 mcc_generated_files/TCPIPLibrary/log.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/rtcc.p1: mcc_generated_files/TCPIPLibrary/rtcc.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files/TCPIPLibrary" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/rtcc.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/rtcc.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/rtcc.p1 mcc_generated_files/TCPIPLibrary/rtcc.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/rtcc.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/rtcc.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/rtcc.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ip_database.p1: mcc_generated_files/TCPIPLibrary/ip_database.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files/TCPIPLibrary" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ip_database.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ip_database.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ip_database.p1 mcc_generated_files/TCPIPLibrary/ip_database.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ip_database.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ip_database.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ip_database.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/network.p1: mcc_generated_files/TCPIPLibrary/network.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files/TCPIPLibrary" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/network.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/network.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/network.p1 mcc_generated_files/TCPIPLibrary/network.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/network.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/network.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/network.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/lfsr.p1: mcc_generated_files/TCPIPLibrary/lfsr.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files/TCPIPLibrary" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/lfsr.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/lfsr.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/lfsr.p1 mcc_generated_files/TCPIPLibrary/lfsr.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/lfsr.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/lfsr.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/lfsr.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.p1: mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files/TCPIPLibrary" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.p1 mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_syslog.p1: mcc_generated_files/TCPIPLibrary/log_syslog.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files/TCPIPLibrary" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_syslog.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_syslog.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_syslog.p1 mcc_generated_files/TCPIPLibrary/log_syslog.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_syslog.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_syslog.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/log_syslog.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ipv4.p1: mcc_generated_files/TCPIPLibrary/ipv4.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files/TCPIPLibrary" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ipv4.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ipv4.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ipv4.p1 mcc_generated_files/TCPIPLibrary/ipv4.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ipv4.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ipv4.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ipv4.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4.p1: mcc_generated_files/TCPIPLibrary/udpv4.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files/TCPIPLibrary" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4.p1 mcc_generated_files/TCPIPLibrary/udpv4.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/udpv4.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/arpv4.p1: mcc_generated_files/TCPIPLibrary/arpv4.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files/TCPIPLibrary" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/arpv4.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/arpv4.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/arpv4.p1 mcc_generated_files/TCPIPLibrary/arpv4.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/arpv4.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/arpv4.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/arpv4.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ETHxxJ6x_driver.p1: mcc_generated_files/TCPIPLibrary/ETHxxJ6x_driver.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files/TCPIPLibrary" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ETHxxJ6x_driver.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ETHxxJ6x_driver.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ETHxxJ6x_driver.p1 mcc_generated_files/TCPIPLibrary/ETHxxJ6x_driver.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ETHxxJ6x_driver.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ETHxxJ6x_driver.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/ETHxxJ6x_driver.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/mac_address.p1: mcc_generated_files/TCPIPLibrary/mac_address.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files/TCPIPLibrary" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/mac_address.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/mac_address.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/mac_address.p1 mcc_generated_files/TCPIPLibrary/mac_address.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/mac_address.d ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/mac_address.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/TCPIPLibrary/mac_address.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/mcc.p1: mcc_generated_files/mcc.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/mcc.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/mcc.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/mcc.p1 mcc_generated_files/mcc.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/mcc.d ${OBJECTDIR}/mcc_generated_files/mcc.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/mcc.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/pin_manager.p1: mcc_generated_files/pin_manager.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/pin_manager.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/pin_manager.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/pin_manager.p1 mcc_generated_files/pin_manager.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/pin_manager.d ${OBJECTDIR}/mcc_generated_files/pin_manager.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/pin_manager.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/device_config.p1: mcc_generated_files/device_config.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/device_config.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/device_config.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/device_config.p1 mcc_generated_files/device_config.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/device_config.d ${OBJECTDIR}/mcc_generated_files/device_config.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/device_config.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/tmr0.p1: mcc_generated_files/tmr0.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/tmr0.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/tmr0.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/tmr0.p1 mcc_generated_files/tmr0.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/tmr0.d ${OBJECTDIR}/mcc_generated_files/tmr0.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/tmr0.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/interrupt_manager.p1: mcc_generated_files/interrupt_manager.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/interrupt_manager.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/interrupt_manager.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/interrupt_manager.p1 mcc_generated_files/interrupt_manager.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/interrupt_manager.d ${OBJECTDIR}/mcc_generated_files/interrupt_manager.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/interrupt_manager.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/mcc_generated_files/tmr1.p1: mcc_generated_files/tmr1.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}/mcc_generated_files" 
	@${RM} ${OBJECTDIR}/mcc_generated_files/tmr1.p1.d 
	@${RM} ${OBJECTDIR}/mcc_generated_files/tmr1.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/mcc_generated_files/tmr1.p1 mcc_generated_files/tmr1.c 
	@-${MV} ${OBJECTDIR}/mcc_generated_files/tmr1.d ${OBJECTDIR}/mcc_generated_files/tmr1.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/mcc_generated_files/tmr1.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/main.p1: main.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/main.p1.d 
	@${RM} ${OBJECTDIR}/main.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/main.p1 main.c 
	@-${MV} ${OBJECTDIR}/main.d ${OBJECTDIR}/main.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/main.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/debounce.p1: debounce.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/debounce.p1.d 
	@${RM} ${OBJECTDIR}/debounce.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/debounce.p1 debounce.c 
	@-${MV} ${OBJECTDIR}/debounce.d ${OBJECTDIR}/debounce.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/debounce.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/bus.p1: bus.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/bus.p1.d 
	@${RM} ${OBJECTDIR}/bus.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/bus.p1 bus.c 
	@-${MV} ${OBJECTDIR}/bus.d ${OBJECTDIR}/bus.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/bus.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/firedep.p1: firedep.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/firedep.p1.d 
	@${RM} ${OBJECTDIR}/firedep.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/firedep.p1 firedep.c 
	@-${MV} ${OBJECTDIR}/firedep.d ${OBJECTDIR}/firedep.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/firedep.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/milisecond_counter.p1: milisecond_counter.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/milisecond_counter.p1.d 
	@${RM} ${OBJECTDIR}/milisecond_counter.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/milisecond_counter.p1 milisecond_counter.c 
	@-${MV} ${OBJECTDIR}/milisecond_counter.d ${OBJECTDIR}/milisecond_counter.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/milisecond_counter.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/setocc.p1: setocc.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/setocc.p1.d 
	@${RM} ${OBJECTDIR}/setocc.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/setocc.p1 setocc.c 
	@-${MV} ${OBJECTDIR}/setocc.d ${OBJECTDIR}/setocc.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/setocc.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/rand.p1: rand.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/rand.p1.d 
	@${RM} ${OBJECTDIR}/rand.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/rand.p1 rand.c 
	@-${MV} ${OBJECTDIR}/rand.d ${OBJECTDIR}/rand.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/rand.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/pathway.p1: pathway.c  nbproject/Makefile-${CND_CONF}.mk 
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/pathway.p1.d 
	@${RM} ${OBJECTDIR}/pathway.p1 
	${MP_CC} $(MP_EXTRA_CC_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -c   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -DXPRJ_default=$(CND_CONF)  -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits $(COMPARISON_BUILD)  -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     -o ${OBJECTDIR}/pathway.p1 pathway.c 
	@-${MV} ${OBJECTDIR}/pathway.d ${OBJECTDIR}/pathway.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/pathway.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
endif

# ------------------------------------------------------------------------------------
# Rules for buildStep: assemble
ifeq ($(TYPE_IMAGE), DEBUG_RUN)
else
endif

# ------------------------------------------------------------------------------------
# Rules for buildStep: assembleWithPreprocess
ifeq ($(TYPE_IMAGE), DEBUG_RUN)
else
endif

# ------------------------------------------------------------------------------------
# Rules for buildStep: link
ifeq ($(TYPE_IMAGE), DEBUG_RUN)
${DISTDIR}/YardController.X.${IMAGE_TYPE}.${OUTPUT_SUFFIX}: ${OBJECTFILES}  nbproject/Makefile-${CND_CONF}.mk    
	@${MKDIR} ${DISTDIR} 
	${MP_CC} $(MP_EXTRA_LD_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -Wl,-Map=${DISTDIR}/YardController.X.${IMAGE_TYPE}.map  -D__DEBUG=1  -mdebugger=pickit3  -DXPRJ_default=$(CND_CONF)  -Wl,--defsym=__MPLAB_BUILD=1   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto        $(COMPARISON_BUILD) -Wl,--memorysummary,${DISTDIR}/memoryfile.xml -o ${DISTDIR}/YardController.X.${IMAGE_TYPE}.${DEBUGGABLE_SUFFIX}  ${OBJECTFILES_QUOTED_IF_SPACED}     
	@${RM} ${DISTDIR}/YardController.X.${IMAGE_TYPE}.hex 
	
	
else
${DISTDIR}/YardController.X.${IMAGE_TYPE}.${OUTPUT_SUFFIX}: ${OBJECTFILES}  nbproject/Makefile-${CND_CONF}.mk   
	@${MKDIR} ${DISTDIR} 
	${MP_CC} $(MP_EXTRA_LD_PRE) -mcpu=$(MP_PROCESSOR_OPTION) -Wl,-Map=${DISTDIR}/YardController.X.${IMAGE_TYPE}.map  -DXPRJ_default=$(CND_CONF)  -Wl,--defsym=__MPLAB_BUILD=1   -mdfp="${DFP_DIR}/xc8"  -fno-short-double -fno-short-float -memi=wordwrite -O0 -fasmfile -maddrqual=ignore -xassembler-with-cpp -mwarn=-3 -Wa,-a -msummary=-psect,-class,+mem,-hex,-file  -ginhx32 -Wl,--data-init -mno-keep-startup -mno-download -mdefault-config-bits -std=c99 -gdwarf-3 -mstack=compiled:auto:auto:auto     $(COMPARISON_BUILD) -Wl,--memorysummary,${DISTDIR}/memoryfile.xml -o ${DISTDIR}/YardController.X.${IMAGE_TYPE}.${DEBUGGABLE_SUFFIX}  ${OBJECTFILES_QUOTED_IF_SPACED}     
	
	
endif


# Subprojects
.build-subprojects:


# Subprojects
.clean-subprojects:

# Clean Targets
.clean-conf: ${CLEAN_SUBPROJECTS}
	${RM} -r ${OBJECTDIR}
	${RM} -r ${DISTDIR}

# Enable dependency checking
.dep.inc: .depcheck-impl

DEPFILES=$(wildcard ${POSSIBLE_DEPFILES})
ifneq (${DEPFILES},)
include ${DEPFILES}
endif
