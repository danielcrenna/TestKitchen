// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace TestKitchen
{
	public interface ITestMessageSink
	{
		void LogInfo(string message);
		void LogWarning(string message);
		void LogError(string message);
	}
}