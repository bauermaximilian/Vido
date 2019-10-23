using System;
using System.Drawing;

namespace Vido
{
    /// <summary>
    /// Represents a method that loads an image.
    /// </summary>
    /// <param name="image">
    /// The loaded image, if the process was successful.
    /// </param>
    /// <param name="error">
    /// The error information, if the process failed.
    /// </param>
    /// <returns>
    /// <c>true</c> if the image was loaded successfully and was stored
    /// in the <paramref name="image"/>, false if the process failed
    /// with the exception stored in the <paramref name="error"/>
    /// parameter.
    /// </returns>
    public delegate bool LoadImage(out Image image, out Exception error);

    /// <summary>
    /// Defines the functionality for navigating and loading through a
    /// collection of images.
    /// </summary>
    public interface IImageEnumerator
    {
        /// <summary>
        /// Occurs when the elements in the enumeration (and eventually the 
        /// current file position) were changed (externally).
        /// This event occurs for every single file changed.
        /// </summary>
        event EventHandler EnumerationUpdated;

        /// <summary>
        /// Gets a boolean indicating whether the current enumerator contains
        /// elements (<c>true</c>) or not (<c>false</c>).
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Gets the loader function for the current element in the list 
        /// without moving the enumerator.
        /// </summary>
        /// <returns>
        /// The <see cref="Func{Image}"/> to load the current image element.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when the current <see cref="IImageEnumerator"/> contains
        /// no elements.
        /// </exception>
        LoadImage GetCurrent();

        /// <summary>
        /// Sets the enumerator to the next element in the list and returns 
        /// the loader function for that element.
        /// </summary>
        /// <returns>
        /// The <see cref="Func{Image}"/> to load the current image element.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when the current <see cref="IImageEnumerator"/> contains
        /// no elements.
        /// </exception>
        LoadImage GetNext();

        /// <summary>
        /// Sets the enumerator to the previous element in the list and returns 
        /// the loader function for that element.
        /// </summary>
        /// <returns>
        /// The <see cref="Func{Image}"/> to load the current image element.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when the current <see cref="IImageEnumerator"/> contains
        /// no elements.
        /// </exception>
        LoadImage GetPrevious();
    }
}
