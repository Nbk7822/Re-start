namespace Return.Runner;

public static class Program
{
    public static int Main(string[] args)
    {
        Console.WriteLine("usage: return-runner run --seed <n> --years <y> [--out <file>]");
        Console.WriteLine("       return-runner soak --worlds <n> --years <y> --out <dir> [--parallel <p>]");
        return args.Length == 0 || args[0] is "-h" or "--help" ? 0 : 2;
    }
}
