using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Mono.Cecil;

namespace ModFixerOne
{
    public static class Preloader
    {
        public static ManualLogSource logSource = Logger.CreateLogSource("Mod Fixer One Preloader");
      
        public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };
        public static void Patch(AssemblyDefinition assembly)
        {
            try
            {                
                var gameModule = assembly.MainModule;

                // Add UIStorageGrid UIGame.inventory
                var uigame = gameModule.Types.First(t => t.FullName == "UIGame");
                var typeReference = gameModule.GetType("UIStorageGrid");
                uigame.Fields.Add(new FieldDefinition("inventory", FieldAttributes.Public, typeReference));

#if DEBUG
                var inventory = uigame.Fields.First(f => f.Name == "inventory");
                logSource.LogDebug("Add " + inventory);
#endif
            }
            catch (Exception err)
            {
                logSource.LogError("Mod Fixer One Preloader error!");
                logSource.LogError(err);
            }
        }
    }
}