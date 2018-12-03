#!/usr/bin/env python
#-----------------------------------------------------------------------------#
#                                                                             #
#                              Python script                                  #
#                                                                             #
#-----------------------------------------------------------------------------#
#
# Author      : M. de Waard
# Description : Widgets for HexaLog Gui application
#
# History
# yyyy-mm-dd : <author> <comment>
# 2018-08-25   MWAA     Creation, based on the gtvXsp....
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
import os, os.path, sys
import copy
import re
import time
import types
import tkinter
import tkinter.filedialog
import tkinter.simpledialog
import tkinter.messagebox

import numpy

from matplotlib.widgets import Cursor
from matplotlib.backends.backend_tkagg import FigureCanvasTkAgg, NavigationToolbar2Tk
from matplotlib.figure import Figure

try:
    # this only works on windows...
    MAJOR, MINOR, BUILD, PLATFORM, TEXT = sys.getwindowsversion ()
except:
    PLATFORM = None

#-----------------------------------------------------------------------------#
#                             Global Variables                                #
#-----------------------------------------------------------------------------#

ICONDIR          = "Icons"
SETTING_FILENAME = "./HexaLog.inf"

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
    raise RuntimeError( "can't find icon directory (%r)" % (ICONDIR,))

#-----------------------------------------------------------------------------#
#                           Status bar base class                             #
#-----------------------------------------------------------------------------#
class clsStatusBar (tkinter.Frame):
    #-----
    # class constructor
    # The master is the widget the statusbar widget is put into. The main_frame parameter
    # is the window of which the 'geometry' function will be called after a move of the
    # sizegrip.
    # The pane_sizes are relative sizes
    def __init__ (self, master, main_frame, nr_panes = 1, pane_sizes = []):
        tkinter.Frame.__init__ (self, master)
        
        # first we do a check: if nr_panes == 1 then pane_sizes must be an empty list
        # if nr_panes > 1, then pane_sizes must have a size for all panes except the
        # last, which will always get the remainder
        if (nr_panes <= 0):
            raise RuntimeError( "Number of panes must be >= 1: (%d)" % (nr_panes,))
        elif (len (pane_sizes) != (nr_panes - 1)):
            raise RuntimeError( "Number of panes does not match length of pane-size list: (%d vs %d must be: %d vs %d)" % (nr_panes, len (pane_sizes), nr_panes, nr_panes - 1,))
        else:
            # OK...
            pass
        
        # keep input parameters
        self._master     = master
        self._main_fr    = main_frame
        self._nr_panes   = copy.copy (nr_panes)    # don't remove copy.copy, it'll surprise ya
        self._pane_sizes = copy.copy (pane_sizes)  # don't say I didn't warn you...
        self._labels     = []
        
        # now we calculate the total of the given pane-sizes and add one more element to make it a total of 100%
        sum = 0.0
        for cnt in range (len (self._pane_sizes)):
            sum = sum + self._pane_sizes [cnt]
        if (sum >= 1.0):
            raise RuntimeError( "Sum of pane sizes must be less than 1.0: (%f)" % (sum,))
        self._pane_sizes.append (1.0 - sum)
        
        # put in a sizegrip...
        bmp_file   = os.path.join (ICONDIR, 'sizegrip.gif')
        self._img  = tkinter.PhotoImage (file = bmp_file)
        self._grip = tkinter.Label (self, image = self._img)
        self._grip.pack (side = "right", fill = "y")
        
        # a progressbar...
        self._pbar = clsMeter (self, width = 150, height = 16)
        self._pbar.pack (side = "right", fill = tkinter.Y)
        
        # put in an extra frame to put the labels on
        self._labelframe = tkinter.Frame (self, bd = 0)
        self._labelframe.pack (fill = tkinter.BOTH, side = "left", expand = 1)
        
        # create panes and labels...
        xpos = 0.0
        ypos = 0.0
        for cnt in range (self._nr_panes):
            self._labels.append (tkinter.Label (self._labelframe, bd = 1, relief = tkinter.SUNKEN, anchor = tkinter.W))
            self._labels [cnt].place (relx = xpos, rely = ypos, relwidth = self._pane_sizes [cnt], relheight = 1.0, anchor = 'nw')
            xpos = xpos + self._pane_sizes [cnt]
        
        # bind grip buttons
        self._grip.bind ("<ButtonPress-1>", self._startmove)
        self._grip.bind ("<ButtonRelease-1>", self._stopmove)
        self._grip.bind ("<B1-Motion>", self._onmotion)
        self._grip.bind ("<Enter>", self._enter_grip)
        self._grip.bind ("<Leave>", self._leave_grip)
        
    #-----
    # LMB down on sizegrip
    def _startmove (self, event):
        # keep current coordinates
        self._x = event.x
        self._y = event.y
    
    #-----
    # LMB up on sizegrip
    def _stopmove (self, event):
        # done...
        self._x = None
        self._y = None
    
    #-----
    # Mouse moves with LMB down on sizegrip
    def _onmotion (self, event):
        deltax = event.x - self._x
        deltay = event.y - self._y
        width  = self._main_fr.winfo_width () + deltax
        height = self._main_fr.winfo_height () + deltay
        if ( (width > 0) and (height > 0) ):
            self._main_fr.geometry ("%sx%s" % (width, height))
            self._main_fr.update_idletasks ()
            
    #-----
    # mouse enters grip, set resize cursor
    def _enter_grip (self, event):
        self._main_fr.config (cursor = "sizing")
        
    #-----
    # mouse leaves grip, set normal cursor
    def _leave_grip (self, event):
        self._main_fr.config (cursor = '')
    
    #-----
    # set a particular text in the status bar
    # This particular version only sets the first pane, it is there for backwards compatibility
    # with existing calls to statusbar.set; better use 'set_text'
    def set (self, format, *args):
        self._labels [0].config (text = format % args)
        self.update_idletasks ()
        
    #-----
    # set the value of a label in a pane
    def set_text (self, text, pane_index):
        if ( (pane_index >= 0) and (pane_index < len (self._labels)) ):
            # set the label for the given pane
            try:
                self._labels [pane_index].config (text = text)
                self.update_idletasks ()
                self.update ()
            except:
                # issue: when running a macro, the macro may not return because it implements a show () call that
                # does not return; then when we quit the application, the show () aborts, leading to the final
                # set_busy (False) call, which would want to do an update on the label, but in the meantime the
                # label also doesn't exist anymore...
                # so, we ignore these error, but still, we must find a solution in the GUI...
                pass
        else:
            # we ignore anything with an incorrect index
            pass
        
    #-----
    # set a progress percentage
    def set_progress (self, value, text = None):
        self._pbar.set (value, text)
        self.update_idletasks ()
        self.update ()
    
    #-----
    # clear the content of the statusbar label
    # pane-index is added, defaults to 0 for backward compatibility
    def clear (self, pane_index = 0):
        self._labels [pane_index].config (text = '')
        self.update_idletasks ()
        self.update ()
        
#-----------------------------------------------------------------------------#
#                         Progress meter base class                           #
#-----------------------------------------------------------------------------#
class clsMeter (tkinter.Frame):
    def __init__ (self, master, width = 300, height = 20, bg = 'white', fillcolor = 'darkgrey',\
                  value = 0.0, text = None, font = None, textcolor = 'black', *args, **kw):
                  
        tkinter.Frame.__init__ (self, master, bg = bg, width = width, height = height, *args, **kw)
        self._value = value
 
        self._canv = tkinter.Canvas (self, bg = self ['bg'], width = self ['width'], height = self ['height'],\
                                     highlightthickness = 0, relief = 'flat', bd = 0)
        self._canv.pack (fill = 'both', expand = 1)
        self._rect = self._canv.create_rectangle (0, 0, 0, self._canv.winfo_reqheight(), fill = fillcolor,\
                                                  width = 0)
        self._text = self._canv.create_text (self._canv.winfo_reqwidth ()/2, self._canv.winfo_reqheight ()/2,\
                                             text = '', fill = textcolor)
        if (font):
            self._canv.itemconfigure (self._text, font = font)
 
        self.set (value, text)
        self.bind ('<Configure>', self._update_coords)
 
    def _update_coords (self, event):
        '''Updates the position of the text and rectangle inside the canvas when the size of
        the widget gets changed.'''
        # looks like we have to call update_idletasks() twice to make sure
        # to get the results we expect
        self._canv.update_idletasks ()
        self._canv.coords (self._text, self._canv.winfo_width ()/2, self._canv.winfo_height ()/2)
        self._canv.coords (self._rect, 0, 0, self._canv.winfo_width ()*self._value, self._canv.winfo_height ())
        self._canv.update_idletasks ()
 
    def get (self):
        return self._value, self._canv.itemcget (self._text, 'text')
 
    def set (self, value = 0.0, text = None):
        #make the value failsafe:
        if (value < 0.0):
            value = 0.0
        elif (value > 1.0):
            value = 1.0
        self._value = value
        if (text == None):
            #if no text is specified use the default percentage string:
            text = str (int (round(100 * value))) + ' %'
        self._canv.coords (self._rect, 0, 0, self._canv.winfo_width () * value, self._canv.winfo_height ())
        self._canv.itemconfigure (self._text, text=text)
        self._canv.update_idletasks ()
        
