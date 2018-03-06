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
FINAL_IMAGE=dist/${CND_CONF}/${IMAGE_TYPE}/SiebwaldeTrackAmplifier2.X.${IMAGE_TYPE}.${OUTPUT_SUFFIX}
else
IMAGE_TYPE=production
OUTPUT_SUFFIX=hex
DEBUGGABLE_SUFFIX=elf
FINAL_IMAGE=dist/${CND_CONF}/${IMAGE_TYPE}/SiebwaldeTrackAmplifier2.X.${IMAGE_TYPE}.${OUTPUT_SUFFIX}
endif

ifeq ($(COMPARE_BUILD), true)
COMPARISON_BUILD=--mafrlcsj
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
SOURCEFILES_QUOTED_IF_SPACED=Peripherals/config.c Peripherals/INT0.c Peripherals/interrupt_manager.c Peripherals/pin_manager.c Peripherals/tmr2.c Peripherals/tmr0.c Peripherals/pwm.c Peripherals/ausart.c main.c port/portevent.c port/portserial.c port/porttimer.c modbus/ascii/mbascii.c modbus/functions/mbfunccoils.c modbus/functions/mbfuncdiag.c modbus/functions/mbfuncdisc.c modbus/functions/mbfuncholding.c modbus/functions/mbfuncinput.c modbus/functions/mbfuncother.c modbus/functions/mbutils.c modbus/mb.c modbus/rtu/mbcrc.c modbus/rtu/mbrtu.c modbus/tcp/mbtcp.c

# Object Files Quoted if spaced
OBJECTFILES_QUOTED_IF_SPACED=${OBJECTDIR}/Peripherals/config.p1 ${OBJECTDIR}/Peripherals/INT0.p1 ${OBJECTDIR}/Peripherals/interrupt_manager.p1 ${OBJECTDIR}/Peripherals/pin_manager.p1 ${OBJECTDIR}/Peripherals/tmr2.p1 ${OBJECTDIR}/Peripherals/tmr0.p1 ${OBJECTDIR}/Peripherals/pwm.p1 ${OBJECTDIR}/Peripherals/ausart.p1 ${OBJECTDIR}/main.p1 ${OBJECTDIR}/port/portevent.p1 ${OBJECTDIR}/port/portserial.p1 ${OBJECTDIR}/port/porttimer.p1 ${OBJECTDIR}/modbus/ascii/mbascii.p1 ${OBJECTDIR}/modbus/functions/mbfunccoils.p1 ${OBJECTDIR}/modbus/functions/mbfuncdiag.p1 ${OBJECTDIR}/modbus/functions/mbfuncdisc.p1 ${OBJECTDIR}/modbus/functions/mbfuncholding.p1 ${OBJECTDIR}/modbus/functions/mbfuncinput.p1 ${OBJECTDIR}/modbus/functions/mbfuncother.p1 ${OBJECTDIR}/modbus/functions/mbutils.p1 ${OBJECTDIR}/modbus/mb.p1 ${OBJECTDIR}/modbus/rtu/mbcrc.p1 ${OBJECTDIR}/modbus/rtu/mbrtu.p1 ${OBJECTDIR}/modbus/tcp/mbtcp.p1
POSSIBLE_DEPFILES=${OBJECTDIR}/Peripherals/config.p1.d ${OBJECTDIR}/Peripherals/INT0.p1.d ${OBJECTDIR}/Peripherals/interrupt_manager.p1.d ${OBJECTDIR}/Peripherals/pin_manager.p1.d ${OBJECTDIR}/Peripherals/tmr2.p1.d ${OBJECTDIR}/Peripherals/tmr0.p1.d ${OBJECTDIR}/Peripherals/pwm.p1.d ${OBJECTDIR}/Peripherals/ausart.p1.d ${OBJECTDIR}/main.p1.d ${OBJECTDIR}/port/portevent.p1.d ${OBJECTDIR}/port/portserial.p1.d ${OBJECTDIR}/port/porttimer.p1.d ${OBJECTDIR}/modbus/ascii/mbascii.p1.d ${OBJECTDIR}/modbus/functions/mbfunccoils.p1.d ${OBJECTDIR}/modbus/functions/mbfuncdiag.p1.d ${OBJECTDIR}/modbus/functions/mbfuncdisc.p1.d ${OBJECTDIR}/modbus/functions/mbfuncholding.p1.d ${OBJECTDIR}/modbus/functions/mbfuncinput.p1.d ${OBJECTDIR}/modbus/functions/mbfuncother.p1.d ${OBJECTDIR}/modbus/functions/mbutils.p1.d ${OBJECTDIR}/modbus/mb.p1.d ${OBJECTDIR}/modbus/rtu/mbcrc.p1.d ${OBJECTDIR}/modbus/rtu/mbrtu.p1.d ${OBJECTDIR}/modbus/tcp/mbtcp.p1.d

