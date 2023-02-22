using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.CommandLine;
using System.CommandLine.IO;
using Spectre.Console;
using System.Text.RegularExpressions;

namespace Maple;

internal class Program
{

    static int Main(string[] args)
    {
        return new Maple().Run(args);
    }
}

public class Maple
{
    private string _description = "Maple is a tool for managing global dotnet tools";

    private Func<string> _getDefaultFile = () => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", ".dotnet-tool-versions");
    private RootCommand BuildCommand()
    {
        RootCommand rootCommand = new RootCommand(_description);
        var fileOption = new Option<string>("--file", _getDefaultFile , "The file to use for export and restore");
        rootCommand.AddGlobalOption(fileOption);
        var export = new Command("export", "Export the current global dotnet tools to a file");
        export.SetHandler(Export, fileOption);
        rootCommand.Add(export);
        var restore = new Command("restore", "Restore the global dotnet tools from a file");
        restore.SetHandler(Restore, fileOption);
        rootCommand.Add(restore);
        var update = new Command("update", "Update all of the global dotnet tools");
        var noExportOption = new Option<bool>("--no-export", "Do not export the current global dotnet tools to a file, defaults to false.");
        update.AddOption(noExportOption);
        update.SetHandler(Update, noExportOption, fileOption);
        rootCommand.Add(update);
        var list = new Command("list", "List global dotnet tools");
        var listInstalledOption = new Option<bool>("--installed", "List installed tools only, defaults to false.");
        list.AddOption(listInstalledOption);
        list.SetHandler(List, listInstalledOption, fileOption);
        rootCommand.Add(list);
        return rootCommand;
    }

    public void Export(string file)
    {
        var toolVersions = GetToolsFromCLI();
        File.WriteAllLines(file, toolVersions.Select(x => $"{x.PackageId}\t\t{x.Version}"));
    }

    public void Restore(string file)
    {
        List<ToolVersion> toolVersions = GetToolsFromFile(file);
        foreach (var toolVersion in toolVersions)
        {
            var r = Shell.Term($"dotnet tool install {toolVersion.PackageId} --version {toolVersion.Version} --global");
            Console.WriteLine(r.stdout);
        }
    }

    public void Update(bool noExport, string file)
    {
        if (AnsiConsole.Ask("Are you sure you want to update all of your global dotnet tools? (y/n)", "y") != "y") 
        {
            Console.WriteLine("Aborting");
            return;
        }
        AnsiConsole.Write("Updating...");
        List<ToolVersion> toolVersions = GetToolsFromCLI();
        foreach (var toolVersion in toolVersions)
        {
            var r = Shell.Term($"dotnet tool update {toolVersion.PackageId} --global");
            Console.WriteLine(r.stdout);
        }
        if (!noExport)
        {
            Export(file);
        }
    }

    internal void List(bool installed, string file)
    {

        List<ToolVersion> toolVersions = installed ? GetToolsFromCLI() : GetToolsFromFile(file);
        //write the tools to the console as a table
        PrintTools(toolVersions);
    }

    private List<ToolVersion> GetToolsFromFile(string file)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", ".dotnet-tool-versions");
        var lines = File.ReadAllLines(path);
        return ParseTools(lines);
    }

    private List<ToolVersion> GetToolsFromCLI()
    {
        var r = Shell.Term("dotnet tool list --global");
        var lines = r.stdout.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Skip(2).ToArray();
        return ParseTools(lines);
    }

    private List<ToolVersion> ParseTools(string[] lines)
    {
        var toolVersions = new List<ToolVersion>();
        foreach (var line in lines)
        {
            var splitLine = line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
            var packageId = splitLine[0];
            var version = splitLine[1];
            toolVersions.Add(new ToolVersion { PackageId = packageId, Version = version });
        }
        return toolVersions;
    }

    private void PrintTools(List<ToolVersion> toolVersions)
    {
        //write the tools to the console as a table
        var table = new Table();
        var packageColumn = new TableColumn("Package Id");
        packageColumn.Alignment = Justify.Left;
        var packageColumnWidth = toolVersions.Max(x => x.PackageId.Length) + 5;
        packageColumn.Width = packageColumnWidth;
        packageColumn.NoWrap = true;
        packageColumn.PadRight(-1);
        table.AddColumn(packageColumn);

        var versionColumn = new TableColumn("Version");
        versionColumn.Alignment = Justify.Left;
        var versionColumnWidth = toolVersions.Max(x => x.Version.Length);
        versionColumn.Width = versionColumnWidth;
        versionColumn.NoWrap = true;
        //versionColumn.PadRight(-1);
        table.AddColumn(versionColumn);
        string[] dashes = table.Columns.Select(x => new string('-', x.Width.Value)).ToArray();
        table.AddRow(dashes);
        table.ShowHeaders = true;
        table.NoBorder();
        foreach (var toolVersion in toolVersions)
        {
            table.AddRow(toolVersion.PackageId, toolVersion.Version);
        }
        AnsiConsole.Write(table);
    }

    public int Run(string[] args)
    {
        var rootCommand = BuildCommand();
        //if no args are passed, show help
        // if (args.Length == 0)
        // {
        //     rootCommand.Invoke("--help");
        //     return 0;
        // }
        return rootCommand.Invoke(args);
    }
}

