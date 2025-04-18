using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace RateMonitor
{
    public class QuickBarButton
    {
        static UIButton enableButton;

        public static void Enable(bool enable)
        {
            if (enable)
            {
                CrateButton();
            }
            else
            {
                Destroy();
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameMain), nameof(GameMain.Start))]
        public static void CrateButton()
        {
            if (enableButton != null) return;

            try
            {
                var infiniteEnergyButton = UIRoot.instance.uiGame.energyBar.infiniteEnergyButton;
                var gameObject = Object.Instantiate(infiniteEnergyButton.gameObject, infiniteEnergyButton.transform.parent.parent);
                gameObject.name = "[Rate Monitor] Toggle";
                var refTransfrom = (RectTransform)UIRoot.instance.uiGame.energyBar.energyChangesTip.transform;

                gameObject.transform.localPosition = new Vector3(refTransfrom.localPosition.x - refTransfrom.rect.width / 1.5f - 20f, gameObject.transform.localPosition.y, gameObject.transform.localPosition.z);
                gameObject.SetActive(true);
                var image = gameObject.transform.Find("icon").GetComponent<Image>();
                image.sprite = LDB.signals.Select(511).iconSprite;

                enableButton = gameObject.GetComponent<UIButton>();
                enableButton.onClick += OnButtonClick;
                enableButton.tips.corner = 8;
                enableButton.tips.tipTitle = "Rate Monitor";
                enableButton.tips.tipText = "Click to open the window and selection tool";
                enableButton.transitions[0].highlightColorOverride = new Color(0.6f, 0.6f, 0.6f, 0.1f); // button background
                enableButton.transitions[1].highlightColorOverride = new Color(1.0f, 1.0f, 1.0f, 1.0f); // icon color blue
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning("Error when creating button!");
                Plugin.Log.LogWarning(ex);
                ModSettings.EnableQuickBarButton.Value = false;
            }
        }

        public static void OnButtonClick(int _)
        {
            if (Plugin.MainTable == null) // Window is not open
            {
                if (!Plugin.LoadLastTable()) Plugin.CreateMainTable(null, new List<int>(0));
            }
            if (VFInput.readyToBuild)
            {
                SelectionTool_Patches.activateInNextTick = true;
            }
        }

        public static void Destroy()
        {
            Object.Destroy(enableButton?.gameObject);
        }
    }
}
