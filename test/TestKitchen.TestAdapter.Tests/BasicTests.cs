// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace TestKitchen.TestAdapter.Tests
{
	public class BasicTests
	{
		public BasicTests(TestContext context)
		{
		}

		public bool Hello_world()
		{
			return true;
		}

		public bool Test_is_skipped(TestContext context)
		{
			return context.Skip("skippy as I wanna be");
		}

		public bool Two_string_instances_are_equal(TestContext context)
		{
			return "aaa".Equals("AAA", StringComparison.OrdinalIgnoreCase);
		}

		public bool Handles_exceptions()
		{
			try
			{
				throw new ArgumentException();
			}
			catch
			{
				return true;
			}
		}
	}
}