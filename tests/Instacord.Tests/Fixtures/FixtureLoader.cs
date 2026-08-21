namespace Instacord.Tests.Fixtures;

public static class FixtureLoader
{
    public static string Load(string name)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
        return File.ReadAllText(path);
    }
}