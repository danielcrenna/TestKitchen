using System;
using System.Collections.Generic;
using System.Diagnostics;
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
			if(!Trace.Listeners.OfType<TestResultTraceListener>().Any())
				Trace.Listeners.Add(new TestResultTraceListener());
			
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
				 
                ExecuteStandardTest(handle, test, type, method, ctx);
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

		private static void ExecuteStandardTest(ITestExecutionRecorder recorder, TestCase test, Type type, MethodInfo method, TestContext context)
        {
	        recorder.SendMessage(TestMessageLevel.Informational, "Creating instance for type " + type.FullName);

			object instance = null;

	        var ctor = type.GetConstructor(new[] {typeof(TestContext)});
			if(ctor != null)
			{
				instance = Activator.CreateInstance(type, context);
			}
	        
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
	        catch (Exception ex)
	        {
		        recorder.SendMessage(TestMessageLevel.Error, ex.Message);
		        throw;
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

	            var listener = Trace.Listeners.OfType<TestResultTraceListener>().SingleOrDefault();
	            if (listener != null)
					testResult.Messages.Add(listener.ToMessage());

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
            if (typeName.StartsWith(Constants.VirtualTests.Namespace))
            {
                logger.SendMessage(TestMessageLevel.Informational, "Test is virtual");
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
    }
}