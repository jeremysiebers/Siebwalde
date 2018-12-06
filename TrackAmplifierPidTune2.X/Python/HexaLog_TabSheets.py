#!/usr/bin/env python
#-----------------------------------------------------------------------------#
#                                                                             #
#                              Python script                                  #
#                                                                             #
#-----------------------------------------------------------------------------#
#
# Author      : M. de Waard
# Description : TabSheets are pages for the tabbed main page of the HexaLog GUI application.
#               These tab sheets are separated into individual classes to avoid the main
#               class grows out of control.
#
# History
# yyyy-mm-dd : <author> <comment>
# 2018-08-25   MWAA     Creation
#-----------------------------------------------------------------------------#
#                       Copyright (c) 2018, JeMaTech                          #
#                            All rights reserved                              #
#-----------------------------------------------------------------------------#

#-----------------------------------------------------------------------------#
#                         Documentation String                                #
#-----------------------------------------------------------------------------#


#-----------------------------------------------------------------------------#
#                         System and ASML imports                             #
#-----------------------------------------------------------------------------#
import os, sys
import time
import types
import struct
import Tkinter
import tkFileDialog
import tkMessageBox
import tkSimpleDialog

import numpy                    # for array handling
import scipy.io                 # for savemat and loadmat

import HexaLog_Widgets as widgets

try:
    # this only works on windows...
    MAJOR, MINOR, BUILD, PLATFORM, TEXT = sys.getwindowsversion ()
except:
    PLATFORM = None

#-----------------------------------------------------------------------------#
#                             Global Variables                                #
#-----------------------------------------------------------------------------#
SPEEDBUTTON_HEIGHT     = 20             # absolute pixels, note: image size should be 16x16
SPEEDBUTTON_WIDTH      = 20             # absolute pixels, note: image size should be 16x16

ICONDIR = "Icons"

# command set for logger
# MUST match the command list implemented by "terminal.c" in the NIOS application!
COMMAND_GET_STATUS = 'S'                # no parameters,              reply will be the 6 alt_u32 words starting at offset B0 in the state manager register map
COMMAND_GET_DATA   = 'D'                # par 1: base, par 2: amount, reply will be the given amount of data from the DDR3 trace buffer, amount must be given in BYTES

# indeces and masks for the logger
# indeces into the status list:
HSM_CHANNEL_ENABLE                  = 0             # enable flags; not used by the logger fabric, but can be used to autofill the channel configuration (which channels to show)
HSM_SPI_CLK_DIVIDER                 = 1             # (IGNORE) SPI clock divider
HSM_SAMPLE_CLK_DIV                  = 2             # high speed sample clock divider
HSM_LOW_FREQ_SAMPLE_CLK_DIV         = 3             # (IGNORE) low frequency sample clock divider
HSM_DDR3_LOG_BASE_ADDRESS           = 4             # 32-bit base address for DDR-3
HSM_POST_TRIGGER_LENGTH             = 5             # 32-bit post trigger length (in samples!!)
HSM_TRIGGER_OCCURRED_ADDRESS        = 6             # absolute DDR-3 address where the trigger occurred (0 if it did not happen yet)
HSM_LOG_CSR                         = 7             # 32-bit control and status register: low 16 bit are control, upper 16 bit are status
HSM_NUM_STATUS_BYTES                = 8 * 4         # the number of bytes in the get status reply

# masks for the CSR register
HSM_CSR_TRIGGER_SET_MASK            = 0x00000002
HSM_CSR_RUNNING_MASK                = 0x00010000
HSM_CSR_WAITING_MASK                = 0x00020000
HSM_CSR_STOPPING_MASK               = 0x00040000
HSM_CSR_STOPPED_MASK                = 0x00080000
HSM_CSR_TRIG_OCCURRED_MASK          = 0x00100000
HSM_CSR_DDR_WRAP_AROUND_MASK        = 0x80000000

# for retrieving sampled data
SAMPLE_BLOCK_SIZE                   = 64            # 4 bytes per channel, 16 channels (0 to 15) in a block ==> SHOULD NEVER CHANGE!!
SAMPLE_UPLOAD_SIZE                  = 1024          # upload size (arbitrary size)                          ==> experiment for efficient transfer (somewhere, 1024 seems to be max however...)
SAMPLE_BLOCKS_PER_TRANSMISSION      = (SAMPLE_UPLOAD_SIZE / SAMPLE_BLOCK_SIZE)

# Look for Icons subdirectory in the same directory as this module
# Note: pre-encoding the images and pre-loading them in the TreeNode class can
#       cause issues when large numbers (>70) of files are loaded. Can't catch
#       the exception, the application just dies. This is solved by loading the
#       icons runtime (which ofcourse is slower as well).
try:
    _icondir = os.path.join (os.path.dirname (__file__), ICONDIR)
except NameError:
    _icondir = ICONDIR
if os.path.isdir (_icondir):
    ICONDIR = _icondir
elif not os.path.isdir (ICONDIR):
    raise RuntimeError, "can't find icon directory (%r)" % (ICONDIR,)

