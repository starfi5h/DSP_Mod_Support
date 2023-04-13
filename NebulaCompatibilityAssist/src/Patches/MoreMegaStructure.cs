using crecheng.DSPModSave;
using HarmonyLib;
using NebulaAPI;
using NebulaCompatibilityAssist.Packets;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NebulaCompatibilityAssist.Patches
{
    public static class MoreMegaStructure
    {
        private const string NAME = "MoreMegaStructure";
        private const string GUID = "Gnimaerd.DSP.plugin.MoreMegaStructure";
        private const string VERSION = "1.1.9";

        private static IModCanSave Save;

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;
            Assembly assembly = pluginInfo.Instance.GetType().Assembly;

            try
            {                
                Save = pluginInfo.Instance as IModCanSave;
                NC_Patch.OnLogin += SendRequest;
                NC_ModSaveRequest.OnReceive += (guid, conn) =>
                {
                    if (guid != GUID) return;
                    conn.SendPacket(new NC_ModSaveData(GUID, Export()));
                };
                NC_ModSaveData.OnReceive += (guid, bytes) =>
                {
                    if (guid != GUID) return;
                    Import(bytes);
                };
                
                // Sync MegaStructure type
                Type classType = assembly.GetType("MoreMegaStructure.MoreMegaStructure");
                harmony.Patch(AccessTools.Method(classType, "SetMegaStructure"), null, new HarmonyMethod(typeof(MoreMegaStructure).GetMethod("SendData")));
                harmony.Patch(AccessTools.Method(classType, "BeforeGameTickPostPatch"), new HarmonyMethod(typeof(MoreMegaStructure).GetMethod("SuppressOnClient")));

                // Sync StarAssembly recipeIds & weights 
                classType = assembly.GetType("MoreMegaStructure.StarAssembly");
                harmony.Patch(AccessTools.Method(classType, "OnRecipePickerReturn"), null, new HarmonyMethod(typeof(MoreMegaStructure).GetMethod("SendData")));
                var sliders = AccessTools.StaticFieldRefAccess<List<Slider>>(classType, "sliders");
                foreach (var slider in sliders)
                {
                    GameObject go = slider.gameObject;
                    GameObject.Destroy(go.GetComponent<PointerDownUpHandler>());
                    var handler = go.AddComponent<PointerDownUpHandler>();
                    handler.onPointerUp += (_) => SendData();
                }

                // Disable UI update when editor window is closed
                harmony.Patch(AccessTools.Method(classType, "UIFrameUpdate"),  new HarmonyMethod(typeof(MoreMegaStructure).GetMethod("SuppressUIupdate")));

                Log.Info($"{NAME} - OK");
                NC_Patch.RequriedPlugins += " +" + NAME;
            }
            catch (Exception e)
            {
                Log.Warn($"{NAME} - Fail! Last target version: {VERSION}");
                NC_Patch.ErrorMessage += $"\n{NAME} (last target version: {VERSION})";
                Log.Debug(e);
            }
        }

        public static void SendRequest()
        {
            if (NebulaModAPI.IsMultiplayerActive && NebulaModAPI.MultiplayerSession.LocalPlayer.IsClient)
            {
                NebulaModAPI.MultiplayerSession.Network.SendPacket(new NC_ModSaveRequest(GUID));
            }
        }

        public static void SendData()
        {
            if (NebulaModAPI.IsMultiplayerActive)
            {
                NebulaModAPI.MultiplayerSession.Network.SendPacket(new NC_ModSaveData(GUID, Export()));
            }
        }

        public static byte[] Export()
        {
            if (Save != null)
            {
                using var p = NebulaModAPI.GetBinaryWriter();
                Save.Export(p.BinaryWriter);
                return p.CloseAndGetBytes();
            }
            else
            {
                return new byte[0];
            }
        }

        public static void Import(byte[] bytes)
        {
            if (Save != null)
            {
                using var p = NebulaModAPI.GetBinaryReader(bytes);
                Save.Import(p.BinaryReader);
            }
        }

        public static bool SuppressOnClient()
        {
            return !NebulaModAPI.IsMultiplayerActive || NebulaModAPI.MultiplayerSession.LocalPlayer.IsHost;
        }

        public static bool SuppressUIupdate()
        {
            return UIRoot.instance.uiGame.dysonEditor.active;
        }

#pragma warning disable IDE1006
        public class PointerDownUpHandler : ManualBehaviour, IPointerDownHandler, IPointerUpHandler
        {
            public int Id;
            public event Action<int> onPointerDown;
            public event Action<int> onPointerUp;

            public void OnPointerDown(PointerEventData pointerEventData)
            {
                onPointerDown?.Invoke(Id);
            }

            public void OnPointerUp(PointerEventData pointerEventData)
            {
                onPointerUp?.Invoke(Id);
            }
        }
#pragma warning restore IDE1006
    }
}
