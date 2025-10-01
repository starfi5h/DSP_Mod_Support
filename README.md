# DSP_Mod_Support
Supportive mod for Dyson Sphere Program

## [DSP Nebula compatible mods list](https://docs.google.com/spreadsheets/d/193h6sISVHSN_CX4N4XAm03pQYxNl-UfuN468o5ris1s/)
List of 100+ mods for compatiblity with Nebula multiplayer mod.  
對於100種以上mod的聯機兼容性測試表單.  
✅綠勾=大致ok, 不需要伺服器和客戶端都安裝  
☑️藍勾=大致ok, 建議兩邊皆安裝並使用相同設定    
⚠️黃標=有輕微兼容問題. 通常會發生在客戶端  
⛔紅標=有嚴重兼容問題  
❌紅叉=mod壞了,當前遊戲版本不可用

## [NebulaCompatibilityAssist](https://dsp.thunderstore.io/package/starfi5h/NebulaCompatibilityAssist/)
Provide Nebula multiplayer mod compatibility for other mods. Sometimes contains hotfix for nebula.  
支援一些mod, 使他們可以兼容Nebula聯機模組. 有時會包含聯機模組熱修補丁.

## [ModFixerOne](https://dsp.thunderstore.io/package/starfi5h/ModFixerOne/)
Try to fix broken mods to make them work in latest DSP version.  
嘗試修復一些壞掉的mod, 使其可以在最新的遊戲版本中使用.  


## Setting up a development environment

1. Fork the repository to your own Github account.
2. Pull git repository locally `git clone ...`
3. Create a folder named `assemblies` in the repo folder
4. Put the following reference dll files into `assemblies` folder  
```
Assembly-CSharp-publicized.dll  
UnityEngine.CoreModule.dll  
UnityEngine.dll  
UnityEngine.IMGUIModule.dll  
UnityEngine.InputLegacyModule.dll
UnityEngine.PhysicsModule.dll
UnityEngine.TextRenderingModule.dll
UnityEngine.UI.dll
UnityEngine.UIModule.dll
```

You can find the Unity modules in game install folder `Dyson Sphere Program\DSPGAME_Data\Managed`  
`Assembly-CSharp-publicized.dll` can be obtained by using [AssemblyPublicizer](https://github.com/BepInEx/BepInEx.AssemblyPublicizer) on game's `Assembly-CSharp.dll` in the same folder.   