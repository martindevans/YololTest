using CommandLine;

namespace YololTest;

public class Options
{
    [Option("max-lines", Required = false, Default = 20)]
    public int MaxLines { get; set; }

    [Option("max-string-length", Required = false, Default = 1024)]
    public int MaxStringLength { get; set; }

    [Option("max-ticks", Required = false, Default = 1048576)]
    public int MaxTicks { get; set; }

    [Option("max-ticks", Required = false, Default = null)]
    public string? Directory { get; set; }
}