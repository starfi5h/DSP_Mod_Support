namespace SF_ChinesePatch
{
    public class BulletTime_Patch
    {
        public const string NAME = "BulletTime";
        public const string GUID = "com.starfi5h.plugin.BulletTime";

        public static void OnAwake()
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(GUID)) return;
            if (!Plugin.Instance.Config.Bind("Enable", NAME, true).Value) return;

            RegisterStrings();
        }

        private static void RegisterStrings()
        {
            StringManager.RegisterString("Pause", "暂停");
            StringManager.RegisterString("Toggle tactical pause mode", "切换战术暂停模式");
            StringManager.RegisterString("Resume", "恢复");
            StringManager.RegisterString("Reset game speed back to 1x", "将游戏速度重设为1倍");
            StringManager.RegisterString("SpeedUp", "加速");
            StringManager.RegisterString("Left click to increase game speed\nRight click to set to max ({0}x)", "左键单击可提高游戏速度\n右键单击可设置为最大({0}x)");

            StringManager.RegisterString("Background autosave", "后台自动保存");
            StringManager.RegisterString("Read-Only", "只读模式");
            StringManager.RegisterString("Can't interact with game world during auto-save\nPlease wait or press ESC to close the window", "自动保存期间无法与游戏世界交互\n请等待或按ESC关闭窗口");
            StringManager.RegisterString("Saving...", "保存中...");
            StringManager.RegisterString("Dyson sphere is rotating", "点击以停止旋转");
            StringManager.RegisterString("Dyson sphere is stopped", "点击以恢复旋转");
            StringManager.RegisterString("Click to stop rotating", "点击以停止旋转");
            StringManager.RegisterString("Click to resume rotating", "点击以恢复旋转");

            StringManager.RegisterString("Host is saving game...", "主机正在保存游戏...");
            StringManager.RegisterString("{0} arriving {1}", "{0} 即将抵达 {1}");
            StringManager.RegisterString("{0} joining the game", "{0} 正在加入游戏");

            StringManager.RegisterString("{0} pause the game", "{0} 暂停游戏");
            StringManager.RegisterString("{0} resume the game", "{0} 继续游戏");
            StringManager.RegisterString("{0} set game speed = {1:F1}", "{0} 设置游戏速度 = {1:F1}");
        }
    }
}
