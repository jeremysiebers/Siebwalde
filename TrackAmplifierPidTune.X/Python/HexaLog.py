#!/usr/bin/env python
#-----------------------------------------------------------------------------#
#                                                                             #
#                              Python script                                  #
#                                                                             #
#-----------------------------------------------------------------------------#
#
# Author      : M. de Waard
# Description : Python application to get data from the HexaLog logger.
#
# interesting stuff:
# https://pythonprogramming.net/how-to-embed-matplotlib-graph-tkinter-gui/
#
# History
# <date>     : <auth>  <ver>    <comment>
# 2018-08-24 : MWaa    T 0.1.0  Initial version, based on my gtvXsp CARM tracefile analyser
#-----------------------------------------------------------------------------#
#                       Copyright (c) 2018, JeMaTech                          #
#                            All rights reserved                              #
#-----------------------------------------------------------------------------#

#-----------------------------------------------------------------------------#
#                         Documentation String                                #
#-----------------------------------------------------------------------------#

"""
HexaLog.py - HexaLog PC interface

Usage:    

Options:    

"""

#-----------------------------------------------------------------------------#
#                              System imports                                 #
#-----------------------------------------------------------------------------#
import os, os.path, sys, subprocess

print '\nPython version:', sys.version

# we may need to load different application specific modules depending on the platform we're running on
try:
    # this only works on windows...
    print '\nChecking system...'
    MAJOR, MINOR, BUILD, PLATFORM, TEXT = sys.getwindowsversion ()
    print '...windows: major =', MAJOR, 'minor =', MINOR, 'build =', BUILD, 'platform =', PLATFORM, 'text =', TEXT
except:
    # assumption Linux/Unix
    print '...Not windows'
    PLATFORM = None

print '\nLoading modules...'
sys.stdout.flush ()

print '...python system'
import string
import time
import socket
import struct
import types
import serial

print '...matplotlib'
#import pylab
#from matplotlib.widgets import Cursor
#from matplotlib import mlab
#from matplotlib import dates
#import numpy
import matplotlib
matplotlib.use ("TkAgg")

print '...tkinter related'
import Tkinter
import tkFileDialog
import tkSimpleDialog
import tkMessageBox

print '...application specific'
import HexaLog_Widgets as widgets
import HexaLog_TabSheets as tabsheets

print '\nDone'
sys.stdout.flush ()

# optimize interpreter performance
sys.setcheckinterval (5000)

#-----------------------------------------------------------------------------#
#                             Global Variables                                #
#-----------------------------------------------------------------------------#
STATUS_UPDATE_INTERVAL = 1000           # [ms]
DEFAULT_COMPORT        = "COM5"
DEFAULT_BAUDRATE       = 250000         # [baud]
DEFAULT_TIMEOUT        = 1.0            # [s]

VERSION_TYPE           = 'T'            # T prefix is for test; V for final
VERSION_MAJOR          = '0'            # major version
VERSION_MINOR          = '1'            # minor version
VERSION_BUILD          = '0'            # build version
VERSION_STRING         = VERSION_TYPE + ' ' + VERSION_MAJOR + '.' + VERSION_MINOR + '.' + VERSION_BUILD

# controller basic settings
DRIVE_SETPOINT= 450
TOTAL_SAMPLES = 500
OUTPUT_SAT_P = 20            # origineel: output_sat * 1
OUTPUT_SAT_M = -20           # origineel: output_sat * -1
PWM_MAX = 600
PWM_MIN = 200
KP = 10
ACCELERATION = 5

