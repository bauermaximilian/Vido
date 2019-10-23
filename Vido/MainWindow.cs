using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Windows.Forms;
using Vido.Properties;

namespace Vido
{
    public partial class MainWindow : Form
    {
        private class MouseMovementHandler : IMessageFilter
        {
            private const int WM_MOUSEMOVE = 0x0200;
            private const int WM_LBUTTONUP = 0x0202;
            private const int WM_MBUTTONUP = 0x0208;
            private const int WM_RBUTTONUP = 0x0205;
            private const int WM_XBUTTONUP = 0x020C;
            private const int XBUTTON1 = 0x0001;
            private const int XBUTTON2 = 0x0002;

            public event EventHandler MovementDetected;

            public event EventHandler<MouseEventArgs> MouseButtonUp;

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg == WM_MOUSEMOVE)
                    MovementDetected?.Invoke(this, EventArgs.Empty);

                if (m.Msg == WM_LBUTTONUP)
                    MouseButtonUp?.Invoke(this,
                        new MouseEventArgs(MouseButtons.Left, 0, 0, 0, 0));
                else if (m.Msg == WM_MBUTTONUP)
                    MouseButtonUp?.Invoke(this,
                        new MouseEventArgs(MouseButtons.Middle, 0, 0, 0, 0));
                else if (m.Msg == WM_RBUTTONUP)
                    MouseButtonUp?.Invoke(this,
                        new MouseEventArgs(MouseButtons.Right, 0, 0, 0, 0));
                else if (m.Msg == WM_XBUTTONUP)
                {
                    int WParam = (((int)m.WParam >> 16) & 0xffff);
                    if (WParam == XBUTTON1)
                        MouseButtonUp?.Invoke(this,
                            new MouseEventArgs(MouseButtons.XButton1, 0,
                            0, 0, 0));
                    if (WParam == XBUTTON2)
                        MouseButtonUp?.Invoke(this,
                            new MouseEventArgs(MouseButtons.XButton2, 0,
                            0, 0, 0));
                }
                return false;
            }
        }

        private const int MouseMovementTreshold = 3;
        private const int ControlBarVisibleTimeoutMs = 3000;

        private DateTime lastMouseMove = DateTime.Now;
        private Point lastMousePosition = Point.Empty;

        private FormWindowState preFullscreenState;

        //private KeyBindings keySettings = KeyBindings.Default;

        private IImageEnumerator imageEnumerator;

        public MainWindow(string imagePath)
        {
            MouseMovementHandler movementHandler = new MouseMovementHandler();
            movementHandler.MovementDetected += MouseMoved;
            movementHandler.MouseButtonUp += MouseButtonUp;
            Application.AddMessageFilter(movementHandler);

            InitializeComponent();

            InitializeUI();
            InitializeTooltips();

            preFullscreenState = WindowState;

            imageViewer.BringToFront();

            if (!string.IsNullOrWhiteSpace(imagePath))
            {
                try
                {
                    imageEnumerator = new ImageDirectory(imagePath);
                    imageEnumerator.EnumerationUpdated 
                        += ImageEnumerationUpdated;
                }
                catch (Exception) { SystemSounds.Exclamation.Play(); }
                ChangeImage(0);
            }

            KeyPreview = true;
        }

        private void UseEmbeddedButtonIcons()
        {
            zoomButton.BackgroundImage = Resources.symbol_zoom;
            previousButton.BackgroundImage = Resources.symbol_left;
            fullscreenButton.BackgroundImage = Resources.symbol_fullscreen;
            nextButton.BackgroundImage = Resources.symbol_right;
            settingsButton.BackgroundImage = Resources.symbol_settings;
        }

        private void InitializeTooltips()
        {
            if (Settings.Default.ShowTooltips)
            {
                toolTip.SetToolTip(zoomButton, "Toggle zoom modes (Shortcut: "
                + Settings.Default.KeyBinding_GoThroughZoomModes.ToString()
                + ")");
                toolTip.SetToolTip(previousButton, "Go to previous image " +
                    "(Shortcut: " + Settings.Default.KeyBinding_Left.ToString()
                    + ")");
                toolTip.SetToolTip(fullscreenButton, "Toggle " +
                    "fullscreen/window mode (Shortcut: "
                    + Settings.Default.KeyBinding_ToggleFullscreen.ToString()
                    + ")");
                toolTip.SetToolTip(nextButton, "Go to next image  (Shortcut: "
                    + Settings.Default.KeyBinding_Right.ToString() + ")");
                toolTip.SetToolTip(settingsButton, "Open settings (Shortcut: "
                    + Settings.Default.KeyBinding_OpenSettings.ToString()
                    + ")");
            }
            else toolTip.RemoveAll();
        }

        private void InitializeUI()
        {
            imageViewer.BackColor = Settings.Default.Color_ImageArea;
            buttonPanel.BackColor = Settings.Default.Color_ControlBar;
            zoomButton.FlatAppearance.BorderColor 
                = Settings.Default.Color_ControlBar;
            previousButton.FlatAppearance.BorderColor
                = Settings.Default.Color_ControlBar;
            fullscreenButton.FlatAppearance.BorderColor
                = Settings.Default.Color_ControlBar;
            nextButton.FlatAppearance.BorderColor
                = Settings.Default.Color_ControlBar;
            settingsButton.FlatAppearance.BorderColor
                = Settings.Default.Color_ControlBar;

            if (TryGetCustomButtonImage(
                Settings.Default.CustomImagePath_ZoomButton,
                out Bitmap iconZoom))
                zoomButton.BackgroundImage = iconZoom;
            else zoomButton.BackgroundImage = Resources.symbol_zoom;

            if (TryGetCustomButtonImage(
                Settings.Default.CustomImagePath_PreviousButton,
                out Bitmap iconPrevious))
                previousButton.BackgroundImage = iconPrevious;
            else previousButton.BackgroundImage = Resources.symbol_left;

            if (TryGetCustomButtonImage(
                Settings.Default.CustomImagePath_FullscreenButton,
                out Bitmap iconFullscreen))
                fullscreenButton.BackgroundImage = iconFullscreen;
            else fullscreenButton.BackgroundImage = 
                    Resources.symbol_fullscreen;

            if (TryGetCustomButtonImage(
                Settings.Default.CustomImagePath_NextButton,
                out Bitmap iconNext))
                nextButton.BackgroundImage = iconNext;
            else nextButton.BackgroundImage = Resources.symbol_right;

            if (TryGetCustomButtonImage(
                Settings.Default.CustomImagePath_SettingsButton,
                out Bitmap iconSettings))
                settingsButton.BackgroundImage = iconSettings;
            else settingsButton.BackgroundImage = Resources.symbol_settings;
        }

        private bool TryGetCustomButtonImage(string path, out Bitmap icon)
        {
            icon = null;

            if (string.IsNullOrWhiteSpace(path)) return false;

            try
            {
                if (File.Exists(path))
                {
                    icon = (Bitmap)Image.FromFile(path);
                    return true;
                }
                else return false;
            }
            catch { return false; }
        }

        private void MouseButtonUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
                imageViewer.GoThroughZoomModes();
            else if (e.Button == MouseButtons.XButton1)
                ChangeImage(-1);
            else if (e.Button == MouseButtons.XButton2)
                ChangeImage(1);
        }

        private void ImageEnumerationUpdated(object sender, EventArgs e)
        {
            ChangeImage(0);
        }

        private void MouseMoved(object sender, EventArgs e)
        {
            Point mouseDelta = new Point(MousePosition.X - lastMousePosition.X,
                    MousePosition.Y - lastMousePosition.Y);

            if (mouseDelta.X > MouseMovementTreshold ||
                mouseDelta.Y > MouseMovementTreshold)
            {
                lastMouseMove = DateTime.Now;
            }
            lastMousePosition = MousePosition;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;  // Turn on WS_EX_COMPOSITED
                return cp;
            }
        }

        private void ChangeWindowState(bool fullscreen)
        {
            if (fullscreen)
            {
                preFullscreenState = WindowState;
                WindowState = FormWindowState.Normal;
                TopMost = true;
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Maximized;
                ControlBox = false;
                buttonPanel.Visible = false;
                Focus();
            }
            else
            {
                TopMost = false;
                ControlBox = true;
                FormBorderStyle = FormBorderStyle.Sizable;
                WindowState = preFullscreenState;
                buttonPanel.Visible = true;
            }
        }

        public bool IsFullscreen { get => TopMost; }

        private void FullscreenButton_Click(object sender, EventArgs e)
        {
            ChangeWindowState(!IsFullscreen);
        }

        private void ChangeImage(int direction)
        {
            if (imageEnumerator != null && !imageEnumerator.IsEmpty
                && !imageViewer.HasRunningImageTransitionAnimation)
            {
                if (direction > 0)
                    imageViewer.SetImageAsync(imageEnumerator.GetNext());
                else if (direction < 0)
                    imageViewer.SetImageAsync(imageEnumerator.GetPrevious());
                else imageViewer.SetImageAsync(imageEnumerator.GetCurrent());
            }
            else if (imageEnumerator == null || imageEnumerator.IsEmpty)
            {
                if (!imageViewer.HasRunningImageTransitionAnimation)
                    imageViewer.SetImage(null);
                SystemSounds.Exclamation.Play();
            }
        }

        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (Settings.Default.KeyBinding_ToggleFullscreen.IsActivated(e))
                ChangeWindowState(!IsFullscreen);
            if (Settings.Default.KeyBinding_LeaveFullscreen.IsActivated(e))
                ChangeWindowState(false);

            if (Settings.Default.KeyBinding_GoThroughZoomModes.IsActivated(e))
                imageViewer.GoThroughZoomModes();
            if (Settings.Default.KeyBinding_ZoomIn.IsActivated(e))
                imageViewer.ZoomStepwise(1, false);
            if (Settings.Default.KeyBinding_ZoomOut.IsActivated(e))
                imageViewer.ZoomStepwise(-1, false);

            if (Settings.Default.KeyBinding_Left.IsActivated(e))
            {
                if (imageViewer.ImageExceedsLeftSide)
                    imageViewer.OffsetImageStepwise(1, 0);
                else if (!imageViewer.HasRunningHorizontalOffsetAnimation)
                    ChangeImage(-1);
            }
            if (Settings.Default.KeyBinding_Up.IsActivated(e))
                imageViewer.OffsetImageStepwise(0, 1);
            if (Settings.Default.KeyBinding_Right.IsActivated(e))
            {
                if (imageViewer.ImageExceedsRightSide)
                    imageViewer.OffsetImageStepwise(-1, 0);
                else if (!imageViewer.HasRunningHorizontalOffsetAnimation)
                    ChangeImage(1);
            }
            if (Settings.Default.KeyBinding_Down.IsActivated(e))
                imageViewer.OffsetImageStepwise(0, -1);
        }

        private void zoomButton_Click(object sender, EventArgs e)
        {
            imageViewer.GoThroughZoomModes();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            imageViewer.ZoomStepwise(e.Delta > 0 ? 1 : -1, true);
        }

        private void MainWindow_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Link;
        }

        private void MainWindow_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            try
            {
                string firstDragDropFile = files.FirstOrDefault();
                imageEnumerator = new ImageDirectory(firstDragDropFile);
                imageEnumerator.EnumerationUpdated += ImageEnumerationUpdated;
                ChangeImage(0);
            }
            catch (Exception) { SystemSounds.Exclamation.Play(); }
        }

        private void nextButton_Click(object sender, EventArgs e)
        {
            ChangeImage(1);
        }

        private void previousButton_Click(object sender, EventArgs e)
        {
            ChangeImage(-1);
        }

        private void uiUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (IsFullscreen)
            {
                bool controlVisibilityTimedOut =
                    (DateTime.Now - lastMouseMove).TotalMilliseconds
                    > ControlBarVisibleTimeoutMs;

                if (controlVisibilityTimedOut && buttonPanel.Visible
                    && (MousePosition.Y < buttonPanel.Location.Y
                    || MousePosition.Y >= buttonPanel.Location.Y 
                    + buttonPanel.Height - 6))
                {
                    buttonPanel.Visible = false;
                    Cursor.Hide();
                }
                else if (!controlVisibilityTimedOut && !buttonPanel.Visible)
                {
                    buttonPanel.Visible = true;
                    Cursor.Show();
                }
            }
        }

        private void settingsButton_Click(object sender, EventArgs e)
        {
            ChangeWindowState(false);
            buttonPanel.Visible = false;
            UseEmbeddedButtonIcons();
            Invalidate();
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
            InitializeUI();
            InitializeTooltips();
            buttonPanel.Visible = true;
        }
    }
}
