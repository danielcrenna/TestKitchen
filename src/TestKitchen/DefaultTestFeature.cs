// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;

namespace TestKitchen
{
	/// <summary>
	///     Default: Public instance methods whose names end with "Test" and return a bool,
	///     for guiding the user towards the "one assertion per test" pattern.
	/// </summary>
	internal class DefaultTestFeature : ITestFeature
	{
		public IEnumerable<MethodInfo> EnumerateTestMethods(Assembly assembly, ITestMessageSink messageSink)
		{
			if (assembly.IsDynamic)
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

					messageSink?.LogInfo($"Found test: {method.Name.Replace("_", " ")}");

					yield return method;
				}
			}
		}
	}
}