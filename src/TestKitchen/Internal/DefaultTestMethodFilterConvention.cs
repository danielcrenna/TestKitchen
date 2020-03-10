// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;

namespace TestKitchen.Internal
{
	internal sealed class DefaultTestMethodFilterConvention : ITestMethodFilterConvention
	{
		public bool IsValidTestMethod(Type owner, MethodInfo method)
		{
			return method.ReturnType == typeof(bool) || 
			       method.ReturnType == typeof(Task<bool>);
		}
	}
}