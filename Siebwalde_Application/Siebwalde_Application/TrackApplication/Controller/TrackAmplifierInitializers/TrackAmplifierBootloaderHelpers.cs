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

        private string PathToFile = null;
        private StreamReader sr;
        private ushort FileCheckSum = 0;
        private List<byte[][]>HexData = new List<byte[][]> { };

        #endregion

        #region Constructor

        /// <summary>
        /// 
        /// </summary>
        public TrackAmplifierBootloaderHelpers(string path)
        {
            PathToFile = path;
            StreamReader sr = new StreamReader(PathToFile);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get the HexFileData
        /// </summary>
        public List<byte[][]>GetHexFileData { get { return HexData; } }
        /// <summary>
        /// Get the File CheckSum
        /// </summary>
        public ushort GetFileCheckSum { get { return FileCheckSum; } }

        /// <summary>
        /// Start the reading process and get all the info
        /// </summary>
        public uint Start()
        {
            ushort ProcessLines = Convert.ToUInt16((Enums.PROGMEMSIZE - Enums.BOOTLOADEROFFSET) / Enums.HEXROWWIDTH);
            string line;
            string buffer;
            // INTEL HEX format + address of used PIC is not bigger then 4 bytes
            byte[] address = new byte[4];
            byte[] data = new byte[32];

            try
            {
                //byte[] HexFile = File.ReadAllBytes(@"c:\localdata\Siebwalde\TrackAmplifier4.X.production.hex");
                if (File.Exists(PathToFile))
                {
                    // Getting the HexData of the source file
                    using (StreamReader sr = new StreamReader(PathToFile))
                    {
                        for (uint i = 0; i < ProcessLines; i++)
                        {
                            line = sr.ReadLine();
                            buffer = line.Substring(3, 4);
                            address = StringToByteArray(buffer);
                            buffer = line.Substring(9, 32);
                            data = StringToByteArray(buffer);
                            //Array.Copy(line, 3, address, 0, 4);
                            //Array.Copy(line, 9, data, 0, 32);
                            HexData.Add(new byte[][] { address, data });
                        }
                    }

                    // Getting the Checksum of the source file
                    uint count = 0;
                    foreach (byte[][] field in HexData)
                    {
                        count++;
                        for (uint i = 0; i < 16; i+=2)
                        {
                            // Checksum does not include the checksum location itself to be added, therefore skip the last 2 bytes.
                            if(!(count == ProcessLines && i == 14))
                            {
                                FileCheckSum = Convert.ToUInt16((field[1][i] + (field[1][i + 1] << 8) + FileCheckSum) & 0xFFFF);
                            }
                            else
                            {
                                FileCheckSum = Convert.ToUInt16(FileCheckSum & 0xFFFF);
                            }
                        }                        
                    }
                }
                else
                {
                    MessageBox.Show(GetType().Name + "The expected Slave firmware file " + PathToFile + " could not be found!");
                    return Enums.Error;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return Enums.Error;
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
