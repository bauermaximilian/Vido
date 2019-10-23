using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Threading;
using System.Windows.Forms;
using Vido.Properties;

namespace Vido
{
    /// <summary>
    /// Provides an image viewer control with zoom and offset control.
    /// </summary>
    public class ImageViewer : Control
    {
        /// <summary>
        /// Defines how much pixels the longest side of the image must have
        /// at least when scaled down. 
        /// </summary>
        /// <example>
        /// For an image with equal height and width, a value of 10 would
        /// mean that the image can be scaled down with the zoom function
        /// until the displayed size of the image is 10x10px.
        /// For images with different values for height and width, it is 
        /// ensured that no side of the image becomes smaller than 10px.
        /// </example>
        private const double ImageZoomedMinPixels = 50;

        /// <summary>
        /// Defines the value which is multiplied with the current zoom level
        /// to calculate the size of a single zoom step (e.g. for mouse wheel
        /// or keyboard events).
        /// </summary>
        private const double ScrollDampValue = 0.18;

        /// <summary>
        /// Defines the offset step size in pixels.
        /// </summary>
        private const int OffsetStepSizePx = 60;

        /// <summary>
        /// Defines the maximum value for the <see cref="Zoom"/> property.
        /// </summary>
        private const double ImageZoomedMax = 4;

        private const double UpdatesPerSecond = 40;
        private const double ZoomUpdateTreshold = 0.005;
        private const double OffsetUpdateTreshold = 0.5;

        private AnimationSpeed currentAnimationSpeed = AnimationSpeed.Medium;
        private AnimationSpeedDefinition currentAnimationSpeedDefinition =
            AnimationSpeedDefinition.Medium;

        private AnimatedImage CurrentImage
        {
            get
            {
                if (imageTransitionProgressMs <=
                    currentAnimationSpeedDefinition.ImageTransitionTimeMs / 2.0
                    || nextImage == null) return mainImage;
                else return nextImage;
            }
        }

        private AnimatedImage mainImage;
        private volatile AnimatedImage nextImage;
        private bool boundsResetFlag = false;

        /// <summary>
        /// Gets a boolean which indicates whether the current picture with
        /// the <see cref="Zoom"/> and <see cref="Offset"/> applied protrudes
        /// the left side of the image viewer.
        /// </summary>
        public bool ImageExceedsLeftSide
        {
            get
            {
                return cachedImageRectangle.Left < 0;
            }
        }

        /// <summary>
        /// Gets a boolean which indicates whether the current picture with
        /// the <see cref="Zoom"/> and <see cref="Offset"/> applied protrudes
        /// the right side of the image viewer.
        /// </summary>
        public bool ImageExceedsRightSide
        {
            get
            {
                return cachedImageRectangle.Right > Width;
            }
        }

        /// <summary>
        /// Gets a boolean which indicates whether the current picture with
        /// the <see cref="Zoom"/> and <see cref="Offset"/> applied protrudes
        /// the left and/or right side of the image viewer.
        /// </summary>
        public bool ImageExceedsHorizontalBoundaries
        {
            get
            {
                return ImageExceedsLeftSide || ImageExceedsRightSide;
            }
        }

        /// <summary>
        /// Gets a boolean which indicates whether the current picture with
        /// the <see cref="Zoom"/> and <see cref="Offset"/> applied protrudes
        /// the upper and/or lower side of the image viewer.
        /// </summary>
        public bool ImageExceedsVerticalBoundaries
        {
            get
            {
                return cachedImageRectangle.Top < 0 
                    || cachedImageRectangle.Bottom > Height;
            }
        }

        /// <summary>
        /// Gets a boolean which indicates whether the current picture with
        /// the <see cref="Zoom"/> and <see cref="Offset"/> applied protrudes
        /// any side of the image viewer.
        /// </summary>
        public bool ImageExceedsBoundaries
        {
            get
            {
                return ImageExceedsHorizontalBoundaries ||
                    ImageExceedsVerticalBoundaries;
            }
        }

        /// <summary>
        /// Gets the zoom mode for the current 
        /// <see cref="ImageViewer"/>, which defines if and how the image is 
        /// scaled automatically to fill the available space.
        /// </summary>
        public ZoomMode ZoomMode { get; private set; }
                
