using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Vido.Properties;

namespace Vido
{
    public partial class SettingsWindow : Form
    {
        private const string ReadmeFileName = "README.txt";

        #region Cached default values of settings (with prefix "pd_")
        private static readonly AnimationSpeed pd_animationSpeed
            = GetDefaultAnimationSpeed("AnimationSpeed");
        private static readonly bool pd_autoZoomImageToTopLeft
            = GetDefaultBooleanPropertyValue("AutoZoomImageToTopLeft");

        private static readonly KeyBinding pd_goThroughZoomModes
            = GetDefaultKeyBinding("KeyBinding_GoThroughZoomModes");
        private static readonly KeyBinding pd_toggleFullscreen
            = GetDefaultKeyBinding("KeyBinding_ToggleFullscreen");
        private static readonly KeyBinding pd_leaveFullscreen
            = GetDefaultKeyBinding("KeyBinding_LeaveFullscreen");
        private static readonly KeyBinding pd_zoomIn
            = GetDefaultKeyBinding("KeyBinding_ZoomIn");
        private static readonly KeyBinding pd_zoomOut
            = GetDefaultKeyBinding("KeyBinding_ZoomOut");
        private static readonly KeyBinding pd_left
            = GetDefaultKeyBinding("KeyBinding_Left");
        private static readonly KeyBinding pd_right
            = GetDefaultKeyBinding("KeyBinding_Right");
        private static readonly KeyBinding pd_up
            = GetDefaultKeyBinding("KeyBinding_Up");
        private static readonly KeyBinding pd_down
            = GetDefaultKeyBinding("KeyBinding_Down");

        private static readonly Color pd_imageAreaColor =
            GetDefaultColor("Color_ImageArea");
        private static readonly Color pd_controlBarColor =
           GetDefaultColor("Color_ControlBar");
        #endregion

        #region The new value candidates for the custom buttom image properties
        private string currentCustomImagePath_ZoomButton;
        private string currentCustomImagePath_PreviousButton;
        private string currentCustomImagePath_FullscreenButton;
        private string currentCustomImagePath_NextButton;
        private string currentCustomImagePath_SettingsButton;
        #endregion

        private static readonly string[] ignoredMainKeys = new string[]
        {
            Enum.GetName(typeof(Keys), Keys.Shift),
            Enum.GetName(typeof(Keys), Keys.Control),
            Enum.GetName(typeof(Keys), Keys.Alt),
            Enum.GetName(typeof(Keys), Keys.NoName),
            Enum.GetName(typeof(Keys), Keys.None),
            Enum.GetName(typeof(Keys), Keys.Packet),
            Enum.GetName(typeof(Keys), Keys.Modifiers),
            Enum.GetName(typeof(Keys), Keys.KeyCode)
        };

        private static Color GetDefaultColor(string propertyName)
        {
            if (propertyName == null)
                throw new ArgumentNullException("propertyName");
            string colorString = (string)Settings.Default
                .Properties[propertyName].DefaultValue;
            ColorConverter colorConverter = new ColorConverter();
            Color color = (Color)(new ColorConverter())
                .ConvertFromInvariantString(colorString);
            return color;
        }

        private static KeyBinding GetDefaultKeyBinding(string propertyName)
        {
            if (propertyName == null)
                throw new ArgumentNullException("propertyName");
            return KeyBinding.Parse((string)Settings.Default.Properties[
                propertyName].DefaultValue);
        }

        private static AnimationSpeed GetDefaultAnimationSpeed(
            string propertyName)
        {
            if (propertyName == null)
                throw new ArgumentNullException("propertyName");
            return (AnimationSpeed)Enum.Parse(typeof(AnimationSpeed),
                (string)Settings.Default.Properties[propertyName]
                .DefaultValue);
        }

