# TestKitchen

TestKitchen is a small library for testing in .NET.

### Integration Test Logging

To run integration tests with all logging statements redirecting to the console, inherit from `ControllerTest<TStartup`:

```csharp
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
```

##### Logo

Logo is from Font Awesome, and is under a [CC BY 4.0 License](https://creativecommons.org/licenses/by/4.0/). 