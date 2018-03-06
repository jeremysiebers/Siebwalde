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
FINAL_IMAGE=dist/${CND_CONF}/${IMAGE_TYPE}/demo.X.${IMAGE_TYPE}.${OUTPUT_SUFFIX}
else
IMAGE_TYPE=production
OUTPUT_SUFFIX=hex
DEBUGGABLE_SUFFIX=elf
FINAL_IMAGE=dist/${CND_CONF}/${IMAGE_TYPE}/demo.X.${IMAGE_TYPE}.${OUTPUT_SUFFIX}
endif

ifeq ($(COMPARE_BUILD), true)
COMPARISON_BUILD=-mafrlcsj
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
SOURCEFILES_QUOTED_IF_SPACED=../demo.c ../modbus/rtu/mbcrc.c ../modbus/rtu/mbrtu.c ../modbus/mb.c ../modbus/functions/mbfunccoils.c ../modbus/functions/mbfuncdiag.c ../modbus/functions/mbfuncdisc.c ../modbus/functions/mbfuncholding.c ../modbus/functions/mbfuncinput.c ../modbus/functions/mbfuncother.c ../modbus/functions/mbutils.c ../port/portevent.c ../port/portserial.c ../port/porttimer.c ../modbus/ascii/mbascii.c

# Object Files Quoted if spaced
OBJECTFILES_QUOTED_IF_SPACED=${OBJECTDIR}/_ext/1472/demo.o ${OBJECTDIR}/_ext/2104501039/mbcrc.o ${OBJECTDIR}/_ext/2104501039/mbrtu.o ${OBJECTDIR}/_ext/984464531/mb.o ${OBJECTDIR}/_ext/669684871/mbfunccoils.o ${OBJECTDIR}/_ext/669684871/mbfuncdiag.o ${OBJECTDIR}/_ext/669684871/mbfuncdisc.o ${OBJECTDIR}/_ext/669684871/mbfuncholding.o ${OBJECTDIR}/_ext/669684871/mbfuncinput.o ${OBJECTDIR}/_ext/669684871/mbfuncother.o ${OBJECTDIR}/_ext/669684871/mbutils.o ${OBJECTDIR}/_ext/760710288/portevent.o ${OBJECTDIR}/_ext/760710288/portserial.o ${OBJECTDIR}/_ext/760710288/porttimer.o ${OBJECTDIR}/_ext/488354351/mbascii.o
POSSIBLE_DEPFILES=${OBJECTDIR}/_ext/1472/demo.o.d ${OBJECTDIR}/_ext/2104501039/mbcrc.o.d ${OBJECTDIR}/_ext/2104501039/mbrtu.o.d ${OBJECTDIR}/_ext/984464531/mb.o.d ${OBJECTDIR}/_ext/669684871/mbfunccoils.o.d ${OBJECTDIR}/_ext/669684871/mbfuncdiag.o.d ${OBJECTDIR}/_ext/669684871/mbfuncdisc.o.d ${OBJECTDIR}/_ext/669684871/mbfuncholding.o.d ${OBJECTDIR}/_ext/669684871/mbfuncinput.o.d ${OBJECTDIR}/_ext/669684871/mbfuncother.o.d ${OBJECTDIR}/_ext/669684871/mbutils.o.d ${OBJECTDIR}/_ext/760710288/portevent.o.d ${OBJECTDIR}/_ext/760710288/portserial.o.d ${OBJECTDIR}/_ext/760710288/porttimer.o.d ${OBJECTDIR}/_ext/488354351/mbascii.o.d

# Object Files
OBJECTFILES=${OBJECTDIR}/_ext/1472/demo.o ${OBJECTDIR}/_ext/2104501039/mbcrc.o ${OBJECTDIR}/_ext/2104501039/mbrtu.o ${OBJECTDIR}/_ext/984464531/mb.o ${OBJECTDIR}/_ext/669684871/mbfunccoils.o ${OBJECTDIR}/_ext/669684871/mbfuncdiag.o ${OBJECTDIR}/_ext/669684871/mbfuncdisc.o ${OBJECTDIR}/_ext/669684871/mbfuncholding.o ${OBJECTDIR}/_ext/669684871/mbfuncinput.o ${OBJECTDIR}/_ext/669684871/mbfuncother.o ${OBJECTDIR}/_ext/669684871/mbutils.o ${OBJECTDIR}/_ext/760710288/portevent.o ${OBJECTDIR}/_ext/760710288/portserial.o ${OBJECTDIR}/_ext/760710288/porttimer.o ${OBJECTDIR}/_ext/488354351/mbascii.o

# Source Files
SOURCEFILES=../demo.c ../modbus/rtu/mbcrc.c ../modbus/rtu/mbrtu.c ../modbus/mb.c ../modbus/functions/mbfunccoils.c ../modbus/functions/mbfuncdiag.c ../modbus/functions/mbfuncdisc.c ../modbus/functions/mbfuncholding.c ../modbus/functions/mbfuncinput.c ../modbus/functions/mbfuncother.c ../modbus/functions/mbutils.c ../port/portevent.c ../port/portserial.c ../port/porttimer.c ../modbus/ascii/mbascii.c


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
	${MAKE}  -f nbproject/Makefile-default.mk dist/${CND_CONF}/${IMAGE_TYPE}/demo.X.${IMAGE_TYPE}.${OUTPUT_SUFFIX}

