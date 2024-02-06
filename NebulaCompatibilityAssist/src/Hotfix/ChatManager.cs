using HarmonyLib;
using System.Collections.Generic;
using System.Linq;

using NebulaCompatibilityAssist.Packets;
using NebulaAPI;
using NebulaModel.DataStructures.Chat;
using NebulaAPI.GameState;

namespace NebulaCompatibilityAssist.Hotfix
{
    public static class ChatManager
    {
        public static NC_AllModList ServerModList { get; set; }

        public static void Init(Harmony harmony)
        {
            // === Mod list check ===
            NebulaModAPI.OnPlayerJoinedGame += OnPlayerJoined;           
            harmony.PatchAll(typeof(ChatManager));
        }

        public static void OnDestory()
        {
            NebulaModAPI.OnPlayerJoinedGame -= OnPlayerJoined;
        }

        private static void OnPlayerJoined (IPlayerData player)
        {
            NebulaModAPI.MultiplayerSession.Network.SendToMatching(new NC_AllModList(0), iplayer => iplayer.Id == player.PlayerId);
        }

        public static void ShowMessageInChat(string message)
        {
            NebulaWorld.MonoBehaviours.Local.Chat.ChatManager.Instance?.SendChatMessage(message, ChatMessageType.SystemInfoMessage);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(NebulaWorld.Chat.Commands.InfoCommandHandler), "GetClientInfoText")]
        private static void GetClientInfoText_Postfix(bool full, ref string __result)
        {
            if (ServerModList == null || ServerModList.GUIDs == null || ServerModList.GUIDs.Length == 0)
                return;

            if (CheckModsVersion(out string report) > 0)
                __result += report;
        }

        public static int CheckModsVersion(out string report)
        {
            int count = 0;
            string serverStr = "";
            string clientStr = "";
            string mismatchStr = "";

            Dictionary<string, string> serverDict = new();
            int serverModCount = ServerModList.GUIDs.Length;
            for (int i = 0; i < serverModCount; i++)
            {
                if (!BepInEx.Bootstrap.Chainloader.PluginInfos.Keys.Contains(ServerModList.GUIDs[i]))
                {
                    serverStr += $"  {ServerModList.Names[i]} {ServerModList.Versions[i]}\n";
                    count++;
                }
                serverDict[ServerModList.GUIDs[i]] = ServerModList.Versions[i];
            }

            foreach (var pair in BepInEx.Bootstrap.Chainloader.PluginInfos)
            {
                string guid = pair.Key;
                string version = pair.Value.Metadata.Version.ToString();

                if (!serverDict.ContainsKey(pair.Key))
                {
                    clientStr += $"  {pair.Value.Metadata.Name} {version}\n";
                    count++;
                }
                else if (version != serverDict[pair.Key])
                {
                    mismatchStr += $"  {guid} {version} - server:{serverDict[pair.Key]}\n";
                    count++;
                }
            }

            report = "\n";
            if (clientStr != "")
                report += "\nNot on serever:\n" + clientStr;
            if (serverStr != "")
                report += "\nNot on client:\n" + serverStr;
            if (mismatchStr != "")
                report += "\nVersion mismatched:\n" + mismatchStr;

            return count;
        }
    }
}
