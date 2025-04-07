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
            GUILayout.BeginVertical(GUI.skin.box);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            #region UI Settings 介面設定
            GUILayout.BeginVertical(GUI.skin.box);
            MiddleLabel(SP.uiSettingsText);

            GUILayout.BeginHorizontal();
            ConfigBoolField(SP.showRealTimeRateText, ModSettings.ShowRealtimeRate);
            ConfigBoolField(SP.showInPercentageText, ModSettings.ShowWorkingRateInPercentage);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            ConfigIntField(SP.rateUnitText, ref rateUnitInput, ModSettings.RateUnit, 1, 14400);
            if (GUILayout.Button("x2", GUILayout.Width(Utils.ShortButtonWidth)))
            {
                ModSettings.RateUnit.Value *= 2;
                RefreshInputs();
                UIWindow.RefreshTitle();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

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
                    ModSettings.RateUnit.Value = CalDB.BeltSpeeds[i];
                    RefreshInputs();
                    UIWindow.RefreshTitle();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            #endregion

            #region Calculate Settings 計算設定
            GUILayout.BeginVertical(GUI.skin.box);

            MiddleLabel(SP.calculateSettingsText);

            bool needRecalculate = false;
            GUILayout.BeginHorizontal();
            if (ConfigBoolField(SP.forceIncText, ModSettings.ForceInc)) needRecalculate = true;
            if (ConfigBoolField(SP.forceLens, ModSettings.ForceLens)) needRecalculate = true;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (ConfigIntField(SP.incLevelText, ref incLevelInput, ModSettings.IncLevel, -1, 10)) needRecalculate = true;
            if (GUILayout.Button("-1", GUILayout.Width(Utils.ShortButtonWidth)))
            {
                if (ModSettings.IncLevel.Value >= 0) ModSettings.IncLevel.Value--;
                needRecalculate = true;
            }
            if (GUILayout.Button("+1", GUILayout.Width(Utils.ShortButtonWidth)))
            {
                if (ModSettings.IncLevel.Value < 10) ModSettings.IncLevel.Value++;
                needRecalculate = true;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (needRecalculate) 
            {
                var entityIds = Plugin.MainTable.GetEntityIds(out var factory);
                Plugin.CreateMainTable(factory, entityIds);
            }

            GUILayout.EndVertical();
            #endregion

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void MiddleLabel(string label)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(label);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private bool ConfigIntField(string name, ref string fieldString, ConfigEntry<int> configEntry, int min, int max = int.MaxValue)
        {
            bool isChanged = false;
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
            return isChanged;
        }

        private bool ConfigBoolField(string name, ConfigEntry<bool> configEntry)
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