        private static bool GetDefaultBooleanPropertyValue(string propertyName)
        {
            if (propertyName == null)
                throw new ArgumentNullException("propertyName");
            return bool.Parse((string)Settings.Default.Properties[propertyName]
                .DefaultValue);
        }
        public SettingsWindow()
        {
            InitializeComponent();

            //Populate animation speed combo box
            foreach (string speed in Enum.GetNames(typeof(AnimationSpeed)))
                animationSpeedComboBox.Items.Add(speed);

            //Populate shortcut shortcut key combo boxes
            PopulateShortcutKeyComboBox(toggleFullscreenModifierKeyComboBox,
                toggleFullscreenKeyComboBox);
            PopulateShortcutKeyComboBox(leaveFullscreenModifierKeyComboBox,
                leaveFullscreenKeyComboBox);
            PopulateShortcutKeyComboBox(toggleZoomModesModifierKeyComboBox,
                toggleZoomModesKeyComboBox);
            PopulateShortcutKeyComboBox(zoomInModifierKeyComboBox,
                zoomInKeyComboBox);
            PopulateShortcutKeyComboBox(zoomOutModifierKeyComboBox,
                zoomOutKeyComboBox);
            PopulateShortcutKeyComboBox(leftModifierKeyComboBox,
                leftKeyComboBox);
            PopulateShortcutKeyComboBox(rightModifierKeyComboBox,
                rightKeyComboBox);
            PopulateShortcutKeyComboBox(upModifierKeyComboBox,
                upKeyComboBox);
            PopulateShortcutKeyComboBox(downModifierKeyComboBox,
                downKeyComboBox);

            //Load settings and update UI accordingly
            LoadSettings();
        }

        private void LoadSettings()
        {
            bool successFlag = true;

            successFlag &= SelectElement(Settings.Default.AnimationSpeed, 
                animationSpeedComboBox);
            autoZoomTopLeftCheckBox.Checked =
                Settings.Default.AutoZoomImageToTopLeft;
            showTooltipsCheckbox.Checked = Settings.Default.ShowTooltips;
            //useOpenGLCheckBox.Checked = Settings.Default.UseOpenGL;

            successFlag &= ApplyShortcutToComboBox(
                Settings.Default.KeyBinding_GoThroughZoomModes,
                toggleZoomModesModifierKeyComboBox,
                toggleZoomModesKeyComboBox);
            successFlag &= ApplyShortcutToComboBox(
                Settings.Default.KeyBinding_ToggleFullscreen,
                toggleFullscreenModifierKeyComboBox,
                toggleFullscreenKeyComboBox);
            successFlag &= ApplyShortcutToComboBox(
                Settings.Default.KeyBinding_LeaveFullscreen,
                leaveFullscreenModifierKeyComboBox,
                leaveFullscreenKeyComboBox);
            successFlag &= ApplyShortcutToComboBox(
                Settings.Default.KeyBinding_ZoomIn,
                zoomInModifierKeyComboBox,
                zoomInKeyComboBox);
            successFlag &= ApplyShortcutToComboBox(
                Settings.Default.KeyBinding_ZoomOut,
                zoomOutModifierKeyComboBox,
                zoomOutKeyComboBox);
            successFlag &= ApplyShortcutToComboBox(
                Settings.Default.KeyBinding_Left,
                leftModifierKeyComboBox,
                leftKeyComboBox);
            successFlag &= ApplyShortcutToComboBox(
                Settings.Default.KeyBinding_Right,
                rightModifierKeyComboBox,
                rightKeyComboBox);
            successFlag &= ApplyShortcutToComboBox(
                Settings.Default.KeyBinding_Up,
                upModifierKeyComboBox,
                upKeyComboBox);
            successFlag &= ApplyShortcutToComboBox(
                Settings.Default.KeyBinding_Down,
                downModifierKeyComboBox,
                downKeyComboBox);

            currentCustomImagePath_ZoomButton =
                Settings.Default.CustomImagePath_ZoomButton;
            currentCustomImagePath_PreviousButton =
                Settings.Default.CustomImagePath_PreviousButton;
            currentCustomImagePath_FullscreenButton =
                Settings.Default.CustomImagePath_FullscreenButton;
            currentCustomImagePath_NextButton =
                Settings.Default.CustomImagePath_NextButton;
            currentCustomImagePath_SettingsButton =
                Settings.Default.CustomImagePath_SettingsButton;

            imageAreaColorPreview.BackColor = Settings.Default.Color_ImageArea;
            controlBarColorPreview.BackColor = 
                Settings.Default.Color_ControlBar;

            UpdateButtonStates();

            if (!successFlag) Debug.WriteLine("The settings contained " +
                "unsupported keys/modifiers or an invalid animation speed.");
        }

