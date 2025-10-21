using System;
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
        public static string ExtraTitleString { get; set; } = "";

        private static UIButton btnClose;
        private static UIButton btnCopy;
        private static UIButton btnInspect;

        private static bool waitingToRefresh;
        private static int astroId;
        private static int entityId;
        private static Vector3 localPos;

        [HarmonyPostfix]
        [HarmonyPatch("_OnRegEvent")]
        private static void OnRegEvent_Postfix()
        {
            if (!Plugin.isRegistered)
                return;
            Plugin.isRegistered = false;

            try
            {
                Application.logMessageReceived -= Plugin.HandleLog;
                if (string.IsNullOrEmpty(Plugin.errorString)) return;
                    
                string[] lines = Plugin.errorStackTrace.Split('\n', '\r');
                Plugin.Log.LogDebug("Error captured during loading. Lines count: " + lines.Length);
                if (lines.Length > 15) // Skip the middle part if the error message is too long
                {
                    var sb = new StringBuilder();
                    for (int i = 0; i < 5; i++)
                    {
                        sb.AppendLine(lines[i].TrimEnd('\n', '\r'));
                    }
                    sb.AppendLine($"...(skip the middle {lines.Length - 15} lines)");
                    for (int i = lines.Length - 10; i < lines.Length; i++)
                    {
                        sb.AppendLine(lines[i].TrimEnd('\n', '\r'));
                    }
                    Plugin.errorStackTrace = sb.ToString();
                }
                UIFatalErrorTip.instance.ShowError(Plugin.errorString, Plugin.errorStackTrace);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("ShowError")]
        private static void ShowError_Prefix(UIFatalErrorTip __instance)
        {
            if (string.IsNullOrEmpty(__instance.errorLogText.text) || waitingToRefresh)
            {
                // Update astroId and entityId
                astroId = TrackEntity_Patch.AstroId;
                entityId = TrackEntity_Patch.EntityId;
                localPos = TrackEntity_Patch.LocalPos;
            }
            if (waitingToRefresh)
            {
                waitingToRefresh = false;
                UIFatalErrorTip.ClearError();
                TryNavigate();
            }
        }

        [HarmonyPostfix, HarmonyPriority(Priority.Low)]
        [HarmonyPatch("_OnOpen")]
        private static void OnOpen_Postfix(UIFatalErrorTip __instance)
        {
            if (btnClose != null || btnCopy != null || btnInspect != null) return;

            TryCreateButton(() => CreateCloseBtn(__instance), "Close Button");
            TryCreateButton(() => CreateCopyBtn(__instance), "Copy Button");
            TryCreateButton(() => CreateInspectBtn(__instance), "Inspect Button");

            __instance.transform.Find("tip-text-0").GetComponent<Text>().text = GetShortTitle();
            __instance.transform.Find("tip-text-1").GetComponent<Text>().text = GetShortTitle();
            Object.Destroy(__instance.transform.Find("tip-text-0").GetComponent<Localizer>());
            Object.Destroy(__instance.transform.Find("tip-text-1").GetComponent<Localizer>());
        }

        [HarmonyPostfix]
        [HarmonyPatch("_OnClose")]
        private static void OnClose_Postfix()
        {
            if (btnClose != null)
            {
                Object.Destroy(btnClose.gameObject);
                btnClose = null;
            }
            if (btnCopy != null)
            {
                Object.Destroy(btnCopy.gameObject);
                btnCopy = null;
            }
            if (btnInspect != null)
            {
                Object.Destroy(btnInspect.gameObject);
                btnInspect = null;
            }
            TrackEntity_Patch.ResetId();
        }

        private static void TryCreateButton(Action createAction, string buttonName)
        {
            try
            {
                createAction();
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"{buttonName} did not patch!\n{e}");
            }
        }

        private static UIButton CreateButton(string path, Transform parent, Vector3 positionOffset, Action<int> onClickAction)
        {
            var go = GameObject.Find(path);
            if (go == null)
            {
                Plugin.Log.LogWarning("Can't find " + path);
                return null;
            }
            return CreateButton(go, parent, positionOffset, onClickAction);
        }

        private static UIButton CreateButton(GameObject originalGo, Transform parent, Vector3 positionOffset, Action<int> onClickAction)
        {
            if (originalGo != null)
            {
                var go = Object.Instantiate(originalGo, parent);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = Vector2.up;
                rect.anchorMax = Vector2.up;
                rect.pivot = Vector2.up;
                rect.anchoredPosition = positionOffset;
                go.SetActive(true);

                var button = go.GetComponent<UIButton>();
                button.onClick += onClickAction;
                button.tips.corner = 1;
                return button;
            }
            return null;
        }

        private static void CreateCloseBtn(UIFatalErrorTip __instance)
        {
            if (btnClose != null) return;

            const string PATH1 = "UI Root/Overlay Canvas/In Game/Common Tools/Color Palette Panel/panel-bg/btn-box/close-wnd-btn"; // new path for version >= 0.10.32
            btnClose = CreateButton(PATH1, __instance.transform, new Vector3(-5, 0, 0), OnCloseClick);
            if (btnClose != null) return;

            const string PATH2 = "UI Root/Overlay Canvas/In Game/Windows/Window Template/panel-bg/btn-box/close-btn"; // old path for version <= 0.10.31
            btnClose = CreateButton(PATH2, __instance.transform, new Vector3(-5, 0, 0), OnCloseClick);
        }

        private static void CreateCopyBtn(UIFatalErrorTip __instance)
        {
            if (btnCopy != null) return;

            const string PATH = "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/hierarchy/layers/blueprint-group/blueprint-2/copy-button";
            btnCopy = CreateButton(PATH, __instance.transform, new Vector3(5, -55, 0), OnCopyClick);
            btnCopy.tips.tipTitle = "Copy Error Message";
            btnCopy.tips.tipText = "Copy the message to clipboard\nShift-click to copy the mod list too";
        }

        private static void CreateInspectBtn(UIFatalErrorTip __instance)
        {
            if (btnInspect != null || GameMain.mainPlayer == null) return;

            var go = UIRoot.instance.uiGame.researchQueue.pauseButton.gameObject;
            btnInspect = CreateButton(go, __instance.transform, new Vector3(20, -80, 0), OnInspectClick);
            btnInspect.transform.Find("icon").GetComponent<Image>().sprite
                = UIRoot.instance.uiGame.starmap.cursorFunctionButton2.transform.Find("icon").GetComponent<Image>().sprite;
            btnInspect.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            btnInspect.onRightClick += ToggleTrackMode;
            btnInspect.tips.tipTitle = "Find The Error Entity";
            btnInspect.tips.tipText = "Left click to navigate\nRight click to toggle debug mode";

            if (astroId > 0)
            {
                if (astroId % 100 != 0)
                {
                    var planetName = GameMain.galaxy.PlanetById(astroId).displayName ?? "";
                    btnInspect.tips.tipText += $"\nentity: {entityId} at [{astroId}] {planetName}";
                }
            }
            SetInsepctButton();
        }

        private static string GetFullTitle()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("Error report: Game version ");
            stringBuilder.Append(GameConfig.gameVersion.ToString());
            stringBuilder.Append('.');
            stringBuilder.Append(GameConfig.gameVersion.Build);
            stringBuilder.Append(" with ");
            stringBuilder.Append(Chainloader.PluginInfos.Values.Count);
            stringBuilder.AppendLine(" mods used.");
            foreach (var pluginInfo in Chainloader.PluginInfos.Values)
            {
                stringBuilder.Append('[');
                stringBuilder.Append(pluginInfo.Metadata.Name);
                stringBuilder.Append(pluginInfo.Metadata.Version);
                stringBuilder.Append("] ");
            }
            return stringBuilder.ToString();
        }

        private static string GetShortTitle()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("Error report: Game version ");
            stringBuilder.Append(GameConfig.gameVersion.ToString());
            stringBuilder.Append('.');
            stringBuilder.Append(GameConfig.gameVersion.Build);
            stringBuilder.Append(" with ");
            stringBuilder.Append(Chainloader.PluginInfos.Values.Count);
            stringBuilder.Append(" mods used.");
            if (!string.IsNullOrWhiteSpace(ExtraTitleString))
            {
                stringBuilder.AppendLine();
                stringBuilder.Append(ExtraTitleString);
            }
            return stringBuilder.ToString();
        }

        private static void OnCopyClick(int id)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("```ini");
            if (VFInput.shift) stringBuilder.AppendLine(GetFullTitle());
            else stringBuilder.AppendLine(GetShortTitle());
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
            stringBuilder.Replace(" (at", "; (");
            stringBuilder.Replace(" inIL_", " ;IL_");
            stringBuilder.AppendLine("```");

            // Copy string to clipboard
            GUIUtility.systemCopyBuffer = stringBuilder.ToString();
            UIRealtimeTip.Popup("Error message copied!", false);
        }

        public static void OnCloseClick(int _)
        {
            waitingToRefresh = false;
            UIFatalErrorTip.ClearError();
            if (!(GameConfig.gameVersion < new Version(0, 10, 33))) ClearErrorCount();
        }

        public static void ClearErrorCount()
        {
            // Note: In ThreadManager.ProcessFrame, if frameErrorCountMainThread and erroredFrameCount exceed limit (5,100)
            // The error message will no longer displayed
            // So we need to reset those values when closing the error
            var threadManager = GameMain.logic?.threadController?.threadManager;
            if (threadManager == null) return;

            // From ThreadManager.Free() that call when the game ends
            Plugin.Log.LogDebug($"Error closed. frameErrorCountMainThread={threadManager.frameErrorCountMainThread} erroredFrameCount={threadManager.erroredFrameCount}");
            threadManager.frameErrorCountAllThreads = 0;
            threadManager.frameErrorCountMainThread = 0;
            threadManager.erroredFrameCount = 0;
            threadManager.totalErrorCount = 0;
        }

        private static void OnInspectClick(int _)
        {
            if (astroId <= 0)
            {
                if (!TrackEntity_Patch.Active)
                {
                    // Turn of the tracking mode and wait for a new error to refresh
                    waitingToRefresh = true;
                    ToggleTrackMode(0);
                }
                return;
            }
            TryNavigate();
        }

        internal static void TryNavigate()
        {
            if (astroId <= 0 || GameMain.mainPlayer == null) return;
            if (astroId % 100 != 0 && GameMain.data.localPlanet?.id == astroId)
            {
                // Locate the entity on the local planet
                var factory = GameMain.data.localPlanet.factory;
                if (entityId >= factory.entityPool.Length)
                {
                    UIRealtimeTip.Popup($"EntityId {entityId} exceed Pool length {factory.entityPool.Length}!");
                    return;
                }
                localPos = entityId > 0 ? factory.entityPool[entityId].pos : localPos;
                // Move camera to local location
                UIRoot.instance.uiGame.globemap.MoveToViewTargetTwoStep(localPos,
                    (float)localPos.magnitude - GameMain.data.localPlanet.realRadius);
                GameMain.data.mainPlayer.Order(OrderNode.MoveTo(localPos), false);
            }
            else
            {
                // Draw navigate line to the astroId
                GameMain.mainPlayer.navigation.indicatorAstroId = astroId;
            }
        }

        private static void ToggleTrackMode(int _)
        {
            try
            {
                TrackEntity_Patch.Enable(!TrackEntity_Patch.Active);
                SetInsepctButton();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Can't enable tracking!\n" + e);
            }
        }

        private static void SetInsepctButton()
        {
            if (btnInspect == null || btnInspect.transitions.Length == 0) return;

            btnInspect.CloseTip();
            if (TrackEntity_Patch.Active)
            {
                btnInspect.tips.tipTitle = "Find The Error Entity (ON)";
                btnInspect.transitions[0].normalColor = new Color(0.7f, 0.7f, 0.3f, 0.4f);
                btnInspect.transitions[0].mouseoverColor = new Color(0.7f, 0.7f, 0.3f, 0.5f);
            }
            else
            {
                btnInspect.tips.tipTitle = "Find The Error Entity (OFF)";
                btnInspect.transitions[0].normalColor = new Color(0.2392f, 0.6f, 0.9f, 0.078f);
                btnInspect.transitions[0].mouseoverColor = new Color(0.2392f, 0.6f, 0.9f, 0.178f);
            }
        }
    }
}
