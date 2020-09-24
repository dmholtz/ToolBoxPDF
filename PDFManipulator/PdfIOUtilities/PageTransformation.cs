using iText.IO.Font.Otf;
using iText.Kernel.Pdf;
using Org.BouncyCastle.Crypto.Modes.Gcm;
using System;
using System.Collections.Generic;
using System.Text;

namespace PdfManipulator.PdfIOUtilities
{
    public interface IPageTransformation
    {
        void Transform(PdfPage page);
    }

    public class PageRotation : IPageTransformation
    {
        private readonly Action<PdfPage> transform;

        public PageRotation(int degAngle)
        {
            if (degAngle % 90 != 0)
            {
                throw new ArgumentException("Rotation angle must be completely divisible by 90.");
            }
            transform = page => page.SetRotation(degAngle);
        }

        public PageRotation(PageOrientation orientation) : this((int)orientation) { }

        public void Transform(PdfPage page)
        {
            transform.Invoke(page);
        }
    }

    public enum PageOrientation
    {
        NoRotation = 0,
        RotateLeft = -90,
        RotateRight = 90,
        UpsideDown = 180
    }
}
