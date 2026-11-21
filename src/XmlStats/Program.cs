// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml;

namespace Microsoft.Xml;

internal static class Program
{
    private static int Main(string[] args)
    {
        var stats = new XmlStats();
        var files = new List<string>();
        bool summary = true;
        bool logo = true;

        for (int i = 0; i < args.Length; i++)
        {
            string argument = args[i];
            if (argument.Length <= 1 || argument[0] != '-')
            {
                AddInput(files, argument);
                continue;
            }

            switch (argument.Trim('-').ToLowerInvariant())
            {
                case "?":
                case "h":
                case "help":
                    return PrintUsage();
                case "f":
                    if (i + 1 == args.Length)
                        return PrintUsage("missing file name after '-f' argument");
                    files = [.. File.ReadLines(args[++i])];
                    break;
                case "s":
                    if (i + 1 == args.Length)
                        return PrintUsage("missing name after '-s' argument");
                    stats.AddFilters(args[++i]);
                    break;
                case "v":
                    summary = false;
                    break;
                case "wn":
                    stats.Whitespace = WhitespaceHandling.None;
                    break;
                case "ws":
                    stats.Whitespace = WhitespaceHandling.Significant;
                    break;
                case "wa":
                    stats.Whitespace = WhitespaceHandling.All;
                    break;
                case "nologo":
                    logo = false;
                    break;
                default:
                    return PrintUsage($"invalid option '{argument}'");
            }
        }

        if (files.Count == 0)
            return PrintUsage();

        if (logo)
        {
            Console.WriteLine($"*** XmlStats {typeof(Program).Assembly.GetName().Version} by Chris Lovett and Andreas Lang");
            Console.WriteLine();
        }

        stats.ProcessFiles(files.ToArray(), summary, Console.Out, "\n");
        if (files.Count > 1)
            Console.WriteLine("*** XmlStats ended.");

        return 0;
    }

    private static void AddInput(List<string> files, string argument)
    {
        if (argument.IndexOf("://", StringComparison.Ordinal) <= 0 && (argument.Contains('*') || argument.Contains('?')))
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), argument);
            files.AddRange(Directory.GetFiles(Path.GetDirectoryName(path)!, Path.GetFileName(path)));
        }
        else
        {
            files.Add(argument);
        }
    }

    private static int PrintUsage(string? error = null)
    {
        if (error is not null)
            Console.WriteLine(error);

        Console.WriteLine("usage: XmlStats [options] <filenames>");
        Console.WriteLine("    reports statistics about elements, attributes and text found");
        Console.WriteLine("    in the specified XML files (URLs or local file names).");
        Console.WriteLine("    filenames: wildcards in local filenames allowed");
        Console.WriteLine("               (for reporting on all files in a directory)");
        Console.WriteLine("options:");
        Console.WriteLine("  -f filename Reports stats on files names found in the given file (one per line).");
        Console.WriteLine("  -v          Generates individual reports for all specified files (default is summary only).");
        Console.WriteLine("  -nologo     Removes logo from the report");
        Console.WriteLine("  -w[a|s|n]   XML whitespace handling: -wa=All (default), -ws=Significant, -wn=None");
        Console.WriteLine("  -s names    Report only on the contents of elements with the given comma separated names.");
        Console.WriteLine("              This will include everything inside the selected elements.");
        return 1;
    }
}