#-----------------------------------------------------------------------------#
#                        Tabbed notebook base class                           #
#-----------------------------------------------------------------------------#
class clsNotebook (object):
    GO_BUTTON_HEIGHT = 16                # taken from the winfo_reqheight function of a button...
    GO_BUTTON_WIDTH  = GO_BUTTON_HEIGHT  # square buttons...
    
    #-----
    # class constructor
    def __init__ (self, master, side = tkinter.LEFT, button_bar = True, frame_width = 2, follow_resize = True, fontdef = None, add_close = False):
        self._active_fr      = None
        self._choice         = tkinter.IntVar (0)
        self._button_list    = []
        self._cbtn_list      = []
        self._frame_id_list  = []
        self._frame_list     = []
        self._on_change_list = []
        self._on_close_list  = []
        self._button_bar     = button_bar
        self._go_lr_visible  = False
        self._is_most_left   = 0
        self._frame_counter  = 0
        self._follow_resize  = follow_resize
        self._fontdef        = fontdef
        self._add_close      = add_close
        
        # determine direction
        if (side in (tkinter.TOP, tkinter.BOTTOM)):
            self._side = tkinter.LEFT
        else:
            self._side = tkinter.TOP
            
        # only place a button bar if requested
        if (self._button_bar):
            # set the button bar and define a resize event handler
            self._rb_fr = tkinter.Frame (master, borderwidth = frame_width, relief = tkinter.RIDGE)
            self._rb_fr.pack (side = side, fill = tkinter.BOTH)
            
            # can we follow frame resize by adding a go-left and a go-right button?
            if (self._follow_resize):
                # yes, set the event handler (after the default handler)
                self._rb_fr.bind ("<Configure>", self._frame_resize, "+")
                
                # we also need go left and go right button images
                self._go_left_photo  = tkinter.PhotoImage (file = ICONDIR + '/go_left.gif')
                self._go_right_photo = tkinter.PhotoImage (file = ICONDIR + '/go_right.gif')
                self._close_photo    = tkinter.PhotoImage (file = ICONDIR + '/close.gif')
                
                # add the two buttons to begin with, but don't show them yet (dimensions hardcoded from original button height)
                self._go_left_button  = tkinter.Button (self._rb_fr, bd = 1, pady = 0, image = self._go_left_photo, height = self.GO_BUTTON_HEIGHT, width = self.GO_BUTTON_WIDTH, command = self._go_left)
                self._go_right_button = tkinter.Button (self._rb_fr, bd = 1, pady = 0, image = self._go_right_photo, height = self.GO_BUTTON_HEIGHT, width = self.GO_BUTTON_WIDTH, command = self._go_right)
        
        # setup the main view frame
        self._screen_fr = tkinter.Frame (master, borderwidth = frame_width, relief = tkinter.RIDGE)
        self._screen_fr.pack (fill = tkinter.BOTH, expand = 1)
        
    #-----
    # return the created screen frame when the class instance is called
    def __call__ (self):
        return self._screen_fr
        
    #-----
    # find an id in the frame_id_list and convert it to a matching index
    def _id_to_index (self, id):
        # for each new added tab we increment a unique frame counter number, which is kept in the frame_id_list
        # we search the list until we find the given id
        index = -1
        for cnt in range (len (self._frame_id_list)):
            if (self._frame_id_list [cnt] == id):
                index = cnt
                break
                
        # index will remain -1 if not found
        return index
        
    #-----
    # frame resize: check dimensions and button sizes, if buttons overflow frame add arrows
    # this will only be called if we allowed resize-follow
    def _frame_resize (self, event = None):
        # check the total width of all buttons combined
        self._frame_width = event.width
        button_width      = 0
        for cnt in range (len (self._button_list)):
            button_width += self._button_list [cnt].winfo_reqwidth ()
            
        # check total width against frame width
        if (button_width > self._frame_width):
            # there are more buttons than the frame can handle, show the go_left and go_right buttons
            if (not (self._go_lr_visible)):
                # only do this if they're not already visible to avoid a new call due to a possible additional resize
                # first forget all existing buttons
                for cnt in range (len (self._button_list)):
                    self._button_list [cnt].pack_forget ()
                
                # then put in the two go-buttons
                self._go_left_button.pack (side = self._side, fill = tkinter.Y)
                self._go_right_button.pack (side = self._side, fill = tkinter.Y)
                
                # then place the others again
                for cnt in range (len (self._button_list)):
                    self._button_list [cnt].pack (fill = tkinter.BOTH, side = self._side)
                
                # and indicate with have the go buttons
                self._go_lr_visible = True
        else:
            # all buttons fit, if needed hide the go_left and go_right buttons
            self._go_left_button.pack_forget ()
            self._go_right_button.pack_forget ()
            
            # now, we may have 'place_forget' buttons due to 'go-right', so first forget them all
            for cnt in range (len (self._button_list)):
                self._button_list [cnt].pack_forget ()
            
            # then place 'm back again
            for cnt in range (len (self._button_list)):
                self._button_list [cnt].pack (fill = tkinter.BOTH, side = self._side)
            
            # and we're done
            self._is_most_left  = 0
            self._go_lr_visible = False
            
    #-----
    # shift the buttons to the left
    # this will only be called if we allowed resize-follow
    def _go_left (self, event = None):
        # only if the most left button is not already visible
        if (self._is_most_left > 0):
            # forget all of 'm
            for cnt in range (len (self._button_list)):
                self._button_list [cnt].pack_forget ()
                
            # decrement our counter
            self._is_most_left -= 1
            
            # make the required buttons visible again; don't need to be difficult with this, buttons
            # can be packed but if the frame is too small they won't be visible and neither will that
            # trigger a resize, so, just show all remaining buttons
            for cnt in range (self._is_most_left, len (self._button_list)):
                self._button_list [cnt].pack (fill = tkinter.BOTH, side = self._side)
            
    #-----
    # shift the buttons to the right
    # this will only be called if we allowed resize-follow
    def _go_right (self, event = None):
        # see what the current frame width is compared to the width of the buttons currently visible
        button_width = 2 * self.GO_BUTTON_WIDTH        # the two 'go' buttons...
        for cnt in range (self._is_most_left, len (self._button_list)):
            button_width += self._button_list [cnt].winfo_reqwidth ()
        
        # see if we need to drop one
        if (self._frame_width < button_width):
            self._button_list [self._is_most_left].pack_forget ()
            self._is_most_left += 1
        
    #-----
    # close handler for active screen --> must be passed to controlling entity for other cleanup!
    def _close_handler (self, id):
        # contert the id into an index
        index = self._id_to_index (id)
        
        # if we didn't find it, that would be strange, but don't try to delete anything then
        if (index == -1):
            return
                
        # call on_change handlers if there are any
        for cnt in range (len (self._on_close_list)):
            # note that we've setup the handler to return a fixed index, NOT the active tab! (that may be different!)
            self._on_close_list [cnt](index)
        
    #-----
    # display a particular frame
    def _display (self, fr):
        # forget the current frame and set a new one
        self._active_fr.forget ()
        fr.pack (fill = tkinter.BOTH, expand = 1)
        self._active_fr = fr
        
        # call on_change handlers if there are any
        for cnt in range (len (self._on_change_list)):
            self._on_change_list [cnt](self._id_to_index (self._choice.get ()))
            
    #-----
    # add a new tab
    def add_screen (self, fr, title, button_loc = None):
        # only do buttons if requested
        if (self._button_bar):
            if (self._fontdef == None):
                # use default system font
                b = tkinter.Radiobutton (self._rb_fr, text = title, indicatoron = 0, variable = self._choice, \
                                         value = self._frame_counter, command = lambda: self._display (fr))
            else:
                # non-default font; only in this situation we could also encounter a close button...
                if (self._add_close):
                    # in order to get the close button on it's location we use a dirty trick: add an image on the expected close
                    # button location to get the space needed for the close button
                    b = tkinter.Radiobutton (self._rb_fr, bd = 1, pady = 0, padx = 2, text = title, indicatoron = 0, variable = self._choice, \
                                             value = self._frame_counter, command = lambda: self._display (fr), font = self._fontdef, \
                                             image = self._close_photo, compound = tkinter.RIGHT)
                else:
                    # no close button
                    b = tkinter.Radiobutton (self._rb_fr, bd = 1, pady = 0, text = title, indicatoron = 0, variable = self._choice, \
                                             value = self._frame_counter, command = lambda: self._display (fr), font = self._fontdef)
            if (button_loc == None):
                # no specific button location, use the pack manger
                b.pack (fill = tkinter.BOTH, side = self._side)
            else:
                # we have a button location, use the gridmanager instead
                r, c = map (int, button_loc.split ('.'))
                b.grid (row = r, column = c)
            self._button_list.append (b)
            
            # see if we need to add a close button
            if (self._add_close):
                ndx = self._frame_counter
                bc  = tkinter.Button (self._rb_fr, bd = 0, image = self._close_photo, command = lambda: self._close_handler (ndx))
                bc.place (in_ = b, relx = 1.0, rely = 0.5, anchor = 'e')
                self._cbtn_list.append (bc)
                
            # we did add a button, so in any case we add the button to the frame id list and increment the frame counter
            self._frame_id_list.append (self._frame_counter)
            self._frame_counter += 1
        
        # add the requested frame
        self._frame_list.append (fr)
        if (not (self._active_fr)):
            fr.pack (fill = tkinter.BOTH, expand = 1)
            self._active_fr = fr
        
        # check if everything still fits
        if (self._follow_resize):
            # trigger a resize event to get the current frame dimensions known and add go-buttons if needed
            ev = tkinter.Event ()
            ev.width = self._rb_fr.winfo_width ()
            self._frame_resize (ev)
        
    #-----
    # lock_tabs: locks the notebook at the current tab, no other tabs can be selected
    def lock_tabs (self):
        # walk through the complete list of radiobuttons (that select each tab) and set the state to DISABLED
        for button in self._button_list:
            button.configure (state = tkinter.DISABLED)
            
    #-----
    # unlock_tabs: return the tab state to normal again
    def unlock_tabs (self):
        # walk through the complete list of radiobuttons (that select each tab) and set the state to DISABLED
        for button in self._button_list:
            button.configure (state = tkinter.NORMAL)
            
    #-----
    # set an active screen
    def set_active_screen (self, index):
        # buttons only if selected
        if (self._button_bar):
            self._button_list [index].select ()
        self._display (self._frame_list [index])
        
    #-----
    # return the currently active screen index
    def get_active_screen (self):
        return self._id_to_index (self._choice.get ())
        
    #-----
    # return the number of tabs present
    def get_num_tabs (self):
        return (len (self._frame_list))
        
    #-----
    # remove a tab; content of tab must already have been discarded or saved!
    def remove_tab (self, index):
        # index starts from 0
        if ( (index < 0) or (index >= len (self._frame_list)) ):
            return
            
        # still here? index is correct if frame is active frame, set next frame or 0 if no more
        # choice.get returns an id, we need to convert that to an index and see if the given index matches it
        index_from_id = self._id_to_index (self._choice.get ())
        if (index == index_from_id):
            # given index is active screen, see if it's the last screen
            if (index == (len (self._frame_list) - 1)):
                # active screen is selected to be deleted AND is the index of the last screen in the list --> set active 0
                # but NOT if the active screen already is 0: all this put together means that:
                # - active is screen 0
                # - the active screen is the last available screen
                # - it is about to be deleted, so no need to activate it
                # And additionally, since it is the last screen to be deleted, it's content have already been cleaned up,
                # so the resulting on_change call could also lead to an issue in the client code...
                if (index_from_id > 0):
                    self.set_active_screen (0)
            else:
                # not the last screen, so set active +1
                self.set_active_screen (index + 1)
                
        # make it disappear...
        self._cbtn_list [index].place_forget ()
        self._button_list [index].pack_forget ()
        self._frame_list [index].pack_forget ()
                
        # now, delete the frame itself and all associated buttons
        del (self._cbtn_list [index])
        del (self._button_list [index])
        del (self._frame_list [index])
        del (self._frame_id_list [index])
        
    #-----
    # set an 'on_change' handler
    def set_on_change_handler (self, handler):
        self._on_change_list.append (handler)
        
    #-----
    # set an 'on_close' handler
    def set_on_close_handler (self, handler):
        self._on_close_list.append (handler)
        
    #-----
    # change the text on a button
    def set_button_text (self, index, text):
        self._button_list [index].config (text = text)
        
