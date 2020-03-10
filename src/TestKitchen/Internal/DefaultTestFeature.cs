// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TestKitchen.Internal
{
	/// <summary>
	///     Default: Public instance methods whose names end with "Test" and return a bool,
	///     for guiding the user towards the "one assertion per test" pattern.
	/// </summary>
	internal sealed class DefaultTestFeature : ITestFeature
	{
		private readonly ITestAssemblyFilterConvention _assemblyFilterConvention;
		private readonly ITestClassFilterConvention _testClassFilterConvention;
		private readonly ITestMethodFilterConvention _testMethodFilterConvention;

		public DefaultTestFeature() : this(new DefaultTestAssemblyFilterConvention(), new DefaultTestClassFilterConvention(), new DefaultTestMethodFilterConvention()) { }

		public DefaultTestFeature(
			ITestAssemblyFilterConvention assemblyFilterConvention, 
			ITestClassFilterConvention testClassFilterConvention,
			ITestMethodFilterConvention testMethodFilterConvention)
		{
			_assemblyFilterConvention = assemblyFilterConvention;
			_testClassFilterConvention = testClassFilterConvention;
			_testMethodFilterConvention = testMethodFilterConvention;
		}

		public IEnumerable<(Type, MethodInfo)> EnumerateTestMethods(Assembly assembly, ITestMessageSink messageSink)
		{
			if (!_assemblyFilterConvention.IsValidTestAssembly(assembly))
				yield break;

			var visited = new HashSet<Type>();
			var types = assembly.GetTypes();

			foreach (var type in types.OrderByDescending(t => t.IsAbstract))
			{
				if (visited.Contains(type))
					continue;
				visited.Add(type);

				if (!_testClassFilterConvention.IsValidTestClass(type))
					continue;
				
				//
				// Regular on-class test methods
				var methods = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
				foreach (var method in methods)
				{
					if (method.DeclaringType == typeof(object))
						continue;
					if (!_testMethodFilterConvention.IsValidTestMethod(type, method))
						continue;
					messageSink?.LogInfo($"Found test: {type.FullName}.{method.Name}");
					yield return (type, method);
				}
			}
		}
	}
}