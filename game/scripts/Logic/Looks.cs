using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Return.Game.Logic;

public enum Build
{
    Slight,
    Average,
    Broad,
    Tall,
}

public enum HairStyle
{
    Short,
    Long,
    Tied,
    Cropped,
    Bald,
}

/// <summary>
/// How one person looks, beyond what the sim models: hair, skin, build, and optionally the cloth
/// colour (otherwise cast.json's). A view asset (game/art/looks.json), not content: the sim never
/// reads it and its shape is not a content schema.
/// </summary>
public sealed record Look(Rgb? Cloth, Rgb Hair, Rgb Skin, Build Build, HairStyle HairStyle, bool Beard, bool Hood)
{
    /// <summary>Width and height multipliers for a build.</summary>
    public (float Width, float Height) Scale => Build switch
    {
        Build.Slight => (0.9f, 0.97f),
        Build.Broad => (1.14f, 1.01f),
        Build.Tall => (0.97f, 1.07f),
        _ => (1f, 1f),
    };
}

/// <summary>
/// Reads game/art/looks.json: a "default" look, a "hostile" look for nameless raiders, and
/// "people" keyed by cast key (seeded from the archived demo's cloth and hair tables).
/// </summary>
public sealed class LookBook
{
    private readonly Dictionary<string, Look> _people;

    private LookBook(Look fallback, Look hostile, Dictionary<string, Look> people)
    {
        Default = fallback;
        Hostile = hostile;
        _people = people;
    }

    public Look Default { get; }

    public Look Hostile { get; }

    public IReadOnlyCollection<string> Keys => _people.Keys;

    /// <summary>The look for a cast key, or the default look for someone not listed.</summary>
    public Look For(string castKey) => _people.TryGetValue(castKey, out Look? look) ? look : Default;

    public bool Has(string castKey) => _people.ContainsKey(castKey);

    /// <summary>A plain default when the file is missing, so the view still draws everyone.</summary>
    public static LookBook Fallback()
    {
        var plain = new Look(null, new Rgb(0.29f, 0.24f, 0.18f), new Rgb(0.72f, 0.59f, 0.47f), Build.Average, HairStyle.Short, false, false);
        var hostile = plain with { Cloth = new Rgb(0.17f, 0.17f, 0.18f), Hood = true };
        return new LookBook(plain, hostile, new Dictionary<string, Look>(StringComparer.Ordinal));
    }

    public static LookBook Load(string path) => Parse(File.ReadAllText(path));

    public static LookBook Parse(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        JsonElement root = doc.RootElement;
        Look fallback = Fallback().Default;
        if (root.TryGetProperty("default", out JsonElement d)) fallback = Read(d, fallback, "default");
        Look hostile = fallback with { Hood = true };
        if (root.TryGetProperty("hostile", out JsonElement h)) hostile = Read(h, hostile, "hostile");
        var people = new Dictionary<string, Look>(StringComparer.Ordinal);
        if (root.TryGetProperty("people", out JsonElement p))
            foreach (JsonProperty person in p.EnumerateObject())
                people[person.Name] = Read(person.Value, fallback, person.Name);
        return new LookBook(fallback, hostile, people);
    }

    private static Look Read(JsonElement e, Look basis, string who)
    {
        Rgb? cloth = e.TryGetProperty("cloth", out JsonElement c) ? Color(c, who, "cloth") : basis.Cloth;
        Rgb hair = e.TryGetProperty("hair", out JsonElement hr) ? Color(hr, who, "hair") : basis.Hair;
        Rgb skin = e.TryGetProperty("skin", out JsonElement s) ? Color(s, who, "skin") : basis.Skin;
        Build build = e.TryGetProperty("build", out JsonElement b) ? Enum<Build>(b, who, "build") : basis.Build;
        HairStyle style = e.TryGetProperty("hairStyle", out JsonElement st) ? Enum<HairStyle>(st, who, "hairStyle") : basis.HairStyle;
        bool beard = e.TryGetProperty("beard", out JsonElement bd) ? bd.GetBoolean() : basis.Beard;
        bool hood = e.TryGetProperty("hood", out JsonElement hd) ? hd.GetBoolean() : basis.Hood;
        return new Look(cloth, hair, skin, build, style, beard, hood);
    }

    private static Rgb Color(JsonElement e, string who, string field) =>
        Rgb.TryParse(e.GetString(), out Rgb rgb) ? rgb : throw new FormatException($"looks.json: {who}.{field} is not a #rrggbb colour.");

    private static T Enum<T>(JsonElement e, string who, string field) where T : struct, Enum =>
        System.Enum.TryParse(e.GetString(), ignoreCase: true, out T value) && System.Enum.IsDefined(value)
            ? value
            : throw new FormatException($"looks.json: {who}.{field} '{e.GetString()}' is not one of {string.Join(", ", System.Enum.GetNames<T>())}.");
}