MP_PROCESSOR_OPTION=32MX460F512L
MP_LINKER_FILE_OPTION=
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
# Rules for buildStep: compile
ifeq ($(TYPE_IMAGE), DEBUG_RUN)
${OBJECTDIR}/_ext/1472/demo.o: ../demo.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/_ext/1472" 
	@${RM} ${OBJECTDIR}/_ext/1472/demo.o.d 
	@${RM} ${OBJECTDIR}/_ext/1472/demo.o 
	@${FIXDEPS} "${OBJECTDIR}/_ext/1472/demo.o.d" $(SILENT) -rsi ${MP_CC_DIR}../  -c ${MP_CC}  $(MP_EXTRA_CC_PRE) -g -D__DEBUG -DPICkit3PlatformTool=1  -fframe-base-loclist  -x c -c -mprocessor=$(MP_PROCESSOR_OPTION)  -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/ascii" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/functions" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/include" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/rtu" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/tcp" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/port" -I"../modbus/ascii" -I"../modbus/include" -I"../modbus/rtu" -I"../port" -I"." -MMD -MF "${OBJECTDIR}/_ext/1472/demo.o.d" -o ${OBJECTDIR}/_ext/1472/demo.o ../demo.c    -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD) 
	
${OBJECTDIR}/_ext/2104501039/mbcrc.o: ../modbus/rtu/mbcrc.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/_ext/2104501039" 
	@${RM} ${OBJECTDIR}/_ext/2104501039/mbcrc.o.d 
	@${RM} ${OBJECTDIR}/_ext/2104501039/mbcrc.o 
	@${FIXDEPS} "${OBJECTDIR}/_ext/2104501039/mbcrc.o.d" $(SILENT) -rsi ${MP_CC_DIR}../  -c ${MP_CC}  $(MP_EXTRA_CC_PRE) -g -D__DEBUG -DPICkit3PlatformTool=1  -fframe-base-loclist  -x c -c -mprocessor=$(MP_PROCESSOR_OPTION)  -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/ascii" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/functions" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/include" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/rtu" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/tcp" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/port" -I"../modbus/ascii" -I"../modbus/include" -I"../modbus/rtu" -I"../port" -I"." -MMD -MF "${OBJECTDIR}/_ext/2104501039/mbcrc.o.d" -o ${OBJECTDIR}/_ext/2104501039/mbcrc.o ../modbus/rtu/mbcrc.c    -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD) 
	
${OBJECTDIR}/_ext/2104501039/mbrtu.o: ../modbus/rtu/mbrtu.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/_ext/2104501039" 
	@${RM} ${OBJECTDIR}/_ext/2104501039/mbrtu.o.d 
	@${RM} ${OBJECTDIR}/_ext/2104501039/mbrtu.o 
	@${FIXDEPS} "${OBJECTDIR}/_ext/2104501039/mbrtu.o.d" $(SILENT) -rsi ${MP_CC_DIR}../  -c ${MP_CC}  $(MP_EXTRA_CC_PRE) -g -D__DEBUG -DPICkit3PlatformTool=1  -fframe-base-loclist  -x c -c -mprocessor=$(MP_PROCESSOR_OPTION)  -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/ascii" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/functions" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/include" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/rtu" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/tcp" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/port" -I"../modbus/ascii" -I"../modbus/include" -I"../modbus/rtu" -I"../port" -I"." -MMD -MF "${OBJECTDIR}/_ext/2104501039/mbrtu.o.d" -o ${OBJECTDIR}/_ext/2104501039/mbrtu.o ../modbus/rtu/mbrtu.c    -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD) 
	
${OBJECTDIR}/_ext/984464531/mb.o: ../modbus/mb.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/_ext/984464531" 
	@${RM} ${OBJECTDIR}/_ext/984464531/mb.o.d 
	@${RM} ${OBJECTDIR}/_ext/984464531/mb.o 
	@${FIXDEPS} "${OBJECTDIR}/_ext/984464531/mb.o.d" $(SILENT) -rsi ${MP_CC_DIR}../  -c ${MP_CC}  $(MP_EXTRA_CC_PRE) -g -D__DEBUG -DPICkit3PlatformTool=1  -fframe-base-loclist  -x c -c -mprocessor=$(MP_PROCESSOR_OPTION)  -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/ascii" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/functions" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/include" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/rtu" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/tcp" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/port" -I"../modbus/ascii" -I"../modbus/include" -I"../modbus/rtu" -I"../port" -I"." -MMD -MF "${OBJECTDIR}/_ext/984464531/mb.o.d" -o ${OBJECTDIR}/_ext/984464531/mb.o ../modbus/mb.c    -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD) 
	
