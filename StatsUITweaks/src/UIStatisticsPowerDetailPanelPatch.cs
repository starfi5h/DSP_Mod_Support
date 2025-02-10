using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace StatsUITweaks
{
    public class UIStatisticsPowerDetailPanelPatch
    {
        static bool initialized;
        static UIButton extendBtn;
        static bool extended;
        static float scrollHeight = 210f;
        static float scrollY = 210f;
        static GameObject sepline;

        [HarmonyPostfix, HarmonyPatch(typeof(UIStatisticsPowerDetailPanel), nameof(UIStatisticsPowerDetailPanel._OnOpen))]
        public static void Init(UIStatisticsPowerDetailPanel __instance)
        {
            if (initialized) return;

            try
            {
                UIButton uIButton0 = UIRoot.instance.uiGame.researchQueue.pauseButton;

                var go = GameObject.Instantiate(uIButton0.gameObject, __instance.transform);
                go.name = "CustomStats_Extend";
                go.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                go.transform.localPosition = new Vector3(420f, -420f, 0); // 365f, -440f
                Image img = go.transform.Find("icon")?.GetComponent<Image>();
                if (img != null)
                {
                    UIStarmap starmap = UIRoot.instance.uiGame.starmap;
                    img.sprite = starmap.cursorFunctionButton2.transform.Find("icon")?.GetComponent<Image>()?.sprite;
                }
                extendBtn = go.GetComponent<UIButton>();
                extendBtn.tips.tipTitle = "Extend 扩展耗电设施";
                extendBtn.tips.tipText = "Click to extend power consumption detail";
                extendBtn.tips.corner = 8;
                extendBtn.onClick += OnFoldButtonClick;
                if (extendBtn.transitions != null)
                    extendBtn.transitions[0].highlightColorOverride = new Color(0.5f, 0.6f, 0.7f, 0.1f); //用於highlighted
                go.SetActive(true);

                // Record original values
                var scrollRect = (RectTransform)__instance.conScrollContentRect.transform.parent.parent.transform;
                scrollHeight = scrollRect.sizeDelta.y;
                scrollY = scrollRect.localPosition.y;

                // Get the reference of separate line
                sepline = __instance.gameObject.transform.Find("table-panel/sep-line-0")?.gameObject;

                initialized = true;
                Plugin.Log.LogDebug("Create UIStatisticsPowerDetailPanel extend button");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("PerformancePanelPatch initial fail!");
                Plugin.Log.LogError(e);
            }
            Toggle(extended);
        }

        public static void OnDestory()
        {
            Toggle(false);
            GameObject.Destroy(extendBtn?.gameObject);
            initialized = false;
        }

        static void OnFoldButtonClick(int obj)
        {
            extended = !extended;
            extendBtn.highlighted = extended;
            Toggle(extended);
        }

        private static void Toggle(bool extendScroll)
        {
            if (initialized)
            {
                var panel = UIRoot.instance.uiGame.statWindow.powerDetailPanel;

                // Hide generator scroll detail when comsumption detail extend
                panel.genScrollContentRect.gameObject.SetActive(!extendScroll);
                sepline?.SetActive(!extendScroll);

                var scrollRect = (RectTransform)panel.conScrollContentRect.transform.parent.parent.transform;

                scrollRect.localPosition = new Vector3(scrollRect.localPosition.x, extendScroll ? scrollY + 50f : scrollY, 0f);
                scrollRect.sizeDelta = new Vector2(scrollRect.sizeDelta.x, extendScroll ? scrollHeight + 210f : scrollHeight);
            }
        }
    }
}