#-----------------------------------------------------------------------------#
#                        Status tabsheet base class                           #
#-----------------------------------------------------------------------------#
class clsStatusTabSheet:
    #-----
    # class constructor
    def __init__ (self, master):
        # copy variables
        self._master = master
        
        # take over local variables from the master
        self.BUTTON_WIDTH     = self._master.BUTTON_WIDTH
        self.BASE_X_POS       = self._master.BASE_X_POS
        self.BASE_Y_POS       = self._master.BASE_Y_POS
        self.X_POS_INDENT     = self._master.X_POS_INDENT
        self.DELTA_Y          = self._master.DELTA_Y
        self.CBX_WIDTH        = self._master.CBX_WIDTH
        self.LBL_WIDTH        = self._master.LBL_WIDTH
        self.BTN_CANVAS_WIDTH = self._master.BTN_CANVAS_WIDTH
        self.ENTRY_WIDTH      = self._master.ENTRY_WIDTH
        self.ENTRY_SEP        = self._master.ENTRY_SEP
        self.SELECT_WIDTH     = self._master.SELECT_WIDTH
        self.X_DATA1_INDENT   = 325
        self.X_DATA2_INDENT   = 450
        
        # set local data
        self._status = ()                       # empty tuple
        self._pos_y  = self.BASE_Y_POS          # from here we start showing fields
        
        # create widgets
        self._create_widgets ()
        
    #-----
    # build widgets for configuration tabsheet
    def _create_widgets (self):
        # create a frame and a canvas to put widgets on
        self._status_frame = Tkinter.Frame (self._master._nb ())
        self._status_canvas = Tkinter.Canvas (self._status_frame)
        self._status_canvas.pack (side = "top", expand = 1, fill = Tkinter.BOTH)
        
        # show the sample rate
        self._samplerate_lbl = Tkinter.Label (self._status_canvas, text = "Sample rate [Hz]", anchor = 'w')
        self._status_canvas.create_window ((self.BASE_X_POS, self._pos_y), anchor = 'w', width = 200, window = self._samplerate_lbl)
        self._samplerate_var = Tkinter.StringVar ()
        self._samplerate_edt = Tkinter.Entry (self._status_canvas, textvariable = self._samplerate_var, state = Tkinter.DISABLED)
        self._status_canvas.create_window ((self.X_DATA1_INDENT, self._pos_y), anchor = 'w', width = self.ENTRY_WIDTH, window = self._samplerate_edt)
        self._pos_y = self._pos_y + self.DELTA_Y
        
        # show the CSR register as a raw value
        self._csr_lbl = Tkinter.Label (self._status_canvas, text = "CSR raw value", anchor = 'w')
        self._status_canvas.create_window ((self.BASE_X_POS, self._pos_y), anchor = 'w', width = 200, window = self._csr_lbl)
        self._csr_var = Tkinter.StringVar ()
        self._csr_edt = Tkinter.Entry (self._status_canvas, textvariable = self._csr_var, state = Tkinter.DISABLED)
        self._status_canvas.create_window ((self.X_DATA1_INDENT, self._pos_y), anchor = 'w', width = self.ENTRY_WIDTH, window = self._csr_edt)
        self._pos_y = self._pos_y + self.DELTA_Y
        
        # has a trigger been defined yes or no
        self._tr_def_lbl = Tkinter.Label (self._status_canvas, text = "Trigger defined y/n", anchor = 'w')
        self._status_canvas.create_window ((self.BASE_X_POS + self.X_POS_INDENT, self._pos_y), anchor = 'w', width = 200, window = self._tr_def_lbl)
        self._tr_def_var = Tkinter.StringVar ()
        self._tr_def_edt = Tkinter.Entry (self._status_canvas, textvariable = self._tr_def_var, state = Tkinter.DISABLED)
        self._status_canvas.create_window ((self.X_DATA1_INDENT, self._pos_y), anchor = 'w', width = self.ENTRY_WIDTH, window = self._tr_def_edt)
        self._pos_y = self._pos_y + self.DELTA_Y
        
        # is the logger running
        self._run_lbl = Tkinter.Label (self._status_canvas, text = "In running state y/n", anchor = 'w')
        self._status_canvas.create_window ((self.BASE_X_POS + self.X_POS_INDENT, self._pos_y), anchor = 'w', width = 200, window = self._run_lbl)
        self._log_run_var = Tkinter.StringVar ()
        self._log_run_edt = Tkinter.Entry (self._status_canvas, textvariable = self._log_run_var, state = Tkinter.DISABLED)
        self._status_canvas.create_window ((self.X_DATA1_INDENT, self._pos_y), anchor = 'w', width = self.ENTRY_WIDTH, window = self._log_run_edt)
        self._pos_y = self._pos_y + self.DELTA_Y
        
        # is the logger waiting for a trigger
        self._wait_tr_lbl = Tkinter.Label (self._status_canvas, text = "Waiting for trigger y/n", anchor = 'w')
        self._status_canvas.create_window ((self.BASE_X_POS + self.X_POS_INDENT, self._pos_y), anchor = 'w', width = 200, window = self._wait_tr_lbl)
        self._wait_tr_var = Tkinter.StringVar ()
        self._wait_tr_edt = Tkinter.Entry (self._status_canvas, textvariable = self._wait_tr_var, state = Tkinter.DISABLED)
        self._status_canvas.create_window ((self.X_DATA1_INDENT, self._pos_y), anchor = 'w', width = self.ENTRY_WIDTH, window = self._wait_tr_edt)
        self._pos_y = self._pos_y + self.DELTA_Y
        
        # is the logger in the stopping state
        self._stopping_lbl = Tkinter.Label (self._status_canvas, text = "Stopping after trigger y/n", anchor = 'w')
        self._status_canvas.create_window ((self.BASE_X_POS + self.X_POS_INDENT, self._pos_y), anchor = 'w', width = 200, window = self._stopping_lbl)
        self._stopping_var = Tkinter.StringVar ()
        self._stopping_edt = Tkinter.Entry (self._status_canvas, textvariable = self._stopping_var, state = Tkinter.DISABLED)
        self._status_canvas.create_window ((self.X_DATA1_INDENT, self._pos_y), anchor = 'w', width = self.ENTRY_WIDTH, window = self._stopping_edt)
        self._pos_y = self._pos_y + self.DELTA_Y
        
        # has the logger stopped
        self._stopped_lbl = Tkinter.Label (self._status_canvas, text = "Stopped y/n", anchor = 'w')
        self._status_canvas.create_window ((self.BASE_X_POS + self.X_POS_INDENT, self._pos_y), anchor = 'w', width = 200, window = self._stopped_lbl)
        self._stopped_var = Tkinter.StringVar ()
        self._stopped_edt = Tkinter.Entry (self._status_canvas, textvariable = self._stopped_var, state = Tkinter.DISABLED)
        self._status_canvas.create_window ((self.X_DATA1_INDENT, self._pos_y), anchor = 'w', width = self.ENTRY_WIDTH, window = self._stopped_edt)
        self._pos_y = self._pos_y + self.DELTA_Y
        
        # has the logger seen a trigger
        self._trig_occur_lbl = Tkinter.Label (self._status_canvas, text = "Trigger occurred y/n", anchor = 'w')
        self._status_canvas.create_window ((self.BASE_X_POS + self.X_POS_INDENT, self._pos_y), anchor = 'w', width = 200, window = self._trig_occur_lbl)
        self._trig_occur_var = Tkinter.StringVar ()
        self._trig_occur_edt = Tkinter.Entry (self._status_canvas, textvariable = self._trig_occur_var, state = Tkinter.DISABLED)
        self._status_canvas.create_window ((self.X_DATA1_INDENT, self._pos_y), anchor = 'w', width = self.ENTRY_WIDTH, window = self._trig_occur_edt)
        self._pos_y = self._pos_y + self.DELTA_Y
        
        # was there a wrap around on the DDR-3 address
        self._wrap_around_lbl = Tkinter.Label (self._status_canvas, text = "DDR-3 wrap-around y/n", anchor = 'w')
        self._status_canvas.create_window ((self.BASE_X_POS + self.X_POS_INDENT, self._pos_y), anchor = 'w', width = 200, window = self._wrap_around_lbl)
        self._wrap_around_var = Tkinter.StringVar ()
        self._wrap_around_edt = Tkinter.Entry (self._status_canvas, textvariable = self._wrap_around_var, state = Tkinter.DISABLED)
        self._status_canvas.create_window ((self.X_DATA1_INDENT, self._pos_y), anchor = 'w', width = self.ENTRY_WIDTH, window = self._wrap_around_edt)
        self._pos_y = self._pos_y + self.DELTA_Y
        
        # show the absolute DDR-3 address where the trigger occurred
        self._trigger_lbl = Tkinter.Label (self._status_canvas, text = "DDR-3 address for trigger", anchor = 'w')
        self._status_canvas.create_window ((self.BASE_X_POS, self._pos_y), anchor = 'w', width = 200, window = self._trigger_lbl)
        self._trigger_var = Tkinter.StringVar ()
        self._trigger_edt = Tkinter.Entry (self._status_canvas, textvariable = self._trigger_var, state = Tkinter.DISABLED)
        self._status_canvas.create_window ((self.X_DATA1_INDENT, self._pos_y), anchor = 'w', width = self.ENTRY_WIDTH, window = self._trigger_edt)
        
        # on the same row we show a label variable that indicates the approximate number of bytes in the trace
        self._num_in_trace_lbl = Tkinter.Label (self._status_canvas, text = "(approx. 0 bytes in trace)", anchor = 'w')
        self._status_canvas.create_window ((self.X_DATA2_INDENT, self._pos_y), anchor = 'w', width = 200, window = self._num_in_trace_lbl)
        self._pos_y = self._pos_y + self.DELTA_Y
        
        # add the tab to the notebook
        self._master._nb.add_screen (self._status_frame, " Status ")
        
    #-----
    # update the status widgets
    def _update_status (self):
        # set the sample rate
        if (self._status [HSM_SAMPLE_CLK_DIV] == 0):
            self._samplerate_var.set ("Unknown")
        else:
            # note: we need to take the clock divider + 1 to calculate the sample rate because in the FPGA it counts down to 0,
            #       after which the next clock is the sample
            thetext = "%d"%(100000000 / (self._status [HSM_SAMPLE_CLK_DIV] + 1))
            self._samplerate_var.set (thetext)
        
        # set the raw CSR value
        thetext = "0x%08X"%(self._status [HSM_LOG_CSR])
        self._csr_var.set (thetext)
        
        # set the details
        self._tr_def_var.set ("yes" if ((self._status [HSM_LOG_CSR] & HSM_CSR_TRIGGER_SET_MASK) != 0) else "no")
        self._log_run_var.set ("yes" if ((self._status [HSM_LOG_CSR] & HSM_CSR_RUNNING_MASK) != 0) else "no")
        self._wait_tr_var.set ("yes" if ((self._status [HSM_LOG_CSR] & HSM_CSR_WAITING_MASK) != 0) else "no")
        self._stopping_var.set ("yes" if ((self._status [HSM_LOG_CSR] & HSM_CSR_STOPPING_MASK) != 0) else "no")
        self._stopped_var.set ("yes" if ((self._status [HSM_LOG_CSR] & HSM_CSR_STOPPED_MASK) != 0) else "no")
        self._trig_occur_var.set ("yes" if ((self._status [HSM_LOG_CSR] & HSM_CSR_TRIG_OCCURRED_MASK) != 0) else "no")
        self._wrap_around_var.set ("yes" if ((self._status [HSM_LOG_CSR] & HSM_CSR_DDR_WRAP_AROUND_MASK) != 0) else "no")
        
        # set the DDR-3 address where the trigger occurred
        thetext = "0x%08X"%(self._status [HSM_TRIGGER_OCCURRED_ADDRESS])
        self._trigger_var.set (thetext)
        
        # calculate the approximate number of bytes in the trace
        if ((self._status [HSM_LOG_CSR] & HSM_CSR_TRIG_OCCURRED_MASK) != 0):
            # we have a valid trigger and the logger has stopped
            numbytes = self._status [HSM_TRIGGER_OCCURRED_ADDRESS] - self._status [HSM_DDR3_LOG_BASE_ADDRESS]       # number of bytes until trigger
            numbytes = numbytes / 64                                                                                # number of samples until trigger
            numbytes = (numbytes + 1) * 64                                                                          # number of bytes including sample moment, all channels
            numbytes = numbytes + (self._status [HSM_POST_TRIGGER_LENGTH] * 64)                                     # number of bytes including post trigger
            nb_text  = "(approx. %d bytes in trace)"%numbytes
        else:
            # we don't have a valid trigger or the logger hasn't stopped yet so we don't know how many bytes there are
            numbytes = 0
            nb_text  = "(0 bytes in trace)"
        self._num_in_trace_lbl ["text"] = nb_text
        
    #-----
    # request the current status from the logger
    def _request_status (self):
        # we send a get status request
        self._master._send_request ("S")
        
        # we expect 24 reply bytes
        r = self._master._receive_reply (HSM_NUM_STATUS_BYTES)
        
        # those HSM_NUM_STATUS_BYTES bytes are a number of words of 32-bit unsigned, little endian
        if (len (r) == HSM_NUM_STATUS_BYTES):
            #  0: HSM_CHANNEL_ENABLE                // enable flags; not used by the logger fabric, but can be used to autofill the channel configuration (which channels to show)
            #  1: HSM_SPI_CLK_DIVIDER               // (IGNORE) SPI clock divider
            #  2: HSM_SAMPLE_CLK_DIV                // high speed sample clock divider
            #  3: HSM_LOW_FREQ_SAMPLE_CLK_DIV       // (IGNORE) low frequency sample clock divider
            #  4: HSM_DDR3_LOG_BASE_ADDRESS         // 32-bit base address for DDR-3
            #  5: HSM_POST_TRIGGER_LENGTH           // 32-bit post trigger length
            #  6: HSM_TRIGGER_OCCURRED_ADDRESS      // absolute DDR-3 address where the trigger occurred (0 if it did not happen yet)
            #  7: HSM_LOG_CSR                       // 32-bit control and status register: low 16 bit are control, upper 16 bit are status
            format_str = "<" + str (HSM_NUM_STATUS_BYTES / 4) + "L"
            self._status = struct.unpack (format_str, r)
        else:
            self._status = (0,) * (HSM_NUM_STATUS_BYTES / 4)
            
        # this function can be called by other clients, so return the retrieved status
        return self._status
    
    #-----
    # resize the graph
    def resize (self, width, height):
        pass
        
    #-----
    # function for periodic processing
    def process (self):
        # request the current status
        self._request_status ()
        
        # update our on-screen status
        self._update_status ()
        
        # inform the master about which channel is enabled
        self._master._set_enabled_channels (self._status [HSM_CHANNEL_ENABLE])
    
