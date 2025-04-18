using UnityEngine;

namespace RateMonitor.UI
{
    public static class Utils
    {
        public static bool IsInit = false;
        public static GUISkin CustomSkin;
        public static float BaseScale = 16;
        public static float RecordHeight = 24;
        public static float IconHeight = 32;
        public static float RateWidth = 56;
        public static float ShortButtonWidth = 72;
        public static float LargeButtonWidth = 200;
        public static float InputWidth = 100;

        static GUILayoutOption[] iconoptions;
        static GUIContent iconTextContent;
        static GUIStyle itemIconStyle;
        static GUIStyle focusItemIconStyle;
        static GUIStyle normalIconStyle;

        public static void SetScale(float baseScale)
        {
            float scale = baseScale / 16.0f;
            BaseScale = baseScale;
            RecordHeight = 24 * scale;
            IconHeight = 32 * scale;
            RateWidth = 56 * scale;
            ShortButtonWidth = 72 * scale;
            LargeButtonWidth = 200 * scale;
            InputWidth = 100 * scale;
            iconoptions = new[] { GUILayout.Width(IconHeight), GUILayout.Height(IconHeight) };

            int fontSize = (int)(baseScale + 0.5f);
            UpdateFontSize(fontSize);
        }

        public static void OnDestroy()
        {
            Object.Destroy(CustomSkin);
        }

        public static void Init()
        {
            IsInit = true;
            CustomSkin = ScriptableObject.CreateInstance<GUISkin>();
            CloneCurrentSkinSettings();

            iconoptions = new[] { GUILayout.Width(IconHeight), GUILayout.Height(IconHeight) };
            iconTextContent = new GUIContent();
            
            var normalBackgroundYellow = new Texture2D(1, 1);
            normalBackgroundYellow.SetPixel(0, 0, new Color(1f, 1f, 0.8f, 0.6f)); // Light yellow (R, G, B)
            normalBackgroundYellow.Apply();
            
            var hoveredBackgroundYellow = new Texture2D(1, 1);
            hoveredBackgroundYellow.SetPixel(0, 0, new Color(1f, 1f, 0.7f, 0.3f)); // Darker light yellow
            hoveredBackgroundYellow.Apply();

            var hoveredBackgroundBlue = new Texture2D(1, 1);
            hoveredBackgroundBlue.SetPixel(0, 0, new Color(0.5f, 0.5f, 1f, 0.3f)); // Light blue
            hoveredBackgroundBlue.Apply();

            normalIconStyle = new GUIStyle();
            normalIconStyle.hover.background = hoveredBackgroundBlue;

            itemIconStyle = new GUIStyle();
            itemIconStyle.hover.background = hoveredBackgroundYellow;

            focusItemIconStyle = new GUIStyle();
            focusItemIconStyle.normal.background = normalBackgroundYellow;
            focusItemIconStyle.hover.background = hoveredBackgroundYellow;
        }

        static void CloneCurrentSkinSettings()
        {
            if (CustomSkin == null || GUI.skin == null) return;

            // Clone all styles from the current skin
            CustomSkin.box = new GUIStyle(GUI.skin.box);
            CustomSkin.button = new GUIStyle(GUI.skin.button);
            CustomSkin.label = new GUIStyle(GUI.skin.label);
            CustomSkin.textField = new GUIStyle(GUI.skin.textField);
            CustomSkin.textArea = new GUIStyle(GUI.skin.textArea);
            CustomSkin.toggle = new GUIStyle(GUI.skin.toggle);
            CustomSkin.window = new GUIStyle(GUI.skin.window);
            CustomSkin.horizontalSlider = new GUIStyle(GUI.skin.horizontalSlider);
            CustomSkin.horizontalSliderThumb = new GUIStyle(GUI.skin.horizontalSliderThumb);
            CustomSkin.verticalSlider = new GUIStyle(GUI.skin.verticalSlider);
            CustomSkin.verticalSliderThumb = new GUIStyle(GUI.skin.verticalSliderThumb);
            CustomSkin.horizontalScrollbar = new GUIStyle(GUI.skin.horizontalScrollbar);
            CustomSkin.horizontalScrollbarThumb = new GUIStyle(GUI.skin.horizontalScrollbarThumb);
            CustomSkin.horizontalScrollbarLeftButton = new GUIStyle(GUI.skin.horizontalScrollbarLeftButton);
            CustomSkin.horizontalScrollbarRightButton = new GUIStyle(GUI.skin.horizontalScrollbarRightButton);
            CustomSkin.verticalScrollbar = new GUIStyle(GUI.skin.verticalScrollbar);
            CustomSkin.verticalScrollbarThumb = new GUIStyle(GUI.skin.verticalScrollbarThumb);
            CustomSkin.verticalScrollbarUpButton = new GUIStyle(GUI.skin.verticalScrollbarUpButton);
            CustomSkin.verticalScrollbarDownButton = new GUIStyle(GUI.skin.verticalScrollbarDownButton);
            CustomSkin.scrollView = new GUIStyle(GUI.skin.scrollView);

            // Copy settings arrays
            //CustomSkin.settings = new GUISettings(GUI.skin.settings);
            CustomSkin.customStyles = GUI.skin.customStyles != null ?
                                      (GUIStyle[])GUI.skin.customStyles.Clone() :
                                      new GUIStyle[0];
        }

