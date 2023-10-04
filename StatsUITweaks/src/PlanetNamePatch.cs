using HarmonyLib;

namespace StatsUITweaks
{
    public class PlanetNamePatch
    {
        [HarmonyPrefix, HarmonyPatch(typeof(PlanetData), nameof(PlanetData.RegenerateName))]
        static void RegenerateName(PlanetData __instance, bool notifychange, ref bool __runOriginal)
        {
            if (!__runOriginal) return;

			string str = (__instance.index + 1).ToString();
            __instance.name = __instance.star.displayName + " " + str + "号星".Translate();
			if (notifychange && string.IsNullOrEmpty(__instance.overrideName))
                __instance.NotifyOnDisplayNameChange();

            __runOriginal = false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GalaxyData), nameof(GalaxyData.RegeneratePlanetNames))]
        static void RegenerateName(GalaxyData __instance, ref bool __runOriginal)
        {
            if (!__runOriginal) return;

            foreach (StarData starData in __instance.stars)
                starData.RegeneratePlanetNames(false); // Regnerate all planet names to replace numbers when load

            __runOriginal = false;
        }
    }
}
