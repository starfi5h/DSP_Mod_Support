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
            if (gameTickMethod.Parameters.Count > 0)
            {
                gameTickMethod.Parameters[0].Name = "time";
            }
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

        internal static bool InputFieldReadOnly(AssemblyDefinition assembly)
        {
            var module = assembly.MainModule;

            // 1. 尋找 InputField 類別
            // 注意：如果 InputField 有 Namespace，請用 FullName 或篩選條件
            var type = module.Types.FirstOrDefault(t => t.Name == "InputField");
            if (type == null)
            {
                Console.WriteLine("Class 'InputField' not found.");
                return false;
            }

            // 2. 檢查屬性是否已存在 (避免重複注入)
            if (type.Properties.Any(p => p.Name == "readOnly"))
            {
                Console.WriteLine("Property 'readOnly' already exists.");
                return false;
            }

            // 3. 獲取背後的欄位 m_ReadOnly (這是 getter/setter 操作的目標)
            var backingField = type.Fields.FirstOrDefault(f => f.Name == "m_ReadOnly");
            if (backingField == null)
            {
                Console.WriteLine("Field 'm_ReadOnly' not found. Cannot create property wrapper.");
                return false;
            }

            // 準備型別引用
            var boolType = module.TypeSystem.Boolean;
            var voidType = module.TypeSystem.Void;

            // 4. 定義屬性 (PropertyDefinition)
            var property = new PropertyDefinition("readOnly", PropertyAttributes.None, boolType);

            // 設定方法屬性: Public + SpecialName (表示這是屬性存取器) + HideBySig
            var methodAttrs = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

            // --- 5. 建立 Getter (get_readOnly) ---
            var getter = new MethodDefinition("get_readOnly", methodAttrs, boolType);
            var getIL = getter.Body.GetILProcessor();

            getIL.Emit(OpCodes.Ldarg_0);        // 載入 this
            getIL.Emit(OpCodes.Ldfld, backingField); // 讀取 m_ReadOnly
            getIL.Emit(OpCodes.Ret);            // 返回值

            // --- 6. 建立 Setter (set_readOnly) ---
            var setter = new MethodDefinition("set_readOnly", methodAttrs, voidType);
            // Setter 需要一個參數 'value'
            setter.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, boolType));
            var setIL = setter.Body.GetILProcessor();

            setIL.Emit(OpCodes.Ldarg_0);        // 載入 this
            setIL.Emit(OpCodes.Ldarg_1);        // 載入 value (參數 1)
            setIL.Emit(OpCodes.Stfld, backingField); // 寫入 m_ReadOnly
            setIL.Emit(OpCodes.Ret);            // 返回

            // --- 7. 綁定並加入類別 ---

            // 將方法設為屬性的 Get/Set 方法
            property.GetMethod = getter;
            property.SetMethod = setter;

            // 將方法加入類別的方法列表
            type.Methods.Add(getter);
            type.Methods.Add(setter);

            // 將屬性加入類別的屬性列表
            type.Properties.Add(property);

            Console.WriteLine("Successfully injected property 'readOnly' into 'InputField'.");
            return true;
        }
    }
}
