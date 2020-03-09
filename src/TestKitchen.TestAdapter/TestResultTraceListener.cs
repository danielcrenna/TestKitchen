// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using TypeKitchen;

namespace TestKitchen.TestAdapter
{
	internal class TestResultTraceListener : TraceListener
	{
		private readonly Queue<string> _queue;

		public TestResultTraceListener() => _queue = new Queue<string>();

		public override void Write(string message)
		{
			_queue.Enqueue(message);
		}

		public override void WriteLine(string message)
		{
			Write(message);
			_queue.Enqueue(Environment.NewLine);
		}

		public TestResultMessage ToMessage()
		{
			var text = Pooling.StringBuilderPool.Scoped(sb =>
			{
				while (_queue.TryDequeue(out var line))
					sb.Append(line);
			});

			return new TestResultMessage(TestResultMessage.StandardOutCategory, text);
		}
	}
}