#-----------------------------------------------------------------------------#
#                             Capture Graph class                             #
#-----------------------------------------------------------------------------#
class clsCaptureGraph (tkinter.Frame):
    #-----
    # class constructor
    def __init__ (self, parent):
        tkinter.Frame.__init__ (self, parent)
        
        # copy parameters to locally kept variables
        self._parent = parent
        
        # Setup some initial arrays, format doesn't really matter
        self._adc_0_data = numpy.array ([])
        self._tb         = numpy.array ([])
        self._legend     = ['Data', 'Kp', 'Plant', 'Error', 'Output', 'PWM', 'Setpoint', 'output_sat']
        
        # instantiate a figure and 2 subplots
        self._fig   = Figure ()
        self._adc_0 = self._fig.add_subplot (111)                              # 1 col, 1 row, top
        self._adc_0.set_title ("Regulator", fontdict = {"fontsize" : 10})
        
        # plot initial data; at this point in time it's empty data, but we need it to supply legend data
        self._adc_0.plot (self._tb, self._adc_0_data, label = "Regulator")
        
        # create a canvas for the figure and the toolbar
        # note: in contrast to gtvX, where the figures have their own window, this figure is drawn on a canvas provided
        # by the main application!
        self._canvas = FigureCanvasTkAgg (self._fig, self)
        self._canvas.show ()
        self._canvas.get_tk_widget ().grid (row = 0, sticky = tkinter.NSEW)
        
        # show the matlab toolbar
        #self._toolbar = NavigationToolbar2Tk (self._canvas, self)
        #self._toolbar.grid (row = 1, sticky = tkinter.W)
        #self._toolbar.update ()
        
        # format the graph
        self._format_graph ()
        
    #-----
    # format the graph axes and font styles
    def _format_graph (self):
        # set small font and cursor on all axes
        for tick in self._adc_0.xaxis.get_major_ticks ():
            tick.label1.set_fontsize (8)
        for tick in self._adc_0.yaxis.get_major_ticks ():
            tick.label1.set_fontsize (8)
        self._adc_0.grid (True)
        self._pat_cursor = Cursor (self._adc_0, useblit = True, color = 'red', linewidth = 1)
        
        # set subplot titles
        self._adc_0.set_title ("Regulator", fontdict = {"fontsize" : 10})
        
        # set labels on all axes
        # MWAA: volt or amp needs to come from the logger settings
        self._adc_0.set_ylabel ('Sensor [V]', fontsize = 'x-small')
        self._adc_0.set_xlabel ('Sample #', fontsize = 'x-small')
        
        # set all legends
        self._adc_0_legend = self._adc_0.legend (loc = 'upper right', borderpad = 0.2, labelspacing = 0)
        if (self._adc_0_legend != None):
            for label in self._adc_0_legend.get_texts ():
                label.set_fontsize ('x-small')
        
        # set autoscaling and relimiting
        self._adc_0.set_autoscaley_on (True)
        
    #-----
    # resize
    def resize (self, width, height):
        # set the canvas size; note that the '-...' in height is needed to make sure the toolbar stays
        # visible; we need to find something better for this hardcoded number...
        # the '-20' in width is to keep a decent frame around the figure...
        self._canvas.get_tk_widget ().config (width = width - 20, height = height - 115)
        
    #-----
    # clear the current sets of data, re-initiate the pattern and capture data and calculate
    # where the capture overlaps the pattern and show the difference of the two
    def update_capture (self, samples, datas, kps, plants, errors, outputs, pwms, setpoints, output_sats):
        # create local timebase from sample numbers
        self._tb = numpy.array (samples)
        
        # create data for graph
        self._adc_0_data = []
        self._adc_0_data.append (datas)
        self._adc_0_data.append (kps)
        self._adc_0_data.append (plants)
        self._adc_0_data.append (errors)
        self._adc_0_data.append (outputs)
        self._adc_0_data.append (pwms)
        self._adc_0_data.append (setpoints)
        self._adc_0_data.append (output_sats)
        self._adc_0_data = numpy.array (self._adc_0_data)
        
        # sowww....
        # using 'axes.lines' doesn't work as you need to set all lines and know beforehand how many there are, this means during __init__
        # orrr... we could chose to delete them entirely if we get a different set of data or visibility; we choose this option, even though
        # it means a little bit more work to recreate the graph, legend, grid etc
        self._adc_0.cla ()
        for cnt in range (len (self._adc_0_data)):
            self._adc_0.plot (self._tb, self._adc_0_data [cnt], label = self._legend [cnt])
        
        # adapt limits
        if (len (self._adc_0_data) > 0):
            self._adc_0.set_ylim (numpy.amin (self._adc_0_data) - 2, numpy.amax (self._adc_0_data) + 2)
        self._adc_0.relim ()
        self._adc_0.autoscale_view ()
        
        # redraw the graph
        self._format_graph ()
        self._fig.canvas.flush_events ()
        self._fig.canvas.draw ()
        
