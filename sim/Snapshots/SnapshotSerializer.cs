using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Return.Sim.Content;

namespace Return.Sim.Snapshots;

/// <summary>
/// Full-state snapshots as UTF-8 JSON (enums as names, so snapshots read cleanly and survive enum
/// reordering). The state hash is SHA-256 of the snapshot bytes; equal hashes mean equal worlds.
/// </summary>
public static class SnapshotSerializer
{
    public static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = false,
    };

    public static byte[] Save(World w) => JsonSerializer.SerializeToUtf8Bytes(w, Options);

    public static World Load(byte[] data, ContentDb content)
    {
        World w = JsonSerializer.Deserialize<World>(data, Options)
            ?? throw new InvalidOperationException("Snapshot is empty.");
        w.Content = content;
        Systems.ScheduleSystem.EnsureDefaults(w); // a snapshot from before the Schedule gets its days from content (ADR-0029); a no-op otherwise
        return w;
    }

    public static string Hash(World w) => Convert.ToHexString(SHA256.HashData(Save(w)));
}
