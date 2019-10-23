using System;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.Windows.Forms;

namespace Vido
{
    /// <summary>
    /// Represents a key binding for a keyboard shortcut of a program function.
    /// </summary>
    [SettingsSerializeAs(SettingsSerializeAs.String)]
    [TypeConverter(typeof(Converter))]
    public class KeyBinding
    {
        /// <summary>
        /// Defines a class which can be used to (de-)serialize a 
        /// <see cref="KeyBinding"/> from or to a string.
        /// </summary>
        internal class Converter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, 
                Type sourceType)
            {
                return sourceType == typeof(string);
            }

            public override object ConvertFrom(ITypeDescriptorContext context,
                CultureInfo culture, object value)
            {
                if (value is string)
                    return Parse((string)value);
                return base.ConvertFrom(context, culture, value);
            }

            public override object ConvertTo(ITypeDescriptorContext context, 
                CultureInfo culture, object value, Type destType)
            {
                if (destType == typeof(string))
                    return ((KeyBinding)value).ToString();
                return base.ConvertTo(context, culture, value, destType);
            }
        }

        /// <summary>
        /// Defines the character which is used to separate the name of the
        /// modifier key (first) and the name of the main key (second) in the
        /// <see cref="string"/> representation of a <see cref="KeyBinding"/>.
        /// If no modifier key is used, the separator is not used.
        /// </summary>
        private const char KeyModifierSeparator = '+';

        /// <summary>
        /// Gets or sets the main key code.
        /// </summary>
        public Keys Key { get; set; }

        /// <summary>
        /// Gets or sets the modifier key code.
        /// </summary>
        public Keys Modifier { get; set; }

        /// <summary>
        /// Gets a boolean indicating whether the current 
        /// <see cref="KeyBinding"/> has a modifier key which needs to be
        /// pressed simultaneously with the main <see cref="Key"/>.
        /// </summary>
        public bool HasModifier => Modifier != Keys.None;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyBinding"/> class
        /// with both the <see cref="Key"/> and the <see cref="Modifier"/>
        /// set to <see cref="Keys.None"/>.
        /// </summary>
        public KeyBinding()
        {
            Key = Keys.None;
            Modifier = Keys.None;
        }

        /// <summary>
        /// Initializes an new instance of the <see cref="KeyBinding"/> class
        /// without a modifier key.
        /// </summary>
        /// <param name="key">The key code.</param>
        public KeyBinding(Keys key)
        {
            Key = key;
            Modifier = Keys.None;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyBinding"/> class
        /// with a modifier key.
        /// </summary>
        /// <param name="key">The key code.</param>
        /// <param name="modifier">The modifier key code.</param>
        public KeyBinding(Keys key, Keys modifier)
        {
            Key = key;
            Modifier = modifier;
        }

        /// <summary>
        /// Checks if the current <see cref="KeyBinding"/> is activated by
        /// the keystrokes in a <see cref="KeyEventArgs"/> object.
        /// </summary>
        /// <param name="args">
        /// The key event args this <see cref="KeyBinding"/> should be 
        /// checked against.
        /// </param>
        /// <returns>
        /// <c>true</c> 
        /// </returns>
        public bool IsActivated(KeyEventArgs args)
        {
            return args.KeyCode == Key && args.Modifiers == Modifier;
        }

        /// <summary>
        /// Returns a string that represents the current 
        /// <see cref="KeyBinding"/>.
        /// </summary>
        /// <returns>A string that represents the current 
        /// <see cref="KeyBinding"/>.</returns>
        public override string ToString()
        {
            if (HasModifier) return Enum.GetName(typeof(Keys), Modifier)
                    + KeyModifierSeparator + Enum.GetName(typeof(Keys), Key);
            else return Enum.GetName(typeof(Keys), Key);
        }

        /// <summary>
        /// Parses a key binding string and creates a new 
        /// <see cref="KeyBinding"/> instance.
        /// </summary>
        /// <param name="keybindingString">
        /// The string in the format of the <see cref="ToString"/> methods'
        /// output.
        /// </param>
        /// <returns>
        /// A new instance of the <see cref="KeyBinding"/> struct.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="keybindingString"/> is null.
        /// </exception>
        /// <exception cref="FormatException">
        /// Is thrown when <paramref name="keybindingString"/> has an 
        /// invalid format.
        /// </exception>
        public static KeyBinding Parse(string keybindingString)
        {
            if (keybindingString == null)
                throw new ArgumentNullException("keybindingString");

            string[] keyNames = keybindingString.Split(KeyModifierSeparator);

            if (keyNames.Length > 2)
                throw new FormatException("The specified string was no " +
                    "valid keybinding string!");

            try
            {
                if (keyNames.Length == 1)
                    return new KeyBinding(ParseKey(keyNames[0]));
                else
                    return new KeyBinding(ParseKey(keyNames[1]),
                        ParseKey(keyNames[0]));
            }
            catch (FormatException) { throw; }
        }

        private static Keys ParseKey(string keyString)
        {
            try { return (Keys)Enum.Parse(typeof(Keys), keyString); }
            catch { throw new FormatException("The key name was invalid!"); }
        }
    }
}
