using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Xml.Linq;

// --- Main Entry Point ---
var (searchDirectory, useClipboard) = ParseArguments(args);
if (string.IsNullOrEmpty(searchDirectory))
{
    return 1;
}

searchDirectory = ValidateAndResolveDirectory(searchDirectory);
if (string.IsNullOrEmpty(searchDirectory))
{
    return 1;
}

var output = GenerateBugReport(searchDirectory);

if (useClipboard)
{
    if (CopyToClipboard(output))
    {
        Console.WriteLine("Bug report copied to clipboard!");
        return 0;
    }
    else
    {
        Console.WriteLine("Warning: Failed to copy to clipboard. Writing to file instead...");
        var filePath = WriteBugReport(output, searchDirectory);
        Console.WriteLine($"Bug report written to: {filePath}");
        return 1;
    }
}
else
{
    var filePath = WriteBugReport(output, searchDirectory);
    Console.WriteLine($"Bug report written to: {filePath}");
    return 0;
}

// --- Argument Parsing ---
static (string? directory, bool useClipboard) ParseArguments(string[] args)
{
    string? searchDirectory = null;
    bool useClipboard = false;
    
    for (int i = 0; i < args.Length; i++)
    {
        var arg = args[i];
        if (arg == "-p" || arg == "-path" || arg == "--path")
        {
            if (i + 1 < args.Length)
            {
                searchDirectory = args[i + 1];
                i++; // Skip the next argument as it's the path value
            }
            else
            {
                Console.WriteLine("Error: Path parameter requires a value.");
                Console.WriteLine("Usage: dotnet-bugreport [-p|-path|--path <path>] [-c|-clipboard|--clipboard]");
                return (null, false);
            }
        }
        else if (arg == "-c" || arg == "-clipboard" || arg == "--clipboard")
        {
            useClipboard = true;
        }
        else if (!arg.StartsWith("-") && searchDirectory == null)
        {
            // Positional argument (backward compatibility)
            searchDirectory = arg;
        }
    }
    
    // Default to current directory if no path specified
    var directory = string.IsNullOrWhiteSpace(searchDirectory) 
        ? Directory.GetCurrentDirectory() 
        : searchDirectory;
    
    return (directory, useClipboard);
}

// --- Directory Validation ---
static string? ValidateAndResolveDirectory(string directory)
{
    var resolvedPath = Path.GetFullPath(directory);
    
    if (!Directory.Exists(resolvedPath))
    {
        Console.WriteLine($"Error: Directory does not exist: {resolvedPath}");
        return null;
    }
    
    return resolvedPath;
}

// --- Report Generation ---
static List<string> GenerateBugReport(string searchDirectory)
{
    var output = new List<string>();
    
    AddHeader(output);
    AddSystemInformation(output);
    AddDotNetInfo(output);
    AddGlobalJson(output, searchDirectory);
    AddProjectFiles(output, searchDirectory);
    AddSolutionFiles(output, searchDirectory);
    AddEnvironmentVariables(output);
    AddGitInformation(output, searchDirectory);
    AddContainerDetection(output);
    
    return output;
}

// --- Report Sections ---
static void AddHeader(List<string> output)
{
    output.Add("# .NET Bug Report");
    output.Add($"Generated: {DateTime.UtcNow:u}\n");
}

static void AddSystemInformation(List<string> output)
{
    output.Add("## System Information");
    output.Add($"OS: {RuntimeInformation.OSDescription}");
    output.Add($"OS Architecture: {RuntimeInformation.OSArchitecture}");
    output.Add($"Process Architecture: {RuntimeInformation.ProcessArchitecture}");
    output.Add($"Framework Description: {RuntimeInformation.FrameworkDescription}");
    output.Add($"Runtime Version: {Environment.Version}");
    output.Add("");
}

static void AddDotNetInfo(List<string> output)
{
    output.Add("### dotnet --info");
    output.Add("```");
    output.Add(RunAndCapture("dotnet", "--info"));
    output.Add("```");
    output.Add("");
}

static void AddGlobalJson(List<string> output, string searchDirectory)
{
    var globalJsonPath = Path.Combine(searchDirectory, "global.json");
    if (File.Exists(globalJsonPath))
    {
        output.Add("## global.json");
        output.Add("```json");
        output.Add(File.ReadAllText(globalJsonPath));
        output.Add("```");
        output.Add("");
    }
}

