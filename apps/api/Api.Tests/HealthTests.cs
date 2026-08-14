using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.Tests;

public class HealthTests(TestAppFactory factory) : IClassFixture<TestAppFactory>
{
    [Fact]
    public async Task Health_ReturnsOk_WithDatabaseUp()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("up", body.GetProperty("database").GetString());
    }
}
