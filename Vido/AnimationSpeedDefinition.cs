using System;

namespace Vido
{
    /// <summary>
    /// Provides the animation speed definitions for the values of
    /// <see cref="AnimationSpeed"/>.
    /// </summary>
    internal struct AnimationSpeedDefinition
    {
        /// <summary>
        /// Gets or sets the speed of the offset animation.
        /// </summary>
        public float SmoothOffsetSpeedPerSecond { get; set; }

        /// <summary>
        /// Gets or sets the speed of the zoom animation.
        /// </summary>
        public float SmoothZoomSpeedPerSecond { get; set; }

        /// <summary>
        /// Gets or sets the amount of time an image transition takes,
        /// in milliseconds.
        /// </summary>
        public float ImageTransitionTimeMs { get; set; }

        /// <summary>
        /// Gets a new instance of the <see cref="AnimationSpeedDefinition"/>
        /// struct for the enum value <see cref="AnimationSpeed.Slow"/>.
        /// </summary>
        public static AnimationSpeedDefinition Slow
        {
            get
            {
                return new AnimationSpeedDefinition()
                {
                    SmoothOffsetSpeedPerSecond = 3,
                    SmoothZoomSpeedPerSecond = 3,
                    ImageTransitionTimeMs = 750
                };
            }
        }

        /// <summary>
        /// Gets a new instance of the <see cref="AnimationSpeedDefinition"/>
        /// struct for the enum value <see cref="AnimationSpeed.Medium"/>.
        /// </summary>
        public static AnimationSpeedDefinition Medium
        {
            get
            {
                return new AnimationSpeedDefinition()
                {
                    SmoothOffsetSpeedPerSecond = 5,
                    SmoothZoomSpeedPerSecond = 5,
                    ImageTransitionTimeMs = 500
                };
            }
        }

        /// <summary>
        /// Gets a new instance of the <see cref="AnimationSpeedDefinition"/>
        /// struct for the enum value <see cref="AnimationSpeed.Fast"/>.
        /// </summary>
        public static AnimationSpeedDefinition Fast
        {
            get
            {
                return new AnimationSpeedDefinition()
                {
                    SmoothOffsetSpeedPerSecond = 7,
                    SmoothZoomSpeedPerSecond = 7,
                    ImageTransitionTimeMs = 250
                };
            }
        }

        /// <summary>
        /// Gets a new instance of the <see cref="AnimationSpeedDefinition"/>
        /// struct for the enum value <see cref="AnimationSpeed.None"/>.
        /// </summary>
        public static AnimationSpeedDefinition None
        {
            get
            {
                return new AnimationSpeedDefinition()
                {
                    SmoothOffsetSpeedPerSecond = 20,
                    SmoothZoomSpeedPerSecond = 20,
                    ImageTransitionTimeMs = 2
                };
            }
        }

        /// <summary>
        /// Creates a new <see cref="AnimationSpeedDefinition"/> instance
        /// for a given <see cref="AnimationSpeed"/> enum value.
        /// </summary>
        /// <param name="value">
        /// The <see cref="AnimationSpeed"/>, for which an 
        /// <see cref="AnimationSpeedDefinition"/> should be generated
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="AnimationSpeedDefinition"/> 
        /// struct.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Is thrown when an invalid/unsupported <see cref="AnimationSpeed"/> 
        /// enum value is specified.
        /// </exception>
        public static AnimationSpeedDefinition FromEnum(AnimationSpeed value)
        {
            switch (value)
            {
                case AnimationSpeed.None: return None;
                case AnimationSpeed.Fast: return Fast;
                case AnimationSpeed.Medium: return Medium;
                case AnimationSpeed.Slow: return Slow;
                default: throw new ArgumentException("The specified " +
                    "animation speed was not supported!");
            }
        }
    }
}
