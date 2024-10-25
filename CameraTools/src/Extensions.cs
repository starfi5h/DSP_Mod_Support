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
            { "Loop", "循环" },
            { "Hide GUI during play", "播放期间隐藏界面" },
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
            { "Copy Planet Rotation Speed", "复制星球自转速度" },
            { "Copy Planet Revolution Speed", "复制星球公转速度" },
            { "Mecha", "机甲" },
            { "Offset to Mecha", "偏移" },
            { "Cam Rotation", "镜头旋转" },
            { "Speed", "速度" },
            { "Period", "周期" },
            { "Speed(°/s)", "速度(°/s)" },
            { "Period(s)", "周期(s)" },
            { "Marker Size", "标记大小" },
            { "Set to mecha Position", "設為當前機甲位置" },
            { "Undo", "撤消" },
            { "Local Mecha Coordinates Info", "机甲本地座标信息" },
            { "xyz:\t", "直角坐标:\t" },
            { "polar:\t", "极坐标:\t" },
            { "Universal Coordinates Info", "宇宙坐标信息" },
            { "Mecha:\t", "机甲:\t" },
            { "Local Planet:", "本地星球:\t" },
            { "Local Star:  ", "本地恒星:\t" },

            { "Path List", "路径列表" },
            { "Add Path", "添加路径" },

            { "Timelapse Record", "缩时摄影" },
            { "Start Record", "开始录制" },
            { "[Recording]", "[录制中]" },
            { "[Recording UPS]", "[录制中(UPS)]" },
            { "Sync UPS", "同步逻辑帧率" },
            { "Select", "选取" },
            { "Clear", "清除" },
            { "Time Interval(s)", "时间间隔(秒)" },
            { "Screenshot Width", "画面宽度" },
            { "Screenshot Height", "画面高度" },
            { "JPG Quality", "JPG质量" },
            { "Folder", "保存路径" },

            { "Mod Config", "模组配置" },
            { "Waiting for key..", "等待输入.." },
            { "Camera List Window", "摄像机列表窗口" },
            { "Camera Path Window", "摄像机路径窗口" },
            { "Record Window", "定时截图窗口" },
            { "Toggle Last Cam", "切换到上次机位" },
            { "Cycle To Next Cam", "循环到下一个机位" },
            { "Play Current Path", "播放当前路径" },
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