#-----------------------------------------------------------------------------#
#                     Configuration tabsheet base class                       #
#-----------------------------------------------------------------------------#
# The intention of this tabsheet is to show a configuration of the visualisation of the traced (logged/sampled whatever...) data.
# This configuration tab is outside of the "Settings", because the settings dictate more overall or generic behavior of the application
# rather than the visualisation of the data.
# This tabsheet will include selection for do/don't show any individual channel, how many graphs to show and in what geometry and
# what channels in what graph. Other relevant data, such as when and where the trigger occurred and how many of the samples to upload
# from the logger are part of the status tabsheet.
# For now, as the more detailed and complex configuration options cost time, the graph tab will hold two graphs, which share horizontal
# scrolling and zooming. 
class clsConfigTabSheet:
    #-----
    # class constructor
    def __init__ (self, master):
        # copy variables
        self._master = master
        
        # take over local variables from the master
        self.BUTTON_WIDTH     = self._master.BUTTON_WIDTH
        self.BASE_X_POS       = self._master.BASE_X_POS
        self.BASE_Y_POS       = self._master.BASE_Y_POS
        self.X_POS_INDENT     = self._master.X_POS_INDENT
        self.DELTA_Y          = self._master.DELTA_Y
        self.CBX_WIDTH        = self._master.CBX_WIDTH
        self.LBL_WIDTH        = self._master.LBL_WIDTH
        self.BTN_CANVAS_WIDTH = self._master.BTN_CANVAS_WIDTH
        self.ENTRY_WIDTH      = self._master.ENTRY_WIDTH
        self.ENTRY_SEP        = self._master.ENTRY_SEP
        self.SELECT_WIDTH     = self._master.SELECT_WIDTH
        self.X_DATA1_INDENT   = 325
        self.X_DATA2_INDENT   = 450
        
        # create local variables
        self._cbx_ch_enabled_val = []           # will keep integer variables for selected yes/no
        self._cbx_ch_enabled_wid = []           # will keep the reference to the widget
        self._enabled_channels   = 0xFFFF       # on power-up all channels are enabled
        
        # create widgets
        self._create_widgets ()
        
    #-----
    # build widgets for status tabsheet
    def _create_widgets (self):
        # create a frame and a canvas to put widgets on
        self._config_frame = Tkinter.Frame (self._master._nb ())
        self._config_canvas = Tkinter.Canvas (self._config_frame)
        self._config_canvas.pack (side = "top", expand = 1, fill = Tkinter.BOTH)
        
        # tell what these checkboxes are for
        self._pos_y = self.BASE_Y_POS
        self._enabled_lbl = Tkinter.Label (self._config_canvas, text = "Regulator settings", anchor = 'w')
        self._config_canvas.create_window ((self.BASE_X_POS, self._pos_y), anchor = 'w', width = 200, window = self._enabled_lbl)
        
        # show the settings we can change
        self._pos_y = self.BASE_Y_POS + self.DELTA_Y
        
        # show the drive setpoint
        self._drivesetp_lbl = Tkinter.Label (self._config_canvas, text = "Drive setpoint", anchor = 'w')
        self._config_canvas.create_window ((self.BASE_X_POS, self._pos_y), anchor = 'w', width = 200, window = self._drivesetp_lbl)
        self._drivesetp_var = Tkinter.IntVar ()
        self._drivesetp_edt = Tkinter.Entry (self._config_canvas, textvariable = self._drivesetp_var)
        self._config_canvas.create_window ((self.X_DATA1_INDENT, self._pos_y), anchor = 'w', width = self.ENTRY_WIDTH, window = self._drivesetp_edt)
        self._drivesetp_var.set (self._master._settings ["DRIVE_SETP"]["value"])
        self._pos_y = self._pos_y + self.DELTA_Y
        
        # show the total samples
        self._totalsample_lbl = Tkinter.Label (self._config_canvas, text = "Total Samples", anchor = 'w')
        self._config_canvas.create_window ((self.BASE_X_POS, self._pos_y), anchor = 'w', width = 200, window = self._totalsample_lbl)
        self._totalsample_var = Tkinter.IntVar ()
        self._totalsample_edt = Tkinter.Entry (self._config_canvas, textvariable = self._totalsample_var)
        self._config_canvas.create_window ((self.X_DATA1_INDENT, self._pos_y), anchor = 'w', width = self.ENTRY_WIDTH, window = self._totalsample_edt)
        self._totalsample_var.set (self._master._settings ["TOTAL_SAMPLES"]["value"])
        self._pos_y = self._pos_y + self.DELTA_Y
        
        # show the output saturation P
        self._outsat_p_lbl = Tkinter.Label (self._config_canvas, text = "Output Saturation P", anchor = 'w')
        self._config_canvas.create_window ((self.BASE_X_POS, self._pos_y), anchor = 'w', width = 200, window = self._outsat_p_lbl)
        self._outsat_p_var = Tkinter.IntVar ()
        self._outsat_p_edt = Tkinter.Entry (self._config_canvas, textvariable = self._outsat_p_var)
        self._config_canvas.create_window ((self.X_DATA1_INDENT, self._pos_y), anchor = 'w', width = self.ENTRY_WIDTH, window = self._outsat_p_edt)
        self._outsat_p_var.set (self._master._settings ["OUTSAT_P"]["value"])
        self._pos_y = self._pos_y + self.DELTA_Y
        
        # show the output saturation M
        self._outsat_n_lbl = Tkinter.Label (self._config_canvas, text = "Output Saturation N", anchor = 'w')
        self._config_canvas.create_window ((self.BASE_X_POS, self._pos_y), anchor = 'w', width = 200, window = self._outsat_n_lbl)
        self._outsat_n_var = Tkinter.IntVar ()
        self._outsat_n_edt = Tkinter.Entry (self._config_canvas, textvariable = self._outsat_n_var)
        self._config_canvas.create_window ((self.X_DATA1_INDENT, self._pos_y), anchor = 'w', width = self.ENTRY_WIDTH, window = self._outsat_n_edt)
        self._outsat_n_var.set (self._master._settings ["OUTSAT_M"]["value"])
        self._pos_y = self._pos_y + self.DELTA_Y
        
        # show the PWM Max
        self._pwm_max_lbl = Tkinter.Label (self._config_canvas, text = "PWM Max", anchor = 'w')
        self._config_canvas.create_window ((self.BASE_X_POS, self._pos_y), anchor = 'w', width = 200, window = self._pwm_max_lbl)
        self._pwm_max_var = Tkinter.IntVar ()
        self._pwm_max_edt = Tkinter.Entry (self._config_canvas, textvariable = self._pwm_max_var)
        self._config_canvas.create_window ((self.X_DATA1_INDENT, self._pos_y), anchor = 'w', width = self.ENTRY_WIDTH, window = self._pwm_max_edt)
        self._pwm_max_var.set (self._master._settings ["PWM_MAX"]["value"])
        self._pos_y = self._pos_y + self.DELTA_Y
        
        # show the PWM Min
        self._pwm_min_lbl = Tkinter.Label (self._config_canvas, text = "PWM Min", anchor = 'w')
        self._config_canvas.create_window ((self.BASE_X_POS, self._pos_y), anchor = 'w', width = 200, window = self._pwm_min_lbl)
        self._pwm_min_var = Tkinter.IntVar ()
        self._pwm_min_edt = Tkinter.Entry (self._config_canvas, textvariable = self._pwm_min_var)
        self._config_canvas.create_window ((self.X_DATA1_INDENT, self._pos_y), anchor = 'w', width = self.ENTRY_WIDTH, window = self._pwm_min_edt)
        self._pwm_min_var.set (self._master._settings ["PWM_MIN"]["value"])
        self._pos_y = self._pos_y + self.DELTA_Y
        
        # show the Kp
        self._kp_lbl = Tkinter.Label (self._config_canvas, text = "Kp", anchor = 'w')
        self._config_canvas.create_window ((self.BASE_X_POS, self._pos_y), anchor = 'w', width = 200, window = self._kp_lbl)
        self._kp_var = Tkinter.IntVar ()
        self._kp_edt = Tkinter.Entry (self._config_canvas, textvariable = self._kp_var)
        self._config_canvas.create_window ((self.X_DATA1_INDENT, self._pos_y), anchor = 'w', width = self.ENTRY_WIDTH, window = self._kp_edt)
        self._kp_var.set (self._master._settings ["KP"]["value"])
        self._pos_y = self._pos_y + self.DELTA_Y
        
        # show the Acceleration
        self._acceleration_lbl = Tkinter.Label (self._config_canvas, text = "Acceleration", anchor = 'w')
        self._config_canvas.create_window ((self.BASE_X_POS, self._pos_y), anchor = 'w', width = 200, window = self._acceleration_lbl)
        self._acceleration_var = Tkinter.IntVar ()
        self._acceleration_edt = Tkinter.Entry (self._config_canvas, textvariable = self._acceleration_var)
        self._config_canvas.create_window ((self.X_DATA1_INDENT, self._pos_y), anchor = 'w', width = self.ENTRY_WIDTH, window = self._acceleration_edt)
        self._acceleration_var.set (self._master._settings ["ACCELERATION"]["value"])
        self._pos_y = self._pos_y + self.DELTA_Y
        
        # add a button to apply this to the graph
        self._pos_y = self._pos_y + self.DELTA_Y
        self._set_apply_button = Tkinter.Button (self._config_canvas, width = self.BUTTON_WIDTH)
        self._set_apply_button ["text"]    = "Apply"
        self._set_apply_button ["fg"]      = "black"
        self._set_apply_button ["command"] = self._apply
        self._config_canvas.create_window ((self.X_POS_INDENT, self._pos_y), anchor = 'w', window = self._set_apply_button)
        
        # add the tab to the notebook
        self._master._nb.add_screen (self._config_frame, " Settings ")
        
    #-----
    # set channel visibility based on checkboxes
    def _apply (self):
        # change values based on input
        self._master._settings ["TOTAL_SAMPLES"]["value"] = self._totalsample_var.get ()
        self._master._settings ["DRIVE_SETP"]["value"] = self._drivesetp_var.get ()
        self._master._settings ["OUTSAT_P"]["value"] = self._outsat_p_var.get()
        self._master._settings ["OUTSAT_M"]["value"] = self._outsat_n_var.get()
        self._master._settings ["PWM_MAX"]["value"] = self._pwm_max_var.get()
        self._master._settings ["PWM_MIN"]["value"] = self._pwm_min_var.get()
        self._master._settings ["KP"]["value"] = self._kp_var.get()
        self._master._settings ["ACCELERATION"]["value"] = self._acceleration_var.get()
    #-----
    # resize the graph
    def resize (self, width, height):
        pass
        
    #-----
    # function for periodic processing
    def process (self):
        pass
    
