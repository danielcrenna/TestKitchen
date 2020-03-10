// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

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

		public static IEnumerable<(Type, MethodInfo)> EnumerateTestMethods(this string source, IEnumerable<ITestFeature> features, ITestMessageSink messageSink)
		{
			var methods = new HashSet<(Type, MethodInfo)>();

			if (ShouldIgnoreAssembly(source))
				return methods;

			try
			{
				var assembly = Assembly.LoadFile(source);

				if (assembly == typeof(TestKitchenTestDiscoverer).Assembly)
					return methods;

				if (assembly.IsDynamic)
					return methods;

				messageSink.LogInfo($"Probing source: {source}");

				foreach (var feature in features)
				{
					foreach (var method in feature.EnumerateTestMethods(assembly, messageSink))
					{
						methods.Add(method);
					}
				}
			}
			catch (Exception e)
			{
				messageSink.LogError(e.ToString());
				throw;
			}

			return methods;
		}
	}
}