using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace TestKitchen.TestAdapter.Internal
{
    internal static class AssemblyExtensions
    {
        private static bool ShouldIgnoreAssembly(string source)
        {
            return Path.GetFileName(source).StartsWith("Microsoft.") ||
                   Path.GetFileName(source).StartsWith("System.") ||
                   Path.GetFileName(source).StartsWith("Newtonsoft.") ||
                   Path.GetFileName(source).StartsWith("NuGet.") ||
                   Path.GetFileName(source) == "testhost.dll" ||
                   Path.GetFileName(source) == "testhost.exe" ||
                   Path.GetFileName(source) == "TestKitchen.dll";
        }

        public static IEnumerable<MethodInfo> EnumerateTestMethods(this string source, out Assembly assembly, IMessageLogger logger)
        {
            var methods = new HashSet<MethodInfo>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic);
            var lookup = assemblies.ToDictionary(k => k.Location, v => v);

            if (ShouldIgnoreAssembly(source))
            {
                assembly = default;
                return methods;
            }

            if (!lookup.TryGetValue(source, out assembly))
            {
                assembly = Assembly.LoadFile(source);
                if (assembly.IsDynamic)
					return methods;

				lookup.Add(source, assembly);
            }

            if (assembly.IsDynamic)
                return methods;

            if (assembly == typeof(TestKitchenTestDiscoverer).Assembly)
                return methods;

            logger.SendMessage(TestMessageLevel.Informational, $"Probing source: {source}");

            foreach (var method in assembly.EnumerateTestMethods(logger))
            {
                methods.Add(method);
            }

            return methods;
        }

        public static IEnumerable<MethodInfo> EnumerateTestMethods(this Assembly assembly, IMessageLogger logger = null)
        {
            if (assembly.IsDynamic)
                yield break;

            if (assembly == typeof(TestKitchenTestDiscoverer).Assembly)
                yield break;

            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                if (!type.Name.EndsWith("Test") && !type.Name.EndsWith("Tests"))
                    continue;

                var methods = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
                foreach (var method in methods)
                {
                    if (method.ReturnType != typeof(bool))
                        continue;

                    logger?.SendMessage(TestMessageLevel.Informational, $"Found test: {method.Name.Replace("_", " ")}");

                    yield return method;
                }
            }
        }
    }
}