#-----------------------------------------------------------------------------#
#                              ComboBox form                                  #
#-----------------------------------------------------------------------------#
class clsComboBox (tkinter.Frame):
    def __init__ (self, parent, DropListSize = 5):
        # Base Frame
        tkinter.Frame.__init__ (self, parent, cnf = {'relief' : 'sunken', 'bd' : 2, 'bg' : 'white', 'class'  : 'ComboBox'})
        self.columnconfigure (0, weight = 1)

        # Instance Variables
        self.ListValues    = []
        self.DropListSize  = DropListSize
        self.ChangeClients = []
        
        # Entry Box
        self.CurrentValue = tkinter.StringVar (self)
        self.entry = tkinter.Entry(self, relief = 'flat', bd = 0, textvariable = self.CurrentValue)
        self.entry.grid (row = 0, column = 0, sticky = 'sew')
        self.CurrentValue.trace ('w', self.HandleChange)

        # Button
        fullname        = os.path.join (ICONDIR, 'arrow.gif')
        self.enabled    = tkinter.PhotoImage (file = fullname)
        fullname        = os.path.join (ICONDIR, 'arrowDis.gif')
        self.disabled   = tkinter.PhotoImage (file = fullname)
        self.DBenabled  = False
        self.DropButton = tkinter.Label (self, cnf = {'relief' : 'raised', 'bd' : 2, 'width'  : 11, 'height' : 14, 'image'  : self.disabled})
        self.DropButton.bind ('<ButtonRelease-1>', self.DropClick)
        self.DropButton.grid (row = 0, column = 1, sticky = 'nsew')

        # List Frame
        self.ListFrame = tkinter.Frame (self.winfo_toplevel (), bd = 1, bg = 'black')
        self.ListFrame.bind ('<ButtonRelease-1>', self.LFclose)
        self.ListFrame.columnconfigure (0, weight = 1)

        self.padder = tkinter.Frame (self.ListFrame)
        self.padder.grid_propagate (False)
        self.padder.grid (row = 0, column = 0, columnspan = 2)

        # List Scrollbar
        self.Scroll = tkinter.Scrollbar(self.ListFrame, orient = 'vertical', command = self.scroll)
        self.Scroll.grid (row = 0, rowspan = DropListSize, column = 1, sticky = 'nsw')
        self.Scroll.grid_remove ()

        # Drop Labels
        self.Labels = [self.CreateDropLabel (i) for i in range (DropListSize)]
       
    # Public Methods
    def addValues (self, *values):
        if not self.DBenabled and values:
            self.DBenable()
        self.ListValues.extend (values)

    def delValue (self, index):
        del self.ListValues [index]
        if self.enabled and not self.ListValues:
            self.DBdisable ()

    def set (self, value):
        self.CurrentValue.set (value)

    def get (self):
        return self.CurrentValue.get ()
        
    def get_content (self):
        return self.ListValues
        
    def set_change_handler (self, handlefunc):
        self.ChangeClients.append (handlefunc)

    # Private Methods
    def HandleChange (self, *args):
        for cnt in range (len (self.ChangeClients)):
            self.ChangeClients [cnt] (self.CurrentValue.get ())
    
    def CreateDropLabel (self, row):
        new = tkinter.Label (self.ListFrame, cnf = {'bg' : 'white', 'anchor' : 'w'})
       
        new.bind ('<Enter>'          , self.DBenter )
        new.bind ('<Leave>'          , self.DBleave )

        new.bind ('<Return>'         , self.DBselect)
        new.bind ('<ButtonRelease-1>', self.DBselect)
        new.bind ('<MouseWheel>'     , self.DBroll  )

        new.grid (row = row, column = 0, sticky = 'ew')
        new.grid_remove ()

        return new

    def DropClick (self, event):
        geo = self.winfo_geometry()
        x1, p1, p2  = geo.find ('x'), geo.find ('+'), geo.rfind('+')
        x    = int (geo [     : x1])
        y    = int (geo [x1+1 : p1])
        xoff = int (geo [p1+1 : p2])
        yoff = int (geo [p2+1 :   ])
        self.padder ['width'] = x - 2
        self.ListFrame.place (x = xoff, y = y + yoff)

        self.values = len (self.ListValues)
        if (self.values == 0):
            return
        if self.values > self.DropListSize:
            self.Scroll.grid ()
            self.end = self.DropListSize
        else:
            self.end = self.values
            self.DBdummy ()

        self.start = 0
        self.units = 1.0 / self.values
        self.Scroll.set (0.0, self.units * self.DropListSize)
       
        for label in self.Labels [self.start : self.end]:
            label.grid ()

        self.LFupdate (self.start, self.end)     
        self.ListFrame.grab_set ()
        self.DropButton ['relief'] = 'groove'
        self.ListFrame.lift ()

    # ListFrame Methods
    def LFclose (self, event = None):
        self.ListFrame.place_forget ()
        self.ListFrame.grab_release ()

        self.DropButton ['relief'] = 'raised'

        self.DBundummy ()

        for label in self.Labels:
            label.grid_remove ()

    def LFupdate (self, start, end):
        for i, value in enumerate (self.ListValues[start : end]):
            self.Labels [i]['text'] = value

    # Scroll Methods
    def scroll (self, type, size, units=None):
        if type == 'scroll':
            if units == 'units':
                size = int (size)
            else:
                size = int (size) * self.DropListSize

            if self.end + size > self.values:
                self.end   = self.values
                self.start = self.values - self.DropListSize
            elif self.start + size < 0:
                self.end   = self.DropListSize
                self.start = 0
            else:
                self.end   += size
                self.start += size   
        else:
            size = int (round (float (size) / self.units, 0))
            self.start = size
            self.end   = size + self.DropListSize


        self.LFupdate (self.start, self.end)
        self.Scroll.set (self.units * self.start, self.units * self.end)
           
    # DropButton Methods
    def DBenable (self):
        self.DropButton ['image'] = self.enabled
        self.DBenabled = True

    def DBdisable (self):
        self.DropButton ['image'] = self.disabled
        self.DBenabled = False

    def DBenter (self, event):
        event.widget.focus_set ()
        event.widget ['bg'] = '#0a246a'
        event.widget ['fg'] = '#ffffff'

    def DBleave (self, event):
        event.widget ['bg'] = '#ffffff'
        event.widget ['fg'] = '#000000'

    def DBroll (self, event):
        if event.delta == 120:
            self.scroll ('scroll', '-1', 'units')
        else:
            self.scroll ('scroll',  '1', 'units')

    def DBselect (self, event):
        self.CurrentValue.set (event.widget.cget('text'))
        self.LFclose ()

    def DBdummy (self):
        for label in self.Labels:
            label.unbind ('<MouseWheel>')

    def DBundummy (self):
        for label in self.Labels:
            label.bind ('<MouseWheel>', self.DBroll)

