using BepInEx.Configuration;
using UnityEngine;

namespace FactoryLocator
{
    public class ModConfig
    {
        // User config
        public ConfigEntry<int> SignalIconId { get; }
        public ConfigEntry<bool> AutoClearQuery { get; }
        
        // Inner settings
        public ConfigEntry<bool> RecordRecipeSignal { get; }

        public ModConfig(ConfigFile config)
        {
            SignalIconId = config.Bind("General", "SignalIconId", 401);
            AutoClearQuery = config.Bind("General", "AutoClearQuery", true);

            RecordRecipeSignal = config.Bind("Record", "RecordRecipeSignal", true);
        }
    }
}
