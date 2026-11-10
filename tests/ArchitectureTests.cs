using System.Reflection;

namespace Return.Tests;

public class ArchitectureTests
{
    [Fact]
    public void SimHasNoGodotOrPackageReferences()
    {
        Assembly sim = typeof(Return.Sim.Marker).Assembly;
        Assert.DoesNotContain(sim.GetReferencedAssemblies(),
            a => (a.Name ?? "").StartsWith("Godot", StringComparison.Ordinal));

        string csproj = File.ReadAllText(Path.Combine(TestPaths.Root, "sim", "Return.Sim.csproj"));
        Assert.DoesNotContain("<PackageReference", csproj);
    }

    [Fact]
    public void SimSourceAvoidsNondeterministicApis()
    {
        string[] banned =
        {
            "System.Random", "new Random(", "Random.Shared", "DateTime.", "Stopwatch",
            "Environment.TickCount", ".GetHashCode(", "HashCode.Combine", "Guid.NewGuid",
            "float ", "double ", "decimal ", "Godot",
        };
        string sep = Path.DirectorySeparatorChar.ToString();
        var hits = new List<string>();
        foreach (string file in Directory.EnumerateFiles(Path.Combine(TestPaths.Root, "sim"), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains(sep + "obj" + sep) || file.Contains(sep + "bin" + sep)) continue;
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
                foreach (string b in banned)
                    if (lines[i].Contains(b, StringComparison.Ordinal))
                        hits.Add($"{Path.GetRelativePath(TestPaths.Root, file)}:{i + 1}: {b}");
        }
        Assert.True(hits.Count == 0, "Nondeterministic or forbidden API in Return.Sim:\n" + string.Join("\n", hits));
    }
}
