using System.Collections.Generic;

namespace CameraTools
{
    public static class Extensions
    {
        static readonly Dictionary<string[], string[]> translatedTexts = new();
        static readonly Dictionary<string, string> strings = new()
        {
            { "View", "查看" },
            { "[Viewing]", "[查看中]" },
            { "Edit", "编辑" },
            { "[Editing]", "[编辑中]" },
            { "Load", "加载" },
            { "Add", "添加" },
            { "Remove", "移除" },
            { "edit", "修改" },
            { "set", "设置" },

            { "Camera List", "摄像机列表" },
            { "Add Camera", "添加摄像机" },
            { "Camera", "摄像机" },
            { "Path", "路径" },
            { "Config", "配置" },

            { "Camera Config", "摄像机配置" },
            { "Unavailable", "无法查看" },
            { "Camera type mismatch to current environment!", "摄影机类型与当前环境不一致!" },
            { "Set to Current View", "设置为当前视角" },
            { "Adjust Mode", "调整模式" },
            { "[Adjusting]", "[调整中]" },
            { "Name", "名称" },
            { "Camera Type", "摄像机类型" },
            { "Planet", "星球" },
            { "Space", "太空" },
            { "Position", "位置" },
            { "Local Position", "本地位置" },
            { "Space Position", "宇宙位置" },
            { "Cartesian", "直角坐标" },
            { "Polar", "极坐标" },
            { "Log", "经度" },
            { "Lat", "纬度" },
            { "Alt", "高度" },
            { "Rotation", "旋转" },
            { "pitch", "俯仰" },
            { "yaw", "偏摆" },
            { "roll", "翻滚" },
            { "Fov", "视野" },
            { "Save All", "保存全部" },
            { "Load All", "加载全部" },

            { "Path Config", "路径配置" },
            { "Progress: ", "进度: " },
            { "Duration(s)", "持续时间(秒)" },
            { "Interp", "插值" },
            { "Linear", "线性" },
            { "Spherical", "球面" },
            { "Curve", "曲线" },
            { "Hide GUI during playback", "播放期间隐藏界面" },
            { "Ratio", "比例" },
            { "Second", "秒" },
            { "Auto Split", "自动分割" },
            { "Insert Keyframe", "插入关键帧" },
            { "Append Keyframe", "添加关键帧" },
            { "Save/Load", "保存/加载" },
            { "Target: ", "朝向: " },

            { "Target Config", "目标配置" },
            { "Type", "类型" },
            { "None", "无" },
            { "Mecha", "机甲" },
            { "Offset to Mecha", "偏移" },
            { "Marker Size", "标记大小" },

            { "Path List", "路径列表" },
            { "Add Path", "添加路径" },

            { "Mod Config", "模组配置" },
            { "Camera List Window", "摄像机列表窗口" },
            { "Camera Path Window", "摄像机路径窗口" },
            { "Toggle Last Cam", "切换到上次机位" },
            { "Cycle To Next Cam", "循环到下一个机位" },
            { "Move Player With Space Camera", "随太空摄像机移动玩家" },
            { "Reset Windows Position", "重置窗口位置" },

            { "I/O", "输入/输出" },
            { "Export cfg File", "导出配置文件" },
            { "Overwrite ", "覆盖文件 " },
            { "Current Path", "当前路径" },
            { "All", "全部" },
            { "Import cfg File", "导入配置文件" },
            { "Load {0} camera and {1} path.", "加载 {0} 个摄像机和 {1} 个路径." }
        };

        internal static string Translate(this string s)
        {
            if (Localization.isZHCN && strings.TryGetValue(s, out string value))
            {
                return value;
            }
            //return Localization.Translate(s);
            return s;
        }

        public static string[] TL(string[] optionTexts)
        {
            if (!Localization.isZHCN) return optionTexts;
            if (translatedTexts.TryGetValue(optionTexts, out var value)) return value;

            value = new string[optionTexts.Length];
            for (int i = 0; i < optionTexts.Length; i++)
            {
                value[i] = optionTexts[i].Translate();
            }
            translatedTexts[optionTexts] = value;
            return value;
        }
    }
}
