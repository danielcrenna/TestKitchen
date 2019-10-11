// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace TestKitchen.AspNetCore
{
	public class SystemUnderTest<TStartup> : WebApplicationFactory<TStartup>
		where TStartup : class
	{
		private ActionTraceListener _actionTraceListener;

		public Guid Id { get; set; }
	    public ITestOutputHelper TestOutputHelper { get; internal set; }

	    protected override void ConfigureWebHost(IWebHostBuilder builder)
		{
			Id = Guid.NewGuid();
			base.ConfigureWebHost(builder);

			builder.ConfigureLogging((context, loggingBuilder) =>
			{
				loggingBuilder.ClearProviders();
				loggingBuilder.AddProvider(new XunitLoggerProvider(TestOutputHelper));
			});

			lock (Trace.Listeners)
			{
				Trace.UseGlobalLock = true;
				_actionTraceListener = new ActionTraceListener(s => TestOutputHelper.WriteLine(s), s => TestOutputHelper.WriteLine(s));
				if (!Trace.Listeners.Contains(_actionTraceListener))
					Trace.Listeners.Add(_actionTraceListener);
			}
		}
    }
}