#-----------------------------------------------------------------------------#
#                    Ask string from a given list or new                      #
#-----------------------------------------------------------------------------#
class clsAskStringFromList (tkinter.simpledialog._QueryString):
    DLG_WIDTH  = 225
    DLG_HEIGHT = 200

    #-----
    # constructor...
    # see http://effbot.org/tkinterbook/tkinter-dialog-windows.htm to check what to override
    # for a user defined simple dialog
    def __init__ (self, parent = None, list = None, title = None, newname = False, label = '', defaultstring = ''):
        # copy local variables
        self._list      = list
        self._title     = title
        self._new       = newname
        self._label_txt = label
        self._default   = defaultstring
        
        # run the dialog
        tkinter.simpledialog.__init__ (self, parent, title)
        
    #-----
    # body
    def body (self, master):
        # nice addition if we have a decent ASML icon
        #if (PLATFORM != None):
        #    # on windows we can change the titlebar bitmap...
        #    self.wm_iconbitmap (os.path.join (get_icondir (), 'gtvXsp.ico'))
        
        # set dialog minimum size
        self.wm_minsize (self.DLG_WIDTH, self.DLG_HEIGHT)
        self.wm_maxsize (self.DLG_WIDTH, self.DLG_HEIGHT)

        # handle local stuff
        self._master = master
        self._main_frame = tkinter.Frame (self)
        self._main_frame.pack (side = "top", anchor = "w", expand = 1, fill = tkinter.BOTH)
        
        # show label
        self._label = tkinter.Label (self._main_frame, text = self._label_txt)
        self._label.pack (side = 'top', anchor = 'w')
        
        # show listbox, only allow new entry if self._new is set
        self._combo = clsComboBox (self._main_frame)
        self._combo.addValues (*self._list)
        if (self._default != ''):
            self._combo.set (self._default)
        self._combo.pack (side = 'top', anchor = 'w')
        
    #-----
    # called when enter is pressed
    def apply (self):
        self.result = self._combo.get ()
        
