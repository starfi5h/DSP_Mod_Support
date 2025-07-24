using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ModFixerOne
{
    public static class Injection
    {
        public static void AddFied(this TypeDefinition typeDefinition, string fieldName, TypeReference fieldType)
        {
            var newField = new FieldDefinition(fieldName, FieldAttributes.Public, fieldType);
            typeDefinition.Fields.Add(newField);
        }

        public static MethodDefinition AddMethod(this TypeDefinition typeDefinition, string methodName, TypeReference returnType, TypeReference[] parmeterTypes)
        {
            var newMethod = new MethodDefinition(methodName, MethodAttributes.Public, returnType);
            foreach (var p in parmeterTypes)
                newMethod.Parameters.Add(new ParameterDefinition(p));
            typeDefinition.Methods.Add(newMethod);
            return newMethod;
        }

        internal static void RefreshTraffic(AssemblyDefinition assembly)
        {
            // Add method: void PlanetTransport.RefreshTraffic(int) to call PlanetTransport.RefreshStationTraffic(int)
            var newMethod = assembly.MainModule.GetType("PlanetTransport").AddMethod("RefreshTraffic", assembly.MainModule.TypeSystem.Void, new TypeReference[] { assembly.MainModule.TypeSystem.Int32 });
            ILProcessor ilProcessor = newMethod.Body.GetILProcessor();
            ilProcessor.Emit(OpCodes.Ldarg_0);
            ilProcessor.Emit(OpCodes.Ldarg_1);

            TypeDefinition localizationType = assembly.MainModule.Types.First(t => t.FullName == "PlanetTransport");
            MethodDefinition translateMethod = localizationType.Methods.First(m => m.Name == "RefreshStationTraffic");
            ilProcessor.Emit(OpCodes.Call, translateMethod);
            ilProcessor.Emit(OpCodes.Ret);
        }

        internal static TypeDefinition Language(AssemblyDefinition assembly)
        {
            // Create the enum https://stackoverflow.com/questions/59324267/how-to-modify-enum-type-by-mono-cecil
            TypeDefinition enumType = new TypeDefinition(
                "",
                "Language",
                TypeAttributes.Public | TypeAttributes.Sealed,
                assembly.MainModule.ImportReference(typeof(Enum))
            );

            // Add enum values
            var fieldAttribtues = FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.Public | FieldAttributes.HasDefault;
            enumType.Fields.Add(new FieldDefinition("value__", FieldAttributes.SpecialName | FieldAttributes.RTSpecialName | FieldAttributes.Public, assembly.MainModule.TypeSystem.Int32));
            enumType.Fields.Add(new FieldDefinition("zhCN", fieldAttribtues, enumType) { Constant = 0 });
            enumType.Fields.Add(new FieldDefinition("enUS", fieldAttribtues, enumType) { Constant = 1 });
            enumType.Fields.Add(new FieldDefinition("frFR", fieldAttribtues, enumType) { Constant = 2 });
            enumType.Fields.Add(new FieldDefinition("Max", fieldAttribtues, enumType) { Constant = 3 });
            assembly.MainModule.Types.Add(enumType);

            // Add public static Language Localization.get_language() that return Localization.lang
            TypeDefinition localizationType = assembly.MainModule.Types.First(t => t.FullName == "Localization");
            var newField = new FieldDefinition("lang", FieldAttributes.Public | FieldAttributes.Static, enumType);
            localizationType.Fields.Add(newField);
            var newMethod = new MethodDefinition("get_language", MethodAttributes.Public | MethodAttributes.Static, enumType);
            ILProcessor ilProcessor = newMethod.Body.GetILProcessor();
            ilProcessor.Emit(OpCodes.Ldsfld, newField);
            ilProcessor.Emit(OpCodes.Ret);
            localizationType.Methods.Add(newMethod);
            return enumType;
        }

        internal static void StringTranslate(AssemblyDefinition assembly, TypeDefinition enumType)
        {
            TypeDefinition newClass = new TypeDefinition(
                "",
                "StringTranslate",
                TypeAttributes.Class | TypeAttributes.Public,
                assembly.MainModule.TypeSystem.Object);
            assembly.MainModule.Types.Add(newClass);

            // Add method: public static string Translate(this string s)
            MethodDefinition newMethod = new MethodDefinition(
                "Translate",                               // Method name
                MethodAttributes.Public | MethodAttributes.Static,
                assembly.MainModule.TypeSystem.String);    // Return type (string in this case)

            // Add the "this" modifier for extension method
            newMethod.Parameters.Add(new ParameterDefinition("s", ParameterAttributes.None, assembly.MainModule.TypeSystem.String));
            newClass.Methods.Add(newMethod);

            // Define the body of the method and call Localization.Translate(s) as return value
            ILProcessor ilProcessor = newMethod.Body.GetILProcessor();
            ilProcessor.Emit(OpCodes.Ldarg_0);
            TypeDefinition localizationType = assembly.MainModule.Types.First(t => t.FullName == "Localization");
            MethodDefinition translateMethod = localizationType.Methods.First(m => m.Name == "Translate");
            ilProcessor.Emit(OpCodes.Call, translateMethod);
            ilProcessor.Emit(OpCodes.Ret);

            // Add method: public static string Translate(this string s, Language _)
            newMethod = new MethodDefinition(
                "Translate",                               // Method name
                MethodAttributes.Public | MethodAttributes.Static,
                assembly.MainModule.TypeSystem.String);    // Return type (string in this case)

            // Add the "this" modifier for extension method
            newMethod.Parameters.Add(new ParameterDefinition("s", ParameterAttributes.None, assembly.MainModule.TypeSystem.String));
            newMethod.Parameters.Add(new ParameterDefinition("_", ParameterAttributes.None, enumType));
            newClass.Methods.Add(newMethod);

            // Define the body of the method and call Localization.Translate(s) as return value
            ilProcessor = newMethod.Body.GetILProcessor();
            ilProcessor.Emit(OpCodes.Ldarg_0);
            localizationType = assembly.MainModule.Types.First(t => t.FullName == "Localization");
            translateMethod = localizationType.Methods.First(m => m.Name == "Translate");
            ilProcessor.Emit(OpCodes.Call, translateMethod);
            ilProcessor.Emit(OpCodes.Ret);
        }

        internal static void StringProto(AssemblyDefinition assembly)
        {
            TypeDefinition protoClass = assembly.MainModule.Types.First(t => t.FullName == "Proto");
            TypeDefinition stringProtoClass = new TypeDefinition(
                "",
                "StringProto",
                TypeAttributes.Class | TypeAttributes.Public,
                assembly.MainModule.TypeSystem.Object)
            {
                BaseType = protoClass
            };
            // Add [Serializable] attribute to StringProto
            CustomAttribute serializableAttribute = new CustomAttribute(
                assembly.MainModule.ImportReference(typeof(SerializableAttribute).GetConstructor(Type.EmptyTypes)));
            stringProtoClass.CustomAttributes.Add(serializableAttribute);

            // Add a default (parameterless) constructor
            MethodDefinition ctor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, assembly.MainModule.TypeSystem.Void);
            ILProcessor ctorIlProcessor = ctor.Body.GetILProcessor();
            ctorIlProcessor.Emit(OpCodes.Ldarg_0);
            ctorIlProcessor.Emit(OpCodes.Call, assembly.MainModule.ImportReference(protoClass.Methods.First(m => m.Name == ".ctor")));
            ctorIlProcessor.Emit(OpCodes.Ret);
            stringProtoClass.Methods.Add(ctor);

            assembly.MainModule.Types.Add(stringProtoClass);
            stringProtoClass.AddFied("ZHCN", assembly.MainModule.TypeSystem.String);
            stringProtoClass.AddFied("ENUS", assembly.MainModule.TypeSystem.String);
            stringProtoClass.AddFied("FRFR", assembly.MainModule.TypeSystem.String);
        }

        internal static bool GameDataGameTick(AssemblyDefinition assembly)
        {
            // TryAdd method: GameData.GameTick(long time) to call at the end of LogicFrame
            var targetType = assembly.MainModule.GetType("GameData");
            if (targetType.Methods.FirstOrDefault(m => m.Name == "GameTick") != null) return false;

            var gameTickMethod = targetType.AddMethod("GameTick", assembly.MainModule.TypeSystem.Void, new TypeReference[] { assembly.MainModule.TypeSystem.Int64 });
            ILProcessor ilProcessor = gameTickMethod.Body.GetILProcessor();
            ilProcessor.Emit(OpCodes.Ret);

            // Append the function call
            targetType = assembly.MainModule.GetType("GameLogic");
            var gameDataField = targetType.Fields.FirstOrDefault(f => f.Name == "data");
            var timeiField = targetType.Fields.FirstOrDefault(f => f.Name == "timei");
            var logicFrameMethod = targetType.Methods.FirstOrDefault(m => m.Name == "LogicFrame");
            if (gameDataField == null || timeiField == null || logicFrameMethod == null) return false;

            ilProcessor = logicFrameMethod.Body.GetILProcessor();
            var instructionsToInject = new Instruction[]
            {
                ilProcessor.Create(OpCodes.Ldarg_0),
                ilProcessor.Create(OpCodes.Ldfld, gameDataField),
                ilProcessor.Create(OpCodes.Ldarg_0),
                ilProcessor.Create(OpCodes.Ldfld, timeiField),
                ilProcessor.Create(OpCodes.Callvirt, gameTickMethod)
            };
            Instruction lastInstruction; // Assume single Ret at last line
            foreach (Instruction instruction in instructionsToInject)
            {
                lastInstruction = logicFrameMethod.Body.Instructions.Last();
                ilProcessor.InsertBefore(lastInstruction, instruction);
            }
            return true;
        }

        /*
        internal static bool UIOptionWindowfullscreenComp(AssemblyDefinition assembly)
        {
            // TryAdd field: UIToggle UIOptionWindow.fullscreenComp
            var targetType = assembly.MainModule.GetType("UIOptionWindow");
            if (targetType.Fields.FirstOrDefault(m => m.Name == "fullscreenComp") != null) return false;

            var uiToggle = assembly.MainModule.Types.First(t => t.FullName == "UIToggle");
            TypeReference uiToggleReference = assembly.MainModule.ImportReference(uiToggle);
            targetType.AddFied("fullscreenComp", uiToggleReference);
            return true;
        }
        */
    }
}
