using ToolBoxPDF.Core.PageRangePackage;
using System;
using System.Collections.Generic;

namespace ToolBoxPDF.Core.IO
{
    /// <summary>
    /// ExportTask bundles a PangeRange instance and a list of transforms which are applied to the every page of the selection.
    /// </summary>
    public class ExportTask
    {
        public PageRange Pages {get;}
        public List<IPageTransformation> TransformPipeline { get; }

        /// <summary>
        /// Export task with transformation pipeline.
        /// </summary>
        /// <param name="transformPipeline"></param>
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
                TransformPipeline = transformPipeline;
            }
        }

        /// <summary>
        /// Export task with single transformation
        /// </summary>
        /// <param name="singelTransformation"></param>
        public ExportTask(PageRange pages, IPageTransformation singelTransformation) : 
            this(pages, new List<IPageTransformation>() { singelTransformation }) { }

        /// <summary>
        /// Export task with no transformation
        public ExportTask(PageRange pages) :
            this(pages, new List<IPageTransformation>() { })
        { }
    }
}
