																									MIP50 Configuration

												OPTO_BOARD_2_PG1									OPTO_BOARD_2_PG5
PORTBbits.RA0 = ETHERNET LED1					PORTBbits.RA0 = ETHERNET LED1                       PORTBbits.RA0 = ETHERNET LED1
PORTBbits.RA1 = ETHERNET LED2                   PORTBbits.RA1 = ETHERNET LED2                       PORTBbits.RA1 = ETHERNET LED2
PORTBbits.RA2 = ADCONVERTER ENC TOP             PORTBbits.RA2 = ADCONVERTER ENC TOP                 PORTBbits.RA2 = ADCONVERTER ENC TOP
PORTBbits.RA3 = ADCONVERTER ENC BOTTOM          PORTBbits.RA3 = ADCONVERTER ENC BOTTOM              PORTBbits.RA3 = ADCONVERTER ENC BOTTOM
PORTBbits.RA4 = FREE                            PORTBbits.RA4 = Led1                                PORTBbits.RA4 = Led1
PORTBbits.RA5 = FREE                            PORTBbits.RA5 = Led2                                PORTBbits.RA5 = Led2
												3.3V, 2mA                                           3.3V, 2mA
                                                                                                    
												OPTO_BOARD_2_PG1                                    OPTO_BOARD_2_PG5                                       
PORTBbits.RB0 = Led1                            PORTBbits.RB0 = Led3                                PORTBbits.RB0 = Led3  
PORTBbits.RB1 = Led2                            PORTBbits.RB1 = SPARE_SWITCH_INPUT_TOP  			PORTBbits.RB1 = SPARE_SWITCH_INPUT_TOP  			
PORTBbits.RB2 = Led3                            PORTBbits.RB2 = SPARE_SWITCH_INPUT_BOTTOM           PORTBbits.RB2 = SPARE_SWITCH_INPUT_BOTTOM           
PORTBbits.RB3 = Led4                            PORTBbits.RB3 = IO_Expander_Reset I^2C              PORTBbits.RB3 = IO_Expander_Reset I^2C
PORTBbits.RB4 = Led5                            PORTBbits.RB4 = Output_Enable                       PORTBbits.RB4 = Output_Enable    
PORTBbits.RB5 = Output_Enable                   PORTBbits.RB5 = Externall_WDT                       PORTBbits.RB5 = Externall_WDT
PORTBbits.RB6 = INCIRCUIT PROGRAM/DEBUG         PORTBbits.RB6 = INCIRCUIT PROGRAM/DEBUG             PORTBbits.RB6 = INCIRCUIT PROGRAM/DEBUG
PORTBbits.RB7 = INCIRCUIT PROGRAM/DEBUG	        PORTBbits.RB7 = INCIRCUIT PROGRAM/DEBUG	            PORTBbits.RB7 = INCIRCUIT PROGRAM/DEBUG	
												5.0V, 25mA									        5.0V, 25mA									
                                                                                                    
												OPTO_BOARD_1_BOT_PG1                                OPTO_BOARD_1_BOT_PG4
PORTCbits.RC0 = M10_ Bottom                     PORTCbits.RC0 = Brake Bottom                        PORTCbits.RC0 = F22_ Bottom
PORTCbits.RC1 = Brake Bottom                    PORTCbits.RC1 = Bridge_10L_ Bottom                  PORTCbits.RC1 = 15V track measurement
PORTCbits.RC2 = PWM Bottom                      PORTCbits.RC2 = PWM Bottom                          PORTCbits.RC2 = EOS_20_ Bottom
PORTCbits.RC3 = M11_ Bottom	                    PORTCbits.RC3 = Bridge_10R_ Bottom                  PORTCbits.RC3 = F23_ Bottom
PORTCbits.RC4 = Bezet_In_5B_ Bottom             PORTCbits.RC4 = F10_ Bottom                         PORTCbits.RC4 = F20_ Bottom
PORTCbits.RC5 = Bezet_In_6_ Bottom              PORTCbits.RC5 = F11_ Bottom                         PORTCbits.RC5 = F21_ Bottom
PORTCbits.RC6 = Bezet_In_7_ Bottom              PORTCbits.RC6 = F12_ Bottom                         PORTCbits.RC6 = TX1
PORTCbits.RC7 = Bezet_Weerstand_ Bottom	        PORTCbits.RC7 = EOS_10_ Bottom                      PORTCbits.RC7 = RX1
												5.0V, 25mA				                            5.0V, 25mA				    
                                                                                                      
												OPTO_BOARD_2_PG1                                    OPTO_BOARD_2_PG5