${OBJECTDIR}/_ext/669684871/mbfunccoils.o: ../modbus/functions/mbfunccoils.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/_ext/669684871" 
	@${RM} ${OBJECTDIR}/_ext/669684871/mbfunccoils.o.d 
	@${RM} ${OBJECTDIR}/_ext/669684871/mbfunccoils.o 
	@${FIXDEPS} "${OBJECTDIR}/_ext/669684871/mbfunccoils.o.d" $(SILENT) -rsi ${MP_CC_DIR}../  -c ${MP_CC}  $(MP_EXTRA_CC_PRE) -g -D__DEBUG -DPICkit3PlatformTool=1  -fframe-base-loclist  -x c -c -mprocessor=$(MP_PROCESSOR_OPTION)  -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/ascii" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/functions" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/include" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/rtu" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/tcp" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/port" -I"../modbus/ascii" -I"../modbus/include" -I"../modbus/rtu" -I"../port" -I"." -MMD -MF "${OBJECTDIR}/_ext/669684871/mbfunccoils.o.d" -o ${OBJECTDIR}/_ext/669684871/mbfunccoils.o ../modbus/functions/mbfunccoils.c    -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD) 
	
${OBJECTDIR}/_ext/669684871/mbfuncdiag.o: ../modbus/functions/mbfuncdiag.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/_ext/669684871" 
	@${RM} ${OBJECTDIR}/_ext/669684871/mbfuncdiag.o.d 
	@${RM} ${OBJECTDIR}/_ext/669684871/mbfuncdiag.o 
	@${FIXDEPS} "${OBJECTDIR}/_ext/669684871/mbfuncdiag.o.d" $(SILENT) -rsi ${MP_CC_DIR}../  -c ${MP_CC}  $(MP_EXTRA_CC_PRE) -g -D__DEBUG -DPICkit3PlatformTool=1  -fframe-base-loclist  -x c -c -mprocessor=$(MP_PROCESSOR_OPTION)  -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/ascii" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/functions" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/include" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/rtu" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/tcp" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/port" -I"../modbus/ascii" -I"../modbus/include" -I"../modbus/rtu" -I"../port" -I"." -MMD -MF "${OBJECTDIR}/_ext/669684871/mbfuncdiag.o.d" -o ${OBJECTDIR}/_ext/669684871/mbfuncdiag.o ../modbus/functions/mbfuncdiag.c    -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD) 
	
${OBJECTDIR}/_ext/669684871/mbfuncdisc.o: ../modbus/functions/mbfuncdisc.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/_ext/669684871" 
	@${RM} ${OBJECTDIR}/_ext/669684871/mbfuncdisc.o.d 
	@${RM} ${OBJECTDIR}/_ext/669684871/mbfuncdisc.o 
	@${FIXDEPS} "${OBJECTDIR}/_ext/669684871/mbfuncdisc.o.d" $(SILENT) -rsi ${MP_CC_DIR}../  -c ${MP_CC}  $(MP_EXTRA_CC_PRE) -g -D__DEBUG -DPICkit3PlatformTool=1  -fframe-base-loclist  -x c -c -mprocessor=$(MP_PROCESSOR_OPTION)  -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/ascii" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/functions" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/include" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/rtu" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/tcp" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/port" -I"../modbus/ascii" -I"../modbus/include" -I"../modbus/rtu" -I"../port" -I"." -MMD -MF "${OBJECTDIR}/_ext/669684871/mbfuncdisc.o.d" -o ${OBJECTDIR}/_ext/669684871/mbfuncdisc.o ../modbus/functions/mbfuncdisc.c    -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD) 
	
${OBJECTDIR}/_ext/669684871/mbfuncholding.o: ../modbus/functions/mbfuncholding.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/_ext/669684871" 
	@${RM} ${OBJECTDIR}/_ext/669684871/mbfuncholding.o.d 
	@${RM} ${OBJECTDIR}/_ext/669684871/mbfuncholding.o 
	@${FIXDEPS} "${OBJECTDIR}/_ext/669684871/mbfuncholding.o.d" $(SILENT) -rsi ${MP_CC_DIR}../  -c ${MP_CC}  $(MP_EXTRA_CC_PRE) -g -D__DEBUG -DPICkit3PlatformTool=1  -fframe-base-loclist  -x c -c -mprocessor=$(MP_PROCESSOR_OPTION)  -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/ascii" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/functions" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/include" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/rtu" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/tcp" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/port" -I"../modbus/ascii" -I"../modbus/include" -I"../modbus/rtu" -I"../port" -I"." -MMD -MF "${OBJECTDIR}/_ext/669684871/mbfuncholding.o.d" -o ${OBJECTDIR}/_ext/669684871/mbfuncholding.o ../modbus/functions/mbfuncholding.c    -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD) 
	
${OBJECTDIR}/_ext/669684871/mbfuncinput.o: ../modbus/functions/mbfuncinput.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/_ext/669684871" 
	@${RM} ${OBJECTDIR}/_ext/669684871/mbfuncinput.o.d 
	@${RM} ${OBJECTDIR}/_ext/669684871/mbfuncinput.o 
	@${FIXDEPS} "${OBJECTDIR}/_ext/669684871/mbfuncinput.o.d" $(SILENT) -rsi ${MP_CC_DIR}../  -c ${MP_CC}  $(MP_EXTRA_CC_PRE) -g -D__DEBUG -DPICkit3PlatformTool=1  -fframe-base-loclist  -x c -c -mprocessor=$(MP_PROCESSOR_OPTION)  -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/ascii" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/functions" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/include" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/rtu" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/tcp" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/port" -I"../modbus/ascii" -I"../modbus/include" -I"../modbus/rtu" -I"../port" -I"." -MMD -MF "${OBJECTDIR}/_ext/669684871/mbfuncinput.o.d" -o ${OBJECTDIR}/_ext/669684871/mbfuncinput.o ../modbus/functions/mbfuncinput.c    -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD) 
	
