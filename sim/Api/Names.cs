using System.Text;

namespace Return.Sim.Api;

public static class Names
{
    /// <summary>PascalCase to snake_case, for building loc keys from enum names ("SeekCover" → "seek_cover").</summary>
    public static string Snake(string pascal)
    {
        var sb = new StringBuilder(pascal.Length + 4);
        for (int i = 0; i < pascal.Length; i++)
        {
            char ch = pascal[i];
            if (char.IsUpper(ch))
            {
                if (i > 0) sb.Append('_');
                sb.Append(char.ToLowerInvariant(ch));
            }
            else sb.Append(ch);
        }
        return sb.ToString();
    }
}
