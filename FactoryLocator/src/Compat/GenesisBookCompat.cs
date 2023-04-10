using System;

namespace FactoryLocator.Compat
{
    public static class GenesisBookCompat
    {
        private const string GUID = "org.LoShin.GenesisBook";
        // last target version: 2.6.0

        public static void Init()
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;

            try
            {
                UIentryCount.ItemCol = 17;
                UIentryCount.RecipeCol = 17;
                UIentryCount.SignalCol = 17;
                Log.Debug("GenesisBook compat - OK");
            }
            catch (Exception e)
            {
                Log.Warn($"Nebula hotfix patch fail! Current version: " + pluginInfo.Metadata.Version);
                Log.Debug(e);
            }
        }
    }
}
