using BepInEx;
using CommonAPI;
using CommonAPI.Systems;
using UnityEngine;
using CommonAPI.Systems.ModLocalization;

namespace MassRecipePaste
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency(CommonAPIPlugin.GUID)]
    [CommonAPISubmoduleDependency(nameof(LocalizationModule), nameof(CustomKeyBindSystem))]
    public class PluginCAPIcompat : BaseUnityPlugin
    {
        // This compatible plugin only load when CommonAPI is present
        public const string GUID = "starfi5h.plugin.MassRecipePaste.CAPIcompat";
        public const string NAME = "MassRecipePasteCAPIcompat";
        public const string VERSION = "1.0.0";
        public static bool IsRegisiter = false;

        public void Awake()
        {
            CustomKeyBindSystem.RegisterKeyBind<PressKeyBind>(new BuiltinKey
            {
                key = new CombineKey((int)KeyCode.Period, CombineKey.CTRL_COMB, ECombineKeyAction.OnceClick, false),
                conflictGroup = 2048,
                name = "MassRecipePaste",
                canOverride = true
            });
            LocalizationModule.RegisterTranslation("KEYMassRecipePaste", "(Mod) Mass Recipe Paste", "(Mod) 范围配方黏贴", "Mass Recipe Paste");
            IsRegisiter = true;
        }

        public static bool IsPressed()
        {
            return CustomKeyBindSystem.GetKeyBind("MassRecipePaste").keyValue;
        }
    }
}