        private void UpdateButtonStates()
        {
            resetImageAreaColorButton.Enabled =
                imageAreaColorPreview.BackColor != pd_imageAreaColor;
            resetControlBarColorButton.Enabled =
                controlBarColorPreview.BackColor != pd_controlBarColor;

            resetZoomIconButton.Enabled = !string.IsNullOrEmpty(
                currentCustomImagePath_ZoomButton);
            resetPreviousIconButton.Enabled = !string.IsNullOrEmpty(
                currentCustomImagePath_PreviousButton);
            resetFullscreenIconButton.Enabled = !string.IsNullOrEmpty(
                currentCustomImagePath_FullscreenButton);
            resetNextIconButton.Enabled = !string.IsNullOrEmpty(
                currentCustomImagePath_NextButton);
            resetSettingsIconButton.Enabled = !string.IsNullOrEmpty(
                currentCustomImagePath_SettingsButton);
        }

        private bool ApplyShortcutToComboBox(KeyBinding keyBinding,
            ComboBox modifierKeyComboBox, ComboBox mainKeyComboBox)
        {
            return SelectElement(keyBinding.Modifier, modifierKeyComboBox)
                && SelectElement(keyBinding.Key, mainKeyComboBox);
        }

        private EnumT ParseEnumFromComboBox<EnumT>(ComboBox comboBox)
            where EnumT : struct
        {
            if (Enum.TryParse(comboBox.SelectedItem.ToString(),
                out EnumT result))
                return result;
            else throw new InvalidCastException("The specified combo box " +
                "value couldn't be parsed as an enum of the specified type!");
        }

        private KeyBinding ParseKeyBindingFromComboBox(
            ComboBox modifierKeyComboBox, ComboBox mainKeyComboBox)
        {
            Keys modKey = ParseEnumFromComboBox<Keys>(modifierKeyComboBox);
            Keys mainKey = ParseEnumFromComboBox<Keys>(mainKeyComboBox);

            return new KeyBinding(mainKey, modKey);
        }

        private bool SelectElement(object element, ComboBox comboBox)
        {
            for (int i = 0; i < comboBox.Items.Count; i++)
                if (comboBox.Items[i].ToString() == element.ToString())
                {
                    comboBox.SelectedIndex = i;
                    return true;
                }
            return false;            
        }

        private void PopulateShortcutKeyComboBox(
            ComboBox modifierKeyComboBox, ComboBox mainKeyComboBox)
        {
            foreach (string key in Enum.GetNames(typeof(Keys)))
            {
                if (!ignoredMainKeys.Contains(key))
                    mainKeyComboBox.Items.Add(key);
            }
            

            modifierKeyComboBox.Items.Add(Enum.GetName(typeof(Keys),
                Keys.None));
            modifierKeyComboBox.Items.Add(Enum.GetName(typeof(Keys),
                Keys.Control));
            modifierKeyComboBox.Items.Add(Enum.GetName(typeof(Keys),
                Keys.Alt));
            modifierKeyComboBox.Items.Add(Enum.GetName(typeof(Keys),
                Keys.Shift));
        }

