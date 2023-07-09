namespace SF_ChinesePatch
{
    public class BulletTime_Patch
    {
        public const string GUID = "com.starfi5h.plugin.BulletTime";

        public static void OnAwake()
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(GUID))
                return;
            RegisterStrings();
        }

        private static void RegisterStrings()
        {
            StringManager.RegisterString("Read-Only", "只读模式");
            StringManager.RegisterString("Can't interact with game world during auto-save\nPlease wait or press ESC to close the window", "自动保存期间无法与游戏世界交互\n请等待或按ESC关闭窗口");
            StringManager.RegisterString("Saving...", "保存中...");
            StringManager.RegisterString("Pause", "暂停");
            StringManager.RegisterString("Click to resume rotating", "点击以恢复旋转");
            StringManager.RegisterString("Click to stop rotating", "点击以停止旋转");

            StringManager.RegisterString("Host is saving game...", "主机正在保存游戏...");
            StringManager.RegisterString("{0} arriving {1}", "{0} 即将抵达 {1}");
            StringManager.RegisterString("{0} joining the game", "{0} 正在加入游戏");
        }
    }
}