${OBJECTDIR}/_ext/669684871/mbfuncother.o: ../modbus/functions/mbfuncother.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/_ext/669684871" 
	@${RM} ${OBJECTDIR}/_ext/669684871/mbfuncother.o.d 
	@${RM} ${OBJECTDIR}/_ext/669684871/mbfuncother.o 
	@${FIXDEPS} "${OBJECTDIR}/_ext/669684871/mbfuncother.o.d" $(SILENT) -rsi ${MP_CC_DIR}../  -c ${MP_CC}  $(MP_EXTRA_CC_PRE) -g -D__DEBUG -DPICkit3PlatformTool=1  -fframe-base-loclist  -x c -c -mprocessor=$(MP_PROCESSOR_OPTION)  -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/ascii" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/functions" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/include" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/rtu" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/tcp" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/port" -I"../modbus/ascii" -I"../modbus/include" -I"../modbus/rtu" -I"../port" -I"." -MMD -MF "${OBJECTDIR}/_ext/669684871/mbfuncother.o.d" -o ${OBJECTDIR}/_ext/669684871/mbfuncother.o ../modbus/functions/mbfuncother.c    -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD) 
	
${OBJECTDIR}/_ext/669684871/mbutils.o: ../modbus/functions/mbutils.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/_ext/669684871" 
	@${RM} ${OBJECTDIR}/_ext/669684871/mbutils.o.d 
	@${RM} ${OBJECTDIR}/_ext/669684871/mbutils.o 
	@${FIXDEPS} "${OBJECTDIR}/_ext/669684871/mbutils.o.d" $(SILENT) -rsi ${MP_CC_DIR}../  -c ${MP_CC}  $(MP_EXTRA_CC_PRE) -g -D__DEBUG -DPICkit3PlatformTool=1  -fframe-base-loclist  -x c -c -mprocessor=$(MP_PROCESSOR_OPTION)  -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/ascii" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/functions" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/include" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/rtu" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/tcp" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/port" -I"../modbus/ascii" -I"../modbus/include" -I"../modbus/rtu" -I"../port" -I"." -MMD -MF "${OBJECTDIR}/_ext/669684871/mbutils.o.d" -o ${OBJECTDIR}/_ext/669684871/mbutils.o ../modbus/functions/mbutils.c    -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD) 
	
${OBJECTDIR}/_ext/760710288/portevent.o: ../port/portevent.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/_ext/760710288" 
	@${RM} ${OBJECTDIR}/_ext/760710288/portevent.o.d 
	@${RM} ${OBJECTDIR}/_ext/760710288/portevent.o 
	@${FIXDEPS} "${OBJECTDIR}/_ext/760710288/portevent.o.d" $(SILENT) -rsi ${MP_CC_DIR}../  -c ${MP_CC}  $(MP_EXTRA_CC_PRE) -g -D__DEBUG -DPICkit3PlatformTool=1  -fframe-base-loclist  -x c -c -mprocessor=$(MP_PROCESSOR_OPTION)  -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/ascii" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/functions" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/include" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/rtu" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/tcp" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/port" -I"../modbus/ascii" -I"../modbus/include" -I"../modbus/rtu" -I"../port" -I"." -MMD -MF "${OBJECTDIR}/_ext/760710288/portevent.o.d" -o ${OBJECTDIR}/_ext/760710288/portevent.o ../port/portevent.c    -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD) 
	
${OBJECTDIR}/_ext/760710288/portserial.o: ../port/portserial.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/_ext/760710288" 
	@${RM} ${OBJECTDIR}/_ext/760710288/portserial.o.d 
	@${RM} ${OBJECTDIR}/_ext/760710288/portserial.o 
	@${FIXDEPS} "${OBJECTDIR}/_ext/760710288/portserial.o.d" $(SILENT) -rsi ${MP_CC_DIR}../  -c ${MP_CC}  $(MP_EXTRA_CC_PRE) -g -D__DEBUG -DPICkit3PlatformTool=1  -fframe-base-loclist  -x c -c -mprocessor=$(MP_PROCESSOR_OPTION)  -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/ascii" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/functions" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/include" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/rtu" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/tcp" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/port" -I"../modbus/ascii" -I"../modbus/include" -I"../modbus/rtu" -I"../port" -I"." -MMD -MF "${OBJECTDIR}/_ext/760710288/portserial.o.d" -o ${OBJECTDIR}/_ext/760710288/portserial.o ../port/portserial.c    -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD) 
	
${OBJECTDIR}/_ext/760710288/porttimer.o: ../port/porttimer.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/_ext/760710288" 
	@${RM} ${OBJECTDIR}/_ext/760710288/porttimer.o.d 
	@${RM} ${OBJECTDIR}/_ext/760710288/porttimer.o 
	@${FIXDEPS} "${OBJECTDIR}/_ext/760710288/porttimer.o.d" $(SILENT) -rsi ${MP_CC_DIR}../  -c ${MP_CC}  $(MP_EXTRA_CC_PRE) -g -D__DEBUG -DPICkit3PlatformTool=1  -fframe-base-loclist  -x c -c -mprocessor=$(MP_PROCESSOR_OPTION)  -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/ascii" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/functions" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/include" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/rtu" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/tcp" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/port" -I"../modbus/ascii" -I"../modbus/include" -I"../modbus/rtu" -I"../port" -I"." -MMD -MF "${OBJECTDIR}/_ext/760710288/porttimer.o.d" -o ${OBJECTDIR}/_ext/760710288/porttimer.o ../port/porttimer.c    -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD) 
	
