# SF模组汉化补丁

海星的个人汉化补丁。支援使用者自定义字典的功能。  
翻译的文本对照可以在mod说明页面的wiki中查看。需要先将游戏语言设置成中文重启才会生效。  
1. [NebulaMultiplayerMod](https://dsp.thunderstore.io/package/nebula/NebulaMultiplayerMod/) 联机mod  
2. [GalacticScale](https://dsp.thunderstore.io/package/Galactic_Scale/GalacticScale/) 银河尺度  
3. [BulletTime](https://dsp.thunderstore.io/package/starfi5h/BulletTime/) 子弹时间  
4. [LSTM](https://dsp.thunderstore.io/package/hetima/LSTM/) 星际物流管理 (配置文字尚未支援)  
5. [DSPStarMapMemo](https://dsp.thunderstore.io/package/appuns/DSPStarMapMemo/) 星球便笺  

## 配置   
配置文件需要先运行过游戏一次才会出现，修改后要重启游戏才会生效。  
管理器安装: 左边选项Config editor -> 找到`starfi5h.plugin.SF_ChinesePatch` -> Edit Config  
手动安装: 更改`BepInEx\config\starfi5h.plugin.SF_ChinesePatch.cfg`文件  

`Enable`-`mod` : 个别mod的汉化开关  
`自定义`-`字典` : 以类似json的形式输入"原文":"翻译"的配对即可新增或覆盖翻译。范例:```"Yes":"是","No":"否"```  

其他使用LDBTool注册字串的mod可以透过`BepInEx\config\LDBTool\LDBTool.CustomLocalization.ZHCN.cfg`修改  

----

## Changelog

v1.1.0 - 修复自定义字典  
v1.0.0 - Initial release. (DSPv0.9.27.15466)  