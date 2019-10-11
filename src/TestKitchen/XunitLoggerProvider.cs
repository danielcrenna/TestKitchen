// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace TestKitchen
{
	public class XunitLoggerProvider : ILoggerProvider
	{
		private readonly ITestOutputHelper _helper;
		public void Dispose() { }

		public XunitLoggerProvider(ITestOutputHelper helper)
		{
			_helper = helper;
		}

		public ILogger CreateLogger(string categoryName)
		{
			return new XunitLogger(_helper);
		}
	}
}