        /// <summary>
        /// Gets the current zoom value, which is used to scale the image 
        /// proportionally. The value can either be automatically calculated
        /// depending on the current value of <see cref="ZoomMode"/> or
        /// changed with the appropirate methods.
        /// </summary>
        public double Zoom { get; private set; }

        /// <summary>
        /// Gets the current offset of the image.
        /// </summary>
        public PointF Offset { get; private set; }

        /// <summary>
        /// Gets a value which indicates whether any transition animations
        /// for changed zoom values are currently played back.
        /// </summary>
        public bool HasRunningZoomAnimations
        {
            get => Math.Abs(animatedZoom - Zoom) > ZoomUpdateTreshold;
        }

        /// <summary>
        /// Gets a value which indicates whether the currently displayed image
        /// is changed to a new one.
        /// </summary>
        public bool HasRunningImageTransitionAnimation
        {
            get => imageTransitionProgressMs > 0;
        }

        /// <summary>
        /// Gets a boolean which indicates whether any transition animations
        /// for changed offset values are currently played back.
        /// </summary>
        public bool HasRunningOffsetAnimations
        {
            get => HasRunningHorizontalOffsetAnimation ||
                HasRunningVerticalOffsetAnimation;
        }

        /// <summary>
        /// Gets a boolean which indicates whether a transition animation for
        /// a change of the X component of offset value is currently 
        /// played back.
        /// </summary>
        public bool HasRunningHorizontalOffsetAnimation
        {
            get =>
                Math.Abs(animatedOffset.X - Offset.X) > OffsetUpdateTreshold;
        }

        /// <summary>
        /// Gets a boolean which indicates whether a transition animation for
        /// a change of the Y component of offset value is currently 
        /// played back.
        /// </summary>
        public bool HasRunningVerticalOffsetAnimation
        {
            get => 
                Math.Abs(animatedOffset.Y - Offset.Y) > OffsetUpdateTreshold;
        }

        private double animatedZoom;
        private PointF animatedOffset;

        private RectangleF cachedImageRectangle;

        private bool RequiresRedraw
        {
            get
            {
                return HasRunningZoomAnimations || HasRunningOffsetAnimations
                    || HasRunningImageTransitionAnimation
                    || (CurrentImage.IsPlaying && CurrentImage.Length > 1);
            }
        }


        private DateTime lastDraw = DateTime.Now;
        private double imageTransitionProgressMs = 0;
        private Point previousMousePosition;
        private bool offsetModification = false;

        private System.Windows.Forms.Timer redrawTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageViewer"/> class.
        /// </summary>
        public ImageViewer()
        {
            mainImage = AnimatedImage.Empty;

            DoubleBuffered = true;

            redrawTimer = new System.Windows.Forms.Timer()
            {
                Interval = (int)(1000 / UpdatesPerSecond),
                Enabled = true
            };

            redrawTimer.Tick += RedrawTimer_Tick;
        }

        /// <summary>
        /// Sets a new image into the image viewer display.
        /// </summary>
        /// <param name="image">
        /// The new image or null to clear the image viewer.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when an image transition is currently in progress and
        /// its completion must be awaited until this method can be called
        /// again.
        /// </exception>
        public void SetImage(Image image)
        {
            if (HasRunningImageTransitionAnimation)
                throw new InvalidOperationException("An image transition " +
                    "is currently in progress and must be finished until " +
                    "the image can be changed again!");

            if (image != null) nextImage = new AnimatedImage(image);
            else nextImage = AnimatedImage.Empty;

            imageTransitionProgressMs = double.Epsilon;
        }

