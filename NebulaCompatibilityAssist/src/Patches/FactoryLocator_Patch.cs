using CommonAPI.Systems;
using FactoryLocator;
using FactoryLocator.UI;
using HarmonyLib;
using NebulaAPI;
using NebulaCompatibilityAssist.Packets;
using System;
using UnityEngine;

namespace NebulaCompatibilityAssist.Patches
{
    public class FactoryLocator_Patch
    {
        public const string NAME = "FactoryLocator";
        public const string GUID = "starfi5h.plugin.FactoryLocator";
        public const string VERSION = "1.2.0";
        public static bool Enable { get; private set; }

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var _))
                return;
            Enable = true;

            try
            {
                harmony.PatchAll(typeof(Warper));

                Log.Info($"{NAME} - OK");
            }
            catch (Exception e)
            {
                Log.Warn($"{NAME} - Fail! Last target version: {VERSION}");
                NC_Patch.ErrorMessage += $"\n{NAME} (last target version: {VERSION})";
                Log.Debug(e);
            }
        }

        public static void OnReceive(NC_PlanetInfoData packet)
        {
            Warper.UpdateStatus(packet);
        }

        public static void OnReceive(NC_LocatorFilter packet, INebulaConnection conn)
        {
            if (NebulaModAPI.MultiplayerSession.LocalPlayer.IsHost)
            {
                if (Enable)
                    Warper.HandleRequest(packet, conn);
                else
                {
                    packet.QueryType = -1;
                    conn.SendPacket(packet);
                }
            }
            else
            {
                if (Enable)
                    Warper.ShowPickerWindow(packet);
            }
        }

        public static void OnReceive(NC_LocatorResult packet, INebulaConnection conn)
        {
            if (NebulaModAPI.MultiplayerSession.LocalPlayer.IsHost)
            {
                if (Enable)
                    Warper.HandleRequest(packet, conn);
            }
            else
            {
                if (Enable)
                    Warper.ShowResult(packet);
            }
        }
        
        public class Warper
        {            
            static int astroId;
            static int detailId;

            [HarmonyPostfix, HarmonyPatch(typeof(UILocatorWindow), nameof(UILocatorWindow.SetViewingTarget))]
            private static void SetViewingTarget_Postfix()
            {
                if (NebulaModAPI.IsMultiplayerActive && NebulaModAPI.MultiplayerSession.LocalPlayer.IsClient)
                {
                    var mainWindow = FactoryLocator.Plugin.mainWindow;
                    int newAstroId = mainWindow.veiwPlanet?.id ?? mainWindow.veiwStar?.id * 100 ?? 0;
                    if (mainWindow.veiwPlanet != null && FactoryLocator.Plugin.mainLogic.factories.Count == 0)
                    {
                        NebulaModAPI.MultiplayerSession.Network.SendPacket(new NC_PlanetInfoRequest(newAstroId));
                        if (newAstroId != astroId)
                            mainWindow.nameText.text = "Loading...";
                    }
                    else if (mainWindow.veiwStar != null)
                    {
                        // Request for whole planets in the star system
                        NebulaModAPI.MultiplayerSession.Network.SendPacket(new NC_PlanetInfoRequest(newAstroId));
                        if (newAstroId != astroId)
                            mainWindow.nameText.text = "Loading...";
                    }
                    astroId = newAstroId;
                }
            }

            public static void UpdateStatus(NC_PlanetInfoData packet)
            {
                var mainWindow = FactoryLocator.Plugin.mainWindow;
                bool flag = false;
                if (packet.PlanetId > 0 && packet.PlanetId == mainWindow.veiwPlanet?.id)
                {
                    mainWindow.nameText.text = mainWindow.veiwPlanet.displayName;
                    flag = true;
                }
                else if (packet.StarId > 0 && packet.StarId == mainWindow.veiwStar?.id)
                {
                    mainWindow.nameText.text = mainWindow.veiwStar.displayName + "空格行星系".Translate();
                    flag = true;
                }
                if (flag)
                {
                    for (int i = 0; i < mainWindow.queryBtns.Length; i++)
                        mainWindow.queryBtns[i].button.enabled = packet.NetworkCount > 0;
                }
                mainWindow.SetStatusTipText(packet.ConsumerRatios, packet.ConsumerCounts);
            }

            [HarmonyPrefix, HarmonyPatch(typeof(UILocatorWindow), nameof(UILocatorWindow.OnQueryClick))]
            public static bool OnQueryClick(UILocatorWindow __instance, int queryType)
            {
                if (__instance.autoclear_enable)
                    WarningSystemPatch.ClearAll();

                bool isClient = NebulaModAPI.IsMultiplayerActive && NebulaModAPI.MultiplayerSession.LocalPlayer.IsClient;
                if (!isClient || (FactoryLocator.Plugin.mainLogic.factories.Count > 0 && __instance.veiwStar == null))
                {
                    switch (queryType)
                    {
                        case 0: FactoryLocator.Plugin.mainLogic.PickBuilding(0); break;
                        case 1: FactoryLocator.Plugin.mainLogic.PickVein(0); break;
                        case 2: FactoryLocator.Plugin.mainLogic.PickAssembler(0); break;
                        case 3: FactoryLocator.Plugin.mainLogic.PickWarning(0); break;
                        case 4: FactoryLocator.Plugin.mainLogic.PickStorage(0); break;
                        case 5: FactoryLocator.Plugin.mainLogic.PickStation(0); break;
                    }
                }
                else
                {
                    NebulaModAPI.MultiplayerSession.Network.SendPacket(new NC_LocatorFilter(astroId, queryType, null));
                }
                return false;
            }

            public static void HandleRequest(NC_LocatorFilter packet, INebulaConnection conn)
            {
                StarData star = null;
                PlanetData planet = null;
                if (packet.AstroId % 100 == 0)
                    star = GameMain.data.galaxy.StarById(packet.AstroId/100);
                else
                    planet = GameMain.data.galaxy.PlanetById(packet.AstroId);

                var logic = new MainLogic();
                logic.SetFactories(star, planet);
                switch (packet.QueryType)
                {
                    case 0: logic.RefreshBuilding(-1); break;
                    case 1: logic.RefreshVein(-1); break;
                    case 2: logic.RefreshAssemblers(-1); break;
                    case 3: logic.RefreshSignal(-1); break;
                    case 4: logic.RefreshStorage(-1); break;
                    case 5: logic.RefreshStation(-1); break;
                }
                conn.SendPacket(new NC_LocatorFilter(packet.AstroId, packet.QueryType, logic.filterIds));
            }

            public static void ShowPickerWindow(NC_LocatorFilter packet)
            {
                // skip if viewing planets are changed
                if (astroId != packet.AstroId)
                    return;

                var filterIds = FactoryLocator.Plugin.mainLogic.filterIds;
                filterIds.Clear();
                for (int i = 0; i < packet.Ids.Length; i++)
                    filterIds[packet.Ids[i]] = packet.Counts[i];

                switch (packet.QueryType)
                {
                    case -1:
                        UIMessageBox.Show("ACCESS DENY", "Server doesn't install FactoryLocator", "确定".Translate(), 3);
                        break;
                    
                    case 0:
                        UIentryCount.OnOpen(ESignalType.Item, filterIds);
                        UIItemPickerExtension.Popup(new Vector2(-300f, 250f), 
                            (itemProto) => {
                                if (itemProto != null)
                                {
                                    NebulaModAPI.MultiplayerSession.Network.SendPacket(new NC_LocatorResult(astroId, packet.QueryType, itemProto.ID));
                                    detailId = itemProto.ID;
                                }
                                UIentryCount.OnClose();
                            }, 
                            itemProto => filterIds.ContainsKey(itemProto.ID));
                        UIRoot.instance.uiGame.itemPicker.OnTypeButtonClick(2);
                        break;
                    
                    case 1:
                        UIentryCount.OnOpen(ESignalType.Item, filterIds);
                        UIItemPickerExtension.Popup(new Vector2(-300f, 250f),
                            (itemProto) => {
                                if (itemProto != null)
                                {
                                    NebulaModAPI.MultiplayerSession.Network.SendPacket(new NC_LocatorResult(astroId, packet.QueryType, itemProto.ID));
                                    detailId = itemProto.ID;
                                }
                                UIentryCount.OnClose();
                            },
                            itemProto => filterIds.ContainsKey(itemProto.ID));
                        UIRoot.instance.uiGame.itemPicker.OnTypeButtonClick(1);
                        break;
                    
                    case 2:
                        UIentryCount.OnOpen(ESignalType.Recipe, filterIds);
                        UIRecipePickerExtension.Popup(new Vector2(-300f, 250f),
                            (recipeProto) => {
                                if (recipeProto != null)
                                {
                                    NebulaModAPI.MultiplayerSession.Network.SendPacket(new NC_LocatorResult(astroId, packet.QueryType, recipeProto.ID));
                                    detailId = SignalProtoSet.SignalId(ESignalType.Recipe, recipeProto.ID);
                                }
                                UIentryCount.OnClose();
                            },
                            recipeProto => filterIds.ContainsKey(recipeProto.ID));
                        break;
                    
                    case 3:
                        UIentryCount.OnOpen(ESignalType.Signal, filterIds);
                        UISignalPickerExtension.Popup(new Vector2(-300f, 250f),
                            (signalId) => {
                                if (signalId > 0)
                                {
                                    NebulaModAPI.MultiplayerSession.Network.SendPacket(new NC_LocatorResult(astroId, packet.QueryType, signalId));
                                    detailId = signalId;
                                }
                                UIentryCount.OnClose();
                            },
                            signalId => filterIds.ContainsKey(signalId));
                        UIRoot.instance.uiGame.signalPicker.OnTypeButtonClick(1);
                        break;

                    case 4:
                        UIentryCount.OnOpen(ESignalType.Item, filterIds);
                        UIItemPickerExtension.Popup(new Vector2(-300f, 250f),
                            (itemProto) => {
                                if (itemProto != null)
                                {
                                    NebulaModAPI.MultiplayerSession.Network.SendPacket(new NC_LocatorResult(astroId, packet.QueryType, itemProto.ID));
                                    detailId = itemProto.ID;
                                }
                                UIentryCount.OnClose();
                            },
                            itemProto => filterIds.ContainsKey(itemProto.ID));
                        break;

                    case 5:
                        UIentryCount.OnOpen(ESignalType.Item, filterIds);
                        UIItemPickerExtension.Popup(new Vector2(-300f, 250f),
                            (itemProto) => {
                                if (itemProto != null)
                                {
                                    NebulaModAPI.MultiplayerSession.Network.SendPacket(new NC_LocatorResult(astroId, packet.QueryType, itemProto.ID));
                                    detailId = itemProto.ID;
                                }
                                UIentryCount.OnClose();
                            },
                            itemProto => filterIds.ContainsKey(itemProto.ID));
                        break;
                }
            }

            public static void HandleRequest(NC_LocatorResult packet, INebulaConnection conn)
            {
                StarData star = null;
                PlanetData planet = null;
                if (packet.AstroId % 100 == 0)
                    star = GameMain.data.galaxy.StarById(packet.AstroId / 100);
                else
                    planet = GameMain.data.galaxy.PlanetById(packet.AstroId);

                var logic = new MainLogic();
                logic.SetFactories(star, planet);
                switch (packet.QueryType)
                {
                    case 0: logic.RefreshBuilding(packet.ProtoId); break;
                    case 1: logic.RefreshVein(packet.ProtoId); break;
                    case 2: logic.RefreshAssemblers(packet.ProtoId); break;
                    case 3: logic.RefreshSignal(packet.ProtoId); break;
                    case 4: logic.RefreshStorage(packet.ProtoId); break;
                    case 5: logic.RefreshStation(packet.ProtoId); break;
                }
                conn.SendPacket(new NC_LocatorResult(packet.QueryType, logic.planetIds, logic.localPos, logic.detailIds));
            }

            public static void ShowResult(NC_LocatorResult packet)
            {
                var mainLogic = FactoryLocator.Plugin.mainLogic;
                mainLogic.planetIds.Clear();
                mainLogic.localPos.Clear();
                mainLogic.detailIds.Clear();
                for (int i = 0; i < packet.PlanetIds.Length; i++)
                {
                    mainLogic.planetIds.Add(packet.PlanetIds[i]);
                    mainLogic.localPos.Add(packet.LocalPos[i].ToVector3());                    
                }
                for (int i = 0; i < packet.DetailIds.Length; i++)
                    mainLogic.detailIds.Add(packet.DetailIds[i]);

                if (packet.QueryType != 3)
                    WarningSystemPatch.AddWarningData(mainLogic.SignalId, detailId, mainLogic.planetIds, mainLogic.localPos);
                else
                    WarningSystemPatch.AddWarningData(mainLogic.SignalId, detailId, mainLogic.planetIds, mainLogic.localPos, mainLogic.detailIds);
            }            
        }
    }
}