#-----------------------------------------------------------------------------#
#                      Class definition for settings                          #
#-----------------------------------------------------------------------------#
class clsSettings (tkinter.simpledialog._QueryString):
    BASE_X_POS       = 5
    X_INDENT         = 25
    X_DATA1_INDENT   = 325
    X_DATA2_INDENT   = 450
    BASE_Y_POS       = 20
    DELTA_Y          = 30
    DELTA_X          = 10
    EDIT_WIDTH       = 65
    DLG_WIDTH        = 575
    DLG_HEIGHT       = 500
    CBX_WIDTH        = 325
    LBL_WIDTH        = 325
    SELECT_WIDTH1    = 425
    PATH_ENTRY_WIDTH = 450
    BUTTON_WIDTH     = 74
    SPEED_BUTTON_SZ  = 20
    
    #-----
    # class constructor
    # NOTE: even though the settings parameter seems to be optional, it is MANDATORY! When a set is
    #       saved with name "default", it is loaded on startup of BPAM_Gui, but we need to be able to
    #       check if the structure has changed! That's done by giving the default startup set to
    #       class constructor as a reference.
    def __init__ (self, parent = None, settings = None, title = None, start_page = 0, current_set = '', top_obj = None):
        # preset a setname if we don't have one (yet)
        if (current_set == None):
            current_set = "default"
        
        # check if we have a parent, if not we're just here to load a default set
        if (parent == None):
            # parent input variable is none, we try to load a default set
            self._settings = settings
            self._all_sets = {}
            self.result    = {}
            self.result ["settings"]    = None
            self.result ["last_screen"] = 0
            self.result ["setname"]     = current_set
            
            # take over principle from gtvX
            self._load_settings ()
            for key in self._all_sets.keys ():
                if (key.upper () == "DEFAULT"):
                    # take over result and setname; load will have updated the set if needed
                    self.result ["settings"] = self._all_sets [key]
                    self.result ["setname"]  = key
        else:
            # there is a parent: copy and set initial settings
            self._settings = settings
            self._pos_y    = self.BASE_Y_POS
            self._start_pg = start_page
            self._setname  = current_set
            self._top_obj  = top_obj
            self._all_sets = {}
            self.result    = {}
            self.result ["settings"]    = settings
            self.result ["last_screen"] = 0
            self.result ["setname"]     = current_set
            
            # prepare variables for path dialogs of system console
            self._syscon_paths          = []
            self._syscon_path_entries   = []
            self._syscon_path_file_btns = []
            
            # load an image for the file dialog speed buttons
            self._select_file_photo = tkinter.PhotoImage (file = os.path.join (ICONDIR, "openfolder.gif"))
            
            # run the dialog
            parent.deiconify ()
            tkinter.simpledialog.__init__ (self, parent, title)
        
    #-----
    # main body of the dialog; this function is called by calling tkinter.simpledialog.__init__ at the
    # end of the local constructor (self.__init__) if this dialog is called with the intention to allow
    # the user to change settings; this same dialog can also be used "passively" as it were in order to
    # get a default set of settings (called with parent == None).
    def body (self, master):
        # for future use when we have a decent icon file
        #if (PLATFORM != None):
        #    # on windows we can change the titlebar bitmap...
        #    self.wm_iconbitmap (os.path.join (ICONDIR, 'some_icon.ico'))
        
        # set dialog minimum size
        self.wm_minsize (self.DLG_WIDTH, self.DLG_HEIGHT)
        self.wm_maxsize (self.DLG_WIDTH, self.DLG_HEIGHT)
        
        # load the settings on disk for later selection
        self._load_settings ()
        
        # create main frame to put everything else in
        self._master     = master
        self._main_frame = tkinter.Frame (self)
        self._main_frame.pack (side = "top", anchor = 'w', expand = 1, fill = tkinter.BOTH)
        
        # create notebook to hold tabs
        self._nb = clsNotebook (self._main_frame, tkinter.TOP)
        
        # define the tabs; all widgets on there will refer to self._nb
        self._build_generic_tab ()
        
        # fill all values from the current settings
        self._fill_values ()
        
        # set the given active page
        self._nb.set_active_screen (self._start_pg)
        
    #-----
    # build the generic settings tab
    def _build_generic_tab (self):
        # create the frame; do NOT pack the frame, only the widgets on it
        self._generic_frame  = tkinter.Frame (self._nb ())
        self._generic_canvas = tkinter.Canvas (self._generic_frame)
        self._generic_canvas.pack (side = "left", expand = 1, fill = tkinter.BOTH)
        self._pos_y          = self.BASE_Y_POS
        
        # Refresh time
        self._refresh_time_lbl = tkinter.Label (self._generic_canvas, text = self._settings ["T_REFRESH"]["text"], anchor = 'w')
        self._generic_canvas.create_window ((self.X_INDENT, self._pos_y), anchor = 'w', width = 200, window = self._refresh_time_lbl)
        self._refresh_time_var = tkinter.StringVar ()
        self._refresh_time_edt = tkinter.Entry (self._generic_canvas, textvariable = self._refresh_time_var)
        self._generic_canvas.create_window ((self.X_DATA1_INDENT, self._pos_y), anchor = 'w', width = self.EDIT_WIDTH, window = self._refresh_time_edt)
        self._pos_y = self._pos_y + self.DELTA_Y
        
        # com port
        self._comport_lbl = tkinter.Label (self._generic_canvas, text = self._settings ["COM_PORT"]["text"], anchor = 'w')
        self._generic_canvas.create_window ((self.X_INDENT, self._pos_y), anchor = 'w', width = 200, window = self._comport_lbl)
        self._comport_var = tkinter.StringVar ()
        self._comport_edt = tkinter.Entry (self._generic_canvas, textvariable = self._comport_var)
        self._generic_canvas.create_window ((self.X_DATA1_INDENT, self._pos_y), anchor = 'w', width = self.EDIT_WIDTH, window = self._comport_edt)
        self._pos_y = self._pos_y + self.DELTA_Y
        
        # baudrate
        self._baudrate_lbl = tkinter.Label (self._generic_canvas, text = self._settings ["BAUDRATE"]["text"], anchor = 'w')
        self._generic_canvas.create_window ((self.X_INDENT, self._pos_y), anchor = 'w', width = 200, window = self._baudrate_lbl)
        self._baudrate_var = tkinter.StringVar ()
        self._baudrate_edt = tkinter.Entry (self._generic_canvas, textvariable = self._baudrate_var)
        self._generic_canvas.create_window ((self.X_DATA1_INDENT, self._pos_y), anchor = 'w', width = self.EDIT_WIDTH, window = self._baudrate_edt)
        self._pos_y = self._pos_y + self.DELTA_Y
        
        # communication timeout
        self._comm_timeout_lbl = tkinter.Label (self._generic_canvas, text = self._settings ["BAUDRATE"]["text"], anchor = 'w')
        self._generic_canvas.create_window ((self.X_INDENT, self._pos_y), anchor = 'w', width = 200, window = self._comm_timeout_lbl)
        self._comm_timeout_var = tkinter.StringVar ()
        self._comm_timeout_edt = tkinter.Entry (self._generic_canvas, textvariable = self._comm_timeout_var)
        self._generic_canvas.create_window ((self.X_DATA1_INDENT, self._pos_y), anchor = 'w', width = self.EDIT_WIDTH, window = self._comm_timeout_edt)
        self._pos_y = self._pos_y + self.DELTA_Y
        
        # add the frame to the notebook
        self._nb.add_screen (self._generic_frame, " Generic ")
                
    #-----
    # change a filename or a path
    # arg is a tuple, first element is the index into the list of paths, second element is True if a file must be selected, False if a directory must be selected
    def _set_file_or_path (self, arg):
        # get the arguments
        ndx     = arg [0]
        getfile = arg [1]       # True for opening a file, False for opening a directory
        
        # for file or directory open, the options as well as the function are different
        if (getfile):
            # setup options for getting the full path and filename to whatever we need
            file_dlg_opt = {}
            file_dlg_opt ["defaultextension"] = ".exe"
            file_dlg_opt ["filetypes"]        = [('EXE files', '.exe'), ('all files', '*')]
            file_dlg_opt ["initialdir"]       = os.path.dirname (self._syscon_paths [ndx].get ())
            file_dlg_opt ["initialfile"]      = os.path.basename (self._syscon_paths [ndx].get ())
            file_dlg_opt ["parent"]           = self._top_obj
            file_dlg_opt ["title"]            = "Select a File..."
            file_dlg_opt ["multiple"]         = False
            
            # open a file dialog to get a file name
            dlg_result = tkFileDialog.askopenfilenames (**file_dlg_opt)
        else:
            # setup options to select a current working directory
            dir_dlg_opt = {}
            dir_dlg_opt ["initialdir"] = self._syscon_paths [ndx].get ()
            dir_dlg_opt ["title"]      = "Select a Directory..."
            dir_dlg_opt ["mustexist"]  = True
            
            # try to select a new directory
            dlg_result = tkFileDialog.askdirectory (**dir_dlg_opt)
            
        # now... depending on the version of tkinter (python/os related) the return is either a string or a
        # tuple or list with one element, so see what it is and make a basic string result
        if ( (type (dlg_result) == types.TupleType) or (type (dlg_result) == types.ListType) ):
            # python version that returns a tuple, returns an empty tuple if cancel is pressed
            if (len (dlg_result) > 0):
                dlg_result = dlg_result [0]
            else:
                dlg_result = ""
            
        # avoid an error message if user cancelled the input
        if ( (dlg_result != "") and (dlg_result != None) ):
            # we have a valid name, set the Entry widget with the given name
            self._syscon_paths [ndx].set (dlg_result)
            
    #-----
    # a number of variables have been defined during building of the tabs; these are now preset with values
    # we take from the current active settings
    def _fill_values (self):
        # presets for generic
        self._refresh_time_var.set (str (self._settings ["T_REFRESH"]["value"]))        # number
        self._comport_var.set (self._settings ["COM_PORT"]["value"])                    # string
        self._baudrate_var.set (str (self._settings ["BAUDRATE"]["value"]))             # number
        self._comm_timeout_var.set (str (self._settings ["COMM_TIMEOUT"]["value"]))     # number
    
    #-----
    # function to activate a loaded set
    def _activate (self):
        # create a list from the currently known settings and ask for a (new) name
        setlist = []
        for key in self._all_sets.keys ():
            setlist.append (key)
        
        # get a setname from the user
        setname = clsAskStringFromList (self._main_frame, setlist, label = 'Select setname from list:', defaultstring = self._setname).result
        
        # if a name was selected, apply those settings
        if ( (setname != '') and (setname != None) ):
            if (setname in self._all_sets.keys ()):
                # activate the given set
                self._setname  = setname
                self._settings = self._all_sets [self._setname]
                
                # fill the values
                self._fill_values ()
            else:
                msg = 'Setname \'' + setname + '\' does not exist'
                tkMessageBox.showerror ('Load Settings', msg)
        
    #-----
    # check if we're OK to leave...
    def _check_ok (self):
        # first make sure all settings are taken over
        self.apply ()
        
        # do some checks....
        setup_err = False
        err_msg   = ''
        
        print ("ToDo: implement input checks")
            
        # evaluate...
        if (not (setup_err)):
            # everything OK then we return to our caller with the new settings
            self.ok ()
        else:
            # otherwise we show an error
            tkMessageBox.showwarning ("Setup", err_msg)
        
    #-----
    # save function for the settings
    def _save (self):
        # make sure new settings are applied in result struct
        self.apply ()
        
        # create a list from the currently known settings and ask for a (new) name
        setlist = []
        for key in self._all_sets.keys ():
            setlist.append (key)
        self._setname = clsAskStringFromList (self._main_frame, setlist, newname = True, label = 'Select (new) setname:', defaultstring = self._setname).result
        
        # open a file and save all settings at once
        if ( (self._setname != "") and (self._setname != None) ):
            self._all_sets [self._setname] = self.result ["settings"]
            fptr = open (SETTING_FILENAME, 'w')
            fptr.write (str (self._all_sets))
            fptr.close ()
        
    #-----
    # load settings from disc
    def _load_settings (self):
        # try to load all settings from disk
        try:
            # try to read settings from disk
            fptr           = open (SETTING_FILENAME, 'r')
            line           = fptr.read ()
            self._all_sets = eval (line)
            fptr.close ()
            
            # now that we have all-sets, do two things:
            # check if each of the sets that was loaded is correct
            # second, create a list of keys for the selection
            updated = False
            for set in self._all_sets.keys ():
                loaded = self._all_sets [set]
            
                # check if all entries are present (auto upgrade from old version)
                # note that self._settings is the current set we got when this class was instantiated
                # this is NOT necessarily the default!
                for key in self._settings.keys ():
                    if (not (key in loaded.keys ())):
                        # key not found in settings read from disk, copy from inputset
                        loaded [key] = self._settings [key]
                        updated      = True
                        
                # copy back
                self._all_sets [set] = loaded
                
            # check if we had an update from an earlier version; if so: do an immediate save to
            # make sure all current sets are saved with the default from the gtvGUI class; Note
            # that this branche only actually is activated once when a new version with a new
            # element is released; this is never entered based on what a user can do.
            if (updated):
                fptr = open (SETTING_FILENAME, 'w')
                fptr.write (str (self._all_sets))
                fptr.close ()
                print ('Updated settings file from earlier version to', VERSION_STRING)
        except:
            # probably settings file does not exist (yet), or some conversion error
            # occurred, ignore, we keep the current set
            nofile = True
        
    #-----
    # process the changes and create a return value for the dialog
    def apply (self):
        # preset result with our 'old' settings, then take over the new settings individually
        result = self._settings
        
        # generic....
        result ["T_REFRESH"]["value"]             = int (self._refresh_time_var.get (), 10)
        result ["COM_PORT"]["value"]              = self._comport_var.get ()
        result ["BAUDRATE"]["value"]              = int (self._baudrate_var.get (), 10)
        result ["COMM_TIMEOUT"]["value"]          = float (self._comm_timeout_var.get ())
        
        # copy and return the new settings
        self.result = {}
        self.result ["settings"]    = result
        self.result ["last_screen"] = self._nb.get_active_screen ()
        self.result ["setname"]     = self._setname
    
    #-----
    # we override the buttonbox method because we want a save button as well
    def buttonbox (self):
        box = tkinter.Frame (self)
        w   = tkinter.Button (box, text = "OK", width = 10, command = self._check_ok, default = tkinter.ACTIVE)
        w.pack (side = "left", padx = 5, pady = 5)
        w   = tkinter.Button (box, text = "Save", width = 10, command = self._save)
        w.pack (side = "left", padx = 5, pady = 5)
        w   = tkinter.Button (box, text = "Load", width = 10, command = self._activate)
        w.pack (side = "left", padx = 5, pady = 5)
        w   = tkinter.Button (box, text = "Cancel", width = 10, command = self.cancel)
        w.pack (side = "right", padx = 5, pady = 5)
        
        self.bind ("<Return>", self.ok)
        self.bind ("<Escape>", self.cancel)
        
        box.pack (fill = tkinter.X)
        
