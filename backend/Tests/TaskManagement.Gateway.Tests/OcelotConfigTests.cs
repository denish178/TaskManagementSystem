using System.Text.Json;

namespace TaskManagement.Gateway.Tests;

public class OcelotConfigTests
{
    [Theory]
    [InlineData("ocelot.json")]
    [InlineData("ocelot.docker.json")]
    public void OcelotConfig_IsValidJson_WithRoutes(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, fileName);
        Assert.True(File.Exists(path), $"Missing {fileName} in test output.");

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("Routes", out var routes));
        Assert.Equal(JsonValueKind.Array, routes.ValueKind);
        Assert.True(routes.GetArrayLength() > 0);
    }
}
