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
        bool use4stack = false;

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

            DisplaySettingsPanel(); //顯示設定
            CalculateSettingsPanel(); //計算設定
            UISettingsPanel(); //UI設定

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        public void RateUnitQuickBar()
        {
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
                if (GUILayout.Button(SP.perBeltTexts[i], GUILayout.Width(Utils.MediumButtonWidth - 2)))
                {
                    ModSettings.RateUnit.Value = use4stack ? CalDB.BeltSpeeds[i] * 4 : CalDB.BeltSpeeds[i];
                    RefreshInputs();
                    UIWindow.RefreshTitle();
                }
            }
            use4stack = GUILayout.Toggle(use4stack, "4 stack");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        public void CountMultiplierSetter()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(SP.countMultiplierText + ": " + CalDB.CountMultiplier.ToString() + " ");
            if (GUILayout.Button("-5", GUILayout.Width(Utils.RateWidth))) CalDB.CountMultiplier -= 5;
            if (GUILayout.Button("-1", GUILayout.Width(Utils.RateWidth))) CalDB.CountMultiplier -= 1;
            if (GUILayout.Button("+1", GUILayout.Width(Utils.RateWidth))) CalDB.CountMultiplier += 1;
            if (GUILayout.Button("+5", GUILayout.Width(Utils.RateWidth))) CalDB.CountMultiplier += 5;
            if (GUILayout.Button(SP.recalculateText, GUILayout.Width(Utils.InputWidth)))
            {
                var entityIds = Plugin.MainTable.GetEntityIds(out var factory);
                Plugin.CreateMainTable(factory, entityIds);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (CalDB.CountMultiplier < 1) CalDB.CountMultiplier = 1;
        }

        private void DisplaySettingsPanel()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            MiddleLabel(SP.displaySettingsText);

            GUILayout.BeginHorizontal();
            ConfigBoolField(SP.showRealTimeRateText, ModSettings.ShowRealtimeRate);
            ConfigBoolField(SP.showInPercentageText, ModSettings.ShowWorkingRateInPercentage);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Rate Unit Input settings
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

            // Rate Unit Quick Bar
            RateUnitQuickBar();

            // Font Size Settings
            GUILayout.BeginHorizontal();
            int fontSize = (int)GUILayout.HorizontalSlider(ModSettings.FontSize.Value, 8f, 48f, GUILayout.Width(Utils.InputWidth * 4));
            if (fontSize != ModSettings.FontSize.Value)
            {
                ModSettings.FontSize.Value = fontSize;
                Utils.SetScale(fontSize);
            }
            GUILayout.Label(SP.fontSizeText + ": " + ModSettings.FontSize.Value);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void CalculateSettingsPanel()
        {
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
        }

        private void UISettingsPanel()
        {
            GUILayout.BeginVertical(GUI.skin.box);

            MiddleLabel(SP.uiSettingsText);
            ConfigBoolField(SP.enableQuickBarButtonText, ModSettings.EnableQuickBarButton);
            ConfigBoolField(SP.enableSingleBuildingText, ModSettings.EnableSingleBuildingClick);

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
