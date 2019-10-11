// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;
using Xunit.Abstractions;

namespace TestKitchen.AspNetCore
{
	public abstract class ControllerTest<TStartup> : IClassFixture<SystemUnderTest<TStartup>> where TStartup : class
	{
		protected readonly SystemUnderTest<TStartup> Factory;

		protected ControllerTest(SystemUnderTest<TStartup> factory, ITestOutputHelper helper)
		{
			Factory = factory;
			Factory.TestOutputHelper = helper;
		}
	}
}