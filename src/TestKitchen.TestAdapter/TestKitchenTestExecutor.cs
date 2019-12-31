﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using TypeKitchen;

namespace TestKitchen.TestAdapter
{
	[ExtensionUri(ExecutorUri)]
    public class TestKitchenTestExecutor : ITestExecutor
    {
        public const string ExecutorUri = "executor://TestKitchen";

        public void RunTests(IEnumerable<TestCase> tests, IRunContext context, IFrameworkHandle handle)
        {
            var fixture = new TestFixture(new ServiceCollection());
            fixture.AddSingleton(fixture); // allow registration in test constructors
             
            using var ctx = new TestContext(fixture, new VsTestRecorder(handle));
            ctx.Begin();

			foreach (var test in tests)
            {
                if (!CanExecuteTest(test, handle, out var type, out var method))
                    continue;

                handle.SendMessage(TestMessageLevel.Informational,
                    $"Evaluating test at path {Path.GetFileName(test.Source)}");

                if (test.FullyQualifiedName.Contains(Constants.VirtualTests.UnhandledExceptions))
                {
                    ExecuteUnhandledExceptionsTest(test.Source, handle);
                    continue;
                }

                ExecuteStandardTest(handle, test, type, method, ctx);
            }

			ctx.End();
        }

        private static void ExecuteUnhandledExceptionsTest(string source, ITestExecutionRecorder recorder)
        {
            recorder.SendMessage(TestMessageLevel.Informational,
                $"Running exception coverage on {Path.GetFileName(source)}");

            foreach (var method in source.EnumerateTestMethods(out var assembly, recorder))
            {
                var test = TestFactory.CreateExceptionCoverageTest(assembly.GetName().Name, source);
                
                ExecuteUnhandledExceptionsTest(test, method, recorder);
            }
        }

        private static void ExecuteUnhandledExceptionsTest(TestCase test, MemberInfo method, ITestExecutionRecorder recorder)
        {
            recorder.SendMessage(TestMessageLevel.Informational, "Starting test");
            recorder.RecordStart(test);

            var type = method.DeclaringType ?? throw new InvalidOperationException();
            var analyzer = new ExceptionAnalyzer(method);
            
            var thrown = analyzer.GetExceptionsThrown().AsList();
            var handled = analyzer.GetExceptionsHandled().AsList();
            var unhandled = analyzer.GetExceptionsUnhandled().AsList();

            var outcome = handled.Count >= thrown.Count ? TestOutcome.Passed : TestOutcome.Failed;
            recorder.RecordEnd(test, outcome);
            recorder.SendMessage(TestMessageLevel.Informational, $"{type}: {method.Name} handles {handled.Count()} exceptions and throws {thrown.Count()}");

            var result = new TestResult(test)
            {
                Outcome = outcome, 
                DisplayName = method.Name.Replace("_", " ")
            };

            if (outcome != TestOutcome.Passed)
            {
                var errorMessage = Pooling.StringBuilderPool.Scoped(sb =>
                {
                    foreach (var exception in unhandled)
                    {
                        sb.AppendLine($"{exception.Name} is potentially thrown by executed code, but is not handled in user code.");
                    }
                });

                result.ErrorMessage = errorMessage;
            }

            recorder.RecordResult(result);
        }

        private static void ExecuteStandardTest(ITestExecutionRecorder recorder, TestCase test, Type type, MethodInfo method, TestContext context)
        {
	        var instance = Instancing.CreateInstance(type, context);

			try

            {
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
			            DisplayName = $"{test.DisplayName} #{occurrence}",
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

	            recorder.SendMessage(TestMessageLevel.Informational, "Starting test");
	            recorder.RecordStart(test);

	            context.BeginTest();
	            var result = ExecuteTestMethod(accessor, instance, context);
	            context.EndTest();

	            var outcome = context.Skipped ? TestOutcome.Skipped : result ? TestOutcome.Passed : TestOutcome.Failed;
	            recorder.SendMessage(TestMessageLevel.Informational, $"{test.DisplayName} => {outcome.ToString().ToLowerInvariant()}");
	            recorder.RecordEnd(test, outcome);

	            var testResult = new TestResult(test) {Outcome = outcome};
	            if (context.Skipped)
		            testResult.Messages.Add(new TestResultMessage(Constants.Categories.Skipped, context.SkipReason));

	            return testResult;
            }
        }

        private static bool ExecuteTestMethod(IMethodCallAccessor accessor, object instance, TestContext ctx)
        {
	        bool result;
	        if (accessor.Parameters.Length == 0)
		        result = (bool) accessor.Call(instance);
	        else
		        result = (bool) accessor.Call(instance, new object[] {ctx});
			
			return result;
        }
		
		private static bool CanExecuteTest(TestCase test, IMessageLogger logger, out Type type, out MethodInfo method)
        {
            var testName = test.FullyQualifiedName;
            logger.SendMessage(TestMessageLevel.Informational, $"Test name = {testName}");

            var lastDot = testName.LastIndexOf(".", StringComparison.Ordinal);
            var typeName = testName.Substring(0, lastDot);
            if (typeName.Contains(Constants.VirtualTests.Namespace))
            {
                logger.SendMessage(TestMessageLevel.Informational, $"Test is virtual");
                type = null;
                method = null;
                return true;
            }

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

            method = type.GetMethod(methodName, BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
            if (method != null)
                return true;

            logger.SendMessage(TestMessageLevel.Warning, $"Could not find method '{methodName}'");
            return false;
        }

        private static Type ResolveType(Assembly assembly, string typeName, bool ignoreCase, string source, IMessageLogger logger)
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
                if(!string.IsNullOrWhiteSpace(source))
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

        public void RunTests(IEnumerable<string> sources, IRunContext context, IFrameworkHandle handle)
        {
            foreach (var source in sources)
            {
                handle.SendMessage(TestMessageLevel.Informational, $"Running all tests in {Path.GetFileName(source)}");
            }
        }
        
        public void Cancel()
        {
            
        }
    }
}