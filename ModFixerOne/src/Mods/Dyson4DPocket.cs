using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace ModFixerOne.Mods
{
    public static class Dyson4DPocket
    {
        public const string NAME = "4D Pocket";
        public const string GUID = "com.github.yyuueexxiinngg.plugin.dyson.4dpocket";
        public const string VERSION = "1.5";

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;
            if (pluginInfo.Metadata.Version.ToString() != VERSION)
                return;
            Assembly assembly = pluginInfo.Instance.GetType().Assembly;

            try
            {
                var classType = assembly.GetType("Dyson4DPocket.Pocket");
                var methodInfo = AccessTools.Method(classType, "OpenStorage");
                var transplier = new HarmonyMethod(AccessTools.Method(typeof(Dyson4DPocket), nameof(OpenStorage_Transpiler)));
                harmony.Patch(methodInfo, null, null, transplier);

                methodInfo = AccessTools.Method(classType, "Update");
                transplier = new HarmonyMethod(AccessTools.Method(typeof(Dyson4DPocket), nameof(Update_Transpiler)));
                harmony.Patch(methodInfo, null, null, transplier);

                Plugin.Log.LogInfo($"{NAME} - OK");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"{NAME} - Fail! Last target version: {VERSION}");
                Fixer_Patch.ErrorMessage += $"\n{NAME} (last target version: {VERSION})";
                Plugin.Log.LogDebug(e);
            }
        }

        private static IEnumerable<CodeInstruction> OpenStorage_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                // set the missing uiStorage.history so it is not null
                var codeMatcher = new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "OnStorageIdChange"))
                    .SetInstruction(
                        Transpilers.EmitDelegate<Action<UIStorageWindow>>(
                            (uiStorage) =>
                            {
                                uiStorage.history = GameMain.history; // origin in UIStorageWindow._OnOpen()
                                uiStorage.OnStorageIdChange();
                            }
                        )
                    );
                return codeMatcher.InstructionEnumeration();
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("OpenStorage_Transpiler fail!");
                Plugin.Log.LogWarning(e);
                return instructions;
            }
        }

        private static IEnumerable<CodeInstruction> Update_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                // replace: this.OpenStation(inputItem.FactoryIndex, inputItem.ItemID);
                // with:    OpenStation(inputItem.FactoryIndex, inputItem.ItemID);
                var codeMatcher = new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "OpenStation"))
                    .Repeat(matcher => {
                        matcher
                            .SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(Dyson4DPocket), nameof(OpenStation)))
                            .Advance(-6)
                            .SetAndAdvance(OpCodes.Nop, null)
                            .Advance(7);
                    });

                return codeMatcher.InstructionEnumeration();
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Update_Transpiler fail!");
                Plugin.Log.LogWarning(e);
                return instructions;
            }
        }

        private static void OpenStation(int factoryIndex, int stationId)
        {
            if (factoryIndex < 0 || stationId < 0) return;
            if (!GameMain.isRunning || GameMain.instance.isMenuDemo) return;
            UIStationWindow _uiStation = UIRoot.instance.uiGame.stationWindow;

            if (!_uiStation.inited) return;

            if (_uiStation.active)
            {
                UIRealtimeTip.Popup("请先关闭目前物流站".Translate());
                return;
            }

            if (GameMain.data.factories != null &&
                GameMain.data.factories.Length > factoryIndex &&
                GameMain.data.factories[factoryIndex] != null)
            {
                try
                {
                    var factory = GameMain.data.factories[factoryIndex];
                    var transport = factory.transport;
                    if (transport.stationPool != null &&
                        transport.stationPool.Length >= stationId &&
                        transport.stationPool[stationId] != null
                    )
                    {
                        _uiStation.stationId = stationId;
                        _uiStation.active = true;
                        if (!_uiStation.gameObject.activeSelf)
                        {
                            _uiStation.gameObject.SetActive(true);
                        }

                        _uiStation.factory = factory;
                        _uiStation.transport = factory.transport;
                        _uiStation.powerSystem = factory.powerSystem;
                        _uiStation.player = GameMain.mainPlayer;
                        _uiStation.OnStationIdChange();

                        _uiStation.nameInput.onValueChanged.AddListener(_uiStation.OnNameInputSubmit);
                        _uiStation.nameInput.onEndEdit.AddListener(_uiStation.OnNameInputSubmit);
                        _uiStation.player.onIntendToTransferItems += _uiStation.OnPlayerIntendToTransferItems;

                        _uiStation.transform.SetAsLastSibling();
                        UIRoot.instance.uiGame.OpenPlayerInventory();
                        //_inspectingStation = true;
                    }
                    else
                    {
                        UIRealtimeTip.Popup("物流站ID不存在".Translate());
                    }
                }
                catch (Exception message)
                {
                    //_inspectingStation = false;
                    Plugin.Log.LogWarning(message);
                }
            }
            else
            {
                UIRealtimeTip.Popup("工厂不存在".Translate());
            }
        }
    }
}