# Object Files
OBJECTFILES=${OBJECTDIR}/Peripherals/config.p1 ${OBJECTDIR}/Peripherals/INT0.p1 ${OBJECTDIR}/Peripherals/interrupt_manager.p1 ${OBJECTDIR}/Peripherals/pin_manager.p1 ${OBJECTDIR}/Peripherals/tmr2.p1 ${OBJECTDIR}/Peripherals/tmr0.p1 ${OBJECTDIR}/Peripherals/pwm.p1 ${OBJECTDIR}/Peripherals/ausart.p1 ${OBJECTDIR}/main.p1 ${OBJECTDIR}/port/portevent.p1 ${OBJECTDIR}/port/portserial.p1 ${OBJECTDIR}/port/porttimer.p1 ${OBJECTDIR}/modbus/ascii/mbascii.p1 ${OBJECTDIR}/modbus/functions/mbfunccoils.p1 ${OBJECTDIR}/modbus/functions/mbfuncdiag.p1 ${OBJECTDIR}/modbus/functions/mbfuncdisc.p1 ${OBJECTDIR}/modbus/functions/mbfuncholding.p1 ${OBJECTDIR}/modbus/functions/mbfuncinput.p1 ${OBJECTDIR}/modbus/functions/mbfuncother.p1 ${OBJECTDIR}/modbus/functions/mbutils.p1 ${OBJECTDIR}/modbus/mb.p1 ${OBJECTDIR}/modbus/rtu/mbcrc.p1 ${OBJECTDIR}/modbus/rtu/mbrtu.p1 ${OBJECTDIR}/modbus/tcp/mbtcp.p1

# Source Files
SOURCEFILES=Peripherals/config.c Peripherals/INT0.c Peripherals/interrupt_manager.c Peripherals/pin_manager.c Peripherals/tmr2.c Peripherals/tmr0.c Peripherals/pwm.c Peripherals/ausart.c main.c port/portevent.c port/portserial.c port/porttimer.c modbus/ascii/mbascii.c modbus/functions/mbfunccoils.c modbus/functions/mbfuncdiag.c modbus/functions/mbfuncdisc.c modbus/functions/mbfuncholding.c modbus/functions/mbfuncinput.c modbus/functions/mbfuncother.c modbus/functions/mbutils.c modbus/mb.c modbus/rtu/mbcrc.c modbus/rtu/mbrtu.c modbus/tcp/mbtcp.c


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
	${MAKE}  -f nbproject/Makefile-default.mk dist/${CND_CONF}/${IMAGE_TYPE}/SiebwaldeTrackAmplifier2.X.${IMAGE_TYPE}.${OUTPUT_SUFFIX}

