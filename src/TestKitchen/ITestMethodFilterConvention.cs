// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace TestKitchen
{
	public interface ITestMethodFilterConvention
	{
		bool IsValidTestMethod(Type owner, MethodInfo method);
	}
}