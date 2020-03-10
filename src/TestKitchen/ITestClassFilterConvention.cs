// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace TestKitchen
{
	public interface ITestClassFilterConvention
	{
		bool IsValidTestClass(Type type);
	}
}