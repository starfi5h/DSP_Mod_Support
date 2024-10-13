using BepInEx.Configuration;
using System;
using System.Linq;
using UnityEngine;

namespace CameraTools
{
    public static class Util
    {
        public static void UniverseSimulatorGameTick(in VectorLF3 uPosition)
        {
            UniverseSimulator universe = GameMain.universeSimulator;
            universe.backgroundStars.transform.position = Camera.main.transform.position;
            if (GameMain.localPlanet != null)
            {
                universe.backgroundStars.transform.rotation = Quaternion.Inverse(GameMain.localPlanet.runtimeRotation);
            }
            else
            {
                universe.backgroundStars.transform.rotation = Quaternion.identity;
            }
            Vector3 position = GameMain.mainPlayer.position;
            //VectorLF3 uPosition = GameMain.mainPlayer.uPosition; // replace with camera's recorded player upos
            Vector3 position2 = GameCamera.main.transform.position;
            Quaternion rotation = GameCamera.main.transform.rotation;
            for (int i = 0; i < universe.starSimulators.Length; i++)
            {
                universe.starSimulators[i].UpdateUniversalPosition(position, uPosition, position2, rotation);
            }
            if (universe.planetSimulators != null)
            {
                for (int j = 0; j < universe.planetSimulators.Length; j++)
                {
                    if (universe.planetSimulators[j] != null)
                    {
                        universe.planetSimulators[j].UpdateUniversalPosition(uPosition, position2);
                    }
                }
            }
        }

        public static void SetWindowPos(ref Rect windowRect, ConfigEntry<Vector2> posConfigEntry, bool reset)
        {
            windowRect.position = reset ? (Vector2)posConfigEntry.DefaultValue : posConfigEntry.Value;
            windowRect = EnsureWindowInsideScreen(windowRect);
        }

        public static Rect EnsureWindowInsideScreen(Rect window)
        {
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            // Check if the window exceeds the screen's left and right edges
            if (window.x < 0)
                window.x = 0; // Set to left edge

            if (window.xMax > screenWidth)
                window.x = screenWidth - window.width; // Set to right edge

            // Check if the window exceeds the screen's top and bottom edges
            if (window.y < 0)
                window.y = 0; // Set to top edge

            if (window.yMax > screenHeight)
                window.y = screenHeight - window.height; // Set to bottom edge

            return window;
        }

        static string editingField;
        static string editingText;

        public static void AddFloatField(string label, ref float value, float delta = 0)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.MinWidth(10));
            if (editingField != label)
            {
                GUILayout.Label(value.ToString(), GUILayout.MinWidth(35));
                if (GUILayout.Button("edit"))
                {
                    editingField = label;
                    editingText = value.ToString();
                }
            }
            else
            {
                editingText = GUILayout.TextField(editingText, 10, GUILayout.MinWidth(35));
                if (GUILayout.Button("set"))
                {
                    editingField = "";
                    float.TryParse(editingText, out float inputValue);
                    value = inputValue;
                }
            }
            if (delta != 0f)
            {
                if (GUILayout.Button("-")) value -= delta;
                if (GUILayout.Button("+")) value += delta;
            }
            GUILayout.EndHorizontal();
        }

        public static void AddFloatFieldInput(string label, ref float value, float delta = 0)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.MinWidth(10));
            if (editingField != label)
            {
                GUILayout.TextField(value.ToString(), 6, GUILayout.MaxWidth(60));
                if (GUILayout.Button("edit", GUILayout.MaxWidth(40)))
                {
                    editingField = label;
                    editingText = value.ToString();
                }
            }
            else
            {
                editingText = GUILayout.TextField(editingText, 6, GUILayout.MaxWidth(60));
                if (GUILayout.Button("set", GUILayout.MaxWidth(40)))
                {
                    editingField = "";
                    float.TryParse(editingText, out float inputValue);
                    value = inputValue;
                }
            }
            if (delta != 0f)
            {
                if (GUILayout.Button("-", GUILayout.MaxWidth(40))) value -= delta;
                if (GUILayout.Button("+", GUILayout.MaxWidth(40))) value += delta;
            }
            GUILayout.EndHorizontal();
        }

        public static void AddToggleField(ConfigEntry<bool> configEntry)
        {
            GUILayout.BeginHorizontal();
            configEntry.Value = GUILayout.Toggle(configEntry.Value, configEntry.Definition.Key);
            GUILayout.EndHorizontal();
        }

        static string waitingKeyBindField = "";
        static KeyCode lastKey = KeyCode.None;
        static readonly KeyCode[] modKeys = { KeyCode.RightShift, KeyCode.LeftShift,
                 KeyCode.RightControl, KeyCode.LeftControl,
                 KeyCode.RightAlt, KeyCode.LeftAlt,
                 KeyCode.LeftCommand,  KeyCode.LeftApple, KeyCode.LeftWindows,
                 KeyCode.RightCommand,  KeyCode.RightApple, KeyCode.RightWindows };

        public static void AddKeyBindField(ConfigEntry<KeyboardShortcut> configEntry)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(configEntry.Definition.Key, GUILayout.MinWidth(10));
            if (waitingKeyBindField != configEntry.Definition.Key)
            {
                GUILayout.Label(configEntry.GetSerializedValue(), GUILayout.MaxWidth(100));
                if (GUILayout.Button("edit", GUILayout.MaxWidth(40)))
                {
                    waitingKeyBindField = configEntry.Definition.Key;
                }
            }
            else
            {
                GUILayout.TextField("Waiting for key..".Translate(), GUILayout.MaxWidth(100));
                if (Input.GetKey(KeyCode.Escape) || Input.GetKey(KeyCode.Mouse0))
                {
                    waitingKeyBindField = null;
                }
                else if (IsKeyInput())
                {
                    configEntry.Value = KeyboardShortcut.Deserialize(GetPressedKeysString());
                    waitingKeyBindField = null;
                }
                if (GUILayout.Button("abort", GUILayout.MaxWidth(40)))
                {
                    waitingKeyBindField = null;
                }
            }
            GUILayout.EndHorizontal();
        }

        public static string GetPressedKeysString()
        {
            var key = lastKey.ToString();
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }
            var mod = "";
            foreach (var modKey in modKeys)
            {
                if (Input.GetKey(modKey))
                {
                    mod += "+" + modKey.ToString();
                }
            }
            if (!string.IsNullOrEmpty(mod))
            {
                key += mod;
            }
            return key;
        }

        public static bool IsKeyInput()
        {
            foreach (KeyCode item in Enum.GetValues(typeof(KeyCode)))
            {
                if (item != KeyCode.None && !modKeys.Contains(item) && Input.GetKey(item))
                {
                    lastKey = item;
                    return true;
                }
            }
            return false;
        }
    }
}
