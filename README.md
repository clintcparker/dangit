# Dangit
Dangit. As in, "Dangit. I wish there was a better way to manage dotnet global tools." 

Well now there is. Try... `dngt`

`d`ot`n`et `g`lobal `t`ool ....

## Why
I couldn't find an equivalent of Brewfile or Gemfile for the dotnet tool ecosystem. They support a manifest for local tools, but not global. So until they add this functionality, I'll be using `dngt`!


## What
**Dangit is a tool for managing global dotnet tools**

```
Description:
  Dangit is a tool for managing global dotnet tools

Usage:
  dngt [command] [options]

Options:
  --file <file>   The file to use for export and restore [default: $HOME/.config/.dotnet-tool-versions]
  --version       Show version information
  -?, -h, --help  Show help and usage information

Commands:
  export   Export the current global dotnet tools to a file
  restore  Restore the global dotnet tools from a file
  update   Update all of the global dotnet tools
  list     List global dotnet tools
```
### Installation
`dotnet tool install --global dangit`

### Usage

```
dngt export
```

#### Export
Keep track of the global tools you're using across machines and time! 
```
Description:
  Export the current global dotnet tools to a file

Usage:
  dngt export [options]

Options:
  --file <file>   The file to use for export and restore [default: $HOME/.config/.dotnet-tool-versions]
  -?, -h, --help  Show help and usage information
```


#### Restore
Get a new machine set up quickly!
```
Description:
  Restore the global dotnet tools from a file

Usage:
  dngt restore [options]

Options:
  --file <file>   The file to use for export and restore [default: $HOME/.config/.dotnet-tool-versions]
  -?, -h, --help  Show help and usage information
```

#### Update
You love the latest and greatest. Update all the tools you care about with a single command!
```
Description:
  Update all of the global dotnet tools

Usage:
  dngt update [options]

Options:
  --no-export     Do not export the current global dotnet tools to a file, defaults to false.
  --file <file>   The file to use for export and restore [default: $HOME/.config/.dotnet-tool-versions]
  -?, -h, --help  Show help and usage information
```
* Note: this will update the file by default



#### List
This is one is pretty simple, but allows for listing the contents of the file OR the `dotnet` CLI.
You can list the tools & versions from each soruce without switching between `cat` and `dotnet tool list`.

```
Description:
  List global dotnet tools

Usage:
  dngt list [options]

Options:
  --installed     List installed tools only, defaults to false.
  --file <file>   The file to use for export and restore [default: $HOME/.config/.dotnet-tool-versions]
  -?, -h, --help  Show help and usage information
```


#### Config file
The tool uses a file (defaulted to  `$HOME/.config/.dotnet-tool-versions`) to easily export and restore your setup. An alternate file can be used if needed.

The structure is really simple, just tool name and version, so the values can be passed back easily to the `dotnet tool` command behind the scenes. The file path is similar to the tool-manifest, so that's nice. 

## More

Why didn't you stick to the `/.config/dotnet-tools.json` manifest format?
> 1. JSON seemed overly cumbersome for this.
> 2. I didn't want to muddy the behavior of the standard/existing invocations. Especially the recursive searching of manifest files and the `isRoot` property. A different file name and structure avoids unnecessary collisions.

## Acknowledgements
This app leverages
- [dein ToolBox](https://github.com/deinsoftware/toolbox)
- [Spectre Console](https://github.com/spectreconsole/spectre.console)
