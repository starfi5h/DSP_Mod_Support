using HarmonyLib;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;

namespace ErrorAnalyzer
{
    /// <summary>
    /// Maps Harmony prefix, postfix and transpiler patches to their target methods and types for analysis.
    /// </summary>
    public class HarmonyPatcherMapper
    {
        readonly Dictionary<string, List<MethodBase>> patchesByTargetType = new();

        /// <summary>
        /// Initializes a new instance of the HarmonyPatcherMapper class and catalogs all Harmony patches.
        /// </summary>
        public HarmonyPatcherMapper()
        {
            foreach (MethodBase patchedMethod in PatchProcessor.GetAllPatchedMethods())
            {
                string key = patchedMethod.DeclaringType.FullName;
                if (!patchesByTargetType.TryGetValue(key, out List<MethodBase> methods))
                {
                    methods = new List<MethodBase>();
                    patchesByTargetType[key] = methods;
                }
                methods.Add(patchedMethod);
            }
        }

        /// <summary>
        /// Determines if a type has been patched by Harmony.
        /// </summary>
        /// <param name="typeName">The fully qualified name of the type to check.</param>
        /// <returns>True if the type has been patched, false otherwise.</returns>
        public bool IsTargetType(string typeName)
        {
            return patchesByTargetType.ContainsKey(typeName);
        }

        /// <summary>
        /// Gets the method with the specified name from the target type.
        /// </summary>
        /// <param name="typeName">The fully qualified name of the type.</param>
        /// <param name="methodName">The name of the method to find.</param>
        /// <returns>The method base if found, otherwise null.</returns>
        public MethodBase GetModMethod(string typeName, string methodName)
        {
            if (patchesByTargetType.TryGetValue(typeName, out var list))
            {
                foreach (var methodBase in list)
                {
                    if (methodBase.Name == methodName) return methodBase;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the set of assemblies that have patched the specified method.
        /// </summary>
        /// <param name="targetMethod">The type to check for patches.</param>
        /// <returns>A set of assemblies that have patches applied to the target type.</returns>
        public HashSet<Assembly> GetModAssembliesFromMethod(MethodBase targetMethod)
        {
            var patchInfo = Harmony.GetPatchInfo(targetMethod);
            if (patchInfo == null)
            {
                return new HashSet<Assembly>(); // No patches applied to this method
            }

            var assemblies = new HashSet<Assembly>();
            foreach (var patch in patchInfo.Prefixes)
            {
                assemblies.Add(patch.PatchMethod.DeclaringType.Assembly);
            }
            foreach (var patch in patchInfo.Postfixes)
            {
                assemblies.Add(patch.PatchMethod.DeclaringType.Assembly);
            }
            foreach (var patch in patchInfo.Transpilers)
            {
                assemblies.Add(patch.PatchMethod.DeclaringType.Assembly);
            }
            return assemblies;
        }

        /// <summary>
        /// Creates a formatted string describing all patches applied to the specified method.
        /// </summary>
        /// <param name="targetMethod">The method to get patch descriptions for.</param>
        /// <returns>A string describing all patches applied to the method.</returns>
        public HashSet<Assembly> GetModAssembliesFromType(string targetTypeName)
        {
            var assemblies = new HashSet<Assembly>();
            if (!patchesByTargetType.TryGetValue(targetTypeName, out List<MethodBase> methodList))
            {
                return assemblies;
            }
            foreach (var method in methodList)
            {
                assemblies.UnionWith(GetModAssembliesFromMethod(method));
            }
            return assemblies;
        }

        /// <summary>
        /// Creates a formatted string describing all patches applied to the specified method.
        /// </summary>
        /// <param name="targetMethod">The method to get patch descriptions for.</param>
        /// <returns>A string describing all patches applied to the method.</returns>
        public string GetPatchesDescription(MethodBase targetMethod)
        {
            var patchInfo = PatchProcessor.GetPatchInfo(targetMethod);
            if (patchInfo == null) return "";

            var sb = new StringBuilder();
            PatchesToString(sb, patchInfo.Prefixes, $"; {targetMethod.DeclaringType.Name + "." + targetMethod.Name}(Prefix)");
            PatchesToString(sb, patchInfo.Postfixes, $"; {targetMethod.DeclaringType.Name + "." + targetMethod.Name}(Postfix)");
            PatchesToString(sb, patchInfo.Transpilers, $"; {targetMethod.DeclaringType.Name + "." + targetMethod.Name}(Transpiler)");
            return sb.ToString();
        }

        /// <summary>
        /// Creates a comprehensive dump of all patches in the system.
        /// </summary>
        /// <returns>A string containing information about all patches.</returns>
        public string DumpPatchMap()
        {
            var sb = new StringBuilder();
            sb.Append("DumpPatchMap type count: ").Append(patchesByTargetType.Keys.Count).AppendLine();
            foreach (var kvp in patchesByTargetType)
            {
                sb.Append("\n[Type: ").Append(kvp.Key).AppendLine("]"); // DeclaringType.FullName
                foreach (var method in kvp.Value)
                {
                    sb.Append("-- ").Append(kvp.Key).Append(".").Append(method.Name).AppendLine(" --");
                    var patchInfo = PatchProcessor.GetPatchInfo(method);
                    PatchesToString(sb, patchInfo.Prefixes, "; (Prefix)");
                    PatchesToString(sb, patchInfo.Postfixes, "; (Postfix)");
                    PatchesToString(sb, patchInfo.Transpilers, "; (Transpiler)");
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Appends formatted patch information to a StringBuilder.
        /// </summary>
        /// <param name="sb">The StringBuilder to append to.</param>
        /// <param name="patches">The collection of patches to format.</param>
        /// <param name="textPostfix">Text to append at the end of each line.</param>
        private void PatchesToString(StringBuilder sb, ReadOnlyCollection<Patch> patches, string textPostfix)
        {
            // Format: static void ModMethod(); OriginalMethod (Harmony patch type)
            foreach (var patch in patches)
            {
                sb.Append(patch.PatchMethod.FullDescription()
                    .Replace("System.Collections.Generic.IEnumerable<HarmonyLib.CodeInstruction>", "var")
                    .Replace("System.Reflection.Emit.ILGenerator", "var")); // shorten transpiler parameters
                sb.Replace("static ", ""); // the PatchMethod is always static function

                sb.AppendLine(textPostfix);
            }
        }
    }
}
