using System;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace TestKitchen.TestAdapter
{
    public static class TestFactory
    {
        public static TestCase CreateExceptionCoverageTest(string assemblyName, string source)
        {
            var executorUri = new Uri(TestKitchenTestExecutor.ExecutorUri, UriKind.Absolute);
            var test = new TestCase($"{assemblyName}.{Constants.VirtualTests.UnhandledExceptions}", executorUri, source);
            return test;
        }
    }
}