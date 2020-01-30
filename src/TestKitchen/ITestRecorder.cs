namespace TestKitchen
{
	public interface ITestRecorder
	{
		void LogInfo(string message);
		void LogWarning(string message);
		void LogError(string message);
	}
}