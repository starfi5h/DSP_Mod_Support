using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SF_ChinesePatch
{
    // 部分翻譯取自貼吧GalacticScale2 自定义星系生成mod介绍 作者:轻型工业机甲
    // https://tieba.baidu.com/p/8297269758?pid=147231461365&cid=147274042722

    public class GalacticScale_Patch
    {
        public const string NAME = "GalacticScale";
        public const string GUID = "dsp.galactic-scale.2";
        private static Dictionary<string, string> typeStrings;

        public static void OnAwake(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(GUID)) return;
            if (!Plugin.Instance.Config.Bind("Enable", NAME, true).Value) return;

            typeStrings = new();
            RegisterStrings();
            harmony.PatchAll(typeof(GalacticScale_Patch));
            harmony.Patch(AccessTools.Method(AccessTools.TypeByName("GalacticScale.SystemDisplay"), "InitHelpText"), null, new HarmonyMethod(typeof(GalacticScale_Patch).GetMethod("InitHelpText_Postfix")));
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlanetData), "typeString", MethodType.Getter)]
        public static void typeString(ref string __result)
        {
            if (__result != null && typeStrings.ContainsKey(__result))
            {
                __result = typeStrings[__result];
            }
        }

        public static void InitHelpText_Postfix()
        {
            var leftGroup = GameObject.Find("UI Root/Overlay Canvas/Galaxy Select/left-group/");
            if (leftGroup == null) return;
            var helpText = leftGroup.GetComponentInChildren<Text>();
            if (helpText == null) return;
            helpText.text = "点击星球查看详细信息\r\n使用鼠标滚轮进行缩放\r\n使用方向键进行平移\r\n按住Shift键可加快缩放/平移速度\r\n按住Alt键可查看所有星球名称\r\n按下空格键重置视图\r\n右击星球设置生成点";
        }

        public static void RegisterPlanetType(string key, string cnTrans)
        {
            if (typeStrings.ContainsKey(key)) return;
            typeStrings.Add(key, cnTrans);
        }

        private static void RegisterStrings()
        {
            #region UI
            StringManager.RegisterString("done", "完成");
            StringManager.RegisterString("Loading Planets: ", "正在加载星球：");
            StringManager.RegisterString("a size", "半径");
            StringManager.RegisterString("Moon ", "卫星 ");
            StringManager.RegisterString("Planet ", "行星 ");
            StringManager.RegisterString("Calculating planet {0}/{1}...\r\n", "计算星球 {0}/{1}...\r\n");

            StringManager.RegisterString("How large the planet is. Standard is 200", "星球有多大。标准大小为 200");
            StringManager.RegisterString("Planet Radius", "星球半径");
            StringManager.RegisterString("Planetary Radius", "星球半径");

            StringManager.RegisterString("WARNING - Galactic Scale savegames can be broken by updates. Read the FAQ @ http://customizing.space", "警告 - Galactic Scale 存档可能会因更新而损坏。请阅读常见问题解答 @ http://customizing.space");
            StringManager.RegisterString("WARNING - Galactic Scale savegames can be broken by updates.Read the FAQ @ http://customizing.space\r\n", "警告 - Galactic Scale 存档可能会因更新而损坏。请阅读常见问题解答 @ http://customizing.space\r\n");
            StringManager.RegisterString("BACKUP YOUR SAVES. This version has potentially BREAKING CHANGES.\r\nNote: Settings for this mod are in the settings menu. Make sure to change the Generator to get the full Galactic Scale experience.\r\n", "备份您的存档。此版本可能存在破坏性变更。\r\n注意：此 mod 的设置位于设置菜单中。请确保更改生成器以获得完整的 Galactic Scale 体验。\r\n");
            StringManager.RegisterString("Update Detected. Please do not save over existing saves \r\nuntil you are sure you can load saves saved with this version!\r\nPlease Click GS2 Help and click the link to join our community on discord for preview builds and to help shape the mod going forward", "检测到更新。请不要在现有存档上保存，\r\n直到您确信可以加载使用此版本保存的存档！\r\n请单击 GS2 帮助并单击链接加入我们的 Discord 社区以获取预览版本并帮助塑造 mod 的未来");
            StringManager.RegisterString("The latest DSP update has added additional planet themes which are yet to be included in GS2. \r\nI'm working on getting them added to the GS2 themeset, as well as implementing their new subtheme system", "最新的 DSP 更新添加了其他行星主题，但尚未包含在 GS2 中。\r\n");
            #endregion

            #region GS2 Theme GS2行星種類
            RegisterPlanetType("Acid Greenhouse", "酸性温室");
            RegisterPlanetType("Obsidian", "黑曜石");
            RegisterPlanetType("Hot Obsidian", "炙热黑曜石");
            RegisterPlanetType("Ice Malusol", "冰岛");
            RegisterPlanetType("Infernal Gas Giant", "地狱气巨星");
            RegisterPlanetType("Dwarf Planet", "矮行星");
            RegisterPlanetType("Barren Satellite", "贫瘠卫星");
            RegisterPlanetType("Sulfurous Sea", "硫酸海");
            RegisterPlanetType("Gigantic Forest", "巨大森林");
            RegisterPlanetType("Red Forest", "红色森林");
            RegisterPlanetType("Molten World", "岩浆世界");
            RegisterPlanetType("Beach", "海滩");
            RegisterPlanetType("SpaceWhale Excrement", "太空鲸粪");
            #endregion

            #region Original Theme 原有行星種類
            RegisterPlanetType("Mediterranean", "地中海");
            RegisterPlanetType("Oceanic Jungle", "海洋丛林");
            RegisterPlanetType("Sakura Ocean", "樱林海");
            RegisterPlanetType("Ocean World", "水世界");
            RegisterPlanetType("Pandora Swamp", "潘多拉沼泽");
            RegisterPlanetType("Pandora Swamp II", "潘多拉沼泽 II");
            RegisterPlanetType("Red Stone", "红石");
            RegisterPlanetType("Prairie", "草原");
            RegisterPlanetType("Savanna", "热带草原");

            RegisterPlanetType("Lava", "熔岩");
            RegisterPlanetType("Volcanic Ash", "火山灰");

            RegisterPlanetType("Gobi Desert", "戈壁");
            RegisterPlanetType("Barren Desert", "贫瘠荒漠");
            RegisterPlanetType("AridDesert", "干旱沙漠");
            RegisterPlanetType("Crystal Desert", "橙晶荒漠");
            RegisterPlanetType("Rocky Salt Lake", "黑石盐滩");
            RegisterPlanetType("Hurricane Stone Forest", "飓风石林");

            RegisterPlanetType("Scarlet Ice Lake", "猩红冰湖");
            RegisterPlanetType("Ashen Gelisol", "灰烬冻土");
            RegisterPlanetType("Ice Field Gelisol", "冰原冻土");
            RegisterPlanetType("Ice Field Gelisol II", "冰原冻土 II");
            RegisterPlanetType("Ice Field Gelisol III", "冰原冻土 III");
            RegisterPlanetType("Frozen Tundra", "极寒冻土");

            RegisterPlanetType("Gas Giant", "气态巨星");
            RegisterPlanetType("Gas Giant II", "气态巨星 II");
            RegisterPlanetType("Gas Giant III", "气态巨星 III");
            RegisterPlanetType("Gas Giant IV", "气态巨星 IV");
            RegisterPlanetType("Gas Giant V", "气态巨星 V");

            RegisterPlanetType("Ice Giant", "冰巨星");
            RegisterPlanetType("Ice Giant II", "冰巨星 II");
            RegisterPlanetType("Ice Giant III", "冰巨星 III");
            RegisterPlanetType("Ice Giant IV", "冰巨星 IV");
            #endregion

            #region Left-side options
            StringManager.RegisterString("Generator", "星系生成方式");
            StringManager.RegisterString("Try them all!", "选择自定义星系的生成器(Generator)");
            StringManager.RegisterString("You cannot change the generator while in game.", "您不能在游戏中更改生成器。");

            StringManager.RegisterString("Quality of Life", "生活质量");
            StringManager.RegisterString("Skip Prologue", "跳过序章");
            StringManager.RegisterString("Skip Tutorials", "跳过教程");
            StringManager.RegisterString("Show/Hide Vein Labels", "显示/隐藏矿脉标签");
            StringManager.RegisterString("When show vein markers is enabled", "(当显示矿脉标记启用时)");
            StringManager.RegisterString("Show Iron Vein Labels", "显示铁矿标签");
            StringManager.RegisterString("Show Copper Vein Labels", "显示铜矿标签");
            StringManager.RegisterString("Show Silicon Vein Labels", "显示硅矿标签");
            StringManager.RegisterString("Show Titanium Vein Labels", "显示钛矿标签");
            StringManager.RegisterString("Show Stone Vein Labels", "显示石矿标签");
            StringManager.RegisterString("Show Coal Vein Labels", "显示煤矿标签");
            StringManager.RegisterString("Show Oil Vein Labels", "显示石油标签");
            StringManager.RegisterString("Show Fire Ice Vein Labels", "显示可燃冰标签");
            StringManager.RegisterString("Show Kimberlite Labels", "显示金伯利石标签");
            StringManager.RegisterString("Show Fractal Silicon Vein Labels", "显示分形硅标签");
            StringManager.RegisterString("Show Organic Crystal Vein Labels", "显示有机水晶标签");
            StringManager.RegisterString("Show Optical Grating Vein Labels", "显示光栅石标签");
            StringManager.RegisterString("Show Spiriform Vein Labels", "显示刺笋结晶标签");
            StringManager.RegisterString("Show Unipolar Vein Labels", "显示单极磁石标签");

            StringManager.RegisterString("Debug Options", "调试选项");
            StringManager.RegisterString("Useful for testing galaxies/themes", "用于测试星系/主题的实用功能");
            StringManager.RegisterString("Debug Log", "调试日志");
            StringManager.RegisterString("Print extra logs to BepInEx console", "将额外日志打印到BepInEx控制台");
            StringManager.RegisterString("Force Rare Spawn", "强制稀有矿物生成");
            StringManager.RegisterString("Ignore randomness/distance checks", "忽略随机性/距离检查");
            StringManager.RegisterString("Enable Teleport", "启用传送");
            StringManager.RegisterString("TP by ctrl-click nav arrow in star map", "在星图中通过Ctrl+单击导航箭头进行传送");
            StringManager.RegisterString("Mecha Scale", "机甲比例");
            StringManager.RegisterString("How big Icarus should be. 1 = default", "伊卡洛斯机甲的大小。1 = 默认");
            StringManager.RegisterString("Ship Speed Multiplier", "飞船曲速倍率");
            StringManager.RegisterString("Multiplier for Warp Speed of Ships", "运输船曲速速度倍率");
            StringManager.RegisterString("GalaxySelect Planet ScaleFactor", "初始界面星球显示倍率");
            StringManager.RegisterString("How big planets should be in the new game system view", "新游戏星系选择界面中行星的大小");
            StringManager.RegisterString("GalaxySelect Star ScaleFactor", "初始界面恒星显示倍率");
            StringManager.RegisterString("How big star should be in the new game system view", "新游戏星系选择界面中恒星的大小");
            StringManager.RegisterString("GalaxySelect Orbit ScaleFactor", "初始界面轨道显示倍率");
            StringManager.RegisterString("How spaced orbits should be in the new game system view", "新游戏星系选择界面中轨道的间距");
            StringManager.RegisterString("GalaxySelect Click Tolerance", "初始界面点击容差");
            StringManager.RegisterString("How close to a star/planet your mouse needs to be to register a click", "鼠标距离星星/行星多近才能注册点击");
            StringManager.RegisterString("Set ResourceMulti Infinite", "使当前存档的资源无限");
            StringManager.RegisterString("Will need to be saved and loaded to apply", "需要保存和加载才能应用");
            StringManager.RegisterString("Revert Scarlet Ice Lake", "重生成猩红冰湖矿物");
            StringManager.RegisterString("2.2.0.23 Had a bug where Ice Lake had no terrain. Enable this to fix issues with saves started prior to .24", "2.2.0.23版本存在冰湖没有地形的错误。启用此选项以解决在24之前开始的存档问题");
            StringManager.RegisterString("Reset Logistic Bot Speed", "重置物流机器人速度");

            StringManager.RegisterString("Custom Galaxy Export/Import", "自定义星系导出/导入");
            StringManager.RegisterString("Export available once in game", "游戏中仅可导出一次");
            StringManager.RegisterString("Export Filename", "导出文件名");
            StringManager.RegisterString("Excluding .json", "不包括 .json");
            StringManager.RegisterString("Minify Exported JSON", "压缩导出的 JSON");
            StringManager.RegisterString("Only save changes", "仅保存更改");
            StringManager.RegisterString("Export Custom Galaxy", "导出自定义星系");
            StringManager.RegisterString("Save Galaxy to File", "保存星系到文件");
            StringManager.RegisterString("Custom Galaxy", "自定义星系");
            StringManager.RegisterString("Load Custom Galaxy", "导入自定义星系");
            StringManager.RegisterString("Will end current game", "将结束当前游戏");

            StringManager.RegisterString("External Themes", "外部主题");
            StringManager.RegisterString("Export All Themes", "导出所有主题");
            StringManager.RegisterString("Success", "成功");
            StringManager.RegisterString("Themes have been exported to ", "主题已导出至");
            StringManager.RegisterString("Galaxy Saved to ", "星系保存至");
            StringManager.RegisterString("Error", "错误");
            StringManager.RegisterString("Please try again after creating a galaxy :)\r\nStart a game, then press ESC and click settings.", "请在创建星系后再试一次：）\r\n开始游戏，然后按 ESC 键并点击设置。");
            StringManager.RegisterString("To use the Custom JSON Generator you must select a file to load.", "要使用自定义 JSON 生成器，必须选择要加载的文件。");
            #endregion

            #region Sol Generateor settings 
            StringManager.RegisterString("Accurate Stars", "精准星系（按现实生成）");
            StringManager.RegisterString("Start in Sol", "从太阳系开始");
            StringManager.RegisterString("Max planets per system", "星系的最大行星数量");
            StringManager.RegisterString("Sol System Day/Night Multi", "日夜周期倍率");
            StringManager.RegisterString("Change the Sol system planets day/night cycle", "改变太阳系的日夜周期");            
            StringManager.RegisterString("Min planet size", "最小行星大小");
            StringManager.RegisterString("Max planet size", "最大行星大小");
            StringManager.RegisterString("Starting planet size", "起始星球大小");
            StringManager.RegisterString("Birth planet Si/Ti", "起始星球解锁硅和钛");
            StringManager.RegisterString("Moons are small", "卫星较小");
            #endregion

            #region GS2 Generator - Overview
            StringManager.RegisterString("Galaxy Settings", "星系设置");
            StringManager.RegisterString("Settings that control Galaxy formation", "控制星系形成的设置");
            StringManager.RegisterString("Birth Planet Settings", "起始星球设置");
            StringManager.RegisterString("Settings that only affect the starting planet", "仅影响出生星球的设置");
            StringManager.RegisterString("System Settings", "行星系设置");
            StringManager.RegisterString("Settings that control how systems are generated", "控制行星系生成的设置");
            StringManager.RegisterString("Planet Settings", "行星设置");
            StringManager.RegisterString("Settings that control how planets are generated", "控制行星生成的设置");
            StringManager.RegisterString("Star Specific Overrides", "特定星体覆盖设置");
            StringManager.RegisterString("Settings that are star specific", "对特定种类的恒星进行设置覆盖");
            StringManager.RegisterString("Reset", "重设");
            #endregion

            #region GS2 Generator - Galaxy Settings
            StringManager.RegisterString("Galaxy Spread", "星系扩散程度");
            StringManager.RegisterString("Lower = Stars are closer to each other. Default is 5", "较低值 = 星体更接近。默认值为5");
            StringManager.RegisterString("Default StarCount", "默认恒星数量");
            StringManager.RegisterString("How many stars should the slider default to", "新游戏滑动条默认恒星数量");
            StringManager.RegisterString("Star Size Multiplier", "恒星大小倍率");
            StringManager.RegisterString("GS2 uses 10x as standard. They just look cooler.", "GS2设为标准10倍。看起来更酷。");
            StringManager.RegisterString("Luminosity Multiplier", "亮度倍率");
            StringManager.RegisterString("Increase the luminosity of all stars by this multiplier", "将所有恒星的亮度倍增此倍率");
            StringManager.RegisterString("Use Vanilla Star Names", "使用原版星体名称");
            StringManager.RegisterString("Use DSP's Name Generator", "使用DSP的名称生成器");

            StringManager.RegisterString("Binary Star Settings", "双星设置");
            StringManager.RegisterString("Settings that control Binary Star formation", "控制双星形成的设置");
            StringManager.RegisterString("Binary Distance Multi", "双星距离倍率");
            StringManager.RegisterString("How close secondary stars should be to primaries", "伴星距离主星的接近程度");
            StringManager.RegisterString("Binary Star Chance %", "双星几率 %");
            StringManager.RegisterString("% Chance of a star having a binary companion", "星体成为双星的几率 %");

            StringManager.RegisterString("Star Relative Frequencies", "星体生成概率");
            StringManager.RegisterString("How often to select a star type", "生成器选择星体类型的概率");            
            StringManager.RegisterString("Freq. BlackHole", "黑洞-出现频率");
            StringManager.RegisterString("Minimum BlackHole", "黑洞-最低数量");
            StringManager.RegisterString("Freq. Neutron", "中子星-出现频率");
            StringManager.RegisterString("Minimum Neutron", "中子星-最低数量");
            StringManager.RegisterString("Freq. WhiteDwarf", "白矮星-出现频率");
            StringManager.RegisterString("Minimum WhiteDwarf", "白矮星-最低数量");

            #endregion

            #region GS2 Generator - Birth Planet Settings
            StringManager.RegisterString("Starting Planet Size", "起始星球大小");
            StringManager.RegisterString("How big the starting planet is. Default is 200", "起始星球的大小。默认值为200");
            StringManager.RegisterString("Starting Planet Unlock", "解锁其他主题的起始星球");
            StringManager.RegisterString("Allow other habitable themes for starting planet", "允许起始星球使用其他宜居主题");
            StringManager.RegisterString("Starting planet Si/Ti", "起始星球解锁硅和钛");
            StringManager.RegisterString("Force Silicon and Titanium on the starting planet", "在起始星球上强制生成硅和钛");
            StringManager.RegisterString("Allow Rares in Starting System", "起始系统中允许稀有资源");
            StringManager.RegisterString("Allow Rares other than Oil and FireIce", "允许起始系统中除了原油和可燃冰之外的稀有资源");
            StringManager.RegisterString("Starting Planet Star", "起始恒星类型");
            StringManager.RegisterString("Type of Star to Start at", "起始系统中星体的类型");
            StringManager.RegisterString("Tidal Lock Starting Planet", "强制起始星球潮汐锁定");
            StringManager.RegisterString("Force the starting planet to be tidally locked", "将起始星球锁定为潮汐锁定");
            StringManager.RegisterString("Birth Planet is a Moon", "起始行星是一个卫星");
            StringManager.RegisterString("... of a Gas Giant", "起始是气态巨星的卫星");
            StringManager.RegisterString("Starting Planet No Rares", "禁止起始星球上的稀有资源");
            StringManager.RegisterString("Disable to allow rare veins on starting planet", "禁用以允许起始星球上有稀有矿脉");
            #endregion

            #region GS2 Generator - System Settings
            StringManager.RegisterString("Solar Power Falloff", "太阳能衰减计算方法");
            StringManager.RegisterString("InverseSquare", "反比平方");
            StringManager.RegisterString("Linear", "线性");
            StringManager.RegisterString("Linear Damping", "线性阻尼"); //NA
            StringManager.RegisterString("How close to 100% the inner and outer planets will be", "内外行星距离100%的接近程度"); //NA
            StringManager.RegisterString("None", "无");

            StringManager.RegisterString("Min/Max Solar", "最小/最大太阳能效率");
            StringManager.RegisterString("Orbit Spacing", "轨道间距");
            StringManager.RegisterString("Minimum gap between planet orbits", "行星轨道间的最小间隔");
            StringManager.RegisterString("Planet Naming Scheme", "行星命名方案"); //NA
            StringManager.RegisterString("How to determine planet names", "行星命名的规则"); //NA
            StringManager.RegisterString("Default", "原版");
            StringManager.RegisterString("Alpha", "Alpha");
            StringManager.RegisterString("Random", "随机");
            StringManager.RegisterString("Tidal Lock Inner Planets", "潮汐锁定内行星");
            StringManager.RegisterString("Force planets below the orbit threshold to be tidally locked", "强制轨道低于阈值的行星潮汐锁定");
            StringManager.RegisterString("Inner Planet Distance (AU)", "内行星距离（AU）");
            StringManager.RegisterString("Distance forced tidal locking stops acting", "强制潮汐锁定停止作用的距离");
            StringManager.RegisterString("Allow Orbital Harmonics", "允许轨道共振");
            StringManager.RegisterString("Allow Orbital Resonance 1:2 and 1:4", "允许轨道共振 1:2 和 1:4");
            StringManager.RegisterString("Moons Are Small", "卫星较小");
            StringManager.RegisterString("Try to ensure moons are 1/2 their planets size or less", "使卫星的大小为其行星的1/2或更小");
            StringManager.RegisterString("Gas Giants Moon Bias", "气态巨行星卫星偏好");
            StringManager.RegisterString("Lower prefers telluric planets, higher gas giants", "较低的偏好岩石行星，较高的偏好气态巨行星");
            StringManager.RegisterString("Secondary satellites", "二级卫星");
            StringManager.RegisterString("Allow moons to have moons", "允许卫星拥有卫星");
            StringManager.RegisterString("Secondary Satellite Chance", "二级卫星概率");
            StringManager.RegisterString("% Chance for a moon to have a moon", "卫星有卫星的几率%");
            StringManager.RegisterString("Recursive Moons", "递归卫星");
            StringManager.RegisterString("Moons of moons can have moons...", "卫星的卫星可以有卫星...");

            StringManager.RegisterString("Default Settings", "默认设置");
            StringManager.RegisterString("Changing these will reset all star specific overrides", "更改这些设置将重置所有特定星体的覆盖设置");
            StringManager.RegisterString("Planet Count", "行星数量");
            StringManager.RegisterString("The amount of planets per star", "每颗恒星的行星数量");
            StringManager.RegisterString("Planet Count Bias", "行星数量偏好");
            StringManager.RegisterString("Prefer Less (lower) or More (higher) Planets", "喜欢较少或较多的行星");
            StringManager.RegisterString("Huge Gas Giants", "巨大气态巨行星");
            StringManager.RegisterString("Allow gas giants larger than 800 radius", "允许半径大于800的气态巨行星");
            StringManager.RegisterString("Chance Gas", "气态行星概率");
            StringManager.RegisterString("% Chance of a planet being a Gas Giant", "行星是气态巨行星的概率%");
            StringManager.RegisterString("Chance Moon", "卫星概率");
            StringManager.RegisterString("% Chance of a rocky planet being a moon", "岩石行星是卫星的几率%");
            StringManager.RegisterString("Enable Comets", "启用小行星"); //NA
            StringManager.RegisterString("Star has a small planetoid with a random rare resource", "恒星附近有一颗带有随机稀有资源的小行星");
            StringManager.RegisterString("Comet Chance", "小行星概率");
            StringManager.RegisterString("% Chance of a star spawning a comet", "恒星生成小行星的几率");
            StringManager.RegisterString("Override Habitable Zone", "覆盖适居带");
            StringManager.RegisterString("Enable the slider below", "启用下方的滑块");
            StringManager.RegisterString("Habitable Zone", "适居带");
            StringManager.RegisterString("Force habitable zone between these distances", "在这些距离间强制适居带");
            StringManager.RegisterString("Override Orbits", "覆盖轨道");
            StringManager.RegisterString("Orbit Range", "轨道范围");
            StringManager.RegisterString("Force the distances planets can spawn between", "强制行星生成的距离范围");
            StringManager.RegisterString("Prefer Inner Planets", "偏好内行星");
            #endregion

            #region GS2 Generator - Planet Settings
            StringManager.RegisterString("Day/Night Multiplier", "昼夜时间倍率");
            StringManager.RegisterString("Increase the duration of night/day", "延长夜晚/白昼的持续时间");
            StringManager.RegisterString("Telluric Planet Size", "岩石行星大小");
            StringManager.RegisterString("Min/Max Size of Rocky Planets", "岩石行星的最小/最大大小");
            StringManager.RegisterString("Limit Planet Sizes", "限制行星大小");
            StringManager.RegisterString("Force Planets to these sizes", "将行星强制设定为这些大小");
            StringManager.RegisterString("Gas Planet Size", "气态行星大小");
            StringManager.RegisterString("Min/Max Size of Gas Planets", "气态行星的最小/最大大小");
            StringManager.RegisterString("Limit Gas Giant Sizes", "限制气态巨行星大小");
            StringManager.RegisterString("Force Gas Giants to these sizes", "将气态巨行星强制设定为这些大小");
            StringManager.RegisterString("Planet Size Bias", "行星大小偏好");
            StringManager.RegisterString("Prefer Smaller (lower) or Larger (higher) Sizes", "更喜欢较小或较大的大小");
            StringManager.RegisterString("Max Inclination", "最大轨道倾角");
            StringManager.RegisterString("Maximum angle of orbit", "轨道的最大倾角");
            StringManager.RegisterString("Random", "随机");
            StringManager.RegisterString("Max Orbit Longitude", "最大轨道经度");
            StringManager.RegisterString("Maximum longitude of the ascending node", "升交点的最大经度");
            StringManager.RegisterString("Rare Vein Chance % Override", "稀有矿脉概率覆盖");
            StringManager.RegisterString("Override the chance of planets spawning rare veins", "覆盖行星生成稀有矿脉的概率%");
            StringManager.RegisterString("Default", "默认");
            #endregion

            #region GS2 Generator - Star Specific Overrides
            StringManager.RegisterString("Will be selected randomly from this range", "将在此范围内随机选择");
            StringManager.RegisterString("Count Bias", "计数偏好");
            StringManager.RegisterString("Prefer Less (lower) or More (higher) Counts", "偏好更高或更低的数量");
            StringManager.RegisterString("Gas Giant Planet Size", "气态巨行星大小");
            StringManager.RegisterString("Telluric Size Bias", "类地行星大小偏差");
            StringManager.RegisterString("Chance for Gas giant", "气态巨星的概率");
            StringManager.RegisterString("Chance for Moon", "卫星的概率");
            StringManager.RegisterString("Prefer Inner Planets", "更偏好内行星");
            StringManager.RegisterString("Luminosity Boost", "亮度提升");
            StringManager.RegisterString("Increase the luminosity of this star type by this amount", "将此星型的亮度增加此数量");

            StringManager.RegisterString("Black Hole Overrides", "黑洞覆盖设置");
            StringManager.RegisterString("Neutron Star Overrides", "中子星覆盖设置");
            StringManager.RegisterString("White Dwarf Overrides", "白矮星覆盖设置");
            #endregion
        }
    }
}
