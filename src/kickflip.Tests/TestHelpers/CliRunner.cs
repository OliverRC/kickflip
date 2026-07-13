using System.Diagnostics;

namespace kickflip.Tests.TestHelpers;

/// <summary>
/// Runs the compiled kickflip CLI as an external process so the whole app can
/// be exercised end-to-end (argument parsing, command wiring and handlers).
/// </summary>
public static class CliRunner
{
    public record Result(int ExitCode, string StandardOutput, string StandardError)
    {
        public string Output => StandardOutput + StandardError;
    }

    public static Result Run(params string[] arguments)
    {
        var assemblyPath = Path.Combine(AppContext.BaseDirectory, "kickflip.dll");
        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException($"Could not find the kickflip CLI assembly at {assemblyPath}");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        startInfo.ArgumentList.Add(assemblyPath);
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new Result(process.ExitCode, standardOutput, standardError);
    }
}
