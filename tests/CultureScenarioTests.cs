using Return.Runner;
using Return.Sim;
using Return.Sim.Events;
using Return.Sim.Stories;

namespace Return.Tests;

/// <summary>
/// Seeded scenarios (ADR-0023): whole worlds run for a year, as the soak runs them, produce stories
/// that each culture hook starts, and every one of them is a story by CLAUDE.md's definition.
/// </summary>
public class CultureScenarioTests
{
    private static readonly Lazy<IReadOnlyList<(ulong Seed, string Link, Story Story)>> CultureStories = new(() =>
    {
        const int Worlds = 40;
        var perWorld = new List<(ulong, string, Story)>[Worlds];
        Parallel.For(0, Worlds, i =>
        {
            ulong seed = (ulong)i + 1;
            Simulation sim = StoryLog.Simulate(TestContent.Db, seed, 1);
            World w = sim.World;
            var found = new List<(ulong, string, Story)>();
            foreach (Story s in StoryLog.Analyze(w).Stories)
                foreach (long id in s.Chain)
                    if (w.Log.Get(id) is { System: SystemTag.Culture } e)
                    {
                        found.Add((seed, Soak.CultureLink(e.Key), s));
                        break;
                    }
            perWorld[i] = found;
        });
        return perWorld.SelectMany(x => x).ToList();
    });

    [Fact]
    public void EachCultureHookStartsStoriesInSeededWorlds()
    {
        var links = CultureStories.Value.Select(s => s.Link).ToHashSet();
        foreach (string link in new[] { "blood_gift", "debt_of_honour", "guest_bread", "leader_last", "sacred_grove" })
            Assert.True(links.Contains(link), $"no story in seeds 1-40 starts with {link}; found {string.Join(", ", links.OrderBy(x => x, StringComparer.Ordinal))}");
        Assert.True(links.Contains("harvest_fire") || links.Contains("longest_night"), "no festival story in seeds 1-40");
        Assert.True(links.Count >= 6);
    }

    [Fact]
    public void EveryCultureStoryIsAStory()
    {
        Assert.NotEmpty(CultureStories.Value);
        foreach ((ulong seed, string link, Story s) in CultureStories.Value)
        {
            Assert.True(s.Chain.Count >= StoryAnalyzer.MinEvents, $"seed {seed}: a {link} story has {s.Chain.Count} events");
            Assert.True(s.Systems.Count >= StoryAnalyzer.MinSystems, $"seed {seed}: a {link} story touches {s.Systems.Count} system");
            Assert.Contains(SystemTag.Culture, s.Systems);
        }
    }
}
