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
                enableButton.tips.tipText = "Click to open/close the window and selection tool";
                enableButton.transitions[1].normalColor = new Color(1.0f, 1.0f, 1.0f, 0.8f); // icon color
                enableButton.transitions[1].mouseoverColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                enableButton.transitions[0].mouseoverColor = new Color(0.6f, 0.6f, 1.0f, 0.2f); //background color
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
            if (Plugin.MainTable == null) // When window is not open, open window and enable selection tool
            {
                if (!Plugin.LoadLastTable()) Plugin.CreateMainTable(null, new List<int>(0));
                if (VFInput.readyToBuild) SelectionTool_Patches.SetEnable(true);
            }
            else // When window is open, close window and disable selection tool 
            {
                if (VFInput.control) // Holding control: enable selection tool
                {
                    if (VFInput.readyToBuild) SelectionTool_Patches.SetEnable(true);
                    return;
                }
                Plugin.SaveCurrentTable();
                Plugin.MainTable = null;
                SelectionTool_Patches.SetEnable(false);
            }
        }

        public static void Destroy()
        {
            Object.Destroy(enableButton?.gameObject);
        }
    }
}
