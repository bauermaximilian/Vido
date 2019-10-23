using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Vido
{
    /// <summary>
    /// Loads an <see cref="Image"/> from a file.
    /// </summary>
    /// <param name="path">
    /// The path to the image file to be imported.
    /// </param>
    /// <returns>
    /// A new <see cref="Image"/> instance.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Is thrown when <paramref name="path"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Is thrown when <paramref name="path"/> has an invalid file format
    /// or couldn't be read.
    /// </exception>
    public delegate Image ImportImage(string path);

    /// <summary>
    /// Provides importers for various image formats. 
    /// </summary>
    public class Importer
    {
        /// <summary>
        /// Defines the amount of pixels an image can have until it's 
        /// downscaled before drawn to screen to reach that amount of pixels
        /// and prevent a laggy animation.
        /// </summary>
        private const int MaxPixels = 4000000;

        /// <summary>
        /// Defines if large images should be downscaled for smoother 
        /// animation.
        /// </summary>
        private const bool DownscaleOversizedImages = true;

        private Dictionary<string, ImportImage> importers =
            new Dictionary<string, ImportImage>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Importer"/> class,
        /// featuring support for the default image formats JPG/JPEG, PNG,
        /// (animated) GIF, BMP and TIF/TIFF.
        /// </summary>
        public Importer()
        {
            ImportImage defaultImporter = delegate (string path)
            {
                if (path == null)
                    throw new ArgumentNullException("path");

                Image source = Image.FromFile(path);

                //Downscale big images right in the importer to improve 
                //performance (only if image is not animated).
                double scaleFactor = Math.Min(1,
                    (double)MaxPixels / (source.Width * source.Height));

                if (AnimatedImage.GetFrameCount(source) > 1 
                    || scaleFactor == 1.0
                    || !DownscaleOversizedImages)
                    return source;

                try
                {
                    Image image = new Bitmap((int)(source.Width * scaleFactor),
                        (int)(source.Height * scaleFactor));

                    using (Graphics graphics = Graphics.FromImage(image))
                    {
                        graphics.InterpolationMode = 
                            InterpolationMode.HighQualityBicubic;
                        graphics.DrawImage(source, 0, 0, image.Width, 
                            image.Height);
                    }

                    return image;
                }
                finally
                {
                    if (source != null) source.Dispose();
                }
            };

            importers.Add("jpg", defaultImporter);
            importers.Add("jpeg", defaultImporter);
            importers.Add("png", defaultImporter);
            importers.Add("gif", defaultImporter);
            importers.Add("bmp", defaultImporter);
            importers.Add("tif", defaultImporter);
            importers.Add("tiff", defaultImporter);
        }

        /// <summary>
        /// Checks if the specified file extension is supported.
        /// </summary>
        /// <param name="fileExtension">
        /// The file extension with or without the preceding period.
        /// This parameter is case-insensitive.
        /// </param>
        /// <returns>
        /// <c>true</c> if files with the specified extension are supported,
        /// <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="fileExtension"/> is null.
        /// </exception>
        public bool Supports(string fileExtension)
        {
            return Supports(fileExtension, out ImportImage nil);
        }

        /// <summary>
        /// Checks if the specified file extension is supported and returns
        /// a suitable <see cref="ImportImage"/> function, if possible.
        /// </summary>
        /// <param name="fileExtension">
        /// The file extension with or without the preceding period.
        /// This parameter is case-insensitive.
        /// </param>
        /// <param name="importerFunction">
        /// Will be used to store the <see cref="ImportImage"/> function,
        /// if the format is supported, or null if the format is not supported.
        /// </param>
        /// <returns>
        /// <c>true</c> if files with the specified extension are supported,
        /// <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="fileExtension"/> is null.
        /// </exception>
        public virtual bool Supports(string fileExtension,
            out ImportImage importerFunction)
        {
            if (fileExtension == null)
                throw new ArgumentNullException("fileExtension");

            fileExtension = fileExtension.TrimStart('.').ToLowerInvariant();

            return importers.TryGetValue(fileExtension, out importerFunction);
        }
    }
}
