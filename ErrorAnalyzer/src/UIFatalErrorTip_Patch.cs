using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ErrorAnalyzer
{
    [HarmonyPatch(typeof(UIFatalErrorTip))]
    internal class UIFatalErrorTip_Patch
    {
        private static GameObject button1;

        [HarmonyPostfix]
        [HarmonyPatch("_OnRegEvent")]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Original Function Name")]
        private static void _OnRegEvent_Postfix()
        {
            if (!Plugin.isRegisitered)
                return;
            Plugin.isRegisitered = false;

            try
            {
                Application.logMessageReceived -= Plugin.HandleLog;
                if (!string.IsNullOrEmpty(Plugin.errorString))
                    UIFatalErrorTip.instance.ShowError(Plugin.errorString, Plugin.errorStackTrace);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("_OnOpen")]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Original Function Name")]
        private static void _OnOpen_Postfix()
        {
            try
            {
                if (button1 == null)
                {
                    var errorPanel = GameObject.Find("UI Root/Overlay Canvas/Fatal Error/errored-panel/");
                    errorPanel.transform.Find("tip-text-0").GetComponent<Text>().text = Title();
                    Object.Destroy(errorPanel.transform.Find("tip-text-0").GetComponent<Localizer>());
                    errorPanel.transform.Find("tip-text-1").GetComponent<Text>().text = Title();
                    Object.Destroy(errorPanel.transform.Find("tip-text-1").GetComponent<Localizer>());

                    button1 = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/hierarchy/layers/blueprint-group/blueprint-2/copy-button");
                    button1 = Object.Instantiate(button1, errorPanel.transform);
                    button1.name = "Copy button";
                    button1.transform.localPosition = errorPanel.transform.Find("icon").localPosition + new Vector3(30, -35, 0);
                    button1.GetComponent<Image>().color = new Color(0.6f, 0.1f, 0.1f, 0.6f);
                    
                    button1.GetComponent<UIButton>().BindOnClickSafe(OnClick1);
                    ref var tips = ref button1.GetComponent<UIButton>().tips;
                    tips.tipTitle = "Copy Error";
                    tips.tipText = "Copy the message to clipboard and close error.";
                    tips.corner = 1;
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"UIFatalErrorTip button did not patch! {e}");
            }
        }

        private static string Title()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("An error has occurred! Game version ");
            stringBuilder.Append(GameConfig.gameVersion.ToString());
            stringBuilder.Append('.');
            stringBuilder.Append(GameConfig.gameVersion.Build);
            stringBuilder.AppendLine();
            stringBuilder.Append(Chainloader.PluginInfos.Values.Count + " Mods used: ");
            foreach (var pluginInfo in Chainloader.PluginInfos.Values)
            {
                stringBuilder.Append('[');
                stringBuilder.Append(pluginInfo.Metadata.Name);
                stringBuilder.Append(pluginInfo.Metadata.Version);
                stringBuilder.Append("] ");
            }
            return stringBuilder.ToString();
        }

        private static void OnClick1(int id)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("```ini");
            stringBuilder.AppendLine(Title());
            var subs = UIFatalErrorTip.instance.errorLogText.text.Split('\n', '\r');
            foreach (var str in subs)
            {
                if (string.IsNullOrEmpty(str))
                {
                    continue;
                }

                // Remove hash string
                var start = str.LastIndexOf(" <", StringComparison.Ordinal);
                var end = str.LastIndexOf(">:", StringComparison.Ordinal);
                if (start != -1 && end > start)
                {
                    stringBuilder.AppendLine(str.Remove(start, end - start + 2));
                }
                else
                {
                    stringBuilder.AppendLine(str);
                }
            }
            // Apply format for ini code style
            stringBuilder.Replace(" (at", ";(");
            stringBuilder.Replace(" inIL_", " ;IL_");
            stringBuilder.AppendLine("```");

            // Copy string to clipboard
            GUIUtility.systemCopyBuffer = stringBuilder.ToString();
            UIFatalErrorTip.ClearError();
            Object.Destroy(button1);
            button1 = null;
        }
    }
}