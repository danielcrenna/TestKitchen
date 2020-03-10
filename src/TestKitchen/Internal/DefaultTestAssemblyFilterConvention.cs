// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using TypeKitchen;

namespace TestKitchen.Internal
{
	internal sealed class DefaultTestAssemblyFilterConvention : ITestAssemblyFilterConvention
	{
		public bool IsValidTestAssembly(Assembly assembly) => !assembly.IsDynamic && !assembly.HasAttribute<CompilerGeneratedAttribute>();
	}
}