#-----------------------------------------------------------------------------#
#                       Class definition for main GUI                         #
#-----------------------------------------------------------------------------#
class clsHexaLog (Tkinter.Frame):
    #-----
    # some class global data
    GEOMETRY         = "1240x800"       # just an initial dimension of the GUI
    BUTTON_WIDTH     = 13               # for no particular reason the same as gtvX...
    BASE_X_POS       = 5                # when we have just a number of widges (such as jesd-link tab), this is the base X-position
    BASE_Y_POS       = 20               # when we have just a number of widges (such as jesd-link tab), this is the base Y-position
    X_POS_INDENT     = 25               # some widgets may be indented to others, then this is the indented x-position
    DELTA_Y          = 30               # distance in Y between widgets
    CBX_WIDTH        = 325              # width for check boxes
    LBL_WIDTH        = 400              # width for labels
    BTN_CANVAS_WIDTH = 100              # button width on canvas (BUTTON_WIDTH is for frames)
    ENTRY_WIDTH      = 75               # width for entry widgets
    ENTRY_SEP        = 10               # separation between 2 entry fields
    SELECT_WIDTH     = 425              # width for selection boxes
    SETTINGS         = {                # we'll override these from saved settings if applicable
        # generic
        "T_REFRESH"             : {"text" : "Refresh Time [ms]",                        "value" : STATUS_UPDATE_INTERVAL},
        "COM_PORT"              : {"text" : "Serial port",                              "value" : DEFAULT_COMPORT},
        "BAUDRATE"              : {"text" : "Baudrate [bit/s]",                         "value" : DEFAULT_BAUDRATE},
        "COMM_TIMEOUT"          : {"text" : "Communication timeout [s]",                "value" : DEFAULT_TIMEOUT},
        "DRIVE_SETP"            : {"text" : "Drive setpoint",                           "value" : DRIVE_SETPOINT},
        "TOTAL_SAMPLES"         : {"text" : "Total samples",                            "value" : TOTAL_SAMPLES},
        "OUTSAT_P"              : {"text" : "Output saturation P",                      "value" : OUTPUT_SAT_P},
        "OUTSAT_M"              : {"text" : "Output saturation M",                      "value" : OUTPUT_SAT_M},
        "PWM_MAX"               : {"text" : "PWM Max",                                  "value" : PWM_MAX},
        "PWM_MIN"               : {"text" : "PWM Min",                                  "value" : PWM_MIN},
        "KP"                    : {"text" : "Kp",                                       "value" : KP},
        "ACCELERATION"          : {"text" : "Acceleration",                             "value" : ACCELERATION}
    }
    
    #-----
    # Class constructor
    def __init__ (self, master = None):
        # initialise some local stuff and create widgets
        self._master = master
        self._master.geometry (self.GEOMETRY)
        Tkinter.Frame.__init__ (self, master)
        self.pack ()
        
        # create local variables
        self._setup_page    = 0
        self._setup_setname = "default"
        self._freeze        = False
        
        # setup our settings
        defset = widgets.clsSettings (settings = self.SETTINGS).result
        if (defset ["settings"] != None):
            self._settings = defset ["settings"]
        else:
            self._settings = self.SETTINGS
        
        # create the generic widgets
        self._create_widgets ()
        
        # open the serial port
        try:
            # open the com port
            self._ser = serial.Serial (port = self._settings ["COM_PORT"]["value"], baudrate = self._settings ["BAUDRATE"]["value"], timeout = self._settings ["COMM_TIMEOUT"]["value"])
            
            # flush the buffer
            #r = self._ser.read ()
            #while (r != ''):
            #    r = self._ser.read ()
                
            # we should do some handshaking to make sure we are connected to HexaLog
            print "TODO"
        except:
            print "Could not open serial port:", sys.exc_info ()[0]
            print "Make sure correct port is set and not occupied"
            print ""
            self._ser = None                    # so we can do some tests we don't need the serial port for...
        
        # set a handler for the close system button
        self._master.protocol ("WM_DELETE_WINDOW", self._quit)
        
        # set some status info
        if (self._ser == None):
            self._sbar.set ("%s", "HexaLog connection failed")
        else:
            self._sbar.set ("%s", "Connected to HexaLog")
        
        # create all the tab sheets
        # Note: order in which tabs are "mentioned" here also defines the order in which they are created in/added to
        #       the notebook. This in turn affects the numbering in the _periodic_update function!
        #self._status_tab = tabsheets.clsStatusTabSheet (self)
        self._graph_tab  = tabsheets.clsGraphTabSheet (self)
        self._config_tab = tabsheets.clsConfigTabSheet (self)
        
        # set an OnChange handler for the notebook: that handler is called each time a different page (tab) is selected
        self._nb.set_on_change_handler (self._tab_page_change)
        
        # activate the first tab
        self._nb.set_active_screen (0)
        
        # perform a first redraw of the screen (to prevent some widgets from staying black during lengthy initialization)
        self._master.update ()

        # when all is done we can bind a resize event to the data frame which holds all interactive stuff
        self._data_frame.bind ("<Configure>", self._data_frame_resize, "+")

        # bind the scroll wheel (<4>, <5> is unix/linux, <MouseWheel> is windows
        self._master.bind ("<4>", self._mouse_wheel)
        self._master.bind ("<5>", self._mouse_wheel)
        self._master.bind ("<MouseWheel>", self._mouse_wheel)
        
        # we're up and running, install the handler for the periodic stuff
        self._cycle_job  = self.after (self._settings ["T_REFRESH"]["value"], self._periodic_update)
        self._auto_cycle = True
        
    #-----
    # create all the widgets
    def _create_widgets (self):
        # create a left pane for some buttons
        self._button_frame = Tkinter.Frame (self._master, bd = 2, relief = Tkinter.GROOVE)
        self._button_frame.pack (side = "left", anchor = "w", expand = 0, fill = Tkinter.Y)
        
        # create a right pane to hold the data
        self._main_frame = Tkinter.Frame (self._master, bd = 2, relief = Tkinter.GROOVE)
        self._main_frame.pack (side = "left", anchor = "w", expand = 1, fill = Tkinter.BOTH)
        
        # really at the bottom we put a statusbar
        self._sbar = widgets.clsStatusBar (self._main_frame, self._master)
        self._sbar.pack (side = "bottom", fill = Tkinter.X)
        
        # and in the middle we put a frame where we put our tabbed notebook on (the "notebooks" will contain the data we're interested in)
        self._data_frame = Tkinter.Frame (self._main_frame, bd = 2, relief = Tkinter.GROOVE)
        self._data_frame.pack (side = "top", anchor = "w", expand = 1, fill = Tkinter.BOTH)
        
        # create the notebook itself
        self._nb = widgets.clsNotebook (self._data_frame, Tkinter.TOP, frame_width = 2)
        
        # Setup: modify application settings
        self._settings_button = Tkinter.Button (self._button_frame, width = self.BUTTON_WIDTH)
        self._settings_button ["text"]    = "Setup"
        self._settings_button ["fg"]      = "black"
        self._settings_button ["command"] = self._edit_settings
        self._settings_button.pack (side = "top", anchor = "w")
        
        # freeze: freeze/unfreeze the updates
        self._freeze_button = Tkinter.Button (self._button_frame, width = self.BUTTON_WIDTH)
        self._freeze_button ["text"]    = "Freeze"
        self._freeze_button ["fg"]      = "black"
        self._freeze_button ["command"] = self._toggle_freeze
        self._freeze_button.pack (side = "top", anchor = "w")
        
        # about: show version information
        self._about_button = Tkinter.Button (self._button_frame, width = self.BUTTON_WIDTH)
        self._about_button ["text"]    = "About"
        self._about_button ["fg"]      = "black"
        self._about_button ["command"] = self._about
        self._about_button.pack (side = "top", anchor = "w")
        
        # quit: end application
        self._quit_button = Tkinter.Button (self._button_frame, width = self.BUTTON_WIDTH)
        self._quit_button ["text"]    = "Quit"
        self._quit_button ["fg"]      = "black"
        self._quit_button ["command"] = self._quit
        self._quit_button.pack (side = "top", anchor = "w")
        
    #-----
    # this function is called when a new tab is selected on the tabbed main page; the ndx parameter represents the index
    # of the page that is being selected
    def _tab_page_change (self, ndx):
        pass
        
    #-----
    # data frame has been resized
    # This function is effectively only needed for tabs that deploy 'embedded' matplotlib plots as these need
    # special handling to take care of the matplotlib toolbar.
    def _data_frame_resize (self, event = None):
        # resize the tabs with embedded matplotlib graphs
        self._graph_tab.resize (event.width, event.height)
        self._config_tab.resize (event.width, event.height)
        
    #-----
    # handle mouse wheel scrolling; this takes care of all main tabs that are scrollable
    # event.num will be "valid" on unix/linux systems, event.delta will be "valid" on windows systems
    def _mouse_wheel (self, event):
        #if ( (event.num == 5) or (event.delta < 0) ):
        #    self._regmap_tab.yview ("scroll", 1, "units")
        #    self._jesd_link_state_tab.yview ("scroll", 1, "units")
        #if ( (event.num == 4) or (event.delta > 0) ):
        #    self._regmap_tab.yview ("scroll", -1, "units")
        #    self._jesd_link_state_tab.yview ("scroll", -1, "units")
        pass
            
    #-----
    # send a request
    # The NIOS on the FPGA needs to receive and interpret the request but does NOT have a buffered UART. This means some
    # space must be inbetween each character to make sure it is received properly.
    def send_request (self, req):
        if (self._ser != None):
            self._cancel_periodic_update ()
            
            # walk the list
            #rqlen = len (req)
            #if (rqlen > 1):
            #    for cnt in range (rqlen):
            #        self._ser.write (req [cnt])
            #        if (cnt < (rqlen - 1)):
            #            time.sleep (0.06)               # we need this otherwise we overrun the NIOS...
            #else:
            #    self._ser.write (req [0])
            self._ser.write (req)
            
            self._restart_periodic_update ()
        else:
            # com port is not open...
            print "Com port not open, command request ignored"
            
    #-----
    # receive data
    def receive_reply (self, num = 0):
        # only if the serial port is open
        if (self._ser != None):
            self._cancel_periodic_update ()
            
            if (num > 0):
                # do a basic read with the expected reply size (and a timeout...)
                repl = self._ser.read (num)
            else:
                # do a basic read and let it timeout
                repl = self._ser.read ()
                
            self._restart_periodic_update ()
        else:
            # we have no way of knowing how much data was requested, so we always reply with an empty string
            print "Com port not open, replying with empty string"
            repl = ''
            
        return repl
            
    #-----
    # convinience function for other clients to retrieve the status of the logger
    def _get_logger_status (self):
        return self._status_tab._request_status ()
        
    #-----
    # cancel periodic updates if needed
    def _cancel_periodic_update (self):
        self._auto_cycle = False
            
    #-----
    # (re-)start periodic update
    def _restart_periodic_update (self):
        self._auto_cycle = True
            
    #-----
    # handler for periodic updates
    def _periodic_update (self):
        # only if refresh is on
        if (self._auto_cycle) and (not (self._freeze)):
            # get the active tab
            tab = self._nb.get_active_screen ()
            
            # call process function of active tab
            if (tab == 0):
                self._graph_tab.process ()
            elif (tab == 1):
                self._config_tab.process ()
            else:
                pass
        
        # restart the timer to call this function again
        self._cycle_job = self.after (self._settings ["T_REFRESH"]["value"], self._periodic_update)
        
    #-----
    # edit system settings
    def _edit_settings (self):
        # create the settings dialog and execute it
        new_settings = widgets.clsSettings (self._master, self._settings, start_page = self._setup_page, current_set = self._setup_setname, top_obj = self).result
        if (new_settings != None):
            # if cancel was pressed on the dialog, the result would be None
            self._settings      = new_settings ["settings"]
            self._setup_page    = new_settings ["last_screen"]
            self._setup_setname = new_settings ["setname"]
            
        print "ToDo: on change, check what variables have changed, if needed to update (eg comport)"
        
    #-----
    # freeze or unfreeze the update
    # ToDo: could replace by calls to cancel and restart periodic update
    def _toggle_freeze (self):
        if (self._freeze):
            self._freeze = False
            self._freeze_button ["text"] = "Freeze"
        else:
            self._freeze = True
            self._freeze_button ["text"] = "UnFreeze"
            
    #-----
    # show version information and copyrights
    def _about (self):
        line = "The Regulator Gui\n"
        line = line + "Version: " + VERSION_STRING + "\n\n"
        line = line + "(c) 2018 JSIF\n\n"
        tkMessageBox.showinfo ("About Regulator Gui", line)
    
    #-----
    # quit function
    def _quit (self):
        # cancel the periodic update
        self._cancel_periodic_update ()
        
        # the refresh time in the settings is in miliseconds; we sleep this time after cancelling the periodic uptate
        # to avoid that the update is called while the application is being destroyed; this is not a real problem, but
        # it is very annoying to be confronted with an exception when the application exits
        time.sleep (0.5)
        
        # close the serial port
        try:
            self._ser.close ()
        except:
            # no use bitching about it if this fails during close
            pass
        
        # now just leave...
        self.master.quit ()
        self.master.destroy ()
        
#-----------------------------------------------------------------------------#
#                            Support Functions                                #
#-----------------------------------------------------------------------------#

#-----------------------------------------------------------------------------#
#                  simple display of how to use this script                   #
#-----------------------------------------------------------------------------#


#-----------------------------------------------------------------------------#
#                            Main Starts Here                                 #
#-----------------------------------------------------------------------------#
if (__name__ == "__main__"):
    # create root widget and start application
    root = Tkinter.Tk ()
    if (PLATFORM == None):
        # on unix we need to set another font to make it all fit
        root.option_add ("*Font", "roman 10")
    hexalog = clsHexaLog (root)
    
    # Nice addition, copied from gtvX, keep for later if we want to make it look a little better...
    # need a better icon: this one is a mess...
    #if (PLATFORM != None):
    #    # on windows we can change the titlebar bitmap...
    #    hexalog.master.wm_iconbitmap (os.path.join (widgets.ICONDIR, 'some_icon.ico'))
    
    hexalog.master.title ("The HexaLog Gui " + VERSION_STRING)
    hexalog.mainloop ()
    try:
        hexalog.destroy ()
    except:
        # no need to show a "can't destroy exception" if gui already destroyed.....
        pass
    
#----------------------------------- End of file -----------------------------#
