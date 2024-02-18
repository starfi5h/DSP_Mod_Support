using HarmonyLib;

namespace SF_ChinesePatch
{
    public class NebulaMultiplayer_Patch
    {
        public const string NAME = "NebulaMultiplayerMod";
        public const string GUID = "dsp.nebula-multiplayer";

        public static void OnAwake(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(GUID)) return;
            if (!Plugin.Instance.Config.Bind("Enable", NAME, true).Value) return;
            RegisterStrings();

            try
            {
                harmony.Patch(AccessTools.Method("NebulaPatcher.Patches.Dynamic.UIGalaxySelect_Patch.DisableDarkFogToggle"),
                    null, null, new HarmonyMethod(typeof(Plugin).GetMethod(nameof(Plugin.TranslateStrings))));
            }
            catch
            {
                Plugin.Log.LogWarning("Skip DisableDarkFogToggle translate");
            }
        }

        private static void RegisterStrings()
        {
            #region Menu UI 主選單
            StringManager.RegisterString("Multiplayer", "多人游戏");
            StringManager.RegisterString("New Game (Host)", "新游戏 (主机)");
            StringManager.RegisterString("Load Game (Host)", "加载存档 (主机)");
            StringManager.RegisterString("Join Game", "加入游戏");
            StringManager.RegisterString("Back", "返回");
            StringManager.RegisterString("Host IP Address", "主机IP地址");
            StringManager.RegisterString("Password (optional)", "密码 (可选)");
            #endregion

            #region Popup response 彈窗回應
            StringManager.RegisterString("OK", "确认");
            StringManager.RegisterString("Close", "关闭");
            StringManager.RegisterString("Accept", "接受");
            StringManager.RegisterString("Reject", "拒绝");
            StringManager.RegisterString("Reload", "重载");
            #endregion

            #region Options 設置
            StringManager.RegisterString("General", "常规");
            StringManager.RegisterString("Network", "网络");
            StringManager.RegisterString("Chat", "聊天");

            StringManager.RegisterString("Nickname", "玩家名称");
            StringManager.RegisterString("NameTagSize", "玩家名称尺寸");
            StringManager.RegisterString("Show Lobby Hints", "显示大厅说明");
            StringManager.RegisterString("Sync Ups", "同步逻辑帧");
            StringManager.RegisterString("If enabled the UPS of each player is synced. This ensures a similar amount of GameTick() calls.", "启用后，会调整本地的逻辑帧率以和远端主机同步");
            StringManager.RegisterString("Sync Soil", "共享砂土");
            StringManager.RegisterString("If enabled the soil count of each players is added together and used as one big pool for everyone. Note that this is a server side setting applied to all clients.", "启用后，砂土设置将从个人转变为群体共享。此项为主机设置");
            StringManager.RegisterString("Streamer mode", "直播模式");
            StringManager.RegisterString("If enabled specific personal information like your IP address is hidden from the ingame chat and input fields.", "直播模式中，隐私数据（IP地址等）将会用星号隐藏");
            StringManager.RegisterString("Enable Achievement", "启用成就");

            StringManager.RegisterString("Server Password", "服务器密码");
            StringManager.RegisterString("If provided, this will set a password for your hosted server.", "设置不为空时，玩家需输入密码才能进入服务器");
            StringManager.RegisterString("Host Port", "主机端口");
            StringManager.RegisterString("Enable UPnp/Pmp Support", "启用UPnp/Pmp支持");
            StringManager.RegisterString("If enabled, attempt to automatically create a port mapping using UPnp/Pmp (only works if your router has this feature and it is enabled)", "尝试使用UPnp/Pmp自动创建端口映射（仅当您的路由器具有此功能并且已启用时有效）");
            StringManager.RegisterString("Enable Experimental Ngrok support", "启用Ngrok支持");
            StringManager.RegisterString("If enabled, when hosting a server this will automatically download and install the Ngrok client and set up an Ngrok tunnel that provides an address at which the server can be joined", "创建服务器时，尝试下载并安装Ngrok客户端，并设置一个Ngrok隧道，提供一个可加入服务器的地址");
            StringManager.RegisterString("Ngrok Authtoken", "Ngrok授权令牌");
            StringManager.RegisterString("This is required for Ngrok support and can be obtained by creating a free account at https://ngrok.com/", "授权令牌可在https://ngrok.com/注册免费帐号获取");
            StringManager.RegisterString("Ngrok Region", "Ngrok地区");
            StringManager.RegisterString("Available Regions: us, eu, au, ap, sa, jp, in ", "可选的地区: us, eu, au, ap, sa, jp, in");
            StringManager.RegisterString("Remember Last IP", "记住上次主机地址");
            StringManager.RegisterString("Remember Last Client Password", "记住上次输入的密码");
            StringManager.RegisterString("Enable Discord RPC (requires restart)", "启用Discord RPC（需要重启）");
            StringManager.RegisterString("Auto accept Discord join requests", "自动允许Discord加入请求");
            StringManager.RegisterString("IP Configuration", "Discord IP设置");
            StringManager.RegisterString("Configure which type of IP should be used by Discord RPC", "设置Discord要使用哪种IP");
            StringManager.RegisterString("Cleanup inactive sessions", "清除不活跃的sessions");
            StringManager.RegisterString("If disabled the underlying networking library will not cleanup inactive connections. This might solve issues with clients randomly disconnecting and hosts having a 'System.ObjectDisposedException'.", "启用后会自动清除不活跃的会话，可能解决客户端随机断开连接和主机出现“System.ObjectDisposedException”错误的问题");

            StringManager.RegisterString("Chat Hotkey", "聊天窗口热键");
            StringManager.RegisterString("Auto Open Chat", "自动开启聊天窗口");
            StringManager.RegisterString("Auto open chat window when receiving message from other players", "收到其他玩家消息时自动开启聊天窗口");
            StringManager.RegisterString("Show system warn message", "显示系统警告消息");
            StringManager.RegisterString("Show system info message", "显示系统通知消息");
            StringManager.RegisterString("Default chat position", "聊天窗口位置");
            StringManager.RegisterString("Default chat size", "聊天字体大小");
            StringManager.RegisterString("Notification duration", "通知停留时间");
            StringManager.RegisterString("Chat Window Opacity", "聊天窗口不透明度");
            #endregion

            #region Server 主機提示
            StringManager.RegisterString("No UPnp or Pmp compatible/enabled NAT device found", "未找到支持/启用UPnp或Pmp的NAT设备");
            StringManager.RegisterString("Could not create UPnp or Pmp port mapping", "无法创建UPnp或Pmp端口映射");
            StringManager.RegisterString("An error occurred while hosting the game: ", "在创建服务器时发生错误: ");

            var Nebula_LobbyMessage = "当玩家首次进入游戏时会进入大厅，在这里可以预览星球资源和选择起始星球\n" +
            "点击恒星可以展开其行星系。行星系中点击星球能查看其详细信息。\n" +
            "在打开行星详细信息面板时，再点击行星一次就可以将其设置为出生星球。\n" +
            "点击外太空可以回到上一层次。滚轮放大/缩小。长按Alt键可查看星球名称。\n\n" +
            "在游戏中 Alt + ~ 可以打开聊天窗口。输入/help可以查看所有命令。\n" +
            "联机mod可能和某些模组不相容。模组NebulaCompatibilityAssist有详细的说明\n" +
            "当出错或失去同步时可以让客户端重连,或主机保存重开。祝游戏愉快!";
            StringManager.RegisterString("The Lobby", "大厅说明");
            StringManager.RegisterString("Nebula_LobbyMessage", Nebula_LobbyMessage);

            StringManager.RegisterString("Not supported in multiplayer", "尚未支援");
            StringManager.RegisterString("Enabling enemy forces is currently not supported in multiplayer.", "目前多人游戏不支持启用战斗模式");
            StringManager.RegisterString("Loading saved games with combat mode enabled is currently not supported in multiplayer.", "目前多人游戏不支持加载已启用战斗模式的存档");
            #endregion

            #region Disconnect reasons 斷線原因
            StringManager.RegisterString("Mod Mismatch", "模组不匹配");
            StringManager.RegisterString("You are missing mod {0}", "你缺少模组 {0}");
            StringManager.RegisterString("Server is missing mod {0}", "服务器缺少模组 {0}");

            StringManager.RegisterString("Mod Version Mismatch", "模组版本不匹配");
            StringManager.RegisterString("Your mod {0} version is not the same as the Host version.\nYou:{1} - Remote:{2}", "你的模组 {0} 版本与主机版本不同。\n你:{1} - 主机:{2}");

            StringManager.RegisterString("Game Version Mismatch", "游戏版本不匹配");
            StringManager.RegisterString("Your version of the game is not the same as the one used by the Host.\nYou:{0} - Remote:{1}", "你的游戏版本与主机使用的版本不同。\n你:{0} - 主机:{1}");

            StringManager.RegisterString("Server Requires Password", "需要密码");
            StringManager.RegisterString("Server is protected. Please enter the correct password:", "服务器受保护。请输入正确的密码:");

            StringManager.RegisterString("Server Busy", "服务器繁忙");
            StringManager.RegisterString("Server is not ready to join. Please try again later.", "服务器暂时无法加入。请稍后再试。");

            StringManager.RegisterString("Connection Lost", "连接中断");
            StringManager.RegisterString("You have been disconnected from the server.", "你已与服务器断开连接。");

            StringManager.RegisterString("Server Unavailable", "无法使用");
            StringManager.RegisterString("Could not reach the server, please try again later.", "无法连接到服务器，请稍后再试。");
            #endregion

            #region Popout 提示
            StringManager.RegisterString("Connecting", "连接中");
            StringManager.RegisterString("Connecting to server...", "正在连接至服务器...");

            StringManager.RegisterString("Connect failed", "连接失败");
            StringManager.RegisterString("Was not able to connect to server", "无法连接至服务器");

            StringManager.RegisterString("Loading", "加载中");
            StringManager.RegisterString("Loading state from server, please wait", "正在从服务器加载状态，请稍候");
            StringManager.RegisterString("Loading Dyson sphere {0}, please wait", "正在加载戴森球 {0}，请稍候");
            StringManager.RegisterString("{0} joining the game, please wait\n(Use BulletTime mod to unfreeze the game)", "{0} 正在加入游戏，请稍候\n（使用BulletTime模组解除冻结）");

            StringManager.RegisterString("Unavailable", "无法使用");
            StringManager.RegisterString("The host is not ready to let you in, please wait!", "主机尚未准备好让您进入，请稍候！");
            StringManager.RegisterString("Milky Way is disabled in multiplayer game.", "银河系功能在多人游戏中禁用");

            StringManager.RegisterString("Desync", "失去同步");
            StringManager.RegisterString("Dyson sphere id[{0}] {1} is desynced.", "戴森球 id[{0}] {1} 不同步。");

            StringManager.RegisterString("Please wait for server respond", "请等待服务器响应");
            StringManager.RegisterString("(Desync) EntityId mismatch {0} != {1} on planet {2}. Clients should reconnect!", "(不同步) EntityId不匹配 {0} != {1} 在星球 {2} 上。请重新连接！");
            StringManager.RegisterString("(Desync) PrebuildId mismatch {0} != {1} on planet {2}. Please reconnect!", "(不同步) PrebuildId不匹配 {0} != {1} 在星球 {2} 上。请重新连接！");
            #endregion

            #region Remote Commands 遠端命令
            StringManager.RegisterString("Tell remote server to save/load", "命令远程服务器进行保存/加载");
            StringManager.RegisterString("Remote server access is not enabled", "远程服务器访问(Remote server access)设置未启用");
            StringManager.RegisterString("You need to login first!", "您需要先登录！");
            StringManager.RegisterString("You have already logged in", "您已经登录了");
            StringManager.RegisterString("Cooldown: {0}s", "冷却时间：{0}秒");
            StringManager.RegisterString("Password incorrect!", "密码不正确！");
            StringManager.RegisterString("Login success!", "登录成功！");
            StringManager.RegisterString("Save list on server: ({0}/{1})", "在服务器上存档列表：({0}/{1})");
            StringManager.RegisterString("Save {0} : {1}", "保存 {0}：{1}");
            StringManager.RegisterString("Success", "成功");
            StringManager.RegisterString("Fail", "失败");
            StringManager.RegisterString("{0} doesn't exist", "{0} 不存在");
            #endregion

            #region Chat 聊天命令
            StringManager.RegisterString("SYSTEM", "系统");
            StringManager.RegisterString("[{0:HH:mm}] {1} connected ({2})", "[{0:HH:mm}] {1} 加入游戏 ({2})");
            StringManager.RegisterString("[{0:HH:mm}] {1} disconnected", "[{0:HH:mm}] {1} 离开游戏");
                        
            StringManager.RegisterString("User not found {0}", "未找到用户 {0}");
            StringManager.RegisterString("Unknown command", "未知命令");
            StringManager.RegisterString("Copy", "复制");
            StringManager.RegisterString("Click to copy to clipboard", "点击复制到剪贴板");
            StringManager.RegisterString("Navigate", "导航");
            StringManager.RegisterString("Click to create a navigate line to the target.", "点击创建导航线到目标");

            StringManager.RegisterString("Clear all chat messages (locally)", "清除所有聊天消息（本地）");

            StringManager.RegisterString("Command {0} was not found! Use /help to get list of known commands.", "找不到命令 {0}！使用 /help 查看已知命令列表。");
            StringManager.RegisterString("Command ", "命令 ");
            StringManager.RegisterString("Usage: ", "用法: ");
            StringManager.RegisterString("Aliases: ", "别名: ");
            StringManager.RegisterString("Known commands:", "已知命令:");
            StringManager.RegisterString("For detailed information about command use /help <command name>", "有关命令的详细信息，请使用 /help <命令名称>");
            StringManager.RegisterString("Get list of existing commands and their usage", "获取现有命令及其用法");

            StringManager.RegisterString("This command can only be used in multiplayer!", "此命令仅适用于多人游戏！");
            StringManager.RegisterString("Pending...", "等待中...");
            StringManager.RegisterString("Server info:", "服务器信息:");
            StringManager.RegisterString("Local IP address: ", "本地 IP 地址: ");
            StringManager.RegisterString("WANv4 IP address: ", "WANv4 IP 地址: ");
            StringManager.RegisterString("WANv6 IP address: ", "WANv6 IP 地址: ");
            StringManager.RegisterString("Ngrok address: ", "Ngrok 地址: ");
            StringManager.RegisterString("Ngrok address: Tunnel Inactive!", "Ngrok 地址: 隧道未激活！");
            StringManager.RegisterString("Port status: ", "端口状态: ");
            StringManager.RegisterString("Data state: ", "数据状态: ");
            StringManager.RegisterString("Uptime: ", "运行时间: ");
            StringManager.RegisterString("Game info:", "游戏信息:");
            StringManager.RegisterString("Game Version: ", "游戏版本: ");
            StringManager.RegisterString("Mod Version: ", "模组版本: ");
            StringManager.RegisterString("Mods installed:", "已安装模组:");
            StringManager.RegisterString("Client info:", "客户端信息:");
            StringManager.RegisterString("Host IP address: ", "主机 IP 地址: ");
            StringManager.RegisterString("Use '/info full' to see mod list.", "使用 '/info full' 查看模组列表。");
            StringManager.RegisterString("Get information about server", "获取服务器信息");

            StringManager.RegisterString("Starting navigation to ", "开始导航至 ");
            StringManager.RegisterString("Failed to start navigation, please check your input.", "导航失败，请检查您的输入。");
            StringManager.RegisterString("navigation cleared", "导航已清除");
            StringManager.RegisterString("Start navigating to a specified destination", "开始导航至指定目的地");

            StringManager.RegisterString("Test command", "测试命令");

            StringManager.RegisterString("This command can only be used in multiplayer and as client!", "此命令仅适用于多人游戏客户端！");
            StringManager.RegisterString("Perform a reconnect.", "重新连接");

            StringManager.RegisterString("Unknown command! Available commands: {login, list, save, load, info}", "未知命令！可用命令: {login, list, save, load, info}");
            StringManager.RegisterString("Not enough arguments!", "参数不足！");
            StringManager.RegisterString("Need to specify a save!", "需要指定一个存档！");

            StringManager.RegisterString(" (moon)", " (卫星)");
            StringManager.RegisterString("Could not find given star '{0}'", "找不到给定的星体 '{0}'");
            StringManager.RegisterString("List planets in a system", "列出星系中的行星");

            StringManager.RegisterString("Player not found: ", "未找到玩家: ");
            StringManager.RegisterString("Not connected, can't send message", "未连接，无法发送消息");
            StringManager.RegisterString("Send direct message to player. Use /who for valid user names", "向玩家发送直接消息。使用 /who 获取有效的用户名");

            StringManager.RegisterString("/who results: ({0} players)\r\n", "/who 结果: {0} 个玩家\r\n");
            StringManager.RegisterString(" (host)", " (主机)");
            StringManager.RegisterString(", in space", ", 在太空");
            StringManager.RegisterString(", at coordinates ", ", 坐标为 ");
            StringManager.RegisterString("List all players and their locations", "列出所有玩家及其位置");

            StringManager.RegisterString(">> Bad command. Use /x -help to get list of known commands.", ">> 错误的命令。使用 /x -help 获取已知命令列表。");
            StringManager.RegisterString("Execute developer console command", "执行开发者控制台命令");

            #endregion
        }
    }
}
