using System;
using TypeKitchen;

namespace TestKitchen
{
	public sealed class TestContext : IServiceProvider, IDisposable
    {
        private readonly TestFixture _fixture;
        private readonly ITestRecorder _recorder;
        private readonly Random _random;
        
        internal bool Skipped { get; private set; }
        internal string SkipReason { get; set; }
        internal int RepeatCount { get; set; }

		public TestContext(TestFixture fixture, ITestRecorder recorder)
		{
			_fixture = fixture;
            _recorder = recorder;
            _random = new Random();
        }

        public bool Skip(string reason = "")
        {
            Skipped = true;
            SkipReason = reason;
            return false;
        }

        public void Dispose()
        {
	        _fixture?.Dispose();
        }

        private bool NextBoolean()
        {
	        return _random.Next(0, 2) == 1;
        }

        public void Maybe(Action action)
        {
	        var result = NextBoolean();
	        _recorder.LogInfo("Maybe: " + result);

	        if (result)
		        action();
        }

        public void MaybeRepeat(int n, Action action)
        {
			var result = NextBoolean();
			_recorder.LogInfo($"MaybeRepeat #1: {result}");

			for (var i = 0; i < n; i++)
			{
				result = NextBoolean();
				_recorder.LogInfo($"MaybeRepeat #{i + 2}: {result}");
				if (result)
		            action();
			}
        }

		public void Repeat(int n = 1)
        {
	        RepeatCount = n;
        }

		public bool Assert<T>(T left, T right)
		{
			var equal = left.Equals(right);
			if (!equal)
				_recorder.LogError($"Expected {left}, but was {right}");
			else
				_recorder.LogInfo($"Values were both {left}");

			return equal;
		}

		public void Fill<T>(T instance)
		{
			var writer = WriteAccessor.Create(instance, scope: AccessorMemberScope.Public, out var members);
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

		public object GetService(Type serviceType) => _fixture.GetService(serviceType);

		internal void Begin() => _fixture.Begin();
		internal void BeginTest() => _fixture.BeginTest();
		internal void EndTest() => _fixture.EndTest();
		internal void End() => _fixture.End();
    }
}