${OBJECTDIR}/_ext/488354351/mbascii.o: ../modbus/ascii/mbascii.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/_ext/488354351" 
	@${RM} ${OBJECTDIR}/_ext/488354351/mbascii.o.d 
	@${RM} ${OBJECTDIR}/_ext/488354351/mbascii.o 
	@${FIXDEPS} "${OBJECTDIR}/_ext/488354351/mbascii.o.d" $(SILENT) -rsi ${MP_CC_DIR}../  -c ${MP_CC}  $(MP_EXTRA_CC_PRE) -g -D__DEBUG -DPICkit3PlatformTool=1  -fframe-base-loclist  -x c -c -mprocessor=$(MP_PROCESSOR_OPTION)  -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/ascii" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/functions" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/include" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/rtu" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/tcp" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/port" -I"../modbus/ascii" -I"../modbus/include" -I"../modbus/rtu" -I"../port" -I"." -MMD -MF "${OBJECTDIR}/_ext/488354351/mbascii.o.d" -o ${OBJECTDIR}/_ext/488354351/mbascii.o ../modbus/ascii/mbascii.c    -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD) 
	
else
${OBJECTDIR}/_ext/1472/demo.o: ../demo.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/_ext/1472" 
	@${RM} ${OBJECTDIR}/_ext/1472/demo.o.d 
	@${RM} ${OBJECTDIR}/_ext/1472/demo.o 
	@${FIXDEPS} "${OBJECTDIR}/_ext/1472/demo.o.d" $(SILENT) -rsi ${MP_CC_DIR}../  -c ${MP_CC}  $(MP_EXTRA_CC_PRE)  -g -x c -c -mprocessor=$(MP_PROCESSOR_OPTION)  -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/ascii" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/functions" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/include" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/rtu" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/tcp" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/port" -I"../modbus/ascii" -I"../modbus/include" -I"../modbus/rtu" -I"../port" -I"." -MMD -MF "${OBJECTDIR}/_ext/1472/demo.o.d" -o ${OBJECTDIR}/_ext/1472/demo.o ../demo.c    -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD) 
	
${OBJECTDIR}/_ext/2104501039/mbcrc.o: ../modbus/rtu/mbcrc.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/_ext/2104501039" 
	@${RM} ${OBJECTDIR}/_ext/2104501039/mbcrc.o.d 
	@${RM} ${OBJECTDIR}/_ext/2104501039/mbcrc.o 
	@${FIXDEPS} "${OBJECTDIR}/_ext/2104501039/mbcrc.o.d" $(SILENT) -rsi ${MP_CC_DIR}../  -c ${MP_CC}  $(MP_EXTRA_CC_PRE)  -g -x c -c -mprocessor=$(MP_PROCESSOR_OPTION)  -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/ascii" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/functions" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/include" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/rtu" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/tcp" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/port" -I"../modbus/ascii" -I"../modbus/include" -I"../modbus/rtu" -I"../port" -I"." -MMD -MF "${OBJECTDIR}/_ext/2104501039/mbcrc.o.d" -o ${OBJECTDIR}/_ext/2104501039/mbcrc.o ../modbus/rtu/mbcrc.c    -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD) 
	
${OBJECTDIR}/_ext/2104501039/mbrtu.o: ../modbus/rtu/mbrtu.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/_ext/2104501039" 
	@${RM} ${OBJECTDIR}/_ext/2104501039/mbrtu.o.d 
	@${RM} ${OBJECTDIR}/_ext/2104501039/mbrtu.o 
	@${FIXDEPS} "${OBJECTDIR}/_ext/2104501039/mbrtu.o.d" $(SILENT) -rsi ${MP_CC_DIR}../  -c ${MP_CC}  $(MP_EXTRA_CC_PRE)  -g -x c -c -mprocessor=$(MP_PROCESSOR_OPTION)  -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/ascii" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/functions" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/include" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/rtu" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/tcp" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/port" -I"../modbus/ascii" -I"../modbus/include" -I"../modbus/rtu" -I"../port" -I"." -MMD -MF "${OBJECTDIR}/_ext/2104501039/mbrtu.o.d" -o ${OBJECTDIR}/_ext/2104501039/mbrtu.o ../modbus/rtu/mbrtu.c    -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD) 
	
${OBJECTDIR}/_ext/984464531/mb.o: ../modbus/mb.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/_ext/984464531" 
	@${RM} ${OBJECTDIR}/_ext/984464531/mb.o.d 
	@${RM} ${OBJECTDIR}/_ext/984464531/mb.o 
	@${FIXDEPS} "${OBJECTDIR}/_ext/984464531/mb.o.d" $(SILENT) -rsi ${MP_CC_DIR}../  -c ${MP_CC}  $(MP_EXTRA_CC_PRE)  -g -x c -c -mprocessor=$(MP_PROCESSOR_OPTION)  -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/ascii" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/functions" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/include" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/rtu" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/tcp" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/port" -I"../modbus/ascii" -I"../modbus/include" -I"../modbus/rtu" -I"../port" -I"." -MMD -MF "${OBJECTDIR}/_ext/984464531/mb.o.d" -o ${OBJECTDIR}/_ext/984464531/mb.o ../modbus/mb.c    -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD) 
	