MP_PROCESSOR_OPTION=16F767
# ------------------------------------------------------------------------------------
# Rules for buildStep: compile
ifeq ($(TYPE_IMAGE), DEBUG_RUN)
${OBJECTDIR}/Peripherals/config.p1: Peripherals/config.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Peripherals" 
	@${RM} ${OBJECTDIR}/Peripherals/config.p1.d 
	@${RM} ${OBJECTDIR}/Peripherals/config.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=pickit3  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/Peripherals/config.p1  Peripherals/config.c 
	@-${MV} ${OBJECTDIR}/Peripherals/config.d ${OBJECTDIR}/Peripherals/config.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/Peripherals/config.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/Peripherals/INT0.p1: Peripherals/INT0.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Peripherals" 
	@${RM} ${OBJECTDIR}/Peripherals/INT0.p1.d 
	@${RM} ${OBJECTDIR}/Peripherals/INT0.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=pickit3  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/Peripherals/INT0.p1  Peripherals/INT0.c 
	@-${MV} ${OBJECTDIR}/Peripherals/INT0.d ${OBJECTDIR}/Peripherals/INT0.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/Peripherals/INT0.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/Peripherals/interrupt_manager.p1: Peripherals/interrupt_manager.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Peripherals" 
	@${RM} ${OBJECTDIR}/Peripherals/interrupt_manager.p1.d 
	@${RM} ${OBJECTDIR}/Peripherals/interrupt_manager.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=pickit3  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/Peripherals/interrupt_manager.p1  Peripherals/interrupt_manager.c 
	@-${MV} ${OBJECTDIR}/Peripherals/interrupt_manager.d ${OBJECTDIR}/Peripherals/interrupt_manager.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/Peripherals/interrupt_manager.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/Peripherals/pin_manager.p1: Peripherals/pin_manager.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Peripherals" 
	@${RM} ${OBJECTDIR}/Peripherals/pin_manager.p1.d 
	@${RM} ${OBJECTDIR}/Peripherals/pin_manager.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=pickit3  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/Peripherals/pin_manager.p1  Peripherals/pin_manager.c 
	@-${MV} ${OBJECTDIR}/Peripherals/pin_manager.d ${OBJECTDIR}/Peripherals/pin_manager.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/Peripherals/pin_manager.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/Peripherals/tmr2.p1: Peripherals/tmr2.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Peripherals" 
	@${RM} ${OBJECTDIR}/Peripherals/tmr2.p1.d 
	@${RM} ${OBJECTDIR}/Peripherals/tmr2.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=pickit3  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/Peripherals/tmr2.p1  Peripherals/tmr2.c 
	@-${MV} ${OBJECTDIR}/Peripherals/tmr2.d ${OBJECTDIR}/Peripherals/tmr2.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/Peripherals/tmr2.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/Peripherals/tmr0.p1: Peripherals/tmr0.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Peripherals" 
	@${RM} ${OBJECTDIR}/Peripherals/tmr0.p1.d 
	@${RM} ${OBJECTDIR}/Peripherals/tmr0.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=pickit3  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/Peripherals/tmr0.p1  Peripherals/tmr0.c 
	@-${MV} ${OBJECTDIR}/Peripherals/tmr0.d ${OBJECTDIR}/Peripherals/tmr0.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/Peripherals/tmr0.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/Peripherals/pwm.p1: Peripherals/pwm.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Peripherals" 
	@${RM} ${OBJECTDIR}/Peripherals/pwm.p1.d 
	@${RM} ${OBJECTDIR}/Peripherals/pwm.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=pickit3  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/Peripherals/pwm.p1  Peripherals/pwm.c 
	@-${MV} ${OBJECTDIR}/Peripherals/pwm.d ${OBJECTDIR}/Peripherals/pwm.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/Peripherals/pwm.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/Peripherals/ausart.p1: Peripherals/ausart.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Peripherals" 
	@${RM} ${OBJECTDIR}/Peripherals/ausart.p1.d 
	@${RM} ${OBJECTDIR}/Peripherals/ausart.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=pickit3  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/Peripherals/ausart.p1  Peripherals/ausart.c 
	@-${MV} ${OBJECTDIR}/Peripherals/ausart.d ${OBJECTDIR}/Peripherals/ausart.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/Peripherals/ausart.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/main.p1: main.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/main.p1.d 
	@${RM} ${OBJECTDIR}/main.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=pickit3  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/main.p1  main.c 
	@-${MV} ${OBJECTDIR}/main.d ${OBJECTDIR}/main.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/main.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/port/portevent.p1: port/portevent.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/port" 
	@${RM} ${OBJECTDIR}/port/portevent.p1.d 
	@${RM} ${OBJECTDIR}/port/portevent.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=pickit3  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/port/portevent.p1  port/portevent.c 
	@-${MV} ${OBJECTDIR}/port/portevent.d ${OBJECTDIR}/port/portevent.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/port/portevent.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/port/portserial.p1: port/portserial.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/port" 
	@${RM} ${OBJECTDIR}/port/portserial.p1.d 
	@${RM} ${OBJECTDIR}/port/portserial.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=pickit3  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/port/portserial.p1  port/portserial.c 
	@-${MV} ${OBJECTDIR}/port/portserial.d ${OBJECTDIR}/port/portserial.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/port/portserial.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/port/porttimer.p1: port/porttimer.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/port" 
	@${RM} ${OBJECTDIR}/port/porttimer.p1.d 
	@${RM} ${OBJECTDIR}/port/porttimer.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=pickit3  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/port/porttimer.p1  port/porttimer.c 
	@-${MV} ${OBJECTDIR}/port/porttimer.d ${OBJECTDIR}/port/porttimer.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/port/porttimer.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/modbus/ascii/mbascii.p1: modbus/ascii/mbascii.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus/ascii" 
	@${RM} ${OBJECTDIR}/modbus/ascii/mbascii.p1.d 
	@${RM} ${OBJECTDIR}/modbus/ascii/mbascii.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=pickit3  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/modbus/ascii/mbascii.p1  modbus/ascii/mbascii.c 
	@-${MV} ${OBJECTDIR}/modbus/ascii/mbascii.d ${OBJECTDIR}/modbus/ascii/mbascii.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/modbus/ascii/mbascii.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/modbus/functions/mbfunccoils.p1: modbus/functions/mbfunccoils.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus/functions" 
	@${RM} ${OBJECTDIR}/modbus/functions/mbfunccoils.p1.d 
	@${RM} ${OBJECTDIR}/modbus/functions/mbfunccoils.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=pickit3  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/modbus/functions/mbfunccoils.p1  modbus/functions/mbfunccoils.c 
	@-${MV} ${OBJECTDIR}/modbus/functions/mbfunccoils.d ${OBJECTDIR}/modbus/functions/mbfunccoils.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/modbus/functions/mbfunccoils.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/modbus/functions/mbfuncdiag.p1: modbus/functions/mbfuncdiag.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus/functions" 
	@${RM} ${OBJECTDIR}/modbus/functions/mbfuncdiag.p1.d 
	@${RM} ${OBJECTDIR}/modbus/functions/mbfuncdiag.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=pickit3  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/modbus/functions/mbfuncdiag.p1  modbus/functions/mbfuncdiag.c 
	@-${MV} ${OBJECTDIR}/modbus/functions/mbfuncdiag.d ${OBJECTDIR}/modbus/functions/mbfuncdiag.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/modbus/functions/mbfuncdiag.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/modbus/functions/mbfuncdisc.p1: modbus/functions/mbfuncdisc.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus/functions" 
	@${RM} ${OBJECTDIR}/modbus/functions/mbfuncdisc.p1.d 
	@${RM} ${OBJECTDIR}/modbus/functions/mbfuncdisc.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=pickit3  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/modbus/functions/mbfuncdisc.p1  modbus/functions/mbfuncdisc.c 
	@-${MV} ${OBJECTDIR}/modbus/functions/mbfuncdisc.d ${OBJECTDIR}/modbus/functions/mbfuncdisc.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/modbus/functions/mbfuncdisc.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/modbus/functions/mbfuncholding.p1: modbus/functions/mbfuncholding.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus/functions" 
	@${RM} ${OBJECTDIR}/modbus/functions/mbfuncholding.p1.d 
	@${RM} ${OBJECTDIR}/modbus/functions/mbfuncholding.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=pickit3  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/modbus/functions/mbfuncholding.p1  modbus/functions/mbfuncholding.c 
	@-${MV} ${OBJECTDIR}/modbus/functions/mbfuncholding.d ${OBJECTDIR}/modbus/functions/mbfuncholding.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/modbus/functions/mbfuncholding.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/modbus/functions/mbfuncinput.p1: modbus/functions/mbfuncinput.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus/functions" 
	@${RM} ${OBJECTDIR}/modbus/functions/mbfuncinput.p1.d 
	@${RM} ${OBJECTDIR}/modbus/functions/mbfuncinput.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=pickit3  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/modbus/functions/mbfuncinput.p1  modbus/functions/mbfuncinput.c 
	@-${MV} ${OBJECTDIR}/modbus/functions/mbfuncinput.d ${OBJECTDIR}/modbus/functions/mbfuncinput.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/modbus/functions/mbfuncinput.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/modbus/functions/mbfuncother.p1: modbus/functions/mbfuncother.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus/functions" 
	@${RM} ${OBJECTDIR}/modbus/functions/mbfuncother.p1.d 
	@${RM} ${OBJECTDIR}/modbus/functions/mbfuncother.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=pickit3  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/modbus/functions/mbfuncother.p1  modbus/functions/mbfuncother.c 
	@-${MV} ${OBJECTDIR}/modbus/functions/mbfuncother.d ${OBJECTDIR}/modbus/functions/mbfuncother.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/modbus/functions/mbfuncother.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/modbus/functions/mbutils.p1: modbus/functions/mbutils.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus/functions" 
	@${RM} ${OBJECTDIR}/modbus/functions/mbutils.p1.d 
	@${RM} ${OBJECTDIR}/modbus/functions/mbutils.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=pickit3  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/modbus/functions/mbutils.p1  modbus/functions/mbutils.c 
	@-${MV} ${OBJECTDIR}/modbus/functions/mbutils.d ${OBJECTDIR}/modbus/functions/mbutils.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/modbus/functions/mbutils.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/modbus/mb.p1: modbus/mb.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus" 
	@${RM} ${OBJECTDIR}/modbus/mb.p1.d 
	@${RM} ${OBJECTDIR}/modbus/mb.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=pickit3  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/modbus/mb.p1  modbus/mb.c 
	@-${MV} ${OBJECTDIR}/modbus/mb.d ${OBJECTDIR}/modbus/mb.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/modbus/mb.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/modbus/rtu/mbcrc.p1: modbus/rtu/mbcrc.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus/rtu" 
	@${RM} ${OBJECTDIR}/modbus/rtu/mbcrc.p1.d 
	@${RM} ${OBJECTDIR}/modbus/rtu/mbcrc.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=pickit3  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/modbus/rtu/mbcrc.p1  modbus/rtu/mbcrc.c 
	@-${MV} ${OBJECTDIR}/modbus/rtu/mbcrc.d ${OBJECTDIR}/modbus/rtu/mbcrc.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/modbus/rtu/mbcrc.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/modbus/rtu/mbrtu.p1: modbus/rtu/mbrtu.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus/rtu" 
	@${RM} ${OBJECTDIR}/modbus/rtu/mbrtu.p1.d 
	@${RM} ${OBJECTDIR}/modbus/rtu/mbrtu.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=pickit3  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/modbus/rtu/mbrtu.p1  modbus/rtu/mbrtu.c 
	@-${MV} ${OBJECTDIR}/modbus/rtu/mbrtu.d ${OBJECTDIR}/modbus/rtu/mbrtu.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/modbus/rtu/mbrtu.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/modbus/tcp/mbtcp.p1: modbus/tcp/mbtcp.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus/tcp" 
	@${RM} ${OBJECTDIR}/modbus/tcp/mbtcp.p1.d 
	@${RM} ${OBJECTDIR}/modbus/tcp/mbtcp.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=pickit3  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/modbus/tcp/mbtcp.p1  modbus/tcp/mbtcp.c 
	@-${MV} ${OBJECTDIR}/modbus/tcp/mbtcp.d ${OBJECTDIR}/modbus/tcp/mbtcp.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/modbus/tcp/mbtcp.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
