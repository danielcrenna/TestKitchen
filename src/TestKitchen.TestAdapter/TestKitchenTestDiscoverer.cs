// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using TestKitchen.TestAdapter.Internal;

namespace TestKitchen.TestAdapter
{
	[FileExtension(".dll")]
	[FileExtension(".exe")]
	[DefaultExecutorUri(TestKitchenTestExecutor.ExecutorUri)]
	[ExtensionUri(TestKitchenTestExecutor.ExecutorUri)]
	public class TestKitchenTestDiscoverer : ITestDiscoverer
	{
		public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext,
			IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
		{
			// FIXME: Check RunSettings for options and feature extension

			var features = new List<ITestFeature> {new DefaultTestFeature()};

			var recorder = new VsTestMessageSink(logger);

			try
			{
				foreach (var source in sources)
				{
					foreach (var method in source.EnumerateTestMethods(features, recorder))
					{
						try
						{
							discoverySink.SendTestCase(CreateTestCase(method, source));
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