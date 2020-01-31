#!/usr/bin/env python
#-----------------------------------------------------------------------------#
#                                                                             #
#                              Python script                                  #
#                                                                             #
#-----------------------------------------------------------------------------#
#
# Author      : M. de Waard
# Version     : 1.0
# Description : Perform testbox cycle test.
#               Start by switching on all outputs, then wait 10 seconds to stabilize
#               the communication bus.
#               Then, on modules 2 through 6 switch off channel 1 A/B, wait 1
#               second, then switch on again and wait 1 second. Then switch off
#               channel 2 A/B, wait 1 second, switch on again, wait 1 second.
#               Continue until all channels done, then repeat forever (until
#               ctrl-C or error).
#               A few channels are excempt from this cycle because these have
#               special functions:
#
#               Let op:
#               Bij module adress 2:
#               Outputs:
#               1 = UP --> altijd AAN, nooit schakelen  --> Channel 1, kan. A
#               3 = US --> altijd AAN, nooit schakelen  --> Channel 1, kan. B
#               4 = S3 --> altijd AAN, nooit schakelen  --> Channel 2, kan. A
#               
#               Testscript is only allowed to run on:
#               Channel 5, A + B
#               Channel 6, A + B
#               Channel 7, A + B
#               Channel 8, A + B
#
#               Module 3, 4, 5
#               Channel 1, A + B
#               Channel 2, A + B
#               Channel 3, A + B
#               Channel 4, A + B
#               Channel 5, A + B
#               Channel 6, A + B
#               Channel 7, A + B
#               Channel 8, A + B
#
#               In general, per channel:
#               b0 = A, b1 = short (do not switch!), b2 = B
#               So only write value 5 (b0 + b2) or 0 (off)
#               
# History
# yyyy-mm-dd : <cr/pr> <author> [<platform> <release>]
#              <comment>
# 2016-10-10 : Initial version
#-----------------------------------------------------------------------------#
#                                                                             #
#      Copyright (c) 2016, ASML Holding N.V. (including affiliates).          #
#                            All rights reserved                              #
#                                                                             #
#-----------------------------------------------------------------------------#

#-----------------------------------------------------------------------------#
#                         Documentation String                                #
#-----------------------------------------------------------------------------#

"""
CycleTest.py - Control a number of ATMega controlled boards for testing of Safety PLC
               functions. Cycles through the I/O of 5 connected DO modules.

Usage:    CycleTest

Options:    

"""

#-----------------------------------------------------------------------------#
#                       System and specific imports                           #
#-----------------------------------------------------------------------------#
import os, sys, time

from TemComIf import TemComIf
from serial.tools.list_ports_windows import comports

#-----------------------------------------------------------------------------#
#                             Global Variables                                #
#-----------------------------------------------------------------------------#

# delay after initial switch on for bus stabilisation (seconds)
SWITCH_ON_BUS_DELAY = 1

# delay between channels (seconds)
DELAY_AFTER_SWITCH_OFF = 1
DELAY_AFTER_SWITCH_ON  = 1

# for now we don't have a module 6, change 'last-module' when module is added
FIRST_MODULE = 2
LAST_MODULE  = 5

# since some module channels have special meaning and other channels for all modules
# should never be switched, we create a dictionary with which channels to test on
# what module; note that if modules are added (last-module increased beyond 6) also
# the table below must be changed.
channels_to_test = {
    2 : [5, 6, 7, 8],
    3 : [1, 2, 3, 4, 5, 6, 7, 8],
    4 : [1, 2, 3, 4, 5, 6, 7, 8],
    5 : [1, 2, 3, 4, 5],
    6 : [1, 2, 3, 4, 5, 6, 7, 8]}

#-----------------------------------------------------------------------------#
#                            Class Definitions                                #
#-----------------------------------------------------------------------------#

#-----------------------------------------------------------------------------#
#                            Support Functions                                #
#-----------------------------------------------------------------------------#

#-----
# Function: GetSerialPorts
# Purpose : Do a scan of (USB) serial ports. Return a list of FTDI USB to RS422
#           port identifiers.
# Remarks : More than one converter can be present, in which case the list length
#           would be > 1. In that case, the choice can't be made automatically and
#           the user needs to be presented with a choice.
def GetSerialPorts ():
    ports = sorted (comports ())
    return [x.device for x in ports if x.hwid.startswith ('USB VID:PID=0403:6001')]
    