else
${OBJECTDIR}/Peripherals/config.p1: Peripherals/config.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Peripherals" 
	@${RM} ${OBJECTDIR}/Peripherals/config.p1.d 
	@${RM} ${OBJECTDIR}/Peripherals/config.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/Peripherals/config.p1  Peripherals/config.c 
	@-${MV} ${OBJECTDIR}/Peripherals/config.d ${OBJECTDIR}/Peripherals/config.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/Peripherals/config.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/Peripherals/INT0.p1: Peripherals/INT0.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Peripherals" 
	@${RM} ${OBJECTDIR}/Peripherals/INT0.p1.d 
	@${RM} ${OBJECTDIR}/Peripherals/INT0.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/Peripherals/INT0.p1  Peripherals/INT0.c 
	@-${MV} ${OBJECTDIR}/Peripherals/INT0.d ${OBJECTDIR}/Peripherals/INT0.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/Peripherals/INT0.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/Peripherals/interrupt_manager.p1: Peripherals/interrupt_manager.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Peripherals" 
	@${RM} ${OBJECTDIR}/Peripherals/interrupt_manager.p1.d 
	@${RM} ${OBJECTDIR}/Peripherals/interrupt_manager.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/Peripherals/interrupt_manager.p1  Peripherals/interrupt_manager.c 
	@-${MV} ${OBJECTDIR}/Peripherals/interrupt_manager.d ${OBJECTDIR}/Peripherals/interrupt_manager.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/Peripherals/interrupt_manager.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/Peripherals/pin_manager.p1: Peripherals/pin_manager.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Peripherals" 
	@${RM} ${OBJECTDIR}/Peripherals/pin_manager.p1.d 
	@${RM} ${OBJECTDIR}/Peripherals/pin_manager.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/Peripherals/pin_manager.p1  Peripherals/pin_manager.c 
	@-${MV} ${OBJECTDIR}/Peripherals/pin_manager.d ${OBJECTDIR}/Peripherals/pin_manager.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/Peripherals/pin_manager.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/Peripherals/tmr2.p1: Peripherals/tmr2.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Peripherals" 
	@${RM} ${OBJECTDIR}/Peripherals/tmr2.p1.d 
	@${RM} ${OBJECTDIR}/Peripherals/tmr2.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/Peripherals/tmr2.p1  Peripherals/tmr2.c 
	@-${MV} ${OBJECTDIR}/Peripherals/tmr2.d ${OBJECTDIR}/Peripherals/tmr2.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/Peripherals/tmr2.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/Peripherals/tmr0.p1: Peripherals/tmr0.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Peripherals" 
	@${RM} ${OBJECTDIR}/Peripherals/tmr0.p1.d 
	@${RM} ${OBJECTDIR}/Peripherals/tmr0.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/Peripherals/tmr0.p1  Peripherals/tmr0.c 
	@-${MV} ${OBJECTDIR}/Peripherals/tmr0.d ${OBJECTDIR}/Peripherals/tmr0.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/Peripherals/tmr0.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/Peripherals/pwm.p1: Peripherals/pwm.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Peripherals" 
	@${RM} ${OBJECTDIR}/Peripherals/pwm.p1.d 
	@${RM} ${OBJECTDIR}/Peripherals/pwm.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/Peripherals/pwm.p1  Peripherals/pwm.c 
	@-${MV} ${OBJECTDIR}/Peripherals/pwm.d ${OBJECTDIR}/Peripherals/pwm.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/Peripherals/pwm.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/Peripherals/ausart.p1: Peripherals/ausart.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/Peripherals" 
	@${RM} ${OBJECTDIR}/Peripherals/ausart.p1.d 
	@${RM} ${OBJECTDIR}/Peripherals/ausart.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/Peripherals/ausart.p1  Peripherals/ausart.c 
	@-${MV} ${OBJECTDIR}/Peripherals/ausart.d ${OBJECTDIR}/Peripherals/ausart.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/Peripherals/ausart.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/main.p1: main.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}" 
	@${RM} ${OBJECTDIR}/main.p1.d 
	@${RM} ${OBJECTDIR}/main.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/main.p1  main.c 
	@-${MV} ${OBJECTDIR}/main.d ${OBJECTDIR}/main.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/main.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/port/portevent.p1: port/portevent.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/port" 
	@${RM} ${OBJECTDIR}/port/portevent.p1.d 
	@${RM} ${OBJECTDIR}/port/portevent.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/port/portevent.p1  port/portevent.c 
	@-${MV} ${OBJECTDIR}/port/portevent.d ${OBJECTDIR}/port/portevent.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/port/portevent.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/port/portserial.p1: port/portserial.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/port" 
	@${RM} ${OBJECTDIR}/port/portserial.p1.d 
	@${RM} ${OBJECTDIR}/port/portserial.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/port/portserial.p1  port/portserial.c 
	@-${MV} ${OBJECTDIR}/port/portserial.d ${OBJECTDIR}/port/portserial.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/port/portserial.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/port/porttimer.p1: port/porttimer.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/port" 
	@${RM} ${OBJECTDIR}/port/porttimer.p1.d 
	@${RM} ${OBJECTDIR}/port/porttimer.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/port/porttimer.p1  port/porttimer.c 
	@-${MV} ${OBJECTDIR}/port/porttimer.d ${OBJECTDIR}/port/porttimer.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/port/porttimer.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/modbus/ascii/mbascii.p1: modbus/ascii/mbascii.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus/ascii" 
	@${RM} ${OBJECTDIR}/modbus/ascii/mbascii.p1.d 
	@${RM} ${OBJECTDIR}/modbus/ascii/mbascii.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/modbus/ascii/mbascii.p1  modbus/ascii/mbascii.c 
	@-${MV} ${OBJECTDIR}/modbus/ascii/mbascii.d ${OBJECTDIR}/modbus/ascii/mbascii.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/modbus/ascii/mbascii.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/modbus/functions/mbfunccoils.p1: modbus/functions/mbfunccoils.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus/functions" 
	@${RM} ${OBJECTDIR}/modbus/functions/mbfunccoils.p1.d 
	@${RM} ${OBJECTDIR}/modbus/functions/mbfunccoils.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/modbus/functions/mbfunccoils.p1  modbus/functions/mbfunccoils.c 
	@-${MV} ${OBJECTDIR}/modbus/functions/mbfunccoils.d ${OBJECTDIR}/modbus/functions/mbfunccoils.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/modbus/functions/mbfunccoils.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/modbus/functions/mbfuncdiag.p1: modbus/functions/mbfuncdiag.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus/functions" 
	@${RM} ${OBJECTDIR}/modbus/functions/mbfuncdiag.p1.d 
	@${RM} ${OBJECTDIR}/modbus/functions/mbfuncdiag.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/modbus/functions/mbfuncdiag.p1  modbus/functions/mbfuncdiag.c 
	@-${MV} ${OBJECTDIR}/modbus/functions/mbfuncdiag.d ${OBJECTDIR}/modbus/functions/mbfuncdiag.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/modbus/functions/mbfuncdiag.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/modbus/functions/mbfuncdisc.p1: modbus/functions/mbfuncdisc.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus/functions" 
	@${RM} ${OBJECTDIR}/modbus/functions/mbfuncdisc.p1.d 
	@${RM} ${OBJECTDIR}/modbus/functions/mbfuncdisc.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/modbus/functions/mbfuncdisc.p1  modbus/functions/mbfuncdisc.c 
	@-${MV} ${OBJECTDIR}/modbus/functions/mbfuncdisc.d ${OBJECTDIR}/modbus/functions/mbfuncdisc.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/modbus/functions/mbfuncdisc.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/modbus/functions/mbfuncholding.p1: modbus/functions/mbfuncholding.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus/functions" 
	@${RM} ${OBJECTDIR}/modbus/functions/mbfuncholding.p1.d 
	@${RM} ${OBJECTDIR}/modbus/functions/mbfuncholding.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/modbus/functions/mbfuncholding.p1  modbus/functions/mbfuncholding.c 
	@-${MV} ${OBJECTDIR}/modbus/functions/mbfuncholding.d ${OBJECTDIR}/modbus/functions/mbfuncholding.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/modbus/functions/mbfuncholding.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/modbus/functions/mbfuncinput.p1: modbus/functions/mbfuncinput.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus/functions" 
	@${RM} ${OBJECTDIR}/modbus/functions/mbfuncinput.p1.d 
	@${RM} ${OBJECTDIR}/modbus/functions/mbfuncinput.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/modbus/functions/mbfuncinput.p1  modbus/functions/mbfuncinput.c 
	@-${MV} ${OBJECTDIR}/modbus/functions/mbfuncinput.d ${OBJECTDIR}/modbus/functions/mbfuncinput.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/modbus/functions/mbfuncinput.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/modbus/functions/mbfuncother.p1: modbus/functions/mbfuncother.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus/functions" 
	@${RM} ${OBJECTDIR}/modbus/functions/mbfuncother.p1.d 
	@${RM} ${OBJECTDIR}/modbus/functions/mbfuncother.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/modbus/functions/mbfuncother.p1  modbus/functions/mbfuncother.c 
	@-${MV} ${OBJECTDIR}/modbus/functions/mbfuncother.d ${OBJECTDIR}/modbus/functions/mbfuncother.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/modbus/functions/mbfuncother.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/modbus/functions/mbutils.p1: modbus/functions/mbutils.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus/functions" 
	@${RM} ${OBJECTDIR}/modbus/functions/mbutils.p1.d 
	@${RM} ${OBJECTDIR}/modbus/functions/mbutils.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/modbus/functions/mbutils.p1  modbus/functions/mbutils.c 
	@-${MV} ${OBJECTDIR}/modbus/functions/mbutils.d ${OBJECTDIR}/modbus/functions/mbutils.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/modbus/functions/mbutils.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/modbus/mb.p1: modbus/mb.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus" 
	@${RM} ${OBJECTDIR}/modbus/mb.p1.d 
	@${RM} ${OBJECTDIR}/modbus/mb.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/modbus/mb.p1  modbus/mb.c 
	@-${MV} ${OBJECTDIR}/modbus/mb.d ${OBJECTDIR}/modbus/mb.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/modbus/mb.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/modbus/rtu/mbcrc.p1: modbus/rtu/mbcrc.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus/rtu" 
	@${RM} ${OBJECTDIR}/modbus/rtu/mbcrc.p1.d 
	@${RM} ${OBJECTDIR}/modbus/rtu/mbcrc.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/modbus/rtu/mbcrc.p1  modbus/rtu/mbcrc.c 
	@-${MV} ${OBJECTDIR}/modbus/rtu/mbcrc.d ${OBJECTDIR}/modbus/rtu/mbcrc.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/modbus/rtu/mbcrc.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/modbus/rtu/mbrtu.p1: modbus/rtu/mbrtu.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus/rtu" 
	@${RM} ${OBJECTDIR}/modbus/rtu/mbrtu.p1.d 
	@${RM} ${OBJECTDIR}/modbus/rtu/mbrtu.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/modbus/rtu/mbrtu.p1  modbus/rtu/mbrtu.c 
	@-${MV} ${OBJECTDIR}/modbus/rtu/mbrtu.d ${OBJECTDIR}/modbus/rtu/mbrtu.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/modbus/rtu/mbrtu.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/modbus/tcp/mbtcp.p1: modbus/tcp/mbtcp.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/modbus/tcp" 
	@${RM} ${OBJECTDIR}/modbus/tcp/mbtcp.p1.d 
	@${RM} ${OBJECTDIR}/modbus/tcp/mbtcp.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib $(COMPARISON_BUILD)  --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"    -o${OBJECTDIR}/modbus/tcp/mbtcp.p1  modbus/tcp/mbtcp.c 
	@-${MV} ${OBJECTDIR}/modbus/tcp/mbtcp.d ${OBJECTDIR}/modbus/tcp/mbtcp.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/modbus/tcp/mbtcp.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
