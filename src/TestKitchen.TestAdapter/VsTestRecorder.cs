﻿using System.Diagnostics;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace TestKitchen.TestAdapter
{
	public class VsTestRecorder : ITestRecorder
	{
		private readonly ITestExecutionRecorder _inner;

		public VsTestRecorder(ITestExecutionRecorder inner)
		{
			_inner = inner;
		}

		public void LogInfo(string message)
		{
			_inner.SendMessage(TestMessageLevel.Informational, message);
			Trace.TraceInformation(message);
		}

		public void LogWarning(string message)
		{
			_inner.SendMessage(TestMessageLevel.Warning, message);
			Trace.TraceWarning(message);
		}

		public void LogError(string message)
		{
			_inner.SendMessage(TestMessageLevel.Error, message);
			Trace.TraceError(message);
		}
	}
}