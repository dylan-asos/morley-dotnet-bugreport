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

---

## Installation

Install globally via the .NET CLI:

```bash
dotnet tool install -g dotnet-bugreport
```

## Usage

Run the tool from any directory containing .NET projects:

```bash
dotnet-bugreport
```

Or specify a directory to scan:

```bash
dotnet-bugreport /path/to/project
```

The tool will generate a `bugreport-YYYYMMDD-HHMMSS.md` file in the current directory (or specified directory) containing all the diagnostic information.