PORTDbits.RD0 = M10_ Top                        PORTDbits.RD0 = M10_ Top                            PORTDbits.RD0 = MIP50_ENABLE Top                   
PORTDbits.RD1 = M11_ Top                        PORTDbits.RD1 = M11_ Top                       	    PORTDbits.RD1 = M11_ Top                   
PORTDbits.RD2 = Bezet_In_5B_ Top                PORTDbits.RD2 = Bezet_Weerstand_ Top                PORTDbits.RD2 = Bezet_Weerstand_ Top
PORTDbits.RD3 = Bezet_In_6_ Top	                PORTDbits.RD3 = M10_ Bottom	                        PORTDbits.RD3 = MIP50_ENABLE Bottom	
PORTDbits.RD4 = Bezet_In_7_ Top                 PORTDbits.RD4 = M11_ Bottom                         PORTDbits.RD4 = M11_ Bottom
PORTDbits.RD5 = SERIAL CLOCK I^2C               PORTDbits.RD5 = SERIAL CLOCK I^2C					PORTDbits.RD5 = SERIAL CLOCK I^2C					
PORTDbits.RD6 = SERIAL DATA I^2C                PORTDbits.RD6 = SERIAL DATA I^2C                    PORTDbits.RD6 = SERIAL DATA I^2C                   
PORTDbits.RD7 = Bezet_Weerstand_ Top            PORTDbits.RD7 = Bezet_Weerstand_ Bottom             PORTDbits.RD7 = Bezet_Weerstand_ Bottom                   				
												5.0V, 8mA                                           5.0V, 8mA                                                                                                   
												                                                    
												OPTO_BOARD_3_PG1									OPTO_BOARD_3_PG7										
PORTEbits.RE0 = F10_ Bottom                     PORTEbits.RE0 = Bezet_In_5B_ Bottom                 PORTEbits.RE0 = Bezet_In_5B_ TOP
PORTEbits.RE1 = F12_ Bottom                     PORTEbits.RE1 = Bezet_In_6_ Bottom                  PORTEbits.RE1 = Bezet_In_6_ TOP
PORTEbits.RE2 = Bezet_Uit_5B_ Bottom            PORTEbits.RE2 = Bezet_In_7_ Bottom					PORTEbits.RE2 = Bezet_In_7_ TOP
PORTEbits.RE3 = Bezet_Uit_6_ Bottom             PORTEbits.RE3 = Bezet_Uit_5B_ Bottom                PORTEbits.RE3 = Bezet_In_SPare TOP         	
PORTEbits.RE4 = Bezet_Uit_7_ Bottom             PORTEbits.RE4 = Bezet_Uit_6_ Bottom                 PORTEbits.RE4 = Bezet_In_16B_ Bottom           
PORTEbits.RE5 = Bezet_Uit_8A_ Bottom            PORTEbits.RE5 = Bezet_Uit_7_ Bottom                 PORTEbits.RE5 = Bezet_In_17_ Bottom             
PORTEbits.RE6 = IO_Expander_Reset               PORTEbits.RE6 = Bezet_Uit_8A_ Bottom                PORTEbits.RE6 = Bezet_In_18_ Bottom                              
PORTEbits.RE7 = Externall_WDT                   PORTEbits.RE7 = BEZET_SPARE / Led4                  PORTEbits.RE7 = Bezet_In_Spare Bottom         	
												5.0V, 8mA                                           5.0V, 8mA  
												                                                    
                                                OPTO_BOARD_1_BOT_PG1                                OPTO_BOARD_1_BOT_PG4                                                                  
PORTFbits.RF0 = CL_10_ bit4 Bottom              PORTFbits.RF0 = CL_10_ bit1 Bottom                  PORTFbits.RF0 = CL_10_ bit1 Bottom                                
PORTFbits.RF1 = CL_10_Heart_ Bottom             PORTFbits.RF1 = CL_10_ bit2 Bottom                  PORTFbits.RF1 = CL_10_ bit2 Bottom                                
PORTFbits.RF2 = BM_10_ Bottom                   PORTFbits.RF2 = CL_10_ bit3 Bottom                  PORTFbits.RF2 = CL_10_ bit3 Bottom
PORTFbits.RF3 = F11_ Bottom                     PORTFbits.RF3 = CL_10_ bit4 Bottom                  PORTFbits.RF3 = CL_10_ bit4 Bottom  
PORTFbits.RF4 = EOS_10_ Bottom                  PORTFbits.RF4 = CL_10_Heart_ Bottom                 PORTFbits.RF4 = CL_10_Heart_ Bottom 
PORTFbits.RF5 = EOS_11_ Bottom                  PORTFbits.RF5 = BM_10_ Bottom                       PORTFbits.RF5 = SPARE_INPUT Bottom
PORTFbits.RF6 = BM_11_ Bottom                   PORTFbits.RF6 = BM_11_ Bottom                       PORTFbits.RF6 = SPARE_INPUT Bottom
PORTFbits.RF7 = Bridge_10R_ Bottom              PORTFbits.RF7 = EOS_11_ Bottom                      PORTFbits.RF7 = EOS_21_ Bottom
												3.3V, 2mA                                           3.3V, 2mA
                                                                                                    
												OPTO_BOARD_1_TOP_PG1                                OPTO_BOARD_1_TOP_PG3
