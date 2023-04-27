using CommandLine;
using Yolol.Execution;
using Yolol.Grammar;
using Yolol.IL.Compiler;
using Yolol.IL.Extensions;
using YololTest;
using Parser = CommandLine.Parser;
using Type = Yolol.Execution.Type;

var parsed = Parser.Default.ParseArguments<Options>(args);

parsed.WithNotParsed(errors =>
{
    foreach (var error in errors)
        Console.Error.WriteLine(error);
});

parsed.WithParsed(opts =>
{
    var tests = (from test in FindTests(opts) select (test, (TestResult?)null)).ToList();
    DrawResult(tests);

    for (var i = 0; i < tests.Count; i++)
    {
        var result = RunTest(tests[i].test, opts);
        tests[i] = (tests[i].test, result);
        DrawResult(tests);
    }
});

void DrawResult(List<(string, TestResult?)> tests)
{
    Console.Clear();

    foreach (var (test, result) in tests)
    {
        var color = ConsoleColor.White;
        if (result?.Success == true)
            color = ConsoleColor.Green;
        else if (result?.Success == false)
            color = ConsoleColor.Red;
        Console.ForegroundColor = color;

        var tick = result == null ? ' ' : result.Success ? '✓' : '❌';
        Console.WriteLine($" - [{tick}] {test}");

        if (result is { Message: not null, Success: false })
        {
            Console.WriteLine("Message:");
            Console.WriteLine(result.Message);
            Console.WriteLine();
        }
    }
}

static IEnumerable<string> FindTests(Options options)
{
    var dir = options.Directory ?? Environment.CurrentDirectory;
    return Directory.EnumerateFiles(dir, "*.yolol");
}

TestResult RunTest(string path, Options options)
{
    if (!File.Exists(path))
        return new TestResult(false, "File not found!");

    var text = File.ReadAllText(path);
    var parseResult = Yolol.Grammar.Parser.ParseProgram(text);

    if (!parseResult.IsOk)
        return new TestResult(false, parseResult.Err.ToString());

    try
    {
        var output = new VariableName(":output");
        var externalsMap = new ExternalsMap();
        var compiled = parseResult.Ok.Compile(externalsMap, options.MaxLines, options.MaxStringLength, changeDetection: true);
        if (!externalsMap.ContainsKey(output))
            return new TestResult(false, "Test never assigns ':output'");

        var csk = externalsMap.ChangeSetKey(output);
        var internals = new Value[compiled.InternalsMap.Count];
        Array.Fill(internals, (Number)0);
        var externals = new Value[externalsMap.Count];
        Array.Fill(externals, (Number)0);
        var ticksRemaining = options.MaxTicks;
        while (ticksRemaining > 0)
        {
            var ticks = compiled.Run(internals, externals, ticksRemaining, csk);
            ticksRemaining -= ticks;

            var outputVal = externals[externalsMap[output]];
            if (outputVal.Type == Type.Number)
                continue;

            if (outputVal.Type == Type.String && outputVal.ToString() == "ok")
                return new TestResult(true, null);

            return new TestResult(false, outputVal.ToString());
        }

        return new TestResult(false, "Executed MaxTicks but ':output' was never set to 'ok'");
    }
    catch (Exception ex)
    {
        return new TestResult(false, ex.Message);
    }
}