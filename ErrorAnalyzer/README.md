# Error Analyzer

- Add a button (Copy) to copy and close the error message.
- List functions of mods on the call stack under the StackTrace.  

![demo](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/ErrorAnalyzer/img/demo.png)   

For exmaple, the above image will copy the following text to clipboard.
```ini
An error has occurred! Game version 0.10.28.20779
3 Mods used: [IlLine1.0.0] [ErrorAnalyzer1.0.0] [MinerInfo1.1.0] 
System.TypeLoadException: Could not resolve type with token 0100003d (from typeref, class/assembly StringTranslate, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null)
  at UIVeinDetailNode._OnUpdate () [0x00005] ;IL_0005 
  at ManualBehaviour._Update () [0x0001f] ;IL_001F 
== Mod patches on the stack ==
_OnUpdate(Prefix): static bool MinerInfo.MaxOutputPatch::UIVeinDetailNode_OnUpdate(UIVeinDetailNode __instance)
```
Usually the namespace of the function is the mod name, so we can find that a prefix patch in the mod MinerInfo is causing the error.  
  
* Note: The list patch functions may not directly relate to the cause of the error. Usually the more upper one is more likely the cause, but it may cause by other patches that is not on the stack trace.*  

----

# 错误分析

- 增加一个可以复制并关闭错误讯息的按钮(Copy)  
- 在最下方列出调用栈上的mod补丁函数名称  
  
通常函数的命名空间就是模组(mod)名称，因此可以从它的英文名称得知是那个mod发生错误。  

第一行回报目前的游戏版本。  
第二行回报目前使用的所有mod名称和版本。  
第三行回报调用栈(stack trace), 可以在这里找是否有mod的调用函式, 越上面的导致出错的嫌疑越大。  
== Mod patches on the stack ==下方显示mod的补丁, 如果调用栈没找到mod函式那就可能在这里。  
以例图来说, 可以得知模组MinerInfo对于UIVeinDetailNode._OnUpdate的前缀补丁MinerInfo.MaxOutputPatch::UIVeinDetailNode_OnUpdate导致了例图中的错误  

## ChangeLogs

\- v1.1.0: Display the first exception that trigger during mods loading. (DSP 0.10.29.21904)  
\- v1.0.0: Initial released. (DSP 0.10.28.20779)  