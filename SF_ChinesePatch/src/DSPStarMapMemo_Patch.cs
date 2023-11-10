namespace SF_ChinesePatch
{
    public class DSPStarMapMemo_Patch
    {
        public const string NAME = "DSPStarMapMemo";
        public const string GUID = "Appun.DSP.plugin.StarMapMemo";

        public static void OnAwake()
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(GUID)) return;
            if (!Plugin.Instance.Config.Bind("Enable", NAME, true).Value) return;
            RegisterStrings();
        }

        private static void RegisterStrings()
        {
            StringManager.RegisterString("Memo", "备注");
            StringManager.RegisterString("Press [Enter] to insert a line break.", "按下 [Enter] 插入换行符。");
            StringManager.RegisterString("Press [CTRL] to hide star memo.", "按下 [CTRL] 隐藏星球备注。");
        }
    }
}