        static void UpdateFontSize(int fontSize)
        {
            if (CustomSkin == null) return;

            CustomSkin.label.fontSize = fontSize;
            CustomSkin.button.fontSize = fontSize;
            CustomSkin.toggle.fontSize = fontSize;
            CustomSkin.textField.fontSize = fontSize;
            CustomSkin.textArea.fontSize = fontSize;
            CustomSkin.box.fontSize = fontSize;
            CustomSkin.window.fontSize = fontSize;
        }

        public static void FocusItemIconButton(int itemId)
        {
            bool isFocus = ProfilePanel.FocusItmeId == itemId;
            ItemProto item = LDB.items.Select(itemId);
            if (GUILayout.Button(item?.iconSprite.texture, isFocus ? focusItemIconStyle : itemIconStyle, iconoptions))
            {
                ProfilePanel.FocusItmeId = isFocus ? 0 : itemId;
            }
        }

        public static bool RecipeExpandButton(int itemId)
        {
            ItemProto item = LDB.items.Select(itemId);
            return GUILayout.Button(item?.iconSprite.texture, normalIconStyle, iconoptions);
        }

        public static void EntityRecordButton(EntityRecord entityRecord)
        {
            var texture = LDB.items.Select(entityRecord.itemId)?.iconSprite.texture;
            iconTextContent.image = texture;
            iconTextContent.text = entityRecord.ToString();
            if (GUILayout.Button(iconTextContent, GUILayout.Height(RecordHeight)))
            {
                NavigateToEntity(UIWindow.Instance.Table.GetFactory(), entityRecord.entityId);
            }
        }

        public static void NavigateToEntity(PlanetFactory factory, int entityId)
        {
            if (GameMain.mainPlayer == null || factory == null) return;
            if (GameMain.data.localPlanet?.factory == factory)
            {
                // Locate the entity on the local planet
                if (entityId <= 0 || entityId >= factory.entityPool.Length)
                {
                    UIRealtimeTip.Popup($"EntityId {entityId} exceed Pool length {factory.entityPool.Length}!");
                    return;
                }
                var localPos = factory.entityPool[entityId].pos;
                // Move camera to local location
                //UIRoot.instance.uiGame.globemap.MoveToViewTargetTwoStep(localPos,
                //    (float)localPos.magnitude - GameMain.data.localPlanet.realRadius);
                GameMain.data.mainPlayer.Order(OrderNode.MoveTo(localPos), false);
                UIRealtimeTip.Popup($"Move to entity {entityId}");
            }
            else
            {
                // Draw navigate line to the astroId
                GameMain.mainPlayer.navigation.indicatorAstroId = factory.planetId;
            }
        }

        public static string RateKMG(float num)
        {
            return KMG(num / ModSettings.RateUnit.Value);
        }

        // https://github.com/jinxOAO/DSPCalculator/blob/master/Utils/Utils.cs
        public static string KMG(float num)
        {
            string tail;
            if (num < 100)
            {
                return num.ToString("0.##");
            }
            else if (num < 1000)
            {
                return num.ToString("0.#");
            }
            else if (num < 10000)
            {
                return num.ToString();
            }
            else if (num < 1000000)
            {
                tail = "k";
                num /= 1000;
            }
            else if (num < 1000000000L)
            {
                tail = "M";
                num /= 1000000;
            }
            else if (num < 1000000000000L)
            {
                tail = "G";
                num /= 1000000000;
            }
            else if (num < 1000000000000000L)
            {
                tail = "T";
                num /= 1000000000000L;
            }
            else if (num < 1000000000000000000L)
            {
                tail = "P";
                num /= 1000000000000000L;
            }
            else
            {
                tail = "E";
                num /= 1000000000000000000L;
            }

            // 不用0.##二用0.00是仅在有kMG的时候才必定保留小数位数。如果是小于10000的时候，小数部分为0的则不显示
            if (num < 9.995)
            {
                return string.Format("{0:0.000} {1}", num, tail); // 0,5:0.###这样也对不齐，这个字体空格比较窄
            }
            else if (num < 99.95)
            {
                return string.Format("{0:0.00} {1}", num, tail);
            }
            else if (num < 999.5)
            {
                return string.Format("{0:0.0} {1}", num, tail);
            }
            else
            {
                return string.Format("{0} {1}", num, tail);
            }
        }
    }
}
