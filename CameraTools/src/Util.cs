﻿using BepInEx.Configuration;
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
            universe.backgroundStars.transform.rotation = GameMain.localPlanet != null ? Quaternion.Inverse(GameMain.localPlanet.runtimeRotation) : Quaternion.identity;
            Vector3 position = GameMain.mainPlayer.position;
            //VectorLF3 uPosition = GameMain.mainPlayer.uPosition; // replace with camera's recorded player upos
            Vector3 position2 = GameCamera.main.transform.position;
            Quaternion rotation = GameCamera.main.transform.rotation;
            foreach (var starSimulator in universe.starSimulators)
            {
                starSimulator.UpdateUniversalPosition(position, uPosition, position2, rotation);
            }
            if (universe.planetSimulators != null)
            {
                foreach (var planetSimulator in universe.planetSimulators)
                {
                    planetSimulator?.UpdateUniversalPosition(uPosition, position2);
                }
            }
        }

        public static string ToString(VectorLF3 vectorLF3, string format = "F0")
        {
            return string.Format("[{0}, {1}, {2}]", 
                vectorLF3.x.ToString(format),
                vectorLF3.y.ToString(format),
                vectorLF3.z.ToString(format)
            );
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

        public static bool AddFloatField(string label, ref float value, float delta = 0, float minInputWidth = 35)
        {
            bool hasChanged = false;
            GUILayout.BeginHorizontal();
            GUILayout.Label(label.Translate(), GUILayout.MinWidth(10));
            if (editingField != label)
            {
                GUILayout.Label(value.ToString("G6"), GUILayout.MinWidth(minInputWidth));
                if (GUILayout.Button("edit", GUILayout.MaxWidth(40)))
                {
                    editingField = label;
                    editingText = value.ToString();
                }
            }
            else
            {
                editingText = GUILayout.TextField(editingText, 8, GUILayout.MinWidth(minInputWidth));
                if (GUILayout.Button("set", GUILayout.MaxWidth(40)))
                {
                    editingField = "";
                    if (float.TryParse(editingText, out float inputValue))
                    {
                        hasChanged = value != inputValue;
                        value = inputValue;
                    }
                }
            }
            if (delta != 0f)
            {
                if (GUILayout.Button("-", GUILayout.MaxWidth(35))) { value -= delta; hasChanged = true; }
                if (GUILayout.Button("+", GUILayout.MaxWidth(35))) { value += delta; hasChanged = true; }
            }
            GUILayout.EndHorizontal();
            return hasChanged;
        }

        public static bool AddDoubleField(string label, ref double value, double delta = 0)
        {
            bool hasChanged = false;
            GUILayout.BeginHorizontal();
            GUILayout.Label(label.Translate(), GUILayout.MinWidth(10));
            if (editingField != label)
            {
                GUILayout.Label(value.ToString("G8"), GUILayout.MinWidth(35));
                if (GUILayout.Button("edit", GUILayout.MaxWidth(40)))
                {
                    editingField = label;
                    editingText = value.ToString();
                }
            }
            else
            {
                editingText = GUILayout.TextField(editingText, 9, GUILayout.MinWidth(35));
                if (GUILayout.Button("set", GUILayout.MaxWidth(40)))
                {
                    editingField = "";
                    if (double.TryParse(editingText, out double inputValue))
                    {
                        hasChanged = value != inputValue;
                        value = inputValue;
                    }
                }
            }
            if (delta != 0f)
            {
                if (GUILayout.Button("-", GUILayout.MaxWidth(35))) { value -= delta; hasChanged = true; }
                if (GUILayout.Button("+", GUILayout.MaxWidth(35))) { value += delta; hasChanged = true; }
            }
            GUILayout.EndHorizontal();
            return hasChanged;
        }

        public static void AddFloatFieldInput(string label, ref float value, float delta = 0)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label.Translate(), GUILayout.MinWidth(10));
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
                if (GUILayout.Button("-", GUILayout.MaxWidth(35))) value -= delta;
                if (GUILayout.Button("+", GUILayout.MaxWidth(35))) value += delta;
            }
            GUILayout.EndHorizontal();
        }

        public static string AddTextFieldInput(string label, string value)
        {
            string newText = null;
            GUILayout.BeginHorizontal();
            GUILayout.Label(label.Translate());
            if (editingField != label)
            {
                if (value.Length > 20)
                {
                    GUILayout.Label(".." + value.Substring(value.Length - 20));
                }
                else
                {
                    GUILayout.Label(value);
                }
                if (GUILayout.Button("edit", GUILayout.MaxWidth(40)))
                {
                    editingField = label;
                    editingText = value;
                }
            }
            else
            {
                editingText = GUILayout.TextField(editingText);
                if (GUILayout.Button("set", GUILayout.MaxWidth(40)))
                {
                    editingField = "";
                    newText = editingText;
                }
            }
            GUILayout.EndHorizontal();
            return newText;
        }

        public static void ConfigIntField(ConfigEntry<int> configEntry)
        {
            GUILayout.BeginHorizontal();
            string label = configEntry.Definition.Key.Translate();
            GUILayout.Label(label);
            GUILayout.FlexibleSpace();
            if (editingField != label)
            {
                string valueText = configEntry.Value.ToString();
                GUILayout.Label(valueText, GUILayout.MaxWidth(100));
                if (GUILayout.Button("edit", GUILayout.MaxWidth(40)))
                {
                    editingField = label;
                    editingText = valueText;
                }
            }
            else
            {
                editingText = GUILayout.TextField(editingText, 8, GUILayout.MaxWidth(100));
                if (GUILayout.Button("set", GUILayout.MaxWidth(40)))
                {
                    editingField = "";
                    int.TryParse(editingText, out int inputValue);
                    configEntry.Value = inputValue;
                }
            }
            GUILayout.EndHorizontal();
        }

        public static void ConfigFloatField(ConfigEntry<float> configEntry)
        {
            GUILayout.BeginHorizontal();
            string label = configEntry.Definition.Key.Translate();
            GUILayout.Label(label);
            GUILayout.FlexibleSpace();
            if (editingField != label)
            {
                string valueText = configEntry.Value.ToString();
                GUILayout.Label(valueText, GUILayout.MaxWidth(100));
                if (GUILayout.Button("edit", GUILayout.MaxWidth(40)))
                {
                    editingField = label;
                    editingText = valueText;
                }
            }
            else
            {
                editingText = GUILayout.TextField(editingText, 8, GUILayout.MaxWidth(100));
                if (GUILayout.Button("set", GUILayout.MaxWidth(40)))
                {
                    editingField = "";
                    float.TryParse(editingText, out float inputValue);
                    configEntry.Value = inputValue;
                }
            }
            GUILayout.EndHorizontal();
        }

        public static void ConfigToggleField(ConfigEntry<bool> configEntry)
        {
            GUILayout.BeginHorizontal();
            configEntry.Value = GUILayout.Toggle(configEntry.Value, configEntry.Definition.Key.Translate());
            GUILayout.EndHorizontal();
        }

        public static void ConfigStringField(ConfigEntry<string> configEntry)
        {
            GUILayout.BeginHorizontal();
            string label = configEntry.Definition.Key.Translate();
            GUILayout.Label(label);            
            if (editingField != label)
            {
                GUILayout.FlexibleSpace();
                string valueText = configEntry.Value;
                GUILayout.Label(valueText, GUILayout.MaxWidth(100));
                if (GUILayout.Button("edit", GUILayout.MaxWidth(40)))
                {
                    editingField = label;
                    editingText = valueText;
                }
            }
            else
            {
                editingText = GUILayout.TextField(editingText);
                if (GUILayout.Button("set", GUILayout.MaxWidth(40)))
                {
                    editingField = "";
                    configEntry.Value = editingText;
                }
            }
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
            GUILayout.Label(configEntry.Definition.Key.Translate(), GUILayout.MinWidth(10));
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
                if (GUILayout.Button("clear", GUILayout.MaxWidth(40)))
                {
                    configEntry.Value = KeyboardShortcut.Empty;
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
