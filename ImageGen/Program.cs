using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ImageGen;

class Program
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleOutputCP(uint wCodePageID);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleCP(uint wCodePageID);

    static int Main(string[] args)
    {
        try
        {
            SetConsoleOutputCP(65001);
            SetConsoleCP(65001);
            Console.OutputEncoding = Encoding.UTF8;
        }
        catch { }

        string explicitConfigPath = null;
        for (int i = 0; i < args.Length; i++)
        {
            if ((args[i] == "-c" || args[i] == "--config") && i + 1 < args.Length)
            {
                explicitConfigPath = args[++i];
            }
        }

        var (profiles, resolvedPath) = ConfigLoader.LoadProfiles(explicitConfigPath);
        string baseDir = resolvedPath != null ? Path.GetDirectoryName(resolvedPath) : Directory.GetCurrentDirectory();

        for (int i = 0; i < args.Length; i++)
        {
            if (profiles.Count > 0)
            {
                var p = profiles[0];
                if (args[i] == "-i" && i + 1 < args.Length) p.InputRoot = args[++i];
                else if (args[i] == "-o" && i + 1 < args.Length) p.OutputRoot = args[++i];
                else if (args[i] == "-name" && i + 1 < args.Length) p.AtlasName = args[++i];
                else if (args[i] == "--force" || args[i] == "-f") p.Force = true;
                else if (args[i] == "-pad" && i + 1 < args.Length && int.TryParse(args[++i], out int pad)) p.Padding = pad;
            }
        }

        bool allSuccess = true;
        foreach (var profile in profiles)
        {
            if (!profile.Enabled)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"\n⏭️  Skipping disabled profile: [{profile.Name}]");
                Console.ResetColor();
                continue;
            }

            var packer = new AtlasPacker(profile, baseDir);
            if (!packer.Execute())
            {
                allSuccess = false;
            }
        }

        return allSuccess ? 0 : 1;
    }
}