using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace UniPDF_UWP.FileManagement
{
    public class FileSize
    {
        private static readonly uint defaultDecimals = 2;

        /// <summary>
        /// Number of bytes of the file
        /// </summary>
        private readonly ulong length;

        /// <summary>
        /// Decimal representation of the file size with respecte to the corresponding unit
        /// </summary>
        private double size;
        private FileSizeUnit unit;

        public FileSize(ulong length)
        {
            this.length = length;
            ComputeDecimalRepresentation();
        }

        private void ComputeDecimalRepresentation()
        {
            if (length < 10)
            {
                size = length;
                unit = FileSizeUnit.Byte;
            }
            else if (length < 100 * (double)FileSizeUnit.kB)
            {
                size = length / (double)FileSizeUnit.kB;
                unit = FileSizeUnit.kB;
            }
            else if (length < 100 * (double)FileSizeUnit.MB)
            {
                size = length / (double)FileSizeUnit.MB;
                unit = FileSizeUnit.MB;
            }
            else
            {
                size = length / (double)FileSizeUnit.GB;
                unit = FileSizeUnit.GB;
            }
        }

        public string ToString(uint decimals)
        {
            double roundedSize = Math.Round(size, (int)decimals);
            return roundedSize.ToString() + " " + unit.ToString();
        }

        public override string ToString()
        {
            return this.ToString(defaultDecimals);
        }

        public enum FileSizeUnit : ulong
        {
            Byte = 1,
            kB = 1000,
            MB = 1000000,
            GB = 1000000000
        }
    }
}
