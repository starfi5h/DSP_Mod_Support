using HarmonyLib;
using NebulaAPI;
using NebulaCompatibilityAssist.Hotfix;

namespace NebulaCompatibilityAssist.Packets
{
    public class NC_AllModList
    {
        public string[] GUIDs { get; set; }
        public string[] Names { get; set; }
        public string[] Versions { get; set; }
        public bool SandboxToolsEnabled { get; set; }

        public NC_AllModList() {}
        public NC_AllModList(int _)
        {
            int count = BepInEx.Bootstrap.Chainloader.PluginInfos.Count;
            GUIDs = new string[count];
            Names = new string[count];
            Versions = new string[count];

            int i = 0;
            foreach (var pair in BepInEx.Bootstrap.Chainloader.PluginInfos)
            {
                GUIDs[i] = pair.Key;
                Names[i] = pair.Value.Metadata.Name;
                Versions[i] = pair.Value.Metadata.Version.ToString();
                i++;
            }
            SandboxToolsEnabled = GameMain.sandboxToolsEnabled;
        }
    }

    [RegisterPacketProcessor]
    internal class NC_AllModListProcessor : BasePacketProcessor<NC_AllModList>
    {
        public override void ProcessPacket(NC_AllModList packet, INebulaConnection conn)
        {
            if (IsClient)
            {
                ChatManager.ServerModList = packet;
                int count = ChatManager.CheckModsVersion(out _);
                if (count > 0)
                {
                    ChatManager.ShowMessageInChat($"Server mods diff = {count}. Type /info full to see details.");
                }
                // Will it too slow to set sandboxToolsEnabled?
                GameMain.sandboxToolsEnabled = packet.SandboxToolsEnabled;
            }
        }
    }
}
