using System;

namespace FactoryLocator.Compat
{
    public static class DSPMoreRecipesCompat
    {
        private const string GUID = "Appun.DSP.plugin.MoreRecipes";
        // last target version: 1.0.3

        public static void Init()
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var _))
                return;

            try
            {
                UIentryCount.RecipeCol = 17;
                Log.Debug("DSPMoreRecipes compat - OK");
            }
            catch (Exception e)
            {
                Log.Warn("DSPMoreRecipesCompat fail! Last target version: 1.0.3");
                Log.Debug(e);
            }
        }
    }
}
