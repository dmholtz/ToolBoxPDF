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
        public double Size { get; private set; }
        public FileSizeUnit Unit { get; private set; }

        public FileSize(ulong length)
        {
            this.length = length;
            ComputeDecimalRepresentation();
        }

        public static FileSize ZeroBytes()
        {
            return new FileSize(0);
        }

        public static FileSize operator +(FileSize a, FileSize b)
        {
            return new FileSize(a.length + b.length);
        }

        private void ComputeDecimalRepresentation()
        {
            if (length < 10)
            {
                Size = length;
                Unit = FileSizeUnit.Byte;
            }
            else if (length < 100 * (double)FileSizeUnit.kB)
            {
                Size = length / (double)FileSizeUnit.kB;
                Unit = FileSizeUnit.kB;
            }
            else if (length < 100 * (double)FileSizeUnit.MB)
            {
                Size = length / (double)FileSizeUnit.MB;
                Unit = FileSizeUnit.MB;
            }
            else
            {
                Size = length / (double)FileSizeUnit.GB;
                Unit = FileSizeUnit.GB;
            }
        }

        public string ToString(uint decimals)
        {
            double roundedSize = Math.Round(Size, (int)decimals);
            return roundedSize.ToString() + " " + Unit.ToString();
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
