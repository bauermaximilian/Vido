using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace Vido
{
    /// <summary>
    /// Provides a wrapper for <see cref="Image"/> instances to play 
    /// any contained animations.
    /// </summary>
    public class AnimatedImage : IDisposable
    {
        /// <summary>
        /// Defines the property ID which stores the delay between frames
        /// in an animated image.
        /// </summary>
        private const int GifFrameDelayPropertyId = 0x5100;

        /// <summary>
        /// Defines the frames per second for an animated image without a 
        /// value associated to the <see cref="GifFrameDelayPropertyId"/>.
        /// </summary>
        private const double DefaultFPS = 15;

        private static readonly AnimatedImage emptyImage;

        /// <summary>
        /// Gets an empty <see cref="AnimatedImage"/> instance, which consists
        /// of a 1x1 fully transparent bitmap.
        /// </summary>
        public static AnimatedImage Empty => emptyImage;

        /// <summary>
        /// Gets or sets a boolean which defines whether the animation
        /// should be looped indefinitely until it is stopped or paused.
        /// </summary>
        public bool Loop { get; set; }

        /// <summary>
        /// Gets the base image, which can be drawn to a 
        /// <see cref="Graphics"/> canvas. Gets disposed if the current
        /// <see cref="AnimatedImage"/> instance gets disposed.
        /// </summary>
        public Image Image { get; private set; }

        /// <summary>
        /// Gets the length of the current animated image in frames.
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        /// Gets the animation speed in frames per seconds.
        /// </summary>
        public double FPS { get; private set; }

        /// <summary>
        /// Gets the current playback position of the animation.
        /// </summary>
        public double PlaybackPosition { get; private set; }

        /// <summary>
        /// Gets a boolean which indicates whether the animation is currently
        /// playing or not.
        /// </summary>
        public bool IsPlaying { get; private set; }

        /// <summary>
        /// Gets the width of the image.
        /// </summary>
        public int Width => Image.Width;

        /// <summary>
        /// Gets the height of the image.
        /// </summary>
        public int Height => Image.Height;

        private readonly FrameDimension frameDimension;

        static AnimatedImage()
        {
            Bitmap emptyBitmap = new Bitmap(1, 1);
            emptyBitmap.SetPixel(0, 0, Color.Transparent);
            emptyImage = new AnimatedImage(emptyBitmap);
        }

        /// <summary>
        /// Retrieves the amount of frames of an image.
        /// </summary>
        /// <param name="image">
        /// The image which should be analyzed.
        /// </param>
        /// <returns>
        /// The amount of frames in the specified image or 1, if the image
        /// is not animated.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="image"/> is null.
        /// </exception>
        public static int GetFrameCount(Image image)
        {
            return GetFrameCount(image, out FrameDimension nil);
        }

        private static int GetFrameCount(Image image,
            out FrameDimension frameDimension)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            frameDimension =
                new FrameDimension(image.FrameDimensionsList[0]);
            return image.GetFrameCount(frameDimension);
        }

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="AnimatedImage"/> class.
        /// </summary>
        /// <param name="baseImage">
        /// The image to be used as base for the current 
        /// <see cref="AnimatedImage"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="baseImage"/> is null.
        /// </exception>
        public AnimatedImage(Image baseImage)
        {
            Image = baseImage ?? throw new ArgumentNullException("baseImage");

            Length = GetFrameCount(baseImage, out frameDimension);
            Loop = true;

            if (Image.PropertyIdList.Contains(GifFrameDelayPropertyId))
            {
                var item = Image.GetPropertyItem(GifFrameDelayPropertyId);
                double frameDist = (item.Value[0] + item.Value[1] * 256);
                if (frameDist > 0) FPS = 100.0 / frameDist;
                else FPS = DefaultFPS;
            }
            else FPS = DefaultFPS;
        }

        /// <summary>
        /// Starts the animation or continues it where it was paused 
        /// previously. If <see cref="Loop"/> is false and the animation
        /// has stopped because the end was reached, it is started again
        /// from the beginning.
        /// </summary>
        public void Play()
        {
            if (!Loop && PlaybackPosition >= (Length - 1))
                PlaybackPosition = 0;
            IsPlaying = true;
        }

        /// <summary>
        /// Pauses the animation at the current position to be resumed 
        /// at a later time with <see cref="Play"/>.
        /// </summary>
        public void Pause()
        {
            IsPlaying = false;
        }

        /// <summary>
        /// Stops the animation and rewinds the playback position to the start.
        /// </summary>
        public void Stop()
        {
            IsPlaying = false;
            PlaybackPosition = 0;
        }

        /// <summary>
        /// Updates the animation.
        /// </summary>
        /// <param name="deltaMs">
        /// The amount of time (in milliseconds) elapsed since the last update.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when <paramref name="deltaMs"/> is negative.
        /// </exception>
        public void Update(double deltaMs)
        {
            if (deltaMs < 0) throw new ArgumentOutOfRangeException("deltaMs");

            if (IsPlaying)
            {
                double newPlaybackPosition = PlaybackPosition +
                    deltaMs * (FPS / 1000.0);

                if (newPlaybackPosition > (Length - 1))
                {
                    if (Loop) PlaybackPosition = newPlaybackPosition % Length;
                    else
                    {
                        PlaybackPosition = (Length - 1);
                        IsPlaying = false;
                    }
                }
                else PlaybackPosition = newPlaybackPosition;

                int currentFrame = (int)PlaybackPosition;
                Image.SelectActiveFrame(frameDimension,
                    currentFrame);
            }
        }

        public void Dispose()
        {
            if (Image != null)
            {
                Image.Dispose();
            }
        }
    }
}