PORTGbits.RG0 = PWM Top                         PORTGbits.RG0 = PWM Top                             PORTGbits.RG0 = SPARE_INPUT Top
PORTGbits.RG1 = Brake Top                       PORTGbits.RG1 = Bridge_10L_ Top                     PORTGbits.RG1 = TX2
PORTGbits.RG2 = Bezet_Uit_7_ Top                PORTGbits.RG2 = Brake Top                           PORTGbits.RG2 = RX2
PORTGbits.RG3 = Bezet_Uit_8A_ Top               PORTGbits.RG3 = Bridge_10R_ Top                     PORTGbits.RG3 = F13_ Top
PORTGbits.RG4 = Bridge_10L_ Bottom              PORTGbits.RG4 = F10_ Top                            PORTGbits.RG4 = F10_ Top
PORTGbits.RG5 = CL_10_ bit1 Bottom              PORTGbits.RG5 = F11_ Top                            PORTGbits.RG5 = F11_ Top
PORTGbits.RG6 = CL_10_ bit2 Bottom              PORTGbits.RG6 = F12_ Top                            PORTGbits.RG6 = F12_ Top
PORTGbits.RG7 = CL_10_ bit3 Bottom              PORTGbits.RG7 = EOS_10_ Top                         PORTGbits.RG7 = EOS_10_ Top
												5.0V, 2mA                                           5.0V, 2mA
                                                                                                    
												OPTO_BOARD_3_PG1                                    OPTO_BOARD_3_PG7
PORTHbits.RH0 = EOS_10_ Top                     PORTHbits.RH0 = Bezet_In_5B_ Top                    PORTHbits.RH0 = Bezet_Uit_5B Top 
PORTHbits.RH1 = EOS_11_ Top                     PORTHbits.RH1 = Bezet_In_6_ Top	                    PORTHbits.RH1 = Bezet_Uit_6 Top	 
PORTHbits.RH2 = BM_11_ Top                      PORTHbits.RH2 = Bezet_In_7_ Top                     PORTHbits.RH2 = Bezet_Uit_7 Top
PORTHbits.RH3 = Bridge_10R_ Top                 PORTHbits.RH3 = Bezet_Uit_5B_ Top                   PORTHbits.RH3 = Bezet_Uit_8A Top
PORTHbits.RH4 = F10_ Top                        PORTHbits.RH4 = Bezet_Uit_6_ Top                    PORTHbits.RH4 = Bezet_Uit_16B Bottom
PORTHbits.RH5 = F12_ Top                        PORTHbits.RH5 = Bezet_Uit_7_ Top                    PORTHbits.RH5 = Bezet_Uit_17 Bottom
PORTHbits.RH6 = Bezet_Uit_5B_ Top               PORTHbits.RH6 = Bezet_Uit_8A_ Top                   PORTHbits.RH6 = Bezet_Uit_18 Bottom
PORTHbits.RH7 = Bezet_Uit_6_ Top                PORTHbits.RH7 = BEZET_SPARE / Led5                  PORTHbits.RH7 = Bezet_Uit_19A Bottom
												3.3V, 2mA                                           3.3V, 2mA
                                                                                                    
												OPTO_BOARD_1_TOP_PG1                                OPTO_BOARD_1_TOP_PG3
PORTJbits.RJ0 = Bridge_10L_ Top                 PORTJbits.RJ0 = CL_10_ bit1 Top                     PORTJbits.RJ0 = CL_10_ bit1 Top
PORTJbits.RJ1 = CL_10_ bit1 Top                 PORTJbits.RJ1 = CL_10_ bit2 Top                     PORTJbits.RJ1 = CL_10_ bit2 Top
PORTJbits.RJ2 = CL_10_ bit2 Top                 PORTJbits.RJ2 = CL_10_ bit3 Top                     PORTJbits.RJ2 = CL_10_ bit3 Top
PORTJbits.RJ3 = CL_10_ bit3 Top                 PORTJbits.RJ3 = CL_10_ bit4 Top                     PORTJbits.RJ3 = CL_10_ bit4 Top
PORTJbits.RJ4 = CL_10_ bit4 Top                 PORTJbits.RJ4 = CL_10_Heart_ Top                    PORTJbits.RJ4 = CL_10_Heart_ Top
PORTJbits.RJ5 = CL_10_Heart_ Top                PORTJbits.RJ5 = BM_10_ Top                          PORTJbits.RJ5 = SPARE_INPUT Top
PORTJbits.RJ6 = BM_10_ Top                      PORTJbits.RJ6 = BM_11_ Top                          PORTJbits.RJ6 = SPARE_INPUT Top
PORTJbits.RJ7 = F11_ Top                        PORTJbits.RJ7 = EOS_11_ Top                         PORTJbits.RJ7 = EOS_11_ Top
												5.0V, 8mA                                           5.0V, 8mA


