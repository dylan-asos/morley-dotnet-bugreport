using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Dotnet.BugReport;

public static class ClipboardService
{
    public static bool CopyToClipboard(List<string> output)
    {
        var text = string.Join(Environment.NewLine, output);
        
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // macOS: use pbcopy
                return CopyToClipboardMac(text);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: use clip.exe
                return CopyToClipboardWindows(text);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Linux: try xclip first, then wl-copy (Wayland)
                return CopyToClipboardLinux(text);
            }

            Console.WriteLine("Unsupported platform for clipboard operations.");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error copying to clipboard: {ex.Message}");
            return false;
        }
    }

    private static bool CopyToClipboardMac(string text)
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

    private static bool CopyToClipboardWindows(string text)
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

    private static bool CopyToClipboardLinux(string text)
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
}