// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace TestKitchen.TestAdapter
{
	public class VsTestMessageSink : ITestMessageSink
	{
		private readonly IMessageLogger _inner;

		public VsTestMessageSink(IMessageLogger inner) => _inner = inner;

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