using System.Net;
using System.Threading.Tasks;
using TestKitchen.AspNetCore;
using Xunit;
using Xunit.Abstractions;

namespace DemoApp.Tests
{
	public class WeatherForecastControllerTests : ControllerTest<Startup>
	{
		public WeatherForecastControllerTests(SystemUnderTest<Startup> factory, ITestOutputHelper helper) : base(factory, helper) { }

		[Fact]
		public async Task Get()
		{
			using var client = Factory.CreateClient();
			var response = await client.GetAsync("WeatherForecast");
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		}
	}
}
