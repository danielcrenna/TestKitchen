// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using TypeKitchen;

namespace TestKitchen
{
	public sealed class TestContext : IServiceProvider, IDisposable
	{
		private readonly TestFixture _fixture;
		private readonly Random _random;
		private readonly ITestMessageSink _messageSink;

		public TestContext(TestFixture fixture, ITestMessageSink messageSink)
		{
			_fixture = fixture;
			_messageSink = messageSink;
			_random = new Random();
		}

		internal bool Skipped { get; private set; }
		internal string SkipReason { get; set; }
		internal int RepeatCount { get; set; }

		public void Dispose()
		{
			_fixture?.Dispose();
		}

		public object GetService(Type serviceType)
		{
			return _fixture.GetService(serviceType);
		}

		public bool Assert<T>(T left, T right)
		{
			var equal = left.Equals(right);
			if (!equal)
				_messageSink.LogError($"Expected {left}, but was {right}");
			else
				_messageSink.LogInfo($"Values were both {left}");

			return equal;
		}

		public bool Skip(string reason = "")
		{
			Skipped = true;
			SkipReason = reason;
			return false;
		}

		private bool NextBoolean()
		{
			return _random.Next(0, 2) == 1;
		}

		public void Maybe(Action action)
		{
			var result = NextBoolean();
			_messageSink.LogInfo($"Maybe: {result}");
			if (result)
				action();
		}

		public void MaybeRepeat(int n, Action action)
		{
			var result = NextBoolean();
			_messageSink.LogInfo($"MaybeRepeat #1: {result}");

			for (var i = 0; i < n; i++)
			{
				result = NextBoolean();
				_messageSink.LogInfo($"MaybeRepeat #{i + 2}: {result}");
				if (result)
					action();
			}
		}

		public void Repeat(int n = 1)
		{
			RepeatCount = n;
		}

		public void Fill<T>(T instance)
		{
			var writer = WriteAccessor.Create(instance, AccessorMemberScope.Public, out var members);
			foreach (var member in members)
			{
				if (!IsNumber(member))
					continue;
				var value = _random.Next(int.MaxValue);
				writer.TrySetValue(instance, member.Name, value);
			}
		}

		private static bool IsNumber(AccessorMember member)
		{
			return member.Type == typeof(int);
		}

		internal void Begin()
		{
			_fixture.Begin();
		}

		internal void BeginTest()
		{
			_fixture.BeginTest();
		}

		internal void EndTest()
		{
			_fixture.EndTest();
		}

		internal void End()
		{
			_fixture.End();
		}
	}
}