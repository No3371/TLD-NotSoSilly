using Il2Cpp;
using ModSettings;
using UnityEngine;

namespace NotSoSillyMod
{
    internal static class Settings
    {
        public static void OnLoad()
        {
            options = new NotSoSillySettings();
            options.AddToModSettings("NotSoSilly Settings");
        }

        public static NotSoSillySettings options;
    }

    internal class NotSoSillySettings : JsonModSettings
    {
        [Name("Retrieve Falling Gear Key")]
        public KeyCode retrieveKey = KeyCode.Backspace;
        [Name("Toggle Gear Gravity")]
        public KeyCode toggle = KeyCode.O;

    }
}