        private bool OpenCustomButtonIconDialog(string buttonName,
            ref string path)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter =
                "Image Files(*.PNG;*.JPG;*.GIF;*.BMP)|*.PNG;*.JPG;*.GIF;*.BMP";
            fileDialog.Multiselect = false;
            fileDialog.Title = "Import icon for button '" + buttonName + "'";
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                path = fileDialog.FileName;
                return true;
            }
            else return false;
        }

        private void resetShortcutsButton_Click(object sender, EventArgs e)
        {
            bool errorFlag = ApplyShortcutToComboBox(pd_goThroughZoomModes,
                toggleZoomModesModifierKeyComboBox,
                toggleZoomModesKeyComboBox);
            errorFlag |= ApplyShortcutToComboBox(pd_toggleFullscreen,
                toggleFullscreenModifierKeyComboBox,
                toggleFullscreenKeyComboBox);
            errorFlag |= ApplyShortcutToComboBox(pd_leaveFullscreen,
                leaveFullscreenModifierKeyComboBox,
                leaveFullscreenKeyComboBox);
            errorFlag |= ApplyShortcutToComboBox(pd_zoomIn,
                zoomInModifierKeyComboBox, zoomInKeyComboBox);
            errorFlag |= ApplyShortcutToComboBox(pd_zoomOut,
                zoomOutModifierKeyComboBox, zoomOutKeyComboBox);
            errorFlag |= ApplyShortcutToComboBox(pd_left,
                leftModifierKeyComboBox, leftKeyComboBox);
            errorFlag |= ApplyShortcutToComboBox(pd_right,
                rightModifierKeyComboBox, rightKeyComboBox);
            errorFlag |= ApplyShortcutToComboBox(pd_up,
                upModifierKeyComboBox, upKeyComboBox);
            errorFlag |= ApplyShortcutToComboBox(pd_down,
                downModifierKeyComboBox, downKeyComboBox);

            if (errorFlag) Debug.WriteLine("The default settings contained " +
                "unsupported keys/modifiers.");
        }

        private void resetSettingsButton_Click(object sender, EventArgs e)
        {
            bool errorFlag = SelectElement(pd_animationSpeed,
                animationSpeedComboBox);
            autoZoomTopLeftCheckBox.Checked = pd_autoZoomImageToTopLeft;

            resetShortcutsButton_Click(this, EventArgs.Empty);

            imageAreaColorPreview.BackColor = pd_imageAreaColor;
            controlBarColorPreview.BackColor = pd_controlBarColor;

            currentCustomImagePath_ZoomButton = "";
            currentCustomImagePath_PreviousButton = "";
            currentCustomImagePath_FullscreenButton = "";
            currentCustomImagePath_NextButton = "";
            currentCustomImagePath_SettingsButton = "";

            UpdateButtonStates();
        }

        private void setImageAreaColorButton_Click(object sender, EventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();
            if (colorDialog.ShowDialog() == DialogResult.OK)
                imageAreaColorPreview.BackColor = colorDialog.Color;
            UpdateButtonStates();
        }

        private void setControlBarColorButton_Click(object sender, EventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();
            if (colorDialog.ShowDialog() == DialogResult.OK)
                controlBarColorPreview.BackColor = colorDialog.Color;
            UpdateButtonStates();
        }

        private void resetImageAreaColorButton_Click(object sender, 
            EventArgs e)
        {
            imageAreaColorPreview.BackColor = pd_imageAreaColor;
            UpdateButtonStates();
        }

        private void resetControlBarColorButton_Click(object sender, 
            EventArgs e)
        {
            controlBarColorPreview.BackColor = pd_controlBarColor;
            UpdateButtonStates();
        }

        private void setZoomIconButton_Click(object sender, EventArgs e)
        {
            OpenCustomButtonIconDialog("Zoom",
                ref currentCustomImagePath_ZoomButton);
            UpdateButtonStates();
        }

        private void setPreviousIconButton_Click(object sender, EventArgs e)
        {
            OpenCustomButtonIconDialog("Previous",
                ref currentCustomImagePath_PreviousButton);
            UpdateButtonStates();
        }

        private void setFullscreenIconButton_Click(object sender, EventArgs e)
        {
            OpenCustomButtonIconDialog("Fullscreen",
                ref currentCustomImagePath_FullscreenButton);
            UpdateButtonStates();
        }

        private void setNextIconButton_Click(object sender, EventArgs e)
        {
            OpenCustomButtonIconDialog("Next",
                ref currentCustomImagePath_NextButton);
            UpdateButtonStates();
        }

        private void setSettingsIconButton_Click(object sender, EventArgs e)
        {
            OpenCustomButtonIconDialog("Settings",
                ref currentCustomImagePath_SettingsButton);
            UpdateButtonStates();
        }

        private void resetZoomIconButton_Click(object sender, EventArgs e)
        {
            currentCustomImagePath_ZoomButton = "";
            UpdateButtonStates();
        }

        private void resetPreviousIconButton_Click(object sender, EventArgs e)
        {
            currentCustomImagePath_PreviousButton = "";
            UpdateButtonStates();
        }

        private void resetFullscreenIconButton_Click(object sender, 
            EventArgs e)
        {
            currentCustomImagePath_FullscreenButton = "";
            UpdateButtonStates();
        }

        private void resetNextIconButton_Click(object sender, EventArgs e)
        {
            currentCustomImagePath_NextButton = "";
            UpdateButtonStates();
        }

        private void resetSettingsIconButton_Click(object sender, EventArgs e)
        {
            currentCustomImagePath_SettingsButton = "";
            UpdateButtonStates();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            Settings.Default.AnimationSpeed =
                ParseEnumFromComboBox<AnimationSpeed>(animationSpeedComboBox);
            Settings.Default.AutoZoomImageToTopLeft =
                autoZoomTopLeftCheckBox.Checked;
            Settings.Default.ShowTooltips = showTooltipsCheckbox.Checked;

            Settings.Default.KeyBinding_ToggleFullscreen =
                ParseKeyBindingFromComboBox(
                    toggleFullscreenModifierKeyComboBox,
                    toggleFullscreenKeyComboBox);
            Settings.Default.KeyBinding_LeaveFullscreen =
                ParseKeyBindingFromComboBox(leaveFullscreenModifierKeyComboBox,
                leaveFullscreenKeyComboBox);
            Settings.Default.KeyBinding_GoThroughZoomModes =
                ParseKeyBindingFromComboBox(toggleZoomModesModifierKeyComboBox,
                toggleZoomModesKeyComboBox);
            Settings.Default.KeyBinding_ZoomIn =
                ParseKeyBindingFromComboBox(zoomInModifierKeyComboBox,
                zoomInKeyComboBox);
            Settings.Default.KeyBinding_ZoomOut =
                ParseKeyBindingFromComboBox(zoomOutModifierKeyComboBox,
                zoomOutKeyComboBox);
            Settings.Default.KeyBinding_Left =
                ParseKeyBindingFromComboBox(leftModifierKeyComboBox,
                leftKeyComboBox);
            Settings.Default.KeyBinding_Right =
                ParseKeyBindingFromComboBox(rightModifierKeyComboBox,
                rightKeyComboBox);
            Settings.Default.KeyBinding_Up =
                ParseKeyBindingFromComboBox(upModifierKeyComboBox,
                upKeyComboBox);
            Settings.Default.KeyBinding_Down =
                ParseKeyBindingFromComboBox(downModifierKeyComboBox,
                downKeyComboBox);

            Settings.Default.Color_ImageArea = imageAreaColorPreview.BackColor;
            Settings.Default.Color_ControlBar =
                controlBarColorPreview.BackColor;

            Settings.Default.CustomImagePath_ZoomButton = 
                ImportIcon(currentCustomImagePath_ZoomButton, "zoom");
            Settings.Default.CustomImagePath_PreviousButton = 
                ImportIcon(currentCustomImagePath_PreviousButton, "previous");
            Settings.Default.CustomImagePath_FullscreenButton =
                ImportIcon(currentCustomImagePath_FullscreenButton,
                "fullscreen");
            Settings.Default.CustomImagePath_NextButton =
                ImportIcon(currentCustomImagePath_NextButton, "next");
            Settings.Default.CustomImagePath_SettingsButton =
                ImportIcon(currentCustomImagePath_SettingsButton, "settings");

            Settings.Default.Save();
        }

        private string ImportIcon(string currentPath, string newIconName)
        {
            if (string.IsNullOrEmpty(currentPath)) return "";

            string targetFolder = Path.Combine(
                Application.LocalUserAppDataPath, "media");
            string newPath = Path.Combine(targetFolder,
                newIconName + ".png");

            if (newPath == currentPath) return currentPath;

            if (File.Exists(currentPath))
            {
                try
                {
                    using (Image icon = Image.FromFile(currentPath))
                    {
                        if (!Directory.Exists(targetFolder))
                            Directory.CreateDirectory(targetFolder);

                        if (File.Exists(newPath)) File.Delete(newPath);
                        icon.Save(newPath);
                        return newPath;
                    }
                }
                catch { }
            }
            MessageBox.Show("The custom icon for the " + newIconName +
                " button couldn't be imported! The current icon is " +
                "retained.", "Invalid image", MessageBoxButtons.OK,
                MessageBoxIcon.Exclamation);

            return "";
        }

        private void licenseButton_Click(object sender, EventArgs e)
        {
            if (!File.Exists(ReadmeFileName))
                MessageBox.Show("The license information file couldn't be " +
                    "found!\nPlease reinstall or update the application.",
                    "Missing application files", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            else Process.Start(ReadmeFileName);
        }
    }
}