endif

# ------------------------------------------------------------------------------------
# Rules for buildStep: assemble
ifeq ($(TYPE_IMAGE), DEBUG_RUN)
else
endif

# ------------------------------------------------------------------------------------
# Rules for buildStep: link
ifeq ($(TYPE_IMAGE), DEBUG_RUN)
dist/${CND_CONF}/${IMAGE_TYPE}/SiebwaldeTrackAmplifier2.X.${IMAGE_TYPE}.${OUTPUT_SUFFIX}: ${OBJECTFILES}  nbproject/Makefile-${CND_CONF}.mk    
	@${MKDIR} dist/${CND_CONF}/${IMAGE_TYPE} 
	${MP_CC} $(MP_EXTRA_LD_PRE) --chip=$(MP_PROCESSOR_OPTION) -G -mdist/${CND_CONF}/${IMAGE_TYPE}/SiebwaldeTrackAmplifier2.X.${IMAGE_TYPE}.map  -D__DEBUG=1 --debugger=pickit3  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"     --rom=default,-1f00-1fff --ram=default,-0-0,-70-70,-80-80,-f0-f0,-100-100,-165-170,-180-180,-1f0-1f0  $(COMPARISON_BUILD) --memorysummary dist/${CND_CONF}/${IMAGE_TYPE}/memoryfile.xml -odist/${CND_CONF}/${IMAGE_TYPE}/SiebwaldeTrackAmplifier2.X.${IMAGE_TYPE}.${DEBUGGABLE_SUFFIX}  ${OBJECTFILES_QUOTED_IF_SPACED}     
	@${RM} dist/${CND_CONF}/${IMAGE_TYPE}/SiebwaldeTrackAmplifier2.X.${IMAGE_TYPE}.hex 
	
