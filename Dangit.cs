using System.CommandLine;
using Spectre.Console;
using Microsoft.DotNet.CommandFactory;

namespace Dangit;

public class Dangit
{
    private bool _debug = false;
    private string _description = "Dangit is a tool for managing dotnet global tools";
    private Func<string> _getDefaultFile = () => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", ".dotnet-tool-versions");

    public int Run(string[] args)
    {
        RootCommand rootCommand = new RootCommand(_description);
        var fileOption = new Option<string>("--file", _getDefaultFile, "The file to use for export and restore");
        fileOption.LegalFilePathsOnly();
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
        rootCommand.AddGlobalOption(DebugOption());
        return rootCommand.Invoke(args);
    }

    public void Export(string file)
    {
        List<ToolVersion> toolVersions = new List<ToolVersion>();
        for (int i = 0; i < 5; i++)
        {
            toolVersions = GetInstalledTools();
            if (toolVersions.Count > 0)
            {
                break;
            }
        }
        if (toolVersions.Count == 0)
        {
            PrintError("[yellow on red]No tools found[/]");
            return;
        }
        try
        {
            var path = Path.GetFullPath(file);
            if (!Path.Exists(path) && file == _getDefaultFile())
            {
                AnsiConsole.MarkupLine($"Creating file: {path}");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            var packageWidth = toolVersions.Max(x => x.PackageId.Length) + 5;
            File.WriteAllLines(path, toolVersions.Select(x => $"{x.PackageId.PadRight(packageWidth)}\t{x.Version}"));
        }
        catch (Exception e)
        {
            PrintError($"[yellow on red]Error writing to file: {file}{Environment.NewLine}{Environment.NewLine}{e.Message}[/]");
        }
    }

    public void Restore(string file)
    {
        var _notificationSystem = ToolBox.Notification.NotificationSystem.Default;
        List<ToolVersion> toolVersions = GetToolsFromFile(file);
        foreach (var toolVersion in toolVersions)
        {
            var r = new CommandFactory().Create("dotnet", new string[] { "tool", "install", toolVersion.PackageId, "--version", toolVersion.Version, "--global" }).Execute();
        }
    }

    public void Update(bool noExport, string file)
    {
        if (!AnsiConsole.Confirm("Are you sure you want to update all of your global dotnet tools?"))
        {
            Console.WriteLine("Aborting");
            return;
        }
        List<ToolVersion> toolVersions = GetInstalledTools();
        foreach (var toolVersion in toolVersions)
        {
            var r = new CommandFactory().Create("dotnet", new string[] { "tool", "update", toolVersion.PackageId, "--global" }).Execute();
            if (!String.IsNullOrWhiteSpace(r.StdOut))
            {
                AnsiConsole.MarkupLine(r.StdOut.Trim());
            }
        }
        if (!noExport)
        {
            Export(file);
        }
    }

    public void List(bool installed, string file)
    {
        List<ToolVersion> toolVersions = installed ? GetInstalledTools() : GetToolsFromFile(file);
        PrintTools(toolVersions);
    }

    private List<ToolVersion> GetToolsFromFile(string file)
    {
        if (!Path.Exists(file))
        {
            PrintError($"[yellow on red]File not found: {file}[/]");
            return new List<ToolVersion>();
        }
        var lines = File.ReadAllLines(file);
        return ParseTools(lines);
    }

    private List<ToolVersion> GetInstalledTools()
    {
        var command = new CommandFactory().Create("dotnet", new string[] { "tool", "list", "--global" });
        command.CaptureStdOut();
        var r = command.Execute();
        var lines = r.StdOut.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Skip(2).ToArray();
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
        if (toolVersions.Count == 0)
        {
            PrintError("[yellow on red]No tools found[/]");
            return;
        }
        var table = new Table();

        var packageColumn = new TableColumn("Package Id");
        packageColumn.Alignment = Justify.Left;
        packageColumn.Width = toolVersions.Max(x => x.PackageId.Length) + 5;
        packageColumn.NoWrap = true;
        packageColumn.PadRight(-1);
        table.AddColumn(packageColumn);

        var versionColumn = new TableColumn("Version");
        versionColumn.Alignment = Justify.Left;
        versionColumn.Width = toolVersions.Max(x => x.Version.Length);
        versionColumn.NoWrap = true;
        table.AddColumn(versionColumn);

        string[] dashes = table.Columns.Select(x => new string('-', x.Width.Value)).ToArray();
        table.AddRow(dashes);
        table.ShowHeaders = true;
        table.NoBorder();
        toolVersions.ForEach(x => table.AddRow(x.PackageId, x.Version));
        AnsiConsole.Write(table);
    }

    private void PrintError(string message, string ErrorId = "")
    {
        if (!string.IsNullOrEmpty(ErrorId))
        {
            ErrorId = $"{ErrorId}: ";
        }
        message = message.Trim();
        if (!string.IsNullOrEmpty(message))
        {
            AnsiConsole.MarkupLine($"[yellow on red]{ErrorId}{message}[/]");
        }
    }

    private Option<bool> DebugOption()
    {
        var debug = new Option<bool>("--debug", "Enable debug logging, defaults to false.");
        debug.AddValidator(result =>
        {
            _debug = result.GetValueOrDefault<bool>();
        });
        debug.IsHidden = true;
        return debug;
    }
}