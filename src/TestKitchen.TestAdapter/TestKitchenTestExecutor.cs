// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using TypeKitchen;
using TypeKitchen.Creation;

namespace TestKitchen.TestAdapter
{
	[ExtensionUri(ExecutorUri)]
	public class TestKitchenTestExecutor : ITestExecutor
	{
		public const string ExecutorUri = "executor://TestKitchen";

		public void RunTests(IEnumerable<TestCase> tests, IRunContext context, IFrameworkHandle handle)
		{
			if (!Trace.Listeners.OfType<TestResultTraceListener>().Any())
				Trace.Listeners.Add(new TestResultTraceListener());

			var services = new ServiceCollection();
			var fixture = new TestFixture(services);

			// allow registration in test constructors
			fixture.TryAddSingleton(fixture);
			fixture.TryAddSingleton<IServiceCollection>(fixture);
			fixture.TryAddSingleton<IServiceProvider>(fixture);

			using var ctx = new TestContext(fixture, new VsTestMessageSink(handle));

			var testPlan = BuildTestPlan(tests, handle, fixture);

			ctx.Begin();
			{
				foreach (var test in testPlan.Keys)
				{
					handle.SendMessage(TestMessageLevel.Informational,
						$"Evaluating test at path {Path.GetFileName(test.Source)}");

					var (method, instance) = testPlan[test];
					ExecuteStandardTest(handle, test, method, ctx, instance);
				}
			}
			ctx.End();
		}

		public void RunTests(IEnumerable<string> sources, IRunContext context, IFrameworkHandle handle)
		{
			foreach (var source in sources)
			{
				handle.SendMessage(TestMessageLevel.Informational, $"Running all tests in {Path.GetFileName(source)}");
			}
		}

		public void Cancel() { }

		private static Dictionary<TestCase, (MethodInfo, object)> BuildTestPlan(IEnumerable<TestCase> tests, IMessageLogger handle, TestFixture fixture)
		{
			var testPlan = new Dictionary<TestCase, (MethodInfo, object)>();
			foreach (var test in tests)
			{
				if (!CanExecuteTest(test, handle, out var type, out var method))
					continue;
				var instance = GetTestContainerInstance(type, fixture, handle);
				if (instance == null)
					continue;
				testPlan[test] = (method, instance);
			}

			return testPlan;
		}

		private static void ExecuteStandardTest(ITestExecutionRecorder recorder, TestCase test, MethodInfo method,
			TestContext context, object instance)
		{
			try
			{
				recorder.SendMessage(TestMessageLevel.Informational, "Running standard test");

				var accessor = CallAccessor.Create(method);
				var occurrences = 0;

				recorder.RecordResult(DutyCycle(accessor, ++occurrences));

				while (!context.Skipped && context.RepeatCount > 0)
				{
					recorder.RecordResult(DutyCycle(accessor, ++occurrences));
					context.RepeatCount--;
				}
			}
			finally
			{
				context.Dispose();
			}

			TestResult DutyCycle(IMethodCallAccessor accessor, int occurrence)
			{
				if (occurrence > 1)
				{
					var clone = new TestCase
					{
						DisplayName = $"{test.DisplayName ?? "<No Name>"} #{occurrence}",
						ExecutorUri = test.ExecutorUri,
						FullyQualifiedName = test.FullyQualifiedName,
						Id = test.Id,
						LineNumber = test.LineNumber,
						LocalExtensionData = test.LocalExtensionData,
						CodeFilePath = test.CodeFilePath,
						Source = test.Source
					};

					clone.Traits.AddRange(test.Traits);

					test = clone;
				}


				TestResult testResult = null;

				try
				{
					recorder.SendMessage(TestMessageLevel.Informational, "Starting test");
					recorder.RecordStart(test);

					context.BeginTest();
					var result = ExecuteTestMethod(accessor, instance, context);
					context.EndTest();

					var outcome = context.Skipped ? TestOutcome.Skipped :
						result ? TestOutcome.Passed : TestOutcome.Failed;
					recorder.SendMessage(TestMessageLevel.Informational,
						$"{test.DisplayName} => {outcome.ToString().ToLowerInvariant()}");

					recorder.SendMessage(TestMessageLevel.Informational, "Ending test");
					recorder.RecordEnd(test, outcome);

					testResult = new TestResult(test) {Outcome = outcome};
					if (context.Skipped)
						testResult.Messages.Add(new TestResultMessage(Constants.Categories.Skipped,
							context.SkipReason));

					var listener = Trace.Listeners.OfType<TestResultTraceListener>().SingleOrDefault();
					if (listener != null)
						testResult.Messages.Add(listener.ToMessage());

					return testResult;
				}
				catch (Exception ex)
				{
					recorder.SendMessage(TestMessageLevel.Error, ex.Message);
					testResult ??= new TestResult(test);

					testResult.Messages.Add(new TestResultMessage(Constants.Categories.Failed,
						"Test failed because an exception prevented execution."));

					testResult.Outcome = TestOutcome.Failed;
					testResult.ErrorMessage = ex.Message;
					testResult.ErrorStackTrace = ex.StackTrace;

					return testResult;
				}
			}
		}