        /// <summary>
        /// Sets a new image into the image viewer display asynchronously.
        /// The image is be loaded while the old image is faded out.
        /// </summary>
        /// <param name="imageLoader">
        /// The function which is invoked asynchronously to provide the 
        /// new image to be displayed.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="imageLoader"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when an image transition is currently in progress and
        /// its completion must be awaited until this method can be called
        /// again.
        /// </exception>
        public void SetImageAsync(LoadImage imageLoader)
        {
            if (imageLoader == null)
                throw new ArgumentNullException("imageLoader");

            if (HasRunningImageTransitionAnimation)
                throw new InvalidOperationException("An image transition " +
                    "is currently in progress and must be finished until " +
                    "the image can be changed again!");

            imageTransitionProgressMs = double.Epsilon;

            Thread thread = new Thread(delegate ()
            {
                try
                {
                    bool success = imageLoader(out Image image,
                        out Exception error);
                    if (success && image != null)
                        nextImage = new AnimatedImage(image);
                    else
                        nextImage = AnimatedImage.Empty;
                }
                catch
                {
                    nextImage = AnimatedImage.Empty;
                }
            })
            {
                Priority = ThreadPriority.Lowest
            };
            thread.Start();
        }

        private void RedrawTimer_Tick(object sender, EventArgs e)
        {
            if (Visible && RequiresRedraw) Invalidate();
            else lastDraw = DateTime.Now;
        }

        /// <summary>
        /// Cycles through the available zoom modes depending on the current
        /// <see cref="Zoom"/> level and the size of the current control.
        /// </summary>
        public void GoThroughZoomModes()
        {
            Offset = Point.Empty;
            if (ImageExceedsBoundaries)
            {
                if (Zoom <= 1 && ZoomMode != ZoomMode.FitToScreen)
                    SetZoom(ZoomMode.FitToScreen);
                else if (Zoom > 1)
                {
                    SetZoom(ZoomMode.None);
                    if (ImageExceedsBoundaries)
                        SetZoom(ZoomMode.FitToScreen);
                }
                else SetZoom(ZoomMode.FillScreen);
            }
            else
            {
                if (Zoom <= 1 && ZoomMode == ZoomMode.FitToScreen)
                {
                    SetZoom(ZoomMode.None);

                    if (ImageExceedsBoundaries)
                        SetZoom(ZoomMode.FillScreen);
                }
                else if (ZoomMode != ZoomMode.FitToScreen)
                    SetZoom(ZoomMode.FitToScreen);
                else SetZoom(ZoomMode.FillScreen);
            }
        }

        /// <summary>
        /// Enlarges or shrinks the current image stepwise.
        /// </summary>
        /// <param name="steps">
        /// The amount of steps the image should be zoomed in 
        /// (positive number) or out (negative number).
        /// </param>
        public void ZoomStepwise(int steps, bool zoomToMouse = false)
        {
            SetZoom(Zoom + steps * (Zoom * ScrollDampValue));
            OffsetImageStepwise(0, 0);

            if (zoomToMouse)
            {
                //TODO: Fix or remove the "zoomToMouse" functionality.
                /*
                Point mouse = PointToClient(MousePosition);

                double rX = -(mouse.X / (double)Width - 0.5)
                    * ((ImageZoomedMax / Zoom) - 1);
                double rY = -(mouse.Y / (double)Height - 0.5)
                    * ((ImageZoomedMax / Zoom) - 1);

                OffsetImageStepwise(rX, rY);*/
            }
        }

        /// <summary>
        /// Changes the position offset of the image stepwise.
        /// </summary>
        /// <param name="stepsX">
        /// The amount of steps the image should be moved in the X direction.
        /// </param>
        /// <param name="stepsY">
        /// The amount of steps the image should be moved in the Y direction.
        /// </param>
        public void OffsetImageStepwise(double stepsX, double stepsY)
        {
            OffsetImage(stepsX * OffsetStepSizePx * Math.Max(1, Zoom),
                stepsY * OffsetStepSizePx * Math.Max(1, Zoom));
        }

        /// <summary>
        /// Resets the current zoom to its initial default.
        /// </summary>
        public void ResetZoomAndOffset()
        {
            Offset = Point.Empty;

            SetZoom(ZoomMode.None);
            if (ImageExceedsBoundaries) SetZoom(ZoomMode.FitToScreen);
        }

