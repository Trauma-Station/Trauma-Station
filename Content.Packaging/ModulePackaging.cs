// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Packaging.Utility;
using System.Diagnostics;

namespace Content.Packaging;

/// <summary>
/// Trauma - helpers for packaging modules
/// </summary>
public static class ModulePackaging
{
    /// <summary>
    /// All independent modules which need to be compiled individually.
    /// </summary>
    public static readonly string[] AllModules = [ "Factory", "Lavaland", "Medical", "Trauma" ];

    /// <summary>
    /// Build every module for the server or client.
    /// </summary>
    public static async Task BuildModules(string side, string configuration, bool logBuild, string? targetOs = null)
    {
        var logArg = $"/bl:{Path.Combine("release", $"{side.ToLowerInvariant()}.binlog")}";
        foreach (var module in AllModules)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                ArgumentList =
                {
                    "build",
                    Path.Combine($"Content.{module}.{side}", $"Content.{module}.{side}.csproj"),
                    "-c", configuration,
                    "--nologo",
                    "/v:m",
                    "/p:FullRelease=true",
                    "/m"
                }
            };
            if (targetOs != null)
                startInfo.ArgumentList.Add($"/p:TargetOs={targetOs}");

            if (logBuild)
            {
                startInfo.ArgumentList.Add(logArg);
                startInfo.ArgumentList.Add("/p:ReportAnalyzer=true");
            }

            await ProcessHelpers.RunCheck(startInfo);
        }
    }

    public static IEnumerable<string> GetContentAssemblyNamesToCopy(DepsHandler deps, string module, string side)
    {
        var depsContent = deps.RecursiveGetLibrariesFrom($"Content.{module}.{side}").SelectMany(GetLibraryNames);
        var depsRobust = deps.RecursiveGetLibrariesFrom($"Robust.{side}").SelectMany(GetLibraryNames);

        var depsContentExclusive = depsContent.Except(depsRobust).ToHashSet();

        // Remove .dll suffix and apply filtering.
        var names = depsContentExclusive.Select(p => p[..^4]).Where(p => !p.StartsWith("JetBrains.Annotations"));

        return names;

        IEnumerable<string> GetLibraryNames(string library) => deps.Libraries[library].GetDllNames();
    }

    public static HashSet<string> GetContentAssemblyNamesToCopy(string sourcePath, string side)
    {
        // hashset so no duplicates
        var names = new HashSet<string>();
        foreach (var module in AllModules)
        {
            var deps = DepsHandler.Load(Path.Combine(sourcePath, $"Content.{module}.{side}.deps.json"));
            var added = GetContentAssemblyNamesToCopy(deps, module, side);
            names.UnionWith(added);
        }
        return names;
    }
}