		// ReSharper disable once SuggestBaseTypeForParameter
		private static object GetTestContainerInstance(Type type, TestFixture fixture, IMessageLogger logger)
		{
			logger?.SendMessage(TestMessageLevel.Informational, $"Creating instance for type {type.FullName}");

			object instance = null;

			try
			{
				if (type.GetConstructor(new[] {typeof(TestFixture)}) != null)
				{
					logger?.SendMessage(TestMessageLevel.Informational, $"{type.Name}(TestFixture fixture) {{ ... }}");
					instance = Instancing.CreateInstance(type, fixture);
				}
				else if (type.GetConstructor(new[] {typeof(IServiceCollection)}) != null)
				{
					logger?.SendMessage(TestMessageLevel.Informational,
						$"{type.Name}(IServiceCollection services) {{ ... }}");
					instance = Instancing.CreateInstance(type, fixture);
				}
				else if (type.GetConstructor(new[] {typeof(IServiceProvider)}) != null)
				{
					logger?.SendMessage(TestMessageLevel.Informational,
						$"{type.Name}(IServiceProvider serviceProvider) {{ ... }}");
					instance = Instancing.CreateInstance(type, fixture);
				}
				else if (type.GetConstructor(Type.EmptyTypes) != null)
				{
					logger?.SendMessage(TestMessageLevel.Informational, $"{type.Name}() {{ ... }}");
					instance = Instancing.CreateInstance(type);
				}
			}
			catch (Exception e)
			{
				logger?.SendMessage(TestMessageLevel.Error, e.ToString());
				instance = null;
			}

			if (instance != null)
				return instance;

			logger?.SendMessage(TestMessageLevel.Error,
				"Could not find a suitable constructor for the test containing class");
			return null;
		}

		private static bool ExecuteTestMethod(IMethodCallAccessor accessor, object instance, TestContext ctx)
		{
			bool result;
			if (accessor.Parameters.Length == 0)
				result = (bool) accessor.Call(instance);
			else
				result = (bool) accessor.Call(instance, ctx);

			return result;
		}

		private static bool CanExecuteTest(TestCase test, IMessageLogger logger, out Type type, out MethodInfo method)
		{
			var testName = test.FullyQualifiedName;
			logger.SendMessage(TestMessageLevel.Informational, $"Test name = {testName}");

			var lastDot = testName.LastIndexOf(".", StringComparison.Ordinal);
			var typeName = testName.Substring(0, lastDot);
			logger.SendMessage(TestMessageLevel.Informational, $"Type name = {typeName}");

			var methodName = testName.Substring(lastDot + 1);
			logger.SendMessage(TestMessageLevel.Informational, $"Method name = {methodName}");

			type = Type.GetType(typeName, an => ResolveAssembly(an, test.Source, logger),
				(a, n, i) => ResolveType(a, n, i, test.Source, logger), false, false);
			if (type == null)
			{
				logger.SendMessage(TestMessageLevel.Warning, $"Could not find type '{typeName}'");
				type = default;
				method = default;
				return false;
			}

			logger.SendMessage(TestMessageLevel.Informational, $"Method name = {methodName}");
			if (string.IsNullOrWhiteSpace(methodName))
			{
				logger.SendMessage(TestMessageLevel.Warning, $"Could not parse method name from '{testName}'");
				type = default;
				method = default;
				return false;
			}

			method = type.GetMethod(methodName,
				BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
			if (method != null)
				return true;

			logger.SendMessage(TestMessageLevel.Warning, $"Could not find method '{methodName}'");
			return false;
		}

		private static Type ResolveType(Assembly assembly, string typeName, bool ignoreCase, string source,
			IMessageLogger logger)
		{
			if (assembly == null && !string.IsNullOrWhiteSpace(source))
			{
				logger.SendMessage(TestMessageLevel.Informational, $"Loading assembly from '{source}'.");
				assembly = Assembly.LoadFrom(source);
			}

			var type = assembly?.GetType(typeName, false, ignoreCase);
			return type;
		}

		private static Assembly ResolveAssembly(AssemblyName assemblyName, string source, IMessageLogger logger)
		{
			if (assemblyName == null)
			{
				if (!string.IsNullOrWhiteSpace(source))
					logger.SendMessage(TestMessageLevel.Informational, $"Loading assembly from '{source}'.");

				return !string.IsNullOrWhiteSpace(source) ? Assembly.LoadFrom(source) : null;
			}

			var domain = AppDomain.CurrentDomain;
			var assembly = domain.GetAssemblies().SingleOrDefault(x => x.GetName() == assemblyName);
			if (assembly != null)
				return assembly;

			logger.SendMessage(TestMessageLevel.Warning, $"Assembly '{assemblyName.Name}' not loaded.");
			assembly = Assembly.Load(assemblyName);

			return assembly;
		}
	}
}