using PdfManipulator.PageRangePackage;
using System;
using System.Collections.Generic;

namespace PdfManipulator.PdfExportUtilities
{
    /// <summary>
    /// ExportTask bundles a PangeRange instance and a list of transforms which are applied to the entire task.
    /// </summary>
    public class ExportTask
    {
        public PageRange Pages {get;}
        public List<IPageTransformation> TransformPipeline { get; }

        public ExportTask(PageRange pages, List<IPageTransformation> transformPipeline)
        {
            Pages = pages ?? throw new ArgumentException("PageRange must not be null");
            if(transformPipeline is null)
            {
                // Interpret null-transformPipeline argument as no transformation
                TransformPipeline = new List<IPageTransformation>();
            }
            else
            {
                TransformPipeline = transformPipeline ?? throw new Exception();
            }
        }
    }
}