else
dist/${CND_CONF}/${IMAGE_TYPE}/SiebwaldeTrackAmplifier2.X.${IMAGE_TYPE}.${OUTPUT_SUFFIX}: ${OBJECTFILES}  nbproject/Makefile-${CND_CONF}.mk   
	@${MKDIR} dist/${CND_CONF}/${IMAGE_TYPE} 
	${MP_CC} $(MP_EXTRA_LD_PRE) --chip=$(MP_PROCESSOR_OPTION) -G -mdist/${CND_CONF}/${IMAGE_TYPE}/SiebwaldeTrackAmplifier2.X.${IMAGE_TYPE}.map  --double=24 --float=24 --opt=+asm,+asmfile,-speed,+space,-debug,-local --addrqual=ignore --mode=free -P -N255 -I"port" -I"modbus/include" -I"modbus/rtu" -I"modbus" -I"modbus/ascii" --warn=0 --asmlist -DXPRJ_default=$(CND_CONF)  --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-osccal,-resetbits,-download,-stackcall,+clib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto "--errformat=%f:%l: error: (%n) %s" "--warnformat=%f:%l: warning: (%n) %s" "--msgformat=%f:%l: advisory: (%n) %s"     $(COMPARISON_BUILD) --memorysummary dist/${CND_CONF}/${IMAGE_TYPE}/memoryfile.xml -odist/${CND_CONF}/${IMAGE_TYPE}/SiebwaldeTrackAmplifier2.X.${IMAGE_TYPE}.${DEBUGGABLE_SUFFIX}  ${OBJECTFILES_QUOTED_IF_SPACED}     
	
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