#-----------------------------------------------------------------------------#
#              Class definition for partial data upload dialog                #
#-----------------------------------------------------------------------------#
class clsUploadDialog (tkinter.simpledialog._QueryString):
    # class global constants
    BASE_X_POS       = 10
    X_INDENT         = 25
    X_DATA1_INDENT   = 275
    X_DATA2_INDENT   = 400
    BASE_Y_POS       = 20
    DELTA_Y          = 30
    DELTA_X          = 10
    EDIT_WIDTH       = 65
    DLG_WIDTH        = 575
    DLG_HEIGHT       = 350
    CBX_WIDTH        = 325
    LBL_WIDTH        = 325
    SELECT_WIDTH1    = 425
    PATH_ENTRY_WIDTH = 450
    BUTTON_WIDTH     = 74
    SPEED_BUTTON_SZ  = 20
    
    #-----
    # class constructor
    def __init__ (self, parent, blocks_to_load, trigger_block_ndx, previous_set = None):
        # copy passed parameters to class global variables
        self._parent = parent
        
        # other class globals
        self._title       = "Data Upload..."
        self._upload_type = tkinter.IntVar ()
        self._pos_y       = self.BASE_Y_POS
        self._num_blocks  = blocks_to_load
        self._trig_ndx    = trigger_block_ndx
        self._previous    = previous_set
        
        # we calculate a load time based on a 1024 byte reply frame, a 9 byte request frame and 1 ms overhead per frame, at 2 Mbd
        # the number of blocks to load is actually the number 16 channel samples, so 64 bytes per block, so 16 blocks in a 1024 byte frame
        num_frames = blocks_to_load / 16                                        # now we have the number of frames (1024 bytes) we need to load
        self._load_time = num_frames * ((10240.0 + 90.0) / 2000000.0 + 0.02)    # 10240 bits in one frame, 90 bits for the request at 2 MBd, is [s], + 20 msec overhead per frame
        
        # run the dialog
        tkinter.simpledialog.__init__ (self, parent, self._title)
        
    #-----
    # main body of the dialog; this function is called by calling tkinter.simpledialog.__init__ at the
    # end of the local constructor (self.__init__) if this dialog is called with the intention to allow
    # the user to change settings; this same dialog can also be used "passively" as it were in order to
    # get a default set of settings (called with parent == None).
    def body (self, master):
        # for future use when we have a decent icon file
        #if (PLATFORM != None):
        #    # on windows we can change the titlebar bitmap...
        #    self.wm_iconbitmap (os.path.join (ICONDIR, 'some_icon.ico'))
        
        # set dialog minimum size
        self.wm_minsize (self.DLG_WIDTH, self.DLG_HEIGHT)
        self.wm_maxsize (self.DLG_WIDTH, self.DLG_HEIGHT)
        
        # handle local stuff
        self._master = master
        
        # setup frame and widgets
        self._main_frame = tkinter.Frame (self, bd = 2, relief = tkinter.GROOVE)
        self._main_frame.pack (side = "top", anchor = "w", expand = 1, fill = tkinter.BOTH)
        self._main_canvas = tkinter.Canvas (self._main_frame)
        self._main_canvas.pack (side = "left", expand = 1, fill = tkinter.BOTH)
        
        # show some statistics and expected load time for the full upload
        label = "Total number of 16-channel samples to load: %d"%self._num_blocks
        self._t1 = tkinter.Label (self._main_canvas, text = label, anchor = 'w')
        self._main_canvas.create_window ((self.BASE_X_POS, self._pos_y), anchor = 'w', width = self.LBL_WIDTH, window = self._t1)
        self._pos_y += self.DELTA_Y
        label = "Index of trigger: %d, post-trigger length: %d"%(self._trig_ndx, (self._num_blocks - self._trig_ndx - 1))
        self._t2 = tkinter.Label (self._main_canvas, text = label, anchor = 'w')
        self._main_canvas.create_window ((self.BASE_X_POS, self._pos_y), anchor = 'w', width = self.LBL_WIDTH, window = self._t2)
        self._pos_y += self.DELTA_Y
        label = "Estimated load time: %.1f [s]"%self._load_time
        self._t3 = tkinter.Label (self._main_canvas, text = label, anchor = 'w')
        self._main_canvas.create_window ((self.BASE_X_POS, self._pos_y), anchor = 'w', width = self.LBL_WIDTH, window = self._t3)
        self._pos_y += self.DELTA_Y
        self._pos_y += self.DELTA_Y
        
        # put in our buttons and fields
        self._full_radio = tkinter.Radiobutton (self._main_canvas, text = "Full upload",        command = self._on_upload_change, variable = self._upload_type, value = 0)
        self._main_canvas.create_window ((self.BASE_X_POS, self._pos_y), anchor = 'w', window = self._full_radio)
        self._pos_y += self.DELTA_Y
        
        self._decimated_radio = tkinter.Radiobutton (self._main_canvas, text = "Decimated upload, 1 out of:",   command = self._on_upload_change, variable = self._upload_type, value = 1)
        self._main_canvas.create_window ((self.BASE_X_POS, self._pos_y), anchor = 'w', window = self._decimated_radio)
        self._decimated_var   = tkinter.IntVar ()
        self._decimated_edt   = tkinter.Entry (self._main_canvas, textvariable = self._decimated_var, state = tkinter.DISABLED)
        self._main_canvas.create_window ((self.X_DATA1_INDENT, self._pos_y), anchor = 'w', width = self.EDIT_WIDTH, window = self._decimated_edt)
        self._pos_y += self.DELTA_Y
        
        self._block_partial_radio = tkinter.Radiobutton (self._main_canvas, text = "Block range upload, from:", command = self._on_upload_change, variable = self._upload_type, value = 2)
        self._main_canvas.create_window ((self.BASE_X_POS, self._pos_y), anchor = 'w', window = self._block_partial_radio)
        self._block_from_var      = tkinter.IntVar ()
        self._block_from_edt      = tkinter.Entry (self._main_canvas, textvariable = self._block_from_var, state = tkinter.DISABLED)
        self._block_to_var        = tkinter.IntVar ()
        self._block_to_edt        = tkinter.Entry (self._main_canvas, textvariable = self._block_to_var, state = tkinter.DISABLED)
        self._main_canvas.create_window ((self.X_DATA1_INDENT, self._pos_y), anchor = 'w', width = self.EDIT_WIDTH, window = self._block_from_edt)
        self._main_canvas.create_window ((self.X_DATA2_INDENT, self._pos_y), anchor = 'w', width = self.EDIT_WIDTH, window = self._block_to_edt)
        self._t4 = tkinter.Label (self._main_canvas, text = "to:", anchor = 'w')
        self._main_canvas.create_window ((self.X_DATA1_INDENT + self.EDIT_WIDTH + 15, self._pos_y), anchor = 'w', width = 20, window = self._t4)
        self._pos_y += self.DELTA_Y
        
        label = "Minimum block index: %d, Maximum block index: %d"%((-self._trig_ndx), (self._num_blocks - self._trig_ndx - 1))
        self._t5 = tkinter.Label (self._main_canvas, text = label, anchor = 'w')
        self._main_canvas.create_window ((self.BASE_X_POS + 20, self._pos_y), anchor = 'w', width = self.LBL_WIDTH, window = self._t5)
        
        # if we had a previous set, we take over the choices made there
        if (self._previous != None):
            self._upload_type.set (self._previous ["choice"])
            self._decimated_var.set (self._previous ["decimation"])
            self._block_from_var.set (self._previous ["block_from"])
            self._block_to_var.set (self._previous ["block_to"])
            self._on_upload_change ()
        
    #-----
    # on change handler for the radio button that selects the type of upload
    def _on_upload_change (self, param = None):
        # get the current value of the radio button
        upload_type = self._upload_type.get ()
        
        # enable / disable input fields
        if (upload_type == 0):
            self._decimated_edt ["state"]  = tkinter.DISABLED
            self._block_from_edt ["state"] = tkinter.DISABLED
            self._block_to_edt ["state"]   = tkinter.DISABLED
        elif (upload_type == 1):
            self._decimated_edt ["state"]  = tkinter.NORMAL
            self._block_from_edt ["state"] = tkinter.DISABLED
            self._block_to_edt ["state"]   = tkinter.DISABLED
        elif (upload_type == 2):
            self._decimated_edt ["state"]  = tkinter.DISABLED
            self._block_from_edt ["state"] = tkinter.NORMAL
            self._block_to_edt ["state"]   = tkinter.NORMAL
        else:
            pass
            
    #-----
    # store the settings we made
    def _store_settings (self, param = None):
        # check if the settings we made are acceptable
        valid_input = True
        msg         = ""
        if (self._upload_type.get () == 1):
            if (self._decimated_var.get () == 0):
                valid_input = False
                msg         = "Invalid decimation (0)"
            
        if (self._upload_type.get () == 2):
            blockfrom = self._block_from_var.get ()
            blockto   = self._block_to_var.get ()
            
            # check if from/to is within min/max range
            if (blockfrom < -self._trig_ndx) or (blockto > (self._num_blocks - self._trig_ndx - 1)):
                valid_input = False
                msg        += "\nInvalid from or to block index"
                
        if (valid_input):
            self.result = {}
            self.result ["choice"]     = self._upload_type.get ()
            self.result ["decimation"] = self._decimated_var.get ()
            self.result ["block_from"] = self._block_from_var.get ()
            self.result ["block_to"]   = self._block_to_var.get ()
            
            # call self.ok, it will return "result"
            self.ok ()
        else:
            print ("Invalid selection:")
            print (msg)
    
    #-----
    # we override the buttonbox method because we want to define our own buttons
    def buttonbox (self):
        # setup some specific buttons and the OK/Cancel buttons; note that self.ok and self.cancel are defined by the
        # simpleDialog class
        box = tkinter.Frame (self)
        w   = tkinter.Button (box, text = "OK", width = 10, command = self._store_settings, default = tkinter.ACTIVE)
        w.pack (side = "left", padx = 5, pady = 5)
        #w   = tkinter.Button (box, text = "Save", width = 10, command = self._save)
        #w.pack (side = "left", padx = 5, pady = 5)
        #w   = tkinter.Button (box, text = "Load", width = 10, command = self._activate)
        #w.pack (side = "left", padx = 5, pady = 5)
        w   = tkinter.Button (box, text = "Cancel", width = 10, command = self.cancel)
        w.pack (side = "right", padx = 5, pady = 5)
        
        # bind keys
        self.bind ("<Return>", self._store_settings)
        self.bind ("<Escape>", self.cancel)
        
        # show box
        box.pack (fill = tkinter.X)
        
