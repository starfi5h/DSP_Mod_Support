# SF模组汉化补丁

![GS2 config](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/SF_ChinesePatch/img/demo1.jpg)  
![LSTM config](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/SF_ChinesePatch/img/demo2.jpg)  
  
MOD汉化补丁。支援使用者自定义字典的功能。  
翻译的文本对照可以在mod说明页面的wiki中查看。需要先将游戏语言设置成中文重启才会生效。  
1. [NebulaMultiplayerMod](https://dsp.thunderstore.io/package/nebula/NebulaMultiplayerMod/) 联机mod  
2. [GalacticScale](https://dsp.thunderstore.io/package/Galactic_Scale/GalacticScale/) 银河尺度  
3. [BulletTime](https://dsp.thunderstore.io/package/starfi5h/BulletTime/) 子弹时间  
4. [DSPStarMapMemo](https://dsp.thunderstore.io/package/appuns/DSPStarMapMemo/) 星球备注  
5. [LSTM](https://dsp.thunderstore.io/package/hetima/LSTM/) 星际物流管理  
6. [PlanetFinder](https://dsp.thunderstore.io/package/hetima/PlanetFinder/) 行星搜索器  

## 配置   
配置文件需要先运行过游戏一次才会出现，修改后要重启游戏才会生效。  
管理器安装: 左边选项Config editor -> 找到`starfi5h.plugin.SF_ChinesePatch` -> Edit Config  
手动安装: 更改`BepInEx\config\starfi5h.plugin.SF_ChinesePatch.cfg`文件  

`Enable`-`mod` : 个别mod的汉化开关  
`自定义`-`字典` : 以类似json的形式输入"原文":"翻译"的配对即可新增或覆盖翻译。范例:```"Yes":"是","No":"否"```  

----

## Changelog

v1.3.4 - 更新联机版本v0.9.15, BulletTime v1.5.5  
v1.3.3 - 更新联机版本v0.9.11, BulletTime v1.5.1  
v1.3.2 - 更新联机版本v0.9.3  
v1.3.1 - 更新GS版本至v2.13.1以及联机版本v0.9.0  
v1.3.0 - 适应黑雾版本 (0.10.28.21014)  
v1.2.0 - 增加PlanetFinder汉化(感谢nga V_itas的汉化版本), LSTM配置汉化  
v1.1.0 - 修复自定义字典  
v1.0.0 - Initial release. (DSPv0.9.27.15466)  