using System.Text;

namespace Return.Sim.Content;

/// <summary>
/// Key to template lookup with <c>{name}</c> placeholders. A missing key renders as
/// <c>⟦key⟧</c> so gaps are visible on screen and in logs instead of silently blank.
/// </summary>
public sealed class LocTable
{
    private readonly Dictionary<string, string> _strings;

    public LocTable(IReadOnlyDictionary<string, string> strings)
    {
        _strings = new Dictionary<string, string>(strings, StringComparer.Ordinal);
    }

    public int Count => _strings.Count;

    public bool Has(string key) => _strings.ContainsKey(key);

    public string Get(string key) => _strings.TryGetValue(key, out string? s) ? s : "⟦" + key + "⟧";

    public string Format(string key, IReadOnlyList<KeyValuePair<string, string>> args)
    {
        string template = Get(key);
        if (args.Count == 0 || template.IndexOf('{') < 0) return template;
        var sb = new StringBuilder(template.Length + 16);
        for (int i = 0; i < template.Length; i++)
        {
            char ch = template[i];
            if (ch == '{')
            {
                int end = template.IndexOf('}', i + 1);
                if (end > i)
                {
                    string name = template.Substring(i + 1, end - i - 1);
                    string? value = null;
                    foreach (KeyValuePair<string, string> kv in args)
                        if (kv.Key == name) { value = kv.Value; break; }
                    if (value != null)
                    {
                        sb.Append(value);
                        i = end;
                        continue;
                    }
                }
            }
            sb.Append(ch);
        }
        return sb.ToString();
    }
}
