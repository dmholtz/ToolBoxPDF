using System;
using System.Threading.Tasks;
using Windows.Data.Pdf;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace UWPApp.UIElementFrames
{
    /// <summary>
    /// Renders a PdfDocument's page
    /// </summary>
    public sealed partial class RenderedPagePreview : Page
    {
        public static RenderedPagePreview Current;
        private static int defaultMaxHeight = 500;

        public bool BorderVisible { get; set; }
        public Brush PageRenderColor { get; set; }

        public PdfDocument RenderedDocument { get; private set; }
        public int RenderedRotationAngle { get; private set; }
        public uint RenderedPageNumber { get { return renderedPageIndex + 1; } }

        private uint renderedPageIndex;


        public RenderedPagePreview()
        {
            this.InitializeComponent();
            Current = this;
            MaxHeight = defaultMaxHeight;

            BorderVisible = true;
            PageRenderColor = new SolidColorBrush(Colors.White);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            PdfDocument renderedDocument = e.Parameter as PdfDocument;
            if (renderedDocument == null)
            {
                throw new ArgumentException("The parameter must be a PdF document and must not be null");
            }
            else
            {
                this.RenderedDocument = renderedDocument;
            }
        }

        /// <summary>
        /// Sets the render options (page number and rotation angle) for this render instance.
        /// Renders the PdfPage according to the render options.
        /// </summary>
        /// <param name="pageNumber">1-indexed</param>
        /// <param name="rotationAngle">only orthogonal rotations allowed</param>
        /// <returns></returns>
        public async Task RenderPageAsync(uint pageNumber = 1, int rotationAngle = 0)
        {
            if (pageNumber < 1 || pageNumber > RenderedDocument.PageCount)
            {
                throw new ArgumentException("pageNumber out of range.");
            }
            if (rotationAngle % 90 != 0)
            {
                throw new ArgumentException("Invalid rotationAnlge: no orthogonal rotation.");
            }
            renderedPageIndex = pageNumber - 1;
            RenderedRotationAngle = rotationAngle;
            await BackgroundPageRenderAsync();
        }

        /// <summary>
        /// Internal render method
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <param name="rotationAngle"></param>
        /// <returns></returns>
        private async Task BackgroundPageRenderAsync()
        {

            using (PdfPage page = RenderedDocument.GetPage(renderedPageIndex))
            {
                var renderOptions = new PdfPageRenderOptions();
                renderOptions.BackgroundColor = Windows.UI.Colors.Transparent;
                double actualWidth = PreviewArea.ActualWidth - PreviewBorder.Margin.Left - PreviewBorder.Margin.Right - 4;
                ScaledRectangle displayedPageDimension;

                if (RenderedRotationAngle % 180 == 0)
                {
                    // Raw page orientation matches displayed page orientation
                    displayedPageDimension = new ScaledRectangle(page.Size.Height, page.Size.Width);
                }
                else
                {
                    // Raw page orientation does not match the displayed page orientation
                    displayedPageDimension = new ScaledRectangle(page.Size.Width, page.Size.Height);
                }

                double stretchedHeight = displayedPageDimension.GetScaledHeight(actualWidth);

                if (stretchedHeight > MaxHeight)
                {
                    renderOptions.DestinationHeight = (uint)(MaxHeight);
                    renderOptions.DestinationWidth = (uint)displayedPageDimension.GetScaledWidth(MaxHeight);
                }
                else
                {
                    renderOptions.DestinationHeight = (uint) stretchedHeight;
                    renderOptions.DestinationWidth = (uint) actualWidth;
                }

                // update decent border around the previewed page
                PreviewBorder.Height = renderOptions.DestinationHeight;
                PreviewBorder.Width = renderOptions.DestinationWidth;

                var stream = new InMemoryRandomAccessStream();
                await page.RenderToStreamAsync(stream, renderOptions);
                BitmapImage src = new BitmapImage();
                await src.SetSourceAsync(stream);
                Preview.Source = src;

                RotateTransform rotationTransform = new RotateTransform()
                {
                    Angle = RenderedRotationAngle,
                    CenterX = (PreviewBorder.Width - 4) / 2,
                    CenterY = (PreviewBorder.Height - 4) / 2
                };

                ScaleTransform scaleTransform;
                if (RenderedRotationAngle % 180 == 0)
                {
                    scaleTransform = new ScaleTransform()
                    {
                        ScaleX = 1,
                        ScaleY = 1,
                    };
                }
                else
                {
                    scaleTransform = new ScaleTransform()
                    {
                        CenterX = (PreviewBorder.Width - 4) / 2,
                        CenterY = (PreviewBorder.Height - 4) / 2,
                        ScaleX = displayedPageDimension.AspectRatio,
                        ScaleY = 1 / displayedPageDimension.AspectRatio,
                    };
                }

                TransformGroup renderTransform = new TransformGroup();
                renderTransform.Children.Add(rotationTransform);
                renderTransform.Children.Add(scaleTransform);

                Preview.RenderTransform = renderTransform;
                SetBorderStyle();
            }
        }

        /// <summary>
        /// Whenever the size of the preview area changes, update the page rendering
        /// </summary>
        private async void PreviewAreaSizeChangedAsync(object sender, SizeChangedEventArgs e)
        {
            if (RenderedDocument != null)
            {
                await BackgroundPageRenderAsync();
            }
        }

        private void SetBorderStyle()
        {
            if (BorderVisible)
            {
                PreviewBorder.BorderBrush = new SolidColorBrush(Colors.Black);
            }
            else
            {
                PreviewBorder.BorderBrush = new SolidColorBrush(Colors.Transparent);
            }
            PreviewBorder.Background = PageRenderColor;
        }

        private class ScaledRectangle
        {
            public double Height { get; }
            public double Width { get; }

            /// <summary>
            /// Aspect Ratio of the rectangle: Width / Height
            /// </summary>
            public double AspectRatio { get; }

            public ScaledRectangle(double height, double width)
            {
                Height = height;
                Width = width;
                AspectRatio = width / height;
            }

            public double GetScaledWidth(double height)
            {
                return height / Height * Width;
            }

            public double GetScaledHeight(double width)
            {
                return width / Width * Height;
            }
        }
    }
}