${OBJECTDIR}/_ext/669684871/mbfunccoils.o: ../modbus/functions/mbfunccoils.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/_ext/669684871" 
	@${RM} ${OBJECTDIR}/_ext/669684871/mbfunccoils.o.d 
	@${RM} ${OBJECTDIR}/_ext/669684871/mbfunccoils.o 
	@${FIXDEPS} "${OBJECTDIR}/_ext/669684871/mbfunccoils.o.d" $(SILENT) -rsi ${MP_CC_DIR}../  -c ${MP_CC}  $(MP_EXTRA_CC_PRE)  -g -x c -c -mprocessor=$(MP_PROCESSOR_OPTION)  -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/ascii" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/functions" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/include" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/rtu" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/tcp" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/port" -I"../modbus/ascii" -I"../modbus/include" -I"../modbus/rtu" -I"../port" -I"." -MMD -MF "${OBJECTDIR}/_ext/669684871/mbfunccoils.o.d" -o ${OBJECTDIR}/_ext/669684871/mbfunccoils.o ../modbus/functions/mbfunccoils.c    -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD) 
	
${OBJECTDIR}/_ext/669684871/mbfuncdiag.o: ../modbus/functions/mbfuncdiag.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/_ext/669684871" 
	@${RM} ${OBJECTDIR}/_ext/669684871/mbfuncdiag.o.d 
	@${RM} ${OBJECTDIR}/_ext/669684871/mbfuncdiag.o 
	@${FIXDEPS} "${OBJECTDIR}/_ext/669684871/mbfuncdiag.o.d" $(SILENT) -rsi ${MP_CC_DIR}../  -c ${MP_CC}  $(MP_EXTRA_CC_PRE)  -g -x c -c -mprocessor=$(MP_PROCESSOR_OPTION)  -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/ascii" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/functions" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/include" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/rtu" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/tcp" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/port" -I"../modbus/ascii" -I"../modbus/include" -I"../modbus/rtu" -I"../port" -I"." -MMD -MF "${OBJECTDIR}/_ext/669684871/mbfuncdiag.o.d" -o ${OBJECTDIR}/_ext/669684871/mbfuncdiag.o ../modbus/functions/mbfuncdiag.c    -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD) 
	
${OBJECTDIR}/_ext/669684871/mbfuncdisc.o: ../modbus/functions/mbfuncdisc.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/_ext/669684871" 
	@${RM} ${OBJECTDIR}/_ext/669684871/mbfuncdisc.o.d 
	@${RM} ${OBJECTDIR}/_ext/669684871/mbfuncdisc.o 
	@${FIXDEPS} "${OBJECTDIR}/_ext/669684871/mbfuncdisc.o.d" $(SILENT) -rsi ${MP_CC_DIR}../  -c ${MP_CC}  $(MP_EXTRA_CC_PRE)  -g -x c -c -mprocessor=$(MP_PROCESSOR_OPTION)  -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/ascii" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/functions" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/include" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/rtu" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/tcp" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/port" -I"../modbus/ascii" -I"../modbus/include" -I"../modbus/rtu" -I"../port" -I"." -MMD -MF "${OBJECTDIR}/_ext/669684871/mbfuncdisc.o.d" -o ${OBJECTDIR}/_ext/669684871/mbfuncdisc.o ../modbus/functions/mbfuncdisc.c    -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD) 
	
${OBJECTDIR}/_ext/669684871/mbfuncholding.o: ../modbus/functions/mbfuncholding.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/_ext/669684871" 
	@${RM} ${OBJECTDIR}/_ext/669684871/mbfuncholding.o.d 
	@${RM} ${OBJECTDIR}/_ext/669684871/mbfuncholding.o 
	@${FIXDEPS} "${OBJECTDIR}/_ext/669684871/mbfuncholding.o.d" $(SILENT) -rsi ${MP_CC_DIR}../  -c ${MP_CC}  $(MP_EXTRA_CC_PRE)  -g -x c -c -mprocessor=$(MP_PROCESSOR_OPTION)  -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/ascii" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/functions" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/include" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/rtu" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/tcp" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/port" -I"../modbus/ascii" -I"../modbus/include" -I"../modbus/rtu" -I"../port" -I"." -MMD -MF "${OBJECTDIR}/_ext/669684871/mbfuncholding.o.d" -o ${OBJECTDIR}/_ext/669684871/mbfuncholding.o ../modbus/functions/mbfuncholding.c    -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD) 
	