static void AddProjectFiles(List<string> output, string searchDirectory)
{
    var csprojFiles = FindProjectFiles(searchDirectory);
    
    if (csprojFiles.Count > 0)
    {
        output.Add("## Project Files");
        output.Add($"Found {csprojFiles.Count} project file(s):\n");
        
        foreach (var csprojFile in csprojFiles)
        {
            AddProjectFileInfo(output, csprojFile, searchDirectory);
        }
    }
    else
    {
        output.Add("## Project Files");
        output.Add("No .csproj files found in the current directory.");
        output.Add("");
    }
}

static void AddProjectFileInfo(List<string> output, string csprojFile, string searchDirectory)
{
    var relativePath = Path.GetRelativePath(searchDirectory, csprojFile);
    output.Add($"### {relativePath}");
    
    try
    {
        var doc = XDocument.Load(csprojFile);
        var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;
        
        var targetFrameworks = ExtractTargetFrameworks(doc, ns);
        if (targetFrameworks.Count > 0)
        {
            output.Add("**Target Framework(s):**");
            output.AddRange(targetFrameworks.Select(tfm => $"- {tfm}"));
            output.Add("");
        }
        
        var packageRefs = ExtractPackageReferences(doc, ns);
        if (packageRefs.Count > 0)
        {
            output.Add("**Package References:**");
            output.Add("```");
            output.AddRange(packageRefs.Select(pkg => $"{pkg.Include}: {pkg.Version}"));
            output.Add("```");
        }
        else
        {
            output.Add("**Package References:** None");
        }
        
        output.Add("");
    }
    catch (Exception ex)
    {
        output.Add($"Error parsing project file: {ex.Message}");
        output.Add("");
    }
}

static List<string> ExtractTargetFrameworks(XDocument doc, XNamespace ns)
{
    var targetFrameworks = doc.Descendants(ns + "TargetFrameworks")
        .SelectMany(e => e.Value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .ToList();
    
    if (targetFrameworks.Count == 0)
    {
        var targetFramework = doc.Descendants(ns + "TargetFramework")
            .Select(e => e.Value)
            .FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(targetFramework))
        {
            targetFrameworks.Add(targetFramework);
        }
    }
    
    return targetFrameworks;
}

static List<(string Include, string Version)> ExtractPackageReferences(XDocument doc, XNamespace ns)
{
    return doc.Descendants(ns + "PackageReference")
        .Select(pr => new
        {
            Include = pr.Attribute("Include")?.Value ?? "",
            Version = pr.Attribute("Version")?.Value ?? 
                     pr.Element(ns + "Version")?.Value ?? 
                     pr.Element(XName.Get("Version"))?.Value ?? "N/A"
        })
        .Where(pr => !string.IsNullOrWhiteSpace(pr.Include))
        .OrderBy(pr => pr.Include)
        .Select(pr => (pr.Include, pr.Version))
        .ToList();
}

static List<string> FindProjectFiles(string searchDirectory)
{
    return Directory.GetFiles(searchDirectory, "*.csproj", SearchOption.AllDirectories)
        .Where(f => !f.Contains("\\bin\\") && !f.Contains("/bin/") && 
                    !f.Contains("\\obj\\") && !f.Contains("/obj/"))
        .OrderBy(f => f)
        .ToList();
}

static void AddSolutionFiles(List<string> output, string searchDirectory)
{
    var slnFiles = Directory.GetFiles(searchDirectory, "*.sln", SearchOption.TopDirectoryOnly);
    if (slnFiles.Length > 0)
    {
        output.Add("## Solution Files");
        foreach (var sln in slnFiles)
        {
            output.Add($"- `{Path.GetFileName(sln)}`");
        }
        output.Add("");
    }
}