        /// <summary>
        /// Sets a <see cref="ZoomMode"/> to the current image viewer, which
        /// automatically zooms and offsets the image depending on the
        /// available space.
        /// </summary>
        /// <param name="mode">
        /// The new <see cref="ZoomMode"/>.
        /// </param>
        public void SetZoom(ZoomMode mode)
        {
            bool useWidthToScale =
                ((double)CurrentImage.Width / CurrentImage.Height) >
                ((double)Size.Width / Size.Height);

            if (mode == ZoomMode.FitToScreen)
            {
                if (useWidthToScale)
                    SetZoom((double)Size.Width / CurrentImage.Width);
                else
                    SetZoom((double)Size.Height / CurrentImage.Height);

                ZoomMode = ZoomMode.FitToScreen;
            }
            else if (mode == ZoomMode.FillScreen)
            {
                SetZoom((double)Size.Width / CurrentImage.Width);
                ZoomMode = ZoomMode.FillScreen;
                if (Settings.Default.AutoZoomImageToTopLeft)
                    OffsetImageStepwise(CurrentImage.Width / OffsetStepSizePx,
                        CurrentImage.Height / OffsetStepSizePx);
            }
            else if (mode == ZoomMode.None)
            {
                SetZoom(1);
            }
        }

        /// <summary>
        /// Sets the <see cref="Zoom"/> factor of the current image to a
        /// specific value and changes the <see cref="ZoomMode"/> to
        /// <see cref="ZoomMode.None"/>.
        /// </summary>
        /// <param name="zoom">
        /// The new zoom factor, which is automatically clamped.
        /// </param>
        public void SetZoom(double zoom)
        {
            ZoomMode = ZoomMode.None;

            double oldZoom = Math.Max(double.Epsilon, Zoom);

            //Clamp zoom
            double minZoom = Math.Min(ImageZoomedMinPixels /
                Math.Max(CurrentImage.Width, CurrentImage.Height), 
                ImageZoomedMax);

            Zoom = Math.Max(minZoom, Math.Min(ImageZoomedMax, Math.Abs(zoom)));

            //Update offset
            double relativeOffsetX = Offset.X / (CurrentImage.Width * oldZoom);
            double relativeOffsetY = Offset.Y / (CurrentImage.Height * oldZoom);

            double newOffsetX = relativeOffsetX * (CurrentImage.Width * Zoom);
            double newOffsetY = relativeOffsetY * (CurrentImage.Height * Zoom);

            Offset = new PointF((float)newOffsetX, (float)newOffsetY);

            UpdateCachedImageRectangle();
        }

        private void UpdateCachedImageRectangle()
        {
            cachedImageRectangle = GetImageRectangle(Zoom, Offset);
        }

        private void OffsetImage(double deltaX, double deltaY)
        {
            RectangleF imageRect = cachedImageRectangle;
            imageRect.Offset((float)deltaX, (float)deltaY);

            bool exceedsTop = imageRect.Top < 0;
            bool exceedsBottom = imageRect.Bottom > Height;

            if (exceedsTop && !exceedsBottom && deltaY <= 0)
                deltaY -= imageRect.Bottom - Height;
            else if (!exceedsTop && exceedsBottom && deltaY >= 0)
                deltaY -= imageRect.Top;
            else if (!exceedsTop && !exceedsBottom)
                deltaY = -Offset.Y;

            bool exceedsRight = imageRect.Right > Width;
            bool exceedsLeft = imageRect.Left < 0;

            if (exceedsLeft && !exceedsRight && deltaX <= 0)
                deltaX -= imageRect.Right - Width;
            else if (!exceedsLeft && exceedsRight && deltaX >= 0)
                deltaX -= imageRect.Left;
            else if (!exceedsLeft && !exceedsRight)
                deltaX = -Offset.X;

            Offset = new PointF(
                Offset.X + (float)deltaX,
                Offset.Y + (float)deltaY);

            UpdateCachedImageRectangle();
        }

