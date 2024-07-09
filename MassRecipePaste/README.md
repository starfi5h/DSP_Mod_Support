# MassRecipePaste

![demo](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/MassRecipePaste/img/demo1.gif)

`Ctrl + >` to drag and paste copied settings over multiple machines of the same type in the selected area.  
The building that can be pasted will high light in blue. After clicking the start and end point, the available buildings in the area will all be pasted with the copied settings.  
When pasting with this tool, priority settings of ILS will be copied too.  

`Ctrl + >`将复制的设置拖动并粘贴到所选区域内的多台同类型机器上。  
可粘贴的建筑物将以蓝色高亮显示。单击起点和终点后，该区域内可用的建筑物将全部使用复制的设置进行粘贴。  
使用此工具粘贴时，星际物流站的优先级设置也将被复制。  

----

## Installation 安装

Via [r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/), or manually download the file and put `MassRecipePaste.dll` in `BepInEx/plugins` folder.  
通过管理器[r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/)，或者手动下载文件并将`SphereEditorTools.dll`放入`BepInEx/plugins`文件夹。  
## Configuration 配置文件

Run the game one time to generate `BepInEx\config\starfi5h.plugin.MassRecipePaste.cfg` file.  
If you're using mod manager, you can find the file in its Config editor.  
The changes will take effects after reboost, or go to game settings and click 'Apply' button.  

管理器安装: 左边选项Config editor -> 找到`starfi5h.plugin.MassRecipePaste` -> Edit Config  
手动安装: 更改`BepInEx\config\starfi5h.plugin.MassRecipePaste.cfg`文件  
配置文件(.cfg)需要先运行过游戏一次才会出现。  
在修改完配置文件后重启游戏, 或进入游戏设置, 点击'应用设置'即可立即套用新的数值设定。  


```
## Settings file was created by plugin MassRecipePaste v1.0.0
## Plugin GUID: starfi5h.plugin.MassRecipePaste

[ExtraCopy]

## 复制物流站名称
# Setting type: Boolean
# Default value: false
CopyStationName = false

## 复制物流站优先行为
# Setting type: Boolean
# Default value: true
CopyStationPriorityBehavior = true

## 复制物流站分组设置
# Setting type: Boolean
# Default value: true
CopyStationGroup = true

## 复制物流站点对点设置
# Setting type: Boolean
# Default value: true
CopyStationP2P = true

[KeyBinds]

## Custom keybind. Default is ctrl + >(paste recipe)
## 没有设置时, 默认为Ctrl + >(配方黏贴键)
# Setting type: KeyboardShortcut
# Default value: 
MassPasteKey = 
```

----

## Acknowledgements

Mod idea inspire by [RecipePasteBrush](https://thunderstore.io/c/dyson-sphere-program/p/wingless/RecipePasteBrush/)  
Area selection code reference [BlueprintTweaks](https://thunderstore.io/c/dyson-sphere-program/p/kremnev8/BlueprintTweaks/)'s [DragRemove](https://github.com/limoka/DSP-Mods/tree/master/Mods/BlueprintTweaks/src/BlueprintTweaks/DragRemove) tool  
Thanks to the mod authors [kremnev8](https://thunderstore.io/c/dyson-sphere-program/p/kremnev8/) and [wingless
](https://thunderstore.io/c/dyson-sphere-program/p/wingless/)  

All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.  