#-----------------------------------------------------------------------------#
#                      Class definition for a table                           #
#-----------------------------------------------------------------------------#
class clsTextTable (tkinter.Frame):
    #-----
    # class constructor
    # Note that though this class is called a TextTable class, it accepts a list of widgets, which are not
    # limited to 'Text' only. However, the 'set' method of this class only works on widgets that have a 
    # text property.
    def __init__ (self, parent, rows):
        # take over input parameters
        self._parent = parent
        self._rows   = rows         # list of widget definitions to put in the rows
        
        # use black background so it "peeks through" to form grid lines
        tkinter.Frame.__init__ (self, self._parent)
        
        # create the table widgets
        self._create_widgets ()
        
    #-----
    # create the widgets we need in the tableeach element from the list of rows is a list
    # containing widget references; these widgets will form the columns in the row.
    def _create_widgets (self):
        # place the widgets
        row = 0
        for the_row in self._rows:
            col = 0
            for widget in the_row:
                widget.grid (row = row, column = col, sticky = "nsew", padx = 1, pady = 1)
                col += 1
            row += 1
            
        # configure the grid; we take the amount of columns from the first row
        for column in range (len (self._rows [0])):
            self.grid_columnconfigure (column, weight = 1)        
            
    #-----
    # set a new value for a widget in the table; note that this will only work for Wdigets
    # that have a 'text' property.
    def set (self, row, col, value):
        the_row = self._rows [row]
        widget  = the_row [col]
        widget.configure (text = value)

#-----------------------------------------------------------------------------#

#                          Temperature Graph class                            #

#-----------------------------------------------------------------------------#

class clsTemperatureGraph (tkinter.Frame):

    #-----

    # class constructor

    def __init__ (self, parent, num_samples = 100):

        tkinter.Frame.__init__ (self, parent)

       

        # copy parameters to locally kept variables

        self._num_samples = num_samples

        self._parent      = parent

       

        # instantiate a figure

        self._fig  = Figure ()

        self._axes = self._fig.add_subplot (111)

       

        # pre-create a few arrays, 1 for x and 3 for y

        self._tb    = numpy.array ([])

        self._t_act = numpy.array ([])

        self._t_min = numpy.array ([])

        self._t_max = numpy.array ([])

       

        # prepare the legend

        self._axes.plot (self._tb, self._t_act, label = "Act")

        self._axes.plot (self._tb, self._t_min, label = "Min")

        self._axes.plot (self._tb, self._t_max, label = "Max")

       

        # create a canvas for the figure and the toolbar

        # note: in contrast to gtvX, where the figures have their own window, this figure is drawn on a canvas provided

        # by the main application!

        self._canvas = FigureCanvasTkAgg (self._fig, self)

        self._canvas.show ()

        self._canvas.get_tk_widget ().grid (row = 0, sticky = tkinter.NSEW)

       

        # show the matlab toolbaar

        self._toolbar = NavigationToolbar2TkAgg (self._canvas, self)

        self._toolbar.grid (row = 1, sticky = tkinter.W)

        self._toolbar.update ()

       

        # set a small font and cursor for the axes

        for tick in self._axes.xaxis.get_major_ticks ():

            tick.label1.set_fontsize (8)

        for tick in self._axes.yaxis.get_major_ticks ():

            tick.label1.set_fontsize (8)

        self._axes.grid (True)

        self._cursor = Cursor (self._axes, useblit = True, color = 'red', linewidth = 1)

       

        # set labels

        self._axes.set_ylabel ('FPGA Temp [C]', fontsize = 'x-small')

        self._axes.set_xlabel ('Measurement #', fontsize = 'x-small')

       

        # set legend

        self._legend = self._axes.legend (loc = 'upper left', borderpad = 0.2, labelspacing = 0)

        for label in self._legend.get_texts ():

            label.set_fontsize ('x-small')

           

        # prepare for autoscaling and relimiting

        self._axes.set_autoscaley_on (True)

       

    #-----

    # resize

    def resize (self, width, height):

        # set the canvas size; note that the '-82' in height is needed to make sure the toolbar stays

        # visible; we need to find something better for this hardcoded number...

        # the '-20' in width is to keep a decent frame around the figure...

        self._canvas.get_tk_widget ().config (width = width - 20, height = height - 82)

       

    #-----

    # update temperature, min and max

    def update_data (self, temp_act, temp_min, temp_max):

        if (self._tb.size == 0):

            # for zero-length we need to start explicitly as we don't yet have a last element in the array

            self._tb    = numpy.array ([0.0])

            self._t_act = numpy.array ([temp_act])

            self._t_min = numpy.array ([temp_min])

            self._t_max = numpy.array ([temp_max])

        elif (self._tb.size < self._num_samples):

            self._tb    = numpy.append (self._tb, [self._tb [-1] + 1])          # simply increase the timebase as at this moment we only count samples

            self._t_act = numpy.append (self._t_act, [temp_act])                # add a next actual temperature element to the array

            self._t_min = numpy.append (self._t_min, [temp_min])                # add a next minimum temperature element to the array

            self._t_max = numpy.append (self._t_max, [temp_max])                # add a next maximum temperature element to the array

        else:

            # first we shift all numbers 1 position to the 'left'

            self._tb    = self._tb [1:]

            self._t_act = self._t_act [1:]

            self._t_min = self._t_min [1:]

            self._t_max = self._t_max [1:]

           

            # then we append the new numbers

            self._tb    = numpy.append (self._tb, [self._tb [-1] + 1])          # simply increase the timebase as at this moment we only count samples

            self._t_act = numpy.append (self._t_act, [temp_act])                # add a next actual temperature element to the array

            self._t_min = numpy.append (self._t_min, [temp_min])                # add a next minimum temperature element to the array

            self._t_max = numpy.append (self._t_max, [temp_max])                # add a next maximum temperature element to the array

       

        # set the data

        self._axes.lines [0].set_xdata (self._tb)

        self._axes.lines [0].set_ydata (self._t_act)

        self._axes.lines [1].set_xdata (self._tb)

        self._axes.lines [1].set_ydata (self._t_min)

        self._axes.lines [2].set_xdata (self._tb)

        self._axes.lines [2].set_ydata (self._t_max)

       

        # rescale the axes

        if (self._tb.size > 1):

            self._axes.set_xlim (self._tb [0], self._tb [-1])

            self._axes.set_ylim (min (self._t_min) - 1, max (self._t_max) + 1)

            self._axes.relim ()

            self._axes.autoscale_view ()

       

        # redraw the graph

        self._fig.canvas.draw ()

        self._fig.canvas.flush_events ()

   

    #-----

    # return the ratio of fill of the buffer

    def get_fill_ratio (self):

        return (float (self._tb.size) / float (self._num_samples))
        
#-----------------------------------------------------------------------------#
#                            Main Starts Here                                 #
#-----------------------------------------------------------------------------#

# no main, part of import of HexaLog_Gui
    
#----------------------------------- End of file -----------------------------#

