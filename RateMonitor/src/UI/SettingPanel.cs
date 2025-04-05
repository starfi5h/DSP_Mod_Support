using BepInEx.Configuration;
using UnityEngine;

namespace RateMonitor.UI
{
    public class SettingPanel // 設置面板
    {
        public bool IsActive { get; set; }

        Vector2 scrollPosition;
        string rateUnitInput;
        string incLevelInput;

        public SettingPanel(StatTable _)
        {
        }

        public void RefreshInputs()
        {
            rateUnitInput = ModSettings.RateUnit.Value.ToString();
            incLevelInput = ModSettings.IncLevel.Value.ToString();
        }

        public void DrawPanel()
        {            
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(SP.uiSettingsText);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            ConfigBoolField(SP.showRealTimeRateText, ModSettings.ShowRealtimeRate);
            ConfigIntField(SP.rateUnitText, ref rateUnitInput, ModSettings.RateUnit, 1, 14400);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(SP.perMinuteText, GUILayout.Width(Utils.InputWidth)))
            {
                ModSettings.RateUnit.Value = 1;
                RefreshInputs();
                UIWindow.RefreshTitle();
            }
            if (GUILayout.Button(SP.perSecondText, GUILayout.Width(Utils.InputWidth)))
            {
                ModSettings.RateUnit.Value = 60;
                RefreshInputs();
                UIWindow.RefreshTitle();
            }
            for (int i = 0; i < 3; i++)
            {
                if (GUILayout.Button(SP.perBeltTexts[i], GUILayout.Width(Utils.InputWidth)))
                {
                    ModSettings.RateUnit.Value = CalDB.BeltSpeeds[i] * CalDB.MaxBeltStack;
                    RefreshInputs();
                    UIWindow.RefreshTitle();
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(SP.calculateSettingsText);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            bool needRecalculate = false;
            if (ConfigBoolField(SP.forceIncText, ModSettings.ForceInc)) needRecalculate = true;
            if (ConfigIntField(SP.incLevelText, ref incLevelInput, ModSettings.IncLevel, -1, 10)) needRecalculate = true;
            if (needRecalculate) 
            {
                var entityIds = Plugin.MainTable.GetEntityIds(out var factory);
                Plugin.CreateMainTable(factory, entityIds);
            }
            GUILayout.EndVertical();

            GUILayout.EndScrollView();            
        }

        public bool ConfigIntField(string name, ref string fieldString, ConfigEntry<int> configEntry, int min, int max = int.MaxValue)
        {
            bool isChanged = false;
            GUILayout.BeginHorizontal();
            GUILayout.Label(name);
            fieldString = GUILayout.TextField(fieldString, GUILayout.Width(Utils.InputWidth));
            if (GUILayout.Button(SP.settingButtonText, GUILayout.Width(Utils.ShortButtonWidth)))
            {
                if (int.TryParse(fieldString, out int value))
                {
                    configEntry.Value = (int)Maths.Clamp(value, min, max);
                    isChanged = true;
                }
                else
                {
                    fieldString = configEntry.Value.ToString();
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            return isChanged;
        }

        public bool ConfigBoolField(string name, ConfigEntry<bool> configEntry)
        {
            bool oldValue = configEntry.Value;
            bool newValue = GUILayout.Toggle(oldValue, name);
            if (oldValue != newValue)
            {
                configEntry.Value = newValue;
                return true;
            }
            return false;
        }
    }
}
