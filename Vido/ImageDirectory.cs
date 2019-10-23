using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace Vido
{
    /// <summary>
    /// Provides an implementation of the <see cref="IImageEnumerator"/> for 
    /// a directory.
    /// </summary>
    public class ImageDirectory : IImageEnumerator
    {
        private class NumericStringComparer : IComparer<string>
        {
            //Slightly altered version of the algorithm by Jerome Viveiros from
            //https://psycodedeveloper.wordpress.com.
            public int Compare(string x, string y)
            {
                if (!string.IsNullOrWhiteSpace(x) && 
                    !string.IsNullOrWhiteSpace(y))
                {
                    int xIndex = 0, yIndex = 0;

                    while (xIndex < x.Length)
                    {
                        if (yIndex >= y.Length) return 1;

                        if (char.IsDigit(x[xIndex]))
                        {
                            if (!char.IsDigit(y[yIndex])) return -1;

                            StringBuilder xBuilder = new StringBuilder(),
                                yBuilder = new StringBuilder();

                            while (xIndex < x.Length
                                && char.IsDigit(x[xIndex]))
                                xBuilder.Append(x[xIndex++]);

                            while (yIndex < y.Length
                                && char.IsDigit(y[yIndex]))
                                yBuilder.Append(y[yIndex++]);

                            long xValue = 0, yValue = 0;

                            try
                            {
                                xValue = Convert.ToInt64(xBuilder.ToString());
                            }
                            catch (OverflowException)
                            {
                                xValue = Int64.MaxValue;
                            }

                            try
                            {
                                yValue = Convert.ToInt64(yBuilder.ToString());
                            }
                            catch (OverflowException)
                            {
                                yValue = Int64.MaxValue;
                            }

                            if (xValue < yValue) return -1;
                            else if (xValue > yValue) return 1;
                        }
                        else if (char.IsDigit(y[yIndex]))
                        {
                            if (!char.IsLetter(x[xIndex])) return -1;
                            else return 1;
                        }
                        else
                        {
                            int difference = string.Compare(
                                x[xIndex].ToString(), y[yIndex].ToString(),
                                StringComparison.InvariantCultureIgnoreCase);

                            if (difference > 0) return 1;
                            else if (difference < 0) return -1;

                            xIndex++;
                            yIndex++;
                        }
                    }

                    if (yIndex < y.Length) return -1;
                }

                return 0;
            }
        }

        /// <summary>
        /// Occurs when the elements in the enumeration (and eventually the 
        /// current file position) were changed (externally).
        /// This event occurs for every single file changed.
        /// </summary>
        public event EventHandler EnumerationUpdated;

        /// <summary>
        /// Gets a boolean indicating whether the current enumerator contains
        /// elements (<c>true</c>) or not (<c>false</c>).
        /// </summary>
        public bool IsEmpty => Count == 0;

        /// <summary>
        /// Gets the amount of supported files in the list.
        /// </summary>
        public int Count => sortedFileList.Count;

        private readonly SortedList<string, string> sortedFileList =
            new SortedList<string, string>(new NumericStringComparer());

        private readonly Importer importer;
        
        private readonly string directory;

        private readonly FileSystemWatcher directoryWatcher;

        private string currentFileKey = "";
        private int currentFileIndex = 0;
        private bool currentFilePositionInvalidated = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageDirectory"/> class
        /// with the default <see cref="Importer"/>.
        /// </summary>
        /// <param name="path">
        /// The path of the image directory or a (supported) image within 
        /// the target directory.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the specified <paramref name="path"/> was no
        /// existing file or directory.
        /// </exception>
        public ImageDirectory(string path)
            : this(path, new Importer()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageDirectory"/> class.
        /// </summary>
        /// <param name="path">
        /// The path of the image directory or a (supported) image within 
        /// the target directory.
        /// </param>
        /// <param name="importer">
        /// The <see cref="Importer"/> which defines which image formats are
        /// supported and imports them.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="path"/> or 
        /// <paramref name="importer"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Is thrown when the specified <paramref name="path"/> was no
        /// existing file or directory.
        /// </exception>
        public ImageDirectory(string path, Importer importer)
        {
            if (path == null) throw new ArgumentNullException("path");

            this.importer = importer ?? 
                throw new ArgumentNullException("importer");

            if (Directory.Exists(path))
                directory = path;
            else if (File.Exists(path))
                directory = Path.GetDirectoryName(path);
            else throw new ArgumentException("The specified path couldn't " +
                "be resolved into an existing file or directory!");

            directoryWatcher = new FileSystemWatcher(directory)
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = false
            };
            directoryWatcher.Changed += DirectoryEntryChanged;
            directoryWatcher.Created += DirectoryEntryCreated;
            directoryWatcher.Deleted += DirectoryEntryDeleted;
            directoryWatcher.Renamed += DirectoryEntryRenamed;
            directoryWatcher.Error += DirectoryWatcherError;

            foreach (string filePath in Directory.EnumerateFiles(directory))
            {
                string fileName = Path.GetFileName(filePath);
                //Ignore files without extension or files with a file name
                //starting with a single '.'.
                if (fileName.LastIndexOf('.') > 0)
                {
                    string fileExtension = fileName.Substring(
                        fileName.LastIndexOf('.'));

                    if (importer.Supports(fileExtension))
                        sortedFileList.Add(fileName, filePath);
                }
            }

            int selectedFileIndex = sortedFileList.IndexOfValue(path);
            if (selectedFileIndex >= 0)
                currentFileIndex = selectedFileIndex;
        }

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
        public LoadImage GetCurrent()
        {
            ChangeFilePosition(0);
            return CreateLoaderFunction();
        }

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
        public LoadImage GetNext()
        {
            ChangeFilePosition(1);
            return CreateLoaderFunction();
        }

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
        public LoadImage GetPrevious()
        {
            ChangeFilePosition(-1);
            return CreateLoaderFunction();
        }

        private LoadImage CreateLoaderFunction()
        {
            return delegate (out Image image, out Exception error)
            {
                try
                {
                    if (importer.Supports(currentFileKey.Substring(
                        currentFileKey.LastIndexOf('.')),
                        out ImportImage fileImporter))
                    {
                        image = fileImporter(
                            sortedFileList.Values[currentFileIndex]);
                        error = null;
                        return true;
                    }
                    else throw new NotSupportedException("The specified " +
                        "file format was not supported!");
                }
                catch (Exception exc)
                {
                    error = exc;
                    image = null;
                    return false;
                }
            };
        }

        private void ChangeFilePosition(int direction)
        {
            if (Math.Abs(direction) > 1)
                throw new ArgumentOutOfRangeException("direction");
            if (Count == 0)
                throw new InvalidOperationException("The directory is empty!");

            if (currentFilePositionInvalidated)
            {
                if (sortedFileList.ContainsKey(currentFileKey))
                    currentFileIndex =
                        sortedFileList.IndexOfKey(currentFileKey);
                else if (direction == 1) direction = 0;
            }

            if (Count > 1)
            {
                int newIndex = (currentFileIndex + direction) % Count;
                if (newIndex < 0) currentFileIndex = Count + newIndex;
                else currentFileIndex = newIndex;
            }

            currentFileKey = sortedFileList.Keys[currentFileIndex];

            currentFilePositionInvalidated = false;
        }

        private void DirectoryWatcherError(object sender, ErrorEventArgs e)
        {
            sortedFileList.Clear();
            currentFileIndex = 0;
            currentFileKey = "";

            OnEnumerationUpdated();
        }

        private void DirectoryEntryRenamed(object sender, RenamedEventArgs e)
        {
            if (sortedFileList.ContainsKey(e.OldName))
            {
                if (currentFileKey == e.OldName)
                    currentFileKey = e.Name;

                sortedFileList.Remove(e.OldName);

                if (importer.Supports(e.Name.Substring(
                    e.Name.LastIndexOf('.'))))
                    sortedFileList.Add(e.Name, e.FullPath);

                currentFilePositionInvalidated = true;

                OnEnumerationUpdated();
            }
        }

        private void DirectoryEntryDeleted(object sender, 
            FileSystemEventArgs e)
        {
            if (sortedFileList.ContainsKey(e.Name))
            {
                sortedFileList.Remove(e.Name);

                currentFilePositionInvalidated = true;

                OnEnumerationUpdated();
            }
        }

        private void DirectoryEntryCreated(object sender, 
            FileSystemEventArgs e)
        {
            if (File.Exists(e.FullPath) && importer.Supports(
                e.Name.Substring(e.Name.LastIndexOf('.'))))
            {
                sortedFileList.Add(e.Name, e.FullPath);

                currentFilePositionInvalidated = true;

                OnEnumerationUpdated();
            }
        }

        private void DirectoryEntryChanged(object sender, 
            FileSystemEventArgs e)
        {
            if (sortedFileList.ContainsKey(e.Name)
                && e.ChangeType == WatcherChangeTypes.Changed)
            {
                currentFilePositionInvalidated = true;
                OnEnumerationUpdated();
            }
        }

        private void OnEnumerationUpdated()
        {
            EnumerationUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
}
