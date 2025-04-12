using BepInEx;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ErrorAnalyzer
{
    /// <summary>
    /// Identifies and catalogs BepInEx plugins by their assemblies and namespaces.
    /// </summary>
    public class BepInExPluginIdentifier
    {
        readonly Dictionary<Assembly, List<PluginInfo>> pluginsByAssembly = new();
        readonly Dictionary<string, Assembly> assemblyByRootNamespace = new();
        readonly HashSet<string> duplicatedRootNamespace = new();
        readonly Dictionary<string, Assembly> assemblyByFullNamespace = new();

        /// <summary>
        /// Initializes a new instance of the BepInExPluginIdentifier class and maps all loaded BepInEx plugins.
        /// </summary>
        public BepInExPluginIdentifier()
        {
            foreach (var pluginInfo in BepInEx.Bootstrap.Chainloader.PluginInfos.Values)
            {
                if (pluginInfo.Instance == null)
                {
                    continue;
                }
                Assembly assembly = pluginInfo.Instance.GetType().Assembly;
                if (pluginsByAssembly.TryGetValue(assembly, out List<PluginInfo> pluginList))
                {
                    pluginList.Add(pluginInfo);
                }
                else
                {
                    pluginsByAssembly.Add(assembly, new List<PluginInfo>() { pluginInfo });
                }

                string fullNamespace = pluginInfo.Instance.GetType().Namespace;
                if (string.IsNullOrEmpty(fullNamespace)) continue;
                string rootNamespace = fullNamespace.Split('.')[0];

                // Add fullNamespace 
                if (assemblyByFullNamespace.TryGetValue(fullNamespace, out Assembly otherAsm))
                {
                    if (assembly != otherAsm)
                    {
                        // $"Namespace {fullNamespace} has duplicated assembly {assembly.GetName()}, {otherAsm.GetName()}"
                    }
                }
                else
                {
                    assemblyByFullNamespace.Add(fullNamespace, assembly);
                }

                // Add rootNamespace
                if (duplicatedRootNamespace.Contains(rootNamespace))
                {
                    continue;
                }
                else if (assemblyByRootNamespace.TryGetValue(rootNamespace, out Assembly valueAsm))
                {
                    if (valueAsm == assembly) // Same assembly having differnt plugins
                    {
                        continue;
                    }
                    else // If different assembly having the same root namespace, then the root spacename cannot be used
                    {
                        duplicatedRootNamespace.Add(rootNamespace);
                        assemblyByRootNamespace.Remove(rootNamespace);
                    }
                }
                else
                {
                    assemblyByRootNamespace.Add(rootNamespace, assembly);
                }
            }
        }

        /// <summary>
        /// Gets the assembly associated with a fully qualified type name.
        /// </summary>
        /// <param name="fullTypeName">The fully qualified name of the type.</param>
        /// <returns>The assembly containing the type, or null if not found.</returns>
        public Assembly GetAssembly(string fullTypeName)
        {
            if (string.IsNullOrEmpty(fullTypeName)) return null;

            string fullNamespace;
            int lastDotIndex = fullTypeName.LastIndexOf('.');
            if (lastDotIndex > 0) fullNamespace = fullTypeName.Substring(0, lastDotIndex);
            else return null; // We don't consider the type with no namespace
            if (assemblyByFullNamespace.TryGetValue(fullNamespace, out Assembly assembly0))
            {
                return assembly0;
            }

            string rootNamespace = fullNamespace;
            int firstDotIndex = fullNamespace.IndexOf('.');
            if (firstDotIndex > 0) rootNamespace = fullNamespace.Substring(0, firstDotIndex);
            if (assemblyByRootNamespace.TryGetValue(rootNamespace, out Assembly assembly1))
            {
                return assembly1;
            }

            return null;
        }

        /// <summary>
        /// Gets a list of plugin information for the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly to get plugin information for.</param>
        /// <returns>A list of PluginInfo objects associated with the assembly, or an empty list if none are found.</returns>
        public List<PluginInfo> GetPluginInfoList(Assembly assembly)
        {
            if (pluginsByAssembly.TryGetValue(assembly, out List<PluginInfo> value))
            {
                return value;
            }
            return new List<PluginInfo>();
        }

        /// <summary>
        /// Creates a formatted string representation of the namespace to assembly mappings.
        /// </summary>
        /// <returns>A string containing the namespace mapping information.</returns>
        public string DumpNamespaceMap()
        {
            var sb = new StringBuilder();

            sb.AppendLine("assembly by root namespace:");
            foreach (var pair in assemblyByRootNamespace)
            {
                sb.AppendLine($"[{pair.Key}] {pair.Value.GetName().Name}");
            }

            if (duplicatedRootNamespace.Count > 0)
            {
                sb.AppendLine("duplicate root namespace:");
                foreach (var value in duplicatedRootNamespace)
                {
                    sb.AppendLine(value);
                }
            }

            sb.AppendLine();
            sb.AppendLine("assembly by full namespace:");
            foreach (var pair in assemblyByFullNamespace)
            {
                sb.AppendLine($"[{pair.Key}]: {pair.Value.GetName().Name}");
            }
            return sb.ToString();
        }
    }
}
