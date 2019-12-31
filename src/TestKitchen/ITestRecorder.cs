namespace TestKitchen
{
	public interface ITestRecorder
	{
		void LogInfo(string message);
		void LogError(string message);
	}
}