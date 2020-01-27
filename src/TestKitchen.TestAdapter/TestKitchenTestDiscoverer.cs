using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace TestKitchen.TestAdapter
{
    [FileExtension(".dll")]
    [FileExtension(".exe")]
    [DefaultExecutorUri(TestKitchenTestExecutor.ExecutorUri)]
    [ExtensionUri(TestKitchenTestExecutor.ExecutorUri)]
    public class TestKitchenTestDiscoverer : ITestDiscoverer
    {
        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
			// FIXME: Check RunSettings for options

			try
            {
                foreach (var source in sources)
                {
                    foreach (var method in source.EnumerateTestMethods(out var assembly, logger))
                    {
                        try
                        {
                            var test = CreateTestCase(method, source);
                            discoverySink.SendTestCase(test);

                            var assemblyName = assembly.GetName().Name;
                            foreach (var virtualTest in CreateVirtualTestCases(assemblyName, source))
                                discoverySink.SendTestCase(virtualTest);
                        }
                        catch (Exception e)
                        {
                            logger.SendMessage(TestMessageLevel.Error, e.ToString());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.SendMessage(TestMessageLevel.Error, e.ToString());
            }
        }
        
        private static IEnumerable<TestCase> CreateVirtualTestCases(string assemblyName, string source)
        {
	        yield break;
        }

        private static TestCase CreateTestCase(MemberInfo member, string source)
        {
            using var session = new DiaSession(source);
            var data = session.GetNavigationData(member.DeclaringType?.FullName, member.Name);
            
            var executorUri = new Uri(TestKitchenTestExecutor.ExecutorUri, UriKind.Absolute);
            var test = new TestCase($"{member.DeclaringType?.FullName}.{member.Name}", executorUri, source)
            {
                CodeFilePath = data.FileName,
                LineNumber = data.MinLineNumber + 1,
                DisplayName = member.Name.Replace("_", " ")
            };
            
            return test;
        }
    }
}