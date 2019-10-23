namespace Vido
{
    /// <summary>
    /// Defines the available automatic modes for scaling 
    /// images proportionally.
    /// </summary>
    public enum ZoomMode
    {
        /// <summary>
        /// The image is not scaled automatically.
        /// </summary>
        None = 0,
        /// <summary>
        /// The image is scaled as large as possible without being cropped.
        /// </summary>
        FitToScreen = 1,
        /// <summary>
        /// The image is scaled so that it occupies all the available space,
        /// being cropped if neccessary.
        /// </summary>
        FillScreen = 2
    }
}
