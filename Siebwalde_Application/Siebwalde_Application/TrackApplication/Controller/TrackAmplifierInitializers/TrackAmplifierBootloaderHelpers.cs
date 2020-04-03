using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace Siebwalde_Application
{
    public class TrackAmplifierBootloaderHelpers
    {

        #region Local variables

        private ILogger mTrackApplicationLogging;
        private string PathToFile = null;
        private StreamReader sr;
        private bool ConfigWordReadSuccessful;
        private byte[] mGetConfigWord;

        #endregion

        #region Constructor

        /// <summary>
        /// constructor
        /// </summary>
        public TrackAmplifierBootloaderHelpers(string path, ILogger trackApplicationLogging)
        {
            mTrackApplicationLogging = trackApplicationLogging;
            ConfigWordReadSuccessful = false;
            PathToFile = path;
            try
            {
                StreamReader sr = new StreamReader(PathToFile);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get the HexFileData
        /// </summary>
        public List<byte[][]> GetHexFileData { get; } = new List<byte[][]> { };

        /// <summary>
        /// Get the File CheckSum
        /// </summary>
        public ushort GetFileCheckSum { get; private set; } = 0;

        /// <summary>
        /// Get the config word
        /// </summary>
        public byte[] GetConfigWord 
        {
            get
            {
                return (byte[])mGetConfigWord.Clone();
            }
            private set
            {
                mGetConfigWord = value;
            } 
        }

        /// <summary>
        /// Get the File CheckSum
        /// </summary>
        public bool HexFileReadSuccessful { get; private set; } = false;

        /// <summary>
        /// Start the reading process and get all the info
        /// </summary>
        public uint Start()
        {
            ushort ProcessLines = Convert.ToUInt16((Enums.PROGMEMSIZE - Enums.BOOTLOADEROFFSET) / Enums.HEXROWWIDTH);
            string line;
            string buffer;
            // INTEL HEX format + address of used PIC is not bigger then 4 bytes
            HexFileReadSuccessful = false;

            try
            {
                if (File.Exists(PathToFile))
                {
                    mTrackApplicationLogging.Log(GetType().Name, "Hex file " + PathToFile + " found, start reading...");
                    // Getting the HexData of the source file
                    using (StreamReader sr = new StreamReader(PathToFile))
                    {
                        for (uint i = 0; i < ProcessLines; i++)
                        {
                            line = sr.ReadLine();
                            buffer = line.Substring(3, 4);
                            byte[] address = StringToByteArray(buffer);
                            buffer = line.Substring(9, 32);
                            byte[] data = StringToByteArray(buffer);
                            GetHexFileData.Add(new byte[][] { address, data });
                        }
                        mTrackApplicationLogging.Log(GetType().Name, "Hex file slave FW data acquired, read config word...");

                        uint loopcounter = 0;
                        bool run = true;
                        while (run)
                        {
                            loopcounter++;

                            line = sr.ReadLine();
                            buffer = line.Substring(1, 2);
                            //Console.WriteLine(buffer.ToCharArray());
                            if (buffer == "0C")
                            {
                                run = false;
                                buffer = line.Substring(9, 24);
                                //Console.WriteLine(buffer.ToCharArray());
                                GetConfigWord = StringToByteArray(buffer);
                                ConfigWordReadSuccessful = true;
                            }

                            if(loopcounter > 1000)
                            {
                                mTrackApplicationLogging.Log(GetType().Name, "Failed to aquire Config word.");
                                ConfigWordReadSuccessful = false;
                                run = false;
                            }
                        }
                        mTrackApplicationLogging.Log(GetType().Name, "Config word acquired, calculating checksum on slave FW data...");

                    }

                    // Getting the Checksum of the source file
                    uint count = 0;
                    foreach (byte[][] field in GetHexFileData)
                    {
                        count++;
                        for (uint i = 0; i < 16; i+=2)
                        {
                            // Checksum does not include the checksum location itself to be added, therefore skip the last 2 bytes.
                            if(!(count == ProcessLines && i == 14))
                            {
                                GetFileCheckSum = Convert.ToUInt16((field[1][i] + (field[1][i + 1] << 8) + GetFileCheckSum) & 0xFFFF);
                            }
                            else
                            {
                                GetFileCheckSum = Convert.ToUInt16(GetFileCheckSum & 0xFFFF);
                            }
                        }                        
                    }
                    mTrackApplicationLogging.Log(GetType().Name, "Checksum of slave FW data acquired.");
                }
                else
                {
                    MessageBox.Show(GetType().Name + "The expected Slave firmware file " + PathToFile + " could not be found!");
                    mTrackApplicationLogging.Log(GetType().Name, "The expected Slave firmware file " + PathToFile + " could not be found!");
                    HexFileReadSuccessful = false;
                    return Enums.Error;
                }
            }
            catch (Exception e)
            {
                mTrackApplicationLogging.Log(GetType().Name, "Exception occured within this program!");
                HexFileReadSuccessful = false;
                MessageBox.Show(e.Message);
                return Enums.Error;
            }

            if(ConfigWordReadSuccessful && GetFileCheckSum != 0)
            {
                HexFileReadSuccessful = true;
                mTrackApplicationLogging.Log(GetType().Name, "HexFileReadSuccessful > Finished.");
            }            
            return Enums.Finished;
        }

        #endregion

        #region Hex string to byte array Converter

        internal static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        #endregion
    }
}