${OBJECTDIR}/_ext/669684871/mbfuncinput.o: ../modbus/functions/mbfuncinput.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/_ext/669684871" 
	@${RM} ${OBJECTDIR}/_ext/669684871/mbfuncinput.o.d 
	@${RM} ${OBJECTDIR}/_ext/669684871/mbfuncinput.o 
	@${FIXDEPS} "${OBJECTDIR}/_ext/669684871/mbfuncinput.o.d" $(SILENT) -rsi ${MP_CC_DIR}../  -c ${MP_CC}  $(MP_EXTRA_CC_PRE)  -g -x c -c -mprocessor=$(MP_PROCESSOR_OPTION)  -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/ascii" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/functions" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/include" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/rtu" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/tcp" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/port" -I"../modbus/ascii" -I"../modbus/include" -I"../modbus/rtu" -I"../port" -I"." -MMD -MF "${OBJECTDIR}/_ext/669684871/mbfuncinput.o.d" -o ${OBJECTDIR}/_ext/669684871/mbfuncinput.o ../modbus/functions/mbfuncinput.c    -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD) 
	
${OBJECTDIR}/_ext/669684871/mbfuncother.o: ../modbus/functions/mbfuncother.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/_ext/669684871" 
	@${RM} ${OBJECTDIR}/_ext/669684871/mbfuncother.o.d 
	@${RM} ${OBJECTDIR}/_ext/669684871/mbfuncother.o 
	@${FIXDEPS} "${OBJECTDIR}/_ext/669684871/mbfuncother.o.d" $(SILENT) -rsi ${MP_CC_DIR}../  -c ${MP_CC}  $(MP_EXTRA_CC_PRE)  -g -x c -c -mprocessor=$(MP_PROCESSOR_OPTION)  -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/ascii" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/functions" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/include" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/rtu" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/tcp" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/port" -I"../modbus/ascii" -I"../modbus/include" -I"../modbus/rtu" -I"../port" -I"." -MMD -MF "${OBJECTDIR}/_ext/669684871/mbfuncother.o.d" -o ${OBJECTDIR}/_ext/669684871/mbfuncother.o ../modbus/functions/mbfuncother.c    -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD) 
	
${OBJECTDIR}/_ext/669684871/mbutils.o: ../modbus/functions/mbutils.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/_ext/669684871" 
	@${RM} ${OBJECTDIR}/_ext/669684871/mbutils.o.d 
	@${RM} ${OBJECTDIR}/_ext/669684871/mbutils.o 
	@${FIXDEPS} "${OBJECTDIR}/_ext/669684871/mbutils.o.d" $(SILENT) -rsi ${MP_CC_DIR}../  -c ${MP_CC}  $(MP_EXTRA_CC_PRE)  -g -x c -c -mprocessor=$(MP_PROCESSOR_OPTION)  -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/ascii" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/functions" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/include" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/rtu" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/tcp" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/port" -I"../modbus/ascii" -I"../modbus/include" -I"../modbus/rtu" -I"../port" -I"." -MMD -MF "${OBJECTDIR}/_ext/669684871/mbutils.o.d" -o ${OBJECTDIR}/_ext/669684871/mbutils.o ../modbus/functions/mbutils.c    -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD) 
	
${OBJECTDIR}/_ext/760710288/portevent.o: ../port/portevent.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/_ext/760710288" 
	@${RM} ${OBJECTDIR}/_ext/760710288/portevent.o.d 
	@${RM} ${OBJECTDIR}/_ext/760710288/portevent.o 
	@${FIXDEPS} "${OBJECTDIR}/_ext/760710288/portevent.o.d" $(SILENT) -rsi ${MP_CC_DIR}../  -c ${MP_CC}  $(MP_EXTRA_CC_PRE)  -g -x c -c -mprocessor=$(MP_PROCESSOR_OPTION)  -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/ascii" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/functions" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/include" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/rtu" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/tcp" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/port" -I"../modbus/ascii" -I"../modbus/include" -I"../modbus/rtu" -I"../port" -I"." -MMD -MF "${OBJECTDIR}/_ext/760710288/portevent.o.d" -o ${OBJECTDIR}/_ext/760710288/portevent.o ../port/portevent.c    -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD) 
	
${OBJECTDIR}/_ext/760710288/portserial.o: ../port/portserial.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/_ext/760710288" 
	@${RM} ${OBJECTDIR}/_ext/760710288/portserial.o.d 
	@${RM} ${OBJECTDIR}/_ext/760710288/portserial.o 
	@${FIXDEPS} "${OBJECTDIR}/_ext/760710288/portserial.o.d" $(SILENT) -rsi ${MP_CC_DIR}../  -c ${MP_CC}  $(MP_EXTRA_CC_PRE)  -g -x c -c -mprocessor=$(MP_PROCESSOR_OPTION)  -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/ascii" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/functions" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/include" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/rtu" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/tcp" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/port" -I"../modbus/ascii" -I"../modbus/include" -I"../modbus/rtu" -I"../port" -I"." -MMD -MF "${OBJECTDIR}/_ext/760710288/portserial.o.d" -o ${OBJECTDIR}/_ext/760710288/portserial.o ../port/portserial.c    -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD) 
	
${OBJECTDIR}/_ext/760710288/porttimer.o: ../port/porttimer.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/_ext/760710288" 
	@${RM} ${OBJECTDIR}/_ext/760710288/porttimer.o.d 
	@${RM} ${OBJECTDIR}/_ext/760710288/porttimer.o 
	@${FIXDEPS} "${OBJECTDIR}/_ext/760710288/porttimer.o.d" $(SILENT) -rsi ${MP_CC_DIR}../  -c ${MP_CC}  $(MP_EXTRA_CC_PRE)  -g -x c -c -mprocessor=$(MP_PROCESSOR_OPTION)  -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/ascii" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/functions" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/include" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/rtu" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/tcp" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/port" -I"../modbus/ascii" -I"../modbus/include" -I"../modbus/rtu" -I"../port" -I"." -MMD -MF "${OBJECTDIR}/_ext/760710288/porttimer.o.d" -o ${OBJECTDIR}/_ext/760710288/porttimer.o ../port/porttimer.c    -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD) 
	
