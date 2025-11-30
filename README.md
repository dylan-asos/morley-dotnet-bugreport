# dotnet-bugreport

A command-line tool that collects useful diagnostic information about your .NET project, environment, and system.  
It generates a single Markdown file that can be pasted directly into GitHub issues or shared with maintainers.

`dotnet-bugreport` is designed to simplify debugging by automatically gathering:

- .NET runtime and SDK information (`dotnet --info`)
- Operating system and architecture details
- All `.csproj` files in a directory (recursively)
- Target framework versions from each project
- Package references and versions from each project
- `global.json` settings
- Relevant environment variables (DOTNET_*, ASPNETCORE_*, MSBUILD_*)
- Git branch, commit, and remote URL
- Container detection (Docker/Kubernetes)

The tool outputs a structured `bugreport-YYYYMMDD-HHMMSS.md` file in the working directory.

---

## Features

- **Zero dependencies** - Uses only built-in .NET APIs (no NuGet packages required)
- Unified diagnostic snapshot for .NET projects
- Recursively finds all `.csproj` files in a directory
- Automatically detects the current Git repository
- Produces clean Markdown suitable for GitHub Issues
- Zero configuration required
- Works cross-platform on Windows, macOS, and Linux
- Supports copying output directly to clipboard

---

## Installation

Install globally via the .NET CLI:

```bash
dotnet tool install -g dotnet-bugreport
```

---

## Usage

Run the tool from any directory containing .NET projects:

```bash
dotnet-bugreport
```

The tool will generate a `bugreport-YYYYMMDD-HHMMSS.md` file in the current directory containing all the diagnostic information.

---

## Parameters

| Parameter | Short | Description |
|-----------|-------|-------------|
| `--path <path>` | `-p` | Specifies the directory to scan for .NET projects. If not provided, the current directory is used. |
| `--clipboard` | `-c` | Copies the generated bug report directly to the system clipboard instead of writing to a file. Falls back to file output if clipboard access fails. |

### Examples

Scan the current directory and write the report to a file:

```bash
dotnet-bugreport
```

Scan a specific directory:

```bash
dotnet-bugreport --path /path/to/project
```

Or using the short form:

```bash
dotnet-bugreport -p /path/to/project
```

Copy the report directly to clipboard:

```bash
dotnet-bugreport --clipboard
```

Or using the short form:

```bash
dotnet-bugreport -c
```

Combine options to scan a specific directory and copy to clipboard:

```bash
dotnet-bugreport -p /path/to/project -c
```

---

## Output

The generated bug report includes the following sections:

1. **System Information** - OS, architecture, and runtime details
2. **dotnet --info** - Full output of the `dotnet --info` command
3. **global.json** - Contents of the global.json file if present
4. **Project Files** - List of all `.csproj` files with their target frameworks and package references
5. **Solution Files** - List of solution files in the root directory
6. **Environment Variables** - Relevant .NET, ASP.NET Core, and MSBuild environment variables
7. **Git Information** - Current branch, commit hash, and remote URL
8. **Container Detection** - Whether the tool is running inside a Docker/Kubernetes container

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