static void AddEnvironmentVariables(List<string> output)
{
    output.Add("## Environment Variables");
    var envPrefixes = new[] { "DOTNET_", "ASPNETCORE_", "MSBUILD_" };
    var relevantEnvVars = Environment.GetEnvironmentVariables()
        .Cast<DictionaryEntry>()
        .Where(e => envPrefixes.Any(prefix => e.Key.ToString()!.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        .OrderBy(e => e.Key.ToString())
        .ToList();
    
    if (relevantEnvVars.Count > 0)
    {
        output.Add("```text");
        foreach (var env in relevantEnvVars)
        {
            output.Add($"{env.Key} = {env.Value}");
        }
        output.Add("```");
    }
    else
    {
        output.Add("No relevant environment variables found.");
    }
    output.Add("");
}

static void AddGitInformation(List<string> output, string searchDirectory)
{
    if (!Directory.Exists(Path.Combine(searchDirectory, ".git")))
    {
        return;
    }
    
    output.Add("## Git Information");
    
    try
    {
        var branch = RunAndCapture("git", "rev-parse --abbrev-ref HEAD", searchDirectory).Trim();
        if (!string.IsNullOrWhiteSpace(branch))
        {
            output.Add($"**Branch:** {branch}");
        }
        
        var commit = RunAndCapture("git", "rev-parse HEAD", searchDirectory).Trim();
        if (!string.IsNullOrWhiteSpace(commit))
        {
            output.Add($"**Commit:** {commit}");
        }
        
        var remote = RunAndCapture("git", "remote get-url origin", searchDirectory).Trim();
        if (!string.IsNullOrWhiteSpace(remote))
        {
            output.Add($"**Remote:** {remote}");
        }
    }
    catch
    {
        output.Add("Unable to retrieve Git information.");
    }
    
    output.Add("");
}

static void AddContainerDetection(List<string> output)
{
    output.Add("## Container Detection");
    bool inDocker = File.Exists("/.dockerenv") ||
                    Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true" ||
                    Environment.GetEnvironmentVariable("container") != null;
    
    output.Add(inDocker ? "Running inside a container." : "Not inside a container.");
    output.Add("");
}

// --- File Operations ---
static string WriteBugReport(List<string> output, string searchDirectory)
{
    var fileName = $"bugreport-{DateTime.UtcNow:yyyyMMdd-HHmmss}.md";
    var filePath = Path.Combine(searchDirectory, fileName);
    File.WriteAllLines(filePath, output);
    return filePath;
}

// --- Clipboard Operations ---
static bool CopyToClipboard(List<string> output)
{
    var text = string.Join(Environment.NewLine, output);
    
    try
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOS: use pbcopy
            return CopyToClipboardMac(text);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows: use clip.exe
            return CopyToClipboardWindows(text);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Linux: try xclip first, then wl-copy (Wayland)
            return CopyToClipboardLinux(text);
        }
        else
        {
            Console.WriteLine("Unsupported platform for clipboard operations.");
            return false;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error copying to clipboard: {ex.Message}");
        return false;
    }
}

static bool CopyToClipboardMac(string text)
{
    try
    {
        var psi = new ProcessStartInfo("pbcopy")
        {
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        using var proc = Process.Start(psi);
        if (proc == null)
        {
            return false;
        }
        
        proc.StandardInput.Write(text);
        proc.StandardInput.Close();
        proc.WaitForExit();
        
        return proc.ExitCode == 0;
    }
    catch
    {
        return false;
    }
}

static bool CopyToClipboardWindows(string text)
{
    try
    {
        var psi = new ProcessStartInfo("clip.exe")
        {
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        using var proc = Process.Start(psi);
        if (proc == null)
        {
            return false;
        }
        
        proc.StandardInput.Write(text);
        proc.StandardInput.Close();
        proc.WaitForExit();
        
        return proc.ExitCode == 0;
    }
    catch
    {
        return false;
    }
}

static bool CopyToClipboardLinux(string text)
{
    // Try xclip first (X11)
    try
    {
        var psi = new ProcessStartInfo("xclip")
        {
            Arguments = "-selection clipboard",
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        using var proc = Process.Start(psi);
        if (proc != null)
        {
            proc.StandardInput.Write(text);
            proc.StandardInput.Close();
            proc.WaitForExit();
            if (proc.ExitCode == 0)
            {
                return true;
            }
        }
    }
    catch
    {
        // xclip not available, try wl-copy
    }
    
    // Try wl-copy (Wayland)
    try
    {
        var psi = new ProcessStartInfo("wl-copy")
        {
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        using var proc = Process.Start(psi);
        if (proc != null)
        {
            proc.StandardInput.Write(text);
            proc.StandardInput.Close();
            proc.WaitForExit();
            return proc.ExitCode == 0;
        }
    }
    catch
    {
        // Neither available
    }
    
    return false;
}

// --- Helper Functions ---
static string RunAndCapture(string file, string args, string? workingDirectory = null)
{
    try
    {
        var psi = new ProcessStartInfo(file, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        if (workingDirectory != null)
        {
            psi.WorkingDirectory = workingDirectory;
        }

        using var proc = Process.Start(psi);
        if (proc == null)
        {
            return "Failed to start process.";
        }
        
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();

        return stdout + (string.IsNullOrWhiteSpace(stderr) ? "" : ("\n[stderr]\n" + stderr));
    }
    catch (Exception ex)
    {
        return $"Error running command: {ex.Message}";
    }
}
