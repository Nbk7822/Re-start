using System.Text.Json.Nodes;

namespace Return.Tests;

/// <summary>
/// Content fixtures for the player design (ADR-0030). The tests written before it pin seeded histories with you as
/// a human with no gift, so <see cref="TestContent.Db"/> loads the shipped content with a mod that leaves only the
/// human in rebirth.json's roll and gives every gift a weight of 0. The mod is made from the shipped files at run
/// time, so it never goes stale.
/// </summary>
internal static class FateFixtures
{
    private static readonly Lazy<string> HumanGiftlessDir = new(MakeHumanGiftless);

    /// <summary>A mod folder: you are always reborn human, with no gift.</summary>
    public static string HumanGiftless => HumanGiftlessDir.Value;

    private static string MakeHumanGiftless()
    {
        string content = Path.Combine(TestPaths.Root, "content");
        string dir = Path.Combine(Path.GetTempPath(), "return-human-giftless-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);

        JsonNode rebirth = JsonNode.Parse(File.ReadAllText(Path.Combine(content, "rebirth.json")))!;
        var peoples = new JsonArray();
        foreach (JsonNode? p in rebirth["peoples"]!.AsArray())
            peoples.Add(new JsonObject
            {
                ["species"] = p!["species"]!.GetValue<string>(),
                ["weight"] = p["species"]!.GetValue<string>() == "human" ? 1 : 0,
                ["openingKey"] = p["openingKey"]!.GetValue<string>(),
            });
        File.WriteAllText(Path.Combine(dir, "rebirth.json"), new JsonObject { ["peoples"] = peoples }.ToJsonString());

        JsonArray gifts = JsonNode.Parse(File.ReadAllText(Path.Combine(content, "gifts.json")))!.AsArray();
        foreach (JsonNode? g in gifts)
        {
            g!["weight"] = 0;
            g["peoples"] = new JsonObject();
        }
        File.WriteAllText(Path.Combine(dir, "gifts.json"), gifts.ToJsonString());
        return dir;
    }
}