#-----------------------------------------------------------------------------#
#                         Graph tabsheet base class                           #
#-----------------------------------------------------------------------------#
# Should be configurable based on settings from the config tab sheet.
# For now, as the more detailed and complex configuration options cost time, the graph tab will hold two graphs, which share horizontal
# scrolling and zooming.
class clsGraphTabSheet:
    #-----
    # class constructor
    def __init__ (self, master):
        # copy variables
        self._master = master
        
        # take over local variables from the master
        self.BUTTON_WIDTH     = self._master.BUTTON_WIDTH
        self.BASE_X_POS       = self._master.BASE_X_POS
        self.BASE_Y_POS       = self._master.BASE_Y_POS
        self.X_POS_INDENT     = self._master.X_POS_INDENT
        self.DELTA_Y          = self._master.DELTA_Y
        self.CBX_WIDTH        = self._master.CBX_WIDTH
        self.LBL_WIDTH        = self._master.LBL_WIDTH
        self.BTN_CANVAS_WIDTH = self._master.BTN_CANVAS_WIDTH
        self.ENTRY_WIDTH      = self._master.ENTRY_WIDTH
        self.ENTRY_SEP        = self._master.ENTRY_SEP
        self.SELECT_WIDTH     = self._master.SELECT_WIDTH
        
        # set local variables
        self._is_uploading     = False
        self._blocks_loaded    = 0
        self._upload_settings  = None
        
        # file dialog options for save data
        self._file_dlg_opt = {}
        self._file_dlg_opt ["defaultextension"] = ".xlsx"
        self._file_dlg_opt ["filetypes"]        = [('Excel files', '.xlsx'), ('Comma separated', '.csv'), ('Matlab', '.mat'), ('Raw binary', '.bin')]
        self._file_dlg_opt ["initialdir"]       = os.path.dirname ('.')
        self._file_dlg_opt ["initialfile"]      = ''
        self._file_dlg_opt ["parent"]           = self._master
        self._file_dlg_opt ["title"]            = "Select a File..."
        
        # create widgets
        self._create_widgets ()
        
    #-----
    # build widgets for capture graphics tabsheet
    def _create_widgets (self):
        # create a frame and a canvas to put widgets on
        self._graph_frame = Tkinter.Frame (self._master._nb ())
        self._graph_canvas = Tkinter.Canvas (self._graph_frame)
        self._graph_canvas.pack (side = "top", expand = 1, fill = Tkinter.BOTH)
        
        # we create 2 additional frames: one on the bottom to hold buttons
        # and one on top to hold the capture graphs
        self._capture_button_frame = Tkinter.Frame (self._graph_canvas, bd = 2, relief = Tkinter.GROOVE)
        self._capture_button_frame.pack (side = "bottom", anchor = 'w', expand = 0, fill = Tkinter.X)
        self._capture_data_frame = Tkinter.Frame (self._graph_canvas, bd = 2, relief = Tkinter.GROOVE)
        self._capture_data_frame.pack (side = "top", anchor = 'w', expand = 1, fill = Tkinter.BOTH)
        
        # put button widgets on the button canvas
        self._upload_button = Tkinter.Button (self._capture_button_frame, width = self.BUTTON_WIDTH)
        self._upload_button ["text"]    = "Measure"
        self._upload_button ["fg"]      = "black"
        self._upload_button ["command"] = self._measure
        self._upload_button.pack (side = "left", anchor = 'w')
        
        self._save_button = Tkinter.Button (self._capture_button_frame, width = self.BUTTON_WIDTH)
        self._save_button ["text"]    = "Save"
        self._save_button ["fg"]      = "black"
        self._save_button ["command"] = self._save_data
        self._save_button ["state"]   = Tkinter.DISABLED
        self._save_button.pack (side = "left", anchor = 'w')
        
        # add graphs: top for ADC-0, 8 channels, bottom for ADC-1, 8 channels
        self._capture_graph = widgets.clsCaptureGraph (self._capture_data_frame)
        self._capture_graph.grid (row = 0, column = 0, sticky = "nsew")
        
        # add the tab to the notebook
        self._master._nb.add_screen (self._graph_frame, " Logging ")
        
        # force a resize of this window to make it fit properly
        self._capture_graph.resize (self._master._data_frame.winfo_width (), self._master._data_frame.winfo_height ())
        
    #-----
    # retrieve the data from the logger
    def _measure (self):
        # prepare arrays for data
        samples = []
        datas = []
        kps = []
        plants = []
        errors = []
        outputs = []
        pwms = []
        setpoints = []
        output_sats = []
        
        run = True
        sample = 0
        init_samples = 0
        setpoint = 512
        setpoint_new = 512
        org_setpoint_new = 0
        error = 0
        output = 0
        kp = 15
        pwm = 399
        pwm_prev = 399
        plant = 10
        output_sat = 5
        direction = ''
        s = []
        acceleration = 5
        acc_count = 0
        jerk = 1
        sample_rate = 150
        state = 'init'
        print '<><><><><><><><>< init ><><><><><><><><>'
        
        #enable amplifier to send data
        self._master.send_request (b'\x55')
        self._master.send_request (b'\x00')
        self._master.send_request (b'\x01')
        self._master.send_request (b'\n')
        self._master.send_request (b'\r')
        
        # determine initial direction
        if(self._master._settings ["DRIVE_SETP"]["value"] < 512):
            direction = "CW"
            pwm_prev = 350 #300 # pre-load pwm value for feedforward
            pwm = 350
        elif(self._master._settings ["DRIVE_SETP"]["value"] > 512):
            direction = "CCW"
            pwm_prev = 450 #500 # pre-load pwm value for feedforward
            pwm = 450
        
        # wait until we see 0xAA so we are in sync with whatever is coming from the micro
        b = 0
        while (b != 0xAA):
            c = self._master.receive_reply ()
            b = struct.unpack ("B", c)[0]
        
        # now we are in sync, start collecting data
        while run:
        
            for c in self._master.receive_reply ():
                b=struct.unpack ("B", c)[0]
                s.append(b)
                if (b == 0xaa):
                    if(len(s) == 5 and s[4] == 170):
                        data = (s[0] + (s[1] << 4) + (s[2] << 8) + (s[3] << 12))
                        
                        if(state == 'init'):
                            
                            sample = sample + 1
                            
                            setpoint_new = self._master._settings ["DRIVE_SETP"]["value"]
                            org_setpoint_new = self._master._settings ["DRIVE_SETP"]["value"]
                            
                            # determine direction
                            if(setpoint_new < 512):
                                direction = "CW"
                            elif(setpoint_new > 511):
                                direction = "CCW"
                            
                            state = 'run'
                            print '<><><><><><><><>< run ><><><><><><><><>'

                        elif(state == 'run'):
                            setpoint_new = self._master._settings ["DRIVE_SETP"]["value"]
                            
                            error = setpoint - data
                            output = int((self._master._settings ["KP"]["value"] * error) / 100) # * plant ) / 1000
                            
                            if(output > self._master._settings ["OUTSAT_P"]["value"]):
                                print 'Output to large: ' + str(output)
                                output = self._master._settings ["OUTSAT_P"]["value"]
                            elif(output < self._master._settings ["OUTSAT_M"]["value"]):
                                print 'Output to small: ' + str(output)
                                output = self._master._settings ["OUTSAT_M"]["value"]
                            
                            if(output < 0):
                                pwm = pwm_prev - abs(output); #When the error is negative (measured BEMF number is higher then setpoint BEMF(300)) the PWM dutycycle needs to be increased hence adding the negative number
                            elif(output > 0):
                                pwm = pwm_prev + output;
                            
                            sample = sample + 1
                            
                            # determine direction
                            if(setpoint_new < 512):
                                direction = "CW"
                            elif(setpoint_new > 511):
                                direction = "CCW"
                            
                            acc_count += 1
                            if(acc_count > self._master._settings ["ACCELERATION"]["value"] and self._master._settings ["ACCELERATION"]["value"] > 0):
                                acc_count = 0
                                if(direction == "CW"    and setpoint_new > (setpoint+jerk) and setpoint_new != setpoint):
                                    setpoint += jerk                                   
                                elif(direction == "CW"  and setpoint_new < (setpoint-jerk) and setpoint_new != setpoint):
                                    setpoint -= jerk                                    
                                elif(direction == "CCW" and setpoint_new < (setpoint-jerk) and setpoint_new != setpoint):
                                    setpoint -= jerk                                    
                                elif(direction == "CCW" and setpoint_new > (setpoint+jerk) and setpoint_new != setpoint):
                                    setpoint += jerk
                                    
                            elif(self._master._settings ["ACCELERATION"]["value"] == 0):
                                setpoint = setpoint_new
                                acc_count = 0
                                
                            if (sample > (self._master._settings ["TOTAL_SAMPLES"]["value"] + init_samples)):
                                state = 'stop'
                                print '<><><><><><><><>< stop ><><><><><><><><>'
                                setpoint_new = 511
                                
                            print str(setpoint) + ' ' + str(output)    
                        
                        elif(state == 'stop'):
                            
                            error = setpoint - data
                            output = int((self._master._settings ["KP"]["value"] * error) / 100) # * plant ) / 1000
                            
                            if(output > self._master._settings ["OUTSAT_P"]["value"]):
                                print 'Output to large: ' + str(output)
                                output = self._master._settings ["OUTSAT_P"]["value"]
                            elif(output < self._master._settings ["OUTSAT_M"]["value"]):
                                print 'Output to small: ' + str(output)
                                output = self._master._settings ["OUTSAT_M"]["value"]
                            
                            if(output < 0):
                                pwm = pwm_prev - abs(output); #When the error is negative (measured BEMF number is higher then setpoint BEMF(300)) the PWM dutycycle needs to be increased hence adding the negative number
                            elif(output > 0):
                                pwm = pwm_prev + output;
                            
                            sample = sample + 1
                            
                            # determine direction
                            if(setpoint_new < 512):
                                direction = "CW"
                            elif(setpoint_new > 511):
                                direction = "CCW"
                            
                            acc_count += 1
                            if(acc_count > (self._master._settings ["ACCELERATION"]["value"] * 3) and self._master._settings ["ACCELERATION"]["value"] > 0):
                                acc_count = 0
                                if(direction == "CW"    and setpoint_new > (setpoint+jerk) and setpoint_new != setpoint):
                                    setpoint += jerk                                   
                                elif(direction == "CW"  and setpoint_new < (setpoint-jerk) and setpoint_new != setpoint):
                                    setpoint -= jerk                                    
                                elif(direction == "CCW" and setpoint_new < (setpoint-jerk) and setpoint_new != setpoint):
                                    setpoint -= jerk                                    
                                elif(direction == "CCW" and setpoint_new > (setpoint+jerk) and setpoint_new != setpoint):
                                    setpoint += jerk
                                
                                    
                            elif(self._master._settings ["ACCELERATION"]["value"] == 0):
                                setpoint = setpoint_new
                                acc_count = 0
                                
                            
                            if(data > 506 and data < 518):
                                run = False
                                print '<><><><><><><><>< done ><><><><><><><><>'
                            else:
                                pwm = setpoint
                                
                            print str(setpoint) + ' ' + str(output)
                            
                        
                        if (direction == "CCW" and pwm > self._master._settings ["PWM_MAX"]["value"]):
                            pwm = self._master._settings ["PWM_MAX"]["value"]
                        elif(direction == "CCW" and pwm < 400):
                            pwm = 400
                        elif(direction == "CW" and pwm < self._master._settings ["PWM_MIN"]["value"]):
                            pwm = self._master._settings ["PWM_MIN"]["value"]
                        elif(direction == "CW" and pwm > 399):
                            pwm = 399                        
                        
                        if(pwm_prev != pwm):
                            self._master.send_request (b'\xAA')
                            self._master.send_request (struct.pack('>B', (int(pwm>> 8))))
                            self._master.send_request (struct.pack('>B', (int(pwm & 0x00FF))))
                            self._master.send_request (b'\n')
                            self._master.send_request (b'\r')
                            pwm_prev = pwm
                        
                        
                        samples.append (sample)
                        datas.append (data)
                        kps.append (kp)
                        plants.append (plant)
                        errors.append (error)
                        outputs.append (output)
                        pwms.append (pwm)
                        setpoints.append (setpoint)
                        output_sats.append (self._master._settings ["OUTSAT_P"]["value"])                        
                        
                        s = []
                        
                        # update idle tasks to keep the GUI 'alive'
                        self._master.update ()
                        
                                                
                        
                    else:
                        print('Data corrupt......')
                        s = []
                        
                #if (sample > (self._master._settings ["TOTAL_SAMPLES"]["value"] + 200 * (acceleration + 1))):
                    #run = False                
        
        
        
        '''
        # now we are in sync, start collecting data
        while run:
        
            for c in self._master.receive_reply ():
                b=struct.unpack ("B", c)[0]
                s.append(b)
                if (b == 0xaa):
                    if(len(s) == 5 and s[4] == 170):
                        data = (s[0] + (s[1] << 4) + (s[2] << 8) + (s[3] << 12))
                        
                        if(state == 'init'):
                            
                            sample = sample + 1
                            
                            setpoint_new = self._master._settings ["DRIVE_SETP"]["value"]
                            org_setpoint_new = self._master._settings ["DRIVE_SETP"]["value"]
                            
                            # determine direction
                            if(setpoint_new < 400):
                                direction = "CW"
                            elif(setpoint_new > 399):
                                direction = "CCW"
                            
                            acc_count += 1
                            if(acc_count > self._master._settings ["ACCELERATION"]["value"] and self._master._settings ["ACCELERATION"]["value"] > 0):
                                acc_count = 0
                                if(direction == "CW"    and setpoint_new > (setpoint+jerk) and setpoint_new != setpoint):
                                    setpoint += jerk                                   
                                elif(direction == "CW"  and setpoint_new < (setpoint-jerk) and setpoint_new != setpoint):
                                    setpoint -= jerk                                    
                                elif(direction == "CCW" and setpoint_new < (setpoint-jerk) and setpoint_new != setpoint):
                                    setpoint -= jerk                                    
                                elif(direction == "CCW" and setpoint_new > (setpoint+jerk) and setpoint_new != setpoint):
                                    setpoint += jerk

                                if(setpoint_new >= (setpoint-jerk) and setpoint_new <= (setpoint+jerk)):
                                    state = 'run'
                                    setpoint_new = data
                                    setpoint = data
                                    init_samples = sample
                                    self._master._config_tab._drivesetp_var.set(setpoint)
                                    self._master.update ()
                                    self._master._settings ["DRIVE_SETP"]["value"] = self._master._config_tab._drivesetp_var.get ()
                                    print '<><><><><><><><>< run ><><><><><><><><>'
                                else:
                                    pwm = setpoint
                                    
                                print str(setpoint)
                                
                            elif(self._master._settings ["ACCELERATION"]["value"] == 0):
                                setpoint = setpoint_new
                                acc_count += 1
                                if(acc_count > 200):
                                    state = 'run'
                                    print '<><><><><><><><>< run ><><><><><><><><>'
                                    acc_count = 0
                                    setpoint_new = data
                                    setpoint = data
                                    init_samples = sample
                                else:
                                    pwm = setpoint


                        elif(state == 'run'):
                            setpoint_new = self._master._settings ["DRIVE_SETP"]["value"]
                            
                            error = setpoint - data
                            output = int((self._master._settings ["KP"]["value"] * error) / 100) # * plant ) / 1000
                            
                            if(output > self._master._settings ["OUTSAT_P"]["value"]):
                                output = self._master._settings ["OUTSAT_P"]["value"]
                            elif(output < self._master._settings ["OUTSAT_M"]["value"]):
                                output = self._master._settings ["OUTSAT_M"]["value"]
                            
                            if(output < 0):
                                pwm = pwm_prev - abs(output); #When the error is negative (measured BEMF number is higher then setpoint BEMF(300)) the PWM dutycycle needs to be increased hence adding the negative number
                            elif(output > 0):
                                pwm = pwm_prev + output;
                            
                            sample = sample + 1
                            
                            # determine direction
                            if(setpoint < 512):
                                direction = "CW"
                            elif(setpoint > 511):
                                direction = "CCW"
                            
                            acc_count += 1
                            if(acc_count > self._master._settings ["ACCELERATION"]["value"] and self._master._settings ["ACCELERATION"]["value"] > 0):
                                acc_count = 100
                                if(direction == "CW"    and setpoint_new > (setpoint+jerk) and setpoint_new != setpoint):
                                    setpoint += jerk                                   
                                elif(direction == "CW"  and setpoint_new < (setpoint-jerk) and setpoint_new != setpoint):
                                    setpoint -= jerk                                    
                                elif(direction == "CCW" and setpoint_new < (setpoint-jerk) and setpoint_new != setpoint):
                                    setpoint -= jerk                                    
                                elif(direction == "CCW" and setpoint_new > (setpoint+jerk) and setpoint_new != setpoint):
                                    setpoint += jerk
                                print str(setpoint) + ' ' + str(output)
                                    
                            elif(self._master._settings ["ACCELERATION"]["value"] == 0):
                                setpoint = setpoint_new
                                acc_count = 0
                                
                            if (sample > (self._master._settings ["TOTAL_SAMPLES"]["value"] + init_samples)):
                                state = 'stop'
                                print '<><><><><><><><>< stop ><><><><><><><><>'
                                setpoint_new = 399
                                setpoint = pwm_prev
                                self._master._config_tab._drivesetp_var.set(org_setpoint_new)
                                self._master.update ()
                        
                        elif(state == 'stop'):
                            sample = sample + 1
                            
                            # determine direction
                            if(setpoint_new < 400):
                                direction = "CW"
                            elif(setpoint_new > 399):
                                direction = "CCW"
                            
                            acc_count += 1
                            if(acc_count > self._master._settings ["ACCELERATION"]["value"] and self._master._settings ["ACCELERATION"]["value"] > 0):
                                acc_count = 0
                                if(direction == "CW"    and setpoint_new > (setpoint+jerk) and setpoint_new != setpoint):
                                    setpoint += jerk                                   
                                elif(direction == "CW"  and setpoint_new < (setpoint-jerk) and setpoint_new != setpoint):
                                    setpoint -= jerk                                    
                                elif(direction == "CCW" and setpoint_new < (setpoint-jerk) and setpoint_new != setpoint):
                                    setpoint -= jerk                                    
                                elif(direction == "CCW" and setpoint_new > (setpoint+jerk) and setpoint_new != setpoint):
                                    setpoint += jerk

                                if(pwm_prev >= (399-jerk) and pwm_prev <= (399+jerk)):
                                    run = False
                                    print '<><><><><><><><>< done ><><><><><><><><>'
                                else:
                                    pwm = setpoint
                                    
                                print str(setpoint)
                            
                        
                        if (direction == "CCW" and pwm > self._master._settings ["PWM_MAX"]["value"]):
                            pwm = self._master._settings ["PWM_MAX"]["value"]
                        elif(direction == "CCW" and pwm < 400):
                            pwm = 400
                        elif(direction == "CW" and pwm < self._master._settings ["PWM_MIN"]["value"]):
                            pwm = self._master._settings ["PWM_MIN"]["value"]
                        elif(direction == "CW" and pwm > 399):
                            pwm = 399                        
                        
                        if(pwm_prev != pwm):
                            self._master.send_request (b'\xAA')
                            self._master.send_request (struct.pack('>B', (int(pwm>> 8))))
                            self._master.send_request (struct.pack('>B', (int(pwm & 0x00FF))))
                            self._master.send_request (b'\n')
                            self._master.send_request (b'\r')
                            pwm_prev = pwm
                        
                        
                        samples.append (sample)
                        datas.append (data)
                        kps.append (kp)
                        plants.append (plant)
                        errors.append (error)
                        outputs.append (output)
                        pwms.append (pwm)
                        setpoints.append (setpoint)
                        output_sats.append (self._master._settings ["OUTSAT_P"]["value"])                        
                        
                        s = []
                        
                        # update idle tasks to keep the GUI 'alive'
                        self._master.update ()
                        
                                                
                        
                    else:
                        print('Data corrupt......')
                        s = []
                        
                #if (sample > (self._master._settings ["TOTAL_SAMPLES"]["value"] + 200 * (acceleration + 1))):
                    #run = False
        '''
        
        
        # Disable amplifier of sending data
        self._master.send_request (b'\x55')
        self._master.send_request (struct.pack('>B', 0))
        self._master.send_request (struct.pack('>B', 0))
        self._master.send_request (b'\n')
        self._master.send_request (b'\r')
        
        # we're out of the loop, see if we have data and update the graph
        if (len (samples) > 1):
            self._capture_graph.update_capture (samples, datas, kps, plants, errors, outputs, pwms, setpoints, output_sats)
            self._save_button ["state"]  = Tkinter.NORMAL
            
            # copy data to local arrays so we can save it
            self._tb = samples
            self._adc0 = []
            self._adc0.append (datas)
            self._adc0.append (kps)
            self._adc0.append (plants)
            self._adc0.append (errors)
            self._adc0.append (outputs)
            self._adc0.append (pwms)
            self._adc0.append (setpoints)
            self._adc0.append (output_sats)
            
            self._matlab_dic = {}
            self._matlab_dic ['sample'] = samples
            self._matlab_dic ['data'] = datas
            self._matlab_dic ['kp'] = kps
            self._matlab_dic ['plant'] = plants
            self._matlab_dic ['error'] = errors
            self._matlab_dic ['outputs'] = outputs
            self._matlab_dic ['pwms'] = pwms
            self._matlab_dic ['setpoints'] = setpoints
            self._matlab_dic ['output_sats'] = output_sats
        else:
            print "No samples..."

    #-----
    # save the data retrieved from the logger as .csv or .xls format
    def _save_data (self):
        # open a file dialog to get a file name
        fname = tkFileDialog.asksaveasfilename (**self._file_dlg_opt)
        
        # now... depending on the version of Tkinter (python related) the return is either a string or a
        # tuple or list with one element, so see what it is and make the correct filename
        if ( (type (fname) == types.TupleType) or (type (fname) == types.ListType) ):
            # python version that returns a tuple, returns an empty tuple if cancel is pressed
            if (len (fname) > 0):
                fname = fname [0]
            else:
                fname = None
            
        # avoid an error message if user cancelled the input
        if ( (fname != '') and (fname != None) ):
            # set the initial directory to the directory of the file we opened
            self._file_dlg_opt ['initialdir'] = os.path.dirname (fname)
            
            # the dialog will always provide the file extension, if we type it or not, so we check which extension
            # we have so we know how to save the data
            name, ext = os.path.splitext (fname.lower ())
            if (ext == ".xlsx"):
                # excel
                print "Todo: excel"
            elif (ext == ".csv"):
                # comma separated value; note that the data we keep globally for the graphs are row oriented, so before we can save as
                # csv, which we define as column oriented, we need to transpose the global data
                adc0 = numpy.array (self._adc0).transpose ()
                
                # now we can write
                fptr = open (fname, "w")
                fptr.write ("Sample, Data, Kp, Plant, Error, Output, PWM, Setpoint, output_sat\n")
                for cnt in range (len (self._tb)):
                    line = "%f"%(self._tb [cnt])
                    adc_row = adc0 [cnt]
                    for col in range (len (adc_row)):
                        line += ",%f"%(adc_row [col])
                    line += "\n"
                    fptr.write (line)
                fptr.close ()
            elif (ext == ".mat"):
                # matlab
                scipy.io.savemat (fname, {"sample" : self._tb, "Regulator" : self._matlab_dic})
            elif (ext == ".bin"):
                # raw binary
                print "Todo: raw binary"
            else:
                message  = "Unknown file type: %s\n"%ext
                message += "No data saved"
                response = tkMessageBox.showerror ("Save data", message, parent = self._master)
        
    #-----
    # resize the graph
    def resize (self, width, height):
        self._capture_graph.resize (width, height)
        
    #-----
    # function for periodic processing
    def process (self):
        pass
        
#-----------------------------------------------------------------------------#
#                            Main Starts Here                                 #
#-----------------------------------------------------------------------------#

# no main, part of import of HexaLog_Gui
    
#----------------------------------- End of file -----------------------------#