#-----
# Function: Initialize
# Purpose : Initialisation function, perform setup and switch on all channels on all modules.
# Remarks : This function needs a check. After module power-on, there may need to be
#           bus stabilisation time, which is implicit by a delay between power up and
#           starting the script. Then again, when module 2 channel 1,3,4 are switched
#           on we may need to wait as well. So we do before we then switch on the
#           remaining modules.
def Initialize (interface):
    # first we broadcast an initialization command
    print 'Sending initialisation command...'
    interface.WriteWord (TemComIf.BROADCAST_ADDRESS, TemComIf.REG_COMMAND, 0)
    
    # now we wait 10 seconds
    time.sleep (1)
    
    # now we switch on UP, US and S3 on module 2
    print 'Switch on UP, US and S3...'
    interface.WriteWord (2, TemComIf.REG_DO_CURRENT_OUTPUT_VALUE_CH1, 5)     # UP and US
    interface.WriteWord (2, TemComIf.REG_DO_CURRENT_OUTPUT_VALUE_CH2, 1)     # S3
    
    # and wait again 10 seconds
    time.sleep (1)
    
    # now we switch on all outputs we have under test, which is essentially the list given by
    # 'channels_to_test' (see under global variables), but limited by FIRST_MODULE and LAST_MODULE
    # note: in the 'for' we need to add 1 to include the last-module itself
    print 'Switch on all relevant output channels...'
    for module in range (FIRST_MODULE, LAST_MODULE + 1):
        # get the list of channels to switch on
        channel_list = channels_to_test [module]
        
        # loop through the channels
        print '  Module:', module
        for channel in channel_list:
            # switch on A and B of the channel
            # the channel number calculation is based on the value of channel 1 (CH1), but the channels
            # are also numbered from 1 .. 8, so if we need to address channel 1 itself by means of the
            # offset which is given in the 'channel' variable, we need to subtract 1 first
            interface.WriteWord (module, TemComIf.REG_DO_CURRENT_OUTPUT_VALUE_CH1 - 1 + channel, 5)
            
    # now everything is on, we wait 1 second (which is part of the test specification)
    print 'Initialisation complete'
    time.sleep (1)
    
#-----
# Function: RunTest
# Purpose : Run the sequence as described in the header section of this file
# Remarks :
def RunTest (interface):
    # for each module and for each channel given in the channels-to-test table and delimited by
    # first-module, last-module we first switch off channel A and B, then wait 1 second, then
    # switch on A and B, wait 1 second and proceed with the next module/channel
    for module in range (FIRST_MODULE, LAST_MODULE + 1):
        # show where we are...
        print 'Module:', module
        
        # get the list of channels to test
        channel_list = channels_to_test [module]
        
        # loop through the channels
        for channel in channel_list:
            # show where we are
            print '  Channel %d Switch off A+B'%channel
            interface.WriteWord (module, TemComIf.REG_DO_CURRENT_OUTPUT_VALUE_CH1 - 1 + channel, 0)
            
            # get feedback
            DOchan = interface.ReadWord (module, TemComIf.REG_DO_CURRENT_OUTPUT_VALUE_CH1 - 1 + channel)
            if (DOchan != 0):
                print '    Write failed, feedback unequal to request (value: %d), expected 0'%DOchan
            else:
                print '    Feedback OK, channel value: %d'%DOchan
            
            # delay
            time.sleep (DELAY_AFTER_SWITCH_OFF)
            
            # switch on again
            print '  Channel %d Switch off A+B'%channel
            interface.WriteWord (module, TemComIf.REG_DO_CURRENT_OUTPUT_VALUE_CH1 - 1 + channel, 5)
            
            # get feedback
            DOchan = interface.ReadWord (module, TemComIf.REG_DO_CURRENT_OUTPUT_VALUE_CH1 - 1 + channel)
            if (DOchan != 5):
                print '    Write failed, feedback unequal to request (value: %d), expected 5'%DOchan
            else:
                print '    Feedback OK, channel value: %d'%DOchan
            
            # delay
            time.sleep (DELAY_AFTER_SWITCH_ON)
            
        # add empty line between modules
        print ''

#-----------------------------------------------------------------------------#
#                  simple display of how to use this script                   #
#-----------------------------------------------------------------------------#

#-----
# No command line help function implemented. Just run the script without command line parameters.
# Possible options: simply double click the application, use a dos prompt: 'CycleTest.py' or
# 'python CycleTest.py' (in both cases without the single quotes).

#-----------------------------------------------------------------------------#
#                            Main Starts Here                                 #
#-----------------------------------------------------------------------------#
if (__name__ == '__main__'):
    # start the application, first see if we can get a valid RS422 port identifier.
    ports = GetSerialPorts ()
    if (len (ports) < 1):
        # A USB to RS422 port must be added or a non-FTDI type is used, either way: user interaction required
        # and maybe a change to this script
        print 'Did not find FTDI USB to RS422 port'
        sys.exit (-1)
    elif (len (ports) > 1):
        # User must make a choice; no code implemented yet to have some user interaction, so in this case the
        # script must be changed or superfluous converters removed
        print 'Found more than 1 FTDI USB to RS422 port'
        sys.exit (-2)
    else:
        # one FTDI USB to RS422 converter found, assume it is the one we need...
        print 'Opening port:', ports[0]
        interf = TemComIf (ports[0])
        
        # switch on all outputs
        Initialize (interf)
        
        # creat an empty line in the output stream
        print ''
        
        # for ever run the test
        # TODO: see if we can catch the KeyboardInterrupt exception
        # while 1:
        #    RunTest (interf)
    
#----------------------------------- End of file -----------------------------#