        private Rectangle GetImageRectangle(double zoom, PointF offset)
        {
            Size size = new Size((int)(CurrentImage.Width * zoom),
                (int)(CurrentImage.Height * zoom));
            Point position = new Point(
                (int)(Size.Width / 2.0f - size.Width / 2.0f + offset.X),
                (int)(Size.Height / 2.0f - size.Height / 2.0f + offset.Y));
            return new Rectangle(position, size);
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            if (Settings.Default.AnimationSpeed !=
                currentAnimationSpeed)
            {
                currentAnimationSpeed = Settings.Default.AnimationSpeed;
                currentAnimationSpeedDefinition = 
                    AnimationSpeedDefinition.FromEnum(currentAnimationSpeed);
            }

            Graphics g = pe.Graphics;

            float deltaSeconds =
                (float)(DateTime.Now - lastDraw).TotalSeconds;
            lastDraw = DateTime.Now;

            CurrentImage.Update(deltaSeconds * 1000);

            double deltaZoom = (Zoom - animatedZoom)
                * currentAnimationSpeedDefinition.SmoothZoomSpeedPerSecond
                * deltaSeconds;

            if ((((Zoom - animatedZoom) > 0 && deltaZoom > 0) ||
                ((Zoom - animatedZoom) < 0 && deltaZoom < 0)))
                animatedZoom = Math.Min(animatedZoom + deltaZoom, 
                    ImageZoomedMax);

            animatedOffset = new PointF(
                animatedOffset.X + (Offset.X - animatedOffset.X)
                * currentAnimationSpeedDefinition.SmoothOffsetSpeedPerSecond
                * deltaSeconds,
                animatedOffset.Y + (Offset.Y - animatedOffset.Y)
                * currentAnimationSpeedDefinition.SmoothOffsetSpeedPerSecond
                * deltaSeconds);

            ImageAttributes imageAttributes = new ImageAttributes();
            if (HasRunningImageTransitionAnimation)
            {
                double transitionHalftime = 
                    currentAnimationSpeedDefinition.ImageTransitionTimeMs / 2;

                double newImageTransitionProgressMs =
                    imageTransitionProgressMs + (deltaSeconds * 1000);

                //Fade out the image and only fade the new one in afterwards
                //if it has been assigned already. Also make sure that the
                //old image fades out completely before the new image fades in.
                if (imageTransitionProgressMs < transitionHalftime &&
                    newImageTransitionProgressMs > transitionHalftime)
                    imageTransitionProgressMs = transitionHalftime;
                else if ((newImageTransitionProgressMs > transitionHalftime) &&
                    nextImage == null)
                    imageTransitionProgressMs = transitionHalftime;
                else imageTransitionProgressMs = newImageTransitionProgressMs;

                float opacity = (float)Math.Min(1.0, Math.Max(0.0,
                    (Math.Abs((transitionHalftime - imageTransitionProgressMs) 
                    / transitionHalftime))));

                //Add a opacity color matrix to the image attributes only when
                //required by a currently running transition to save CPU.
                imageAttributes.SetColorMatrix(
                    new ColorMatrix { Matrix33 = opacity },
                    ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                //If the new image is available and starts to fade in, the
                //zoom and offset should be reset to the new image.
                if (imageTransitionProgressMs > transitionHalftime
                    && !boundsResetFlag)
                {
                    boundsResetFlag = true;
                    ResetZoomAndOffset();
                }

                //When the transition was completed, swap the private image
                //fields, reset the used flags and timers and free the field 
                //for the next image.
                if (imageTransitionProgressMs >= 
                    currentAnimationSpeedDefinition.ImageTransitionTimeMs)
                {
                    if (mainImage != null) mainImage.Dispose();
                    mainImage = nextImage;
                    nextImage = null;
                    imageTransitionProgressMs = 0;
                    boundsResetFlag = false;
                    CurrentImage.Play();
                }

                g.CompositingQuality = CompositingQuality.HighSpeed;
                g.InterpolationMode = InterpolationMode.Low;
            }
            else
            {
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            }
            
            g.DrawImage(CurrentImage.Image,
                GetImageRectangle(animatedZoom, animatedOffset),
                0, 0, CurrentImage.Width, CurrentImage.Height,
                GraphicsUnit.Pixel, imageAttributes);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            SetZoom(ZoomMode);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            offsetModification = true;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            offsetModification = false;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (offsetModification)
            {
                OffsetImage(e.Location.X - previousMousePosition.X,
                    e.Location.Y - previousMousePosition.Y);
            }

            previousMousePosition = e.Location;
        }
    }
}
