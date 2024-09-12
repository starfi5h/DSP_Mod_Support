using HarmonyLib;
using NebulaAPI;
using SphereOpt;
using System;

namespace NebulaCompatibilityAssist.Patches
{
    public class SphereOpt_Patch
    {
        public const string NAME = "SphereOpt";
        public const string GUID = "SphereOpt";
        public const string VERSION = "0.9.1";

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var _))
                return;

            try
            {
                NebulaModAPI.OnDysonSphereLoadFinished += Warper.OnDysonSphereLoadFinished;
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

        public static class Warper
        {
            public static void OnDysonSphereLoadFinished(int starIndex)
            {
                var dysonSphere = GameMain.data.dysonSpheres[starIndex];
                Log.Debug($"OnDysonSphereLoad [{starIndex}] {dysonSphere.starData.displayName}");

                if (SphereOpt.SphereOpt.instRenderers.TryGetValue(dysonSphere.starData.id, out var renderer))
                {
                    // Reset dysonSphere reference after dyson sphere is loaded in client 
                    renderer.dysonSphere = dysonSphere;
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(InstDysonShellRenderer), nameof(InstDysonShellRenderer.RenderShells))]
            static bool RenderShells_Prefix()
            {
                // in line: sunPos = ((localPlanet == null) ? (this.starData.uPosition - mainPlayer.uPosition) : Maths.QInvRotateLF(localPlanet.runtimeRotation, this.starData.uPosition - localPlanet.uPosition));
                // there is an issue that mainPlayer may be null and cause NRE
                // So just don't render shells in demo to avoid the error
                return GameMain.mainPlayer != null;
            }
        }
    }
}
