using iText.Kernel.Pdf;
using System;

namespace ToolBoxPDF.Core.IO
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

        public PageRotation(PageOrientation orientation) : this(orientation.DegAngle) { }

        public void Transform(PdfPage page)
        {
            transform.Invoke(page);
        }
    }

    public class PageOrientation
    {
        public int DegAngle { get; private set; }

        private PageOrientation(int degAngle)
        {
            DegAngle = degAngle;
        }

        /// <summary>
        /// Rotates the pageOrientation instance clockwise.
        /// </summary>
        /// <param name="pageOrientation"></param>
        /// <returns></returns>
        public static PageOrientation operator ++(PageOrientation pageOrientation)
        {
            pageOrientation.DegAngle += 90;
            if (pageOrientation.DegAngle > 180)
            {
                pageOrientation.DegAngle -= 360;
            }
            return pageOrientation;
        }

        public static PageOrientation NoRotation()
        {
            return new PageOrientation(0);
        }

        public static PageOrientation RotateLeft()
        {
            return new PageOrientation(-90);
        }

        public static PageOrientation RotateRight()
        {
            return new PageOrientation(90);
        }

        public static PageOrientation UpsideDown()
        {
            return new PageOrientation(180);
        }
    }
}