${OBJECTDIR}/_ext/488354351/mbascii.o: ../modbus/ascii/mbascii.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} "${OBJECTDIR}/_ext/488354351" 
	@${RM} ${OBJECTDIR}/_ext/488354351/mbascii.o.d 
	@${RM} ${OBJECTDIR}/_ext/488354351/mbascii.o 
	@${FIXDEPS} "${OBJECTDIR}/_ext/488354351/mbascii.o.d" $(SILENT) -rsi ${MP_CC_DIR}../  -c ${MP_CC}  $(MP_EXTRA_CC_PRE)  -g -x c -c -mprocessor=$(MP_PROCESSOR_OPTION)  -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/ascii" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/functions" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/include" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/rtu" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/modbus/tcp" -I"../../../../../../../../Documents and Settings/dbordwell/Desktop/Work/CODE/Modbus/freemodbus-v1.4.0/freemodbus-v1.4.0/demo/PIC32/port" -I"../modbus/ascii" -I"../modbus/include" -I"../modbus/rtu" -I"../port" -I"." -MMD -MF "${OBJECTDIR}/_ext/488354351/mbascii.o.d" -o ${OBJECTDIR}/_ext/488354351/mbascii.o ../modbus/ascii/mbascii.c    -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD) 
	
endif

# ------------------------------------------------------------------------------------
# Rules for buildStep: compileCPP
ifeq ($(TYPE_IMAGE), DEBUG_RUN)
else
endif

# ------------------------------------------------------------------------------------
# Rules for buildStep: link
ifeq ($(TYPE_IMAGE), DEBUG_RUN)
dist/${CND_CONF}/${IMAGE_TYPE}/demo.X.${IMAGE_TYPE}.${OUTPUT_SUFFIX}: ${OBJECTFILES}  nbproject/Makefile-${CND_CONF}.mk    
	@${MKDIR} dist/${CND_CONF}/${IMAGE_TYPE} 
	${MP_CC} $(MP_EXTRA_LD_PRE) -g -mdebugger -DPICkit3PlatformTool=1 -mprocessor=$(MP_PROCESSOR_OPTION)  -o dist/${CND_CONF}/${IMAGE_TYPE}/demo.X.${IMAGE_TYPE}.${OUTPUT_SUFFIX} ${OBJECTFILES_QUOTED_IF_SPACED}          -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD)   -mreserve=data@0x0:0x1FC -mreserve=boot@0x1FC02000:0x1FC02FEF -mreserve=boot@0x1FC02000:0x1FC024FF  -Wl,--defsym=__MPLAB_BUILD=1$(MP_EXTRA_LD_POST)$(MP_LINKER_FILE_OPTION),--defsym=__MPLAB_DEBUG=1,--defsym=__DEBUG=1,-D=__DEBUG_D,--defsym=PICkit3PlatformTool=1,--no-code-in-dinit,--no-dinit-in-serial-mem,-L"../../../../../../../../Program Files/Microchip/MPLAB C32/lib",-L"../../../../../../../../Program Files/Microchip/MPLAB C32/pic32mx/lib",-L".",-Map="${DISTDIR}/demo.X.${IMAGE_TYPE}.map",--memorysummary,dist/${CND_CONF}/${IMAGE_TYPE}/memoryfile.xml
	
else
dist/${CND_CONF}/${IMAGE_TYPE}/demo.X.${IMAGE_TYPE}.${OUTPUT_SUFFIX}: ${OBJECTFILES}  nbproject/Makefile-${CND_CONF}.mk   
	@${MKDIR} dist/${CND_CONF}/${IMAGE_TYPE} 
	${MP_CC} $(MP_EXTRA_LD_PRE)  -mprocessor=$(MP_PROCESSOR_OPTION)  -o dist/${CND_CONF}/${IMAGE_TYPE}/demo.X.${IMAGE_TYPE}.${DEBUGGABLE_SUFFIX} ${OBJECTFILES_QUOTED_IF_SPACED}          -DXPRJ_default=$(CND_CONF)  -no-legacy-libc  $(COMPARISON_BUILD)  -Wl,--defsym=__MPLAB_BUILD=1$(MP_EXTRA_LD_POST)$(MP_LINKER_FILE_OPTION),--no-code-in-dinit,--no-dinit-in-serial-mem,-L"../../../../../../../../Program Files/Microchip/MPLAB C32/lib",-L"../../../../../../../../Program Files/Microchip/MPLAB C32/pic32mx/lib",-L".",-Map="${DISTDIR}/demo.X.${IMAGE_TYPE}.map",--memorysummary,dist/${CND_CONF}/${IMAGE_TYPE}/memoryfile.xml
	${MP_CC_DIR}\\xc32-bin2hex dist/${CND_CONF}/${IMAGE_TYPE}/demo.X.${IMAGE_TYPE}.${DEBUGGABLE_SUFFIX} 
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