public class ToolVersion
{
    public string PackageId { get; set; }
    public string Version { get; set; }
}


public static class LocalTableExtensions
{
    public static void AddEmptyRow(this Table table, char c)
    {
        string[] chars = table.Columns.Select(x => new string('-', x.Width.Value)).ToArray();
    }
}


public static class Shell
{
    public class Response
    {
        public int code { get; set; }
        public string stdout { get; set; }
        public string stderr { get; set; }
    }

    public enum Output
    {
        Hidden,
        Internal,
        External
    }
    public static class OS
    {
        public static bool IsWin() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static bool IsMac() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        public static bool IsGnu() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        public static string GetCurrent()
        {
            return
            (IsWin() ? "win" : null) ??
            (IsMac() ? "mac" : null) ??
            (IsGnu() ? "gnu" : null) ??
            String.Empty;
        }
    }
    private static IConsole _console = new SystemConsole();
    private static string GetFileName()
    {
        string fileName = "";
        try
        {
            switch (OS.GetCurrent())
            {
                case "win":
                    fileName = "cmd.exe";
                    break;
                case "mac":
                case "gnu":
                    fileName = "/bin/bash";
                    break;
            }
        }
        catch (Exception Ex)
        {
            Console.WriteLine(Ex.Message);
        }
        return fileName;
    }


    private static string CommandConstructor(string cmd, Output? output = Output.Hidden, string dir = "")
    {
        try
        {
            switch (OS.GetCurrent())
            {
                case "win":
                    if (!String.IsNullOrEmpty(dir))
                    {
                        dir = $" \"{dir}\"";
                    }
                    if (output == Output.External)
                    {
                        cmd = $"{Directory.GetCurrentDirectory()}/cmd.win.bat \"{cmd}\"{dir}";
                    }
                    cmd = $"/c \"{cmd}\"";
                    break;
                case "mac":
                case "gnu":
                    if (!String.IsNullOrEmpty(dir))
                    {
                        dir = $" '{dir}'";
                    }
                    if (output == Output.External)
                    {
                        cmd = $"sh {Directory.GetCurrentDirectory()}/cmd.mac.sh '{cmd}'{dir}";
                    }
                    cmd = $"-c \"{cmd}\"";
                    break;
            }
        }
        catch (Exception Ex)
        {
            Console.WriteLine(Ex.Message);
        }
        return cmd;
    }

    public static Response Term(string cmd, Output? output = Output.Hidden, string dir = "")
    {
        var result = new Response();
        var stderr = new StringBuilder();
        var stdout = new StringBuilder();
        var _console = new SystemConsole();
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = GetFileName();
            startInfo.Arguments = CommandConstructor(cmd, output, dir);
            startInfo.RedirectStandardOutput = !(output == Output.External);
            startInfo.RedirectStandardError = !(output == Output.External);
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = !(output == Output.External);
            if (!String.IsNullOrEmpty(dir) && output != Output.External)
            {
                startInfo.WorkingDirectory = dir;
            }

            using (Process process = Process.Start(startInfo))
            {
                switch (output)
                {
                    case Output.Internal:

                        while (!process.StandardOutput.EndOfStream)
                        {
                            string line = process.StandardOutput.ReadLine();
                            stdout.AppendLine(line);
                            Console.WriteLine(line);
                        }

                        while (!process.StandardError.EndOfStream)
                        {
                            string line = process.StandardError.ReadLine();
                            stderr.AppendLine(line);
                            Console.WriteLine(line);
                        }
                        break;
                    case Output.Hidden:
                        stdout.AppendLine(process.StandardOutput.ReadToEnd());
                        stderr.AppendLine(process.StandardError.ReadToEnd());
                        break;
                }
                process.WaitForExit();
                result.stdout = stdout.ToString();
                result.stderr = stderr.ToString();
                result.code = process.ExitCode;
            }
        }
        catch (Exception Ex)
        {
            Console.WriteLine(Ex.Message);
        }
        return result;
    }
}
