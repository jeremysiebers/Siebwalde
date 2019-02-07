# README	General information
* Unified Bootloader Host
- Used with MCU8 devices which use the MCC Bootloader Generated Software Library, or MCU32 AN1388 bootloader.
The graphical front-end of the MCU8 Bootloader project. 

# AUTHORS	Credits
Microchip - MCU8 Applications
MCC_Support@microchip.com

# THANKS	Acknowledgments
n/a

# Support 
This application requires the Java 1.8 runtime.
Users of Microchip products can receive assistance through several channels:

 - Distributor or Representative
 - Local Sales Office
 - Field Application Engineer (FAE)
 - Technical Support

Customers should contact their distributor, representative or Field Application Engineer (FAE) for support. 
Local sales offices are also available to help customers. 
A listing of sales offices and locations may be found on the [Microchip web site][3].

Technical support is available through the [Microchip web site][4].

# CHANGELOG	A detailed changelog, intended for programmers
v 0.1.9:	Added readme, added logger, added checksum support to PIC32; added handling for record type 0x05 (currently ignored) 
v 0.1.10:	Internal release
v 0.1.11:	Internal release
v 0.1.14:	Added PIC32 CRC bytes used at end of packets. Added Erase feature to USB PIC32 command chain. Extended Timeouts, added support for 24-bit packet length.

# NEWS	A basic changelog, intended for users
-Incremented version infromation; see Changelog
-Added readme file on release of version: 0.1.14
-Added CheckSum support for PIC32
-Fixed Typo in Help-->About text which indicated incorrect Utility version 
-Added logger support: requires launching from command promt with below text:
	*-Djava.util.logging.config.file="C:\<DirectoryLocation>"
i.e.: To Launch from Command Prompt
>java -Djava.util.logging.config.file="C:\MyDirectory\UnifiedHost-0.1.14\logging.properties" -jar UnifiedHost-0.1.14.jar

# INSTALL	Installation instructions
Installation of Java JRE

# COPYING / LICENSE	Copyright and licensing information
See LICENSE.txt for licensing information.

# BUGS	Known bugs and instructions on reporting new ones
