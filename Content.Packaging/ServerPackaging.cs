using System.Diagnostics;
using System.IO.Compression;
using Robust.Packaging;
using Robust.Packaging.AssetProcessing;
using Robust.Packaging.AssetProcessing.Passes;
using Robust.Packaging.Utility;
using Robust.Shared.Timing;

namespace Content.Packaging;

public static class ServerPackaging
{
    private static readonly List<PlatformReg> Platforms = new()
    {
        new PlatformReg("win-x64", "Windows", true),
        new PlatformReg("win-arm64", "Windows", true),
        new PlatformReg("linux-x64", "Linux", true),
        new PlatformReg("linux-arm64", "Linux", true),
        new PlatformReg("osx-x64", "MacOS", true),
        new PlatformReg("osx-arm64", "MacOS", true),
        // Non-default platforms (i.e. for Watchdog Git)
        new PlatformReg("freebsd-x64", "FreeBSD", false),
    };

    private static IReadOnlySet<string> ServerContentIgnoresResources { get; } = new HashSet<string>
    {
        "ServerInfo",
        "Changelog",
    };

    private static List<string> PlatformRids => Platforms
        .Select(o => o.Rid)
        .ToList();

    private static List<string> PlatformRidsDefault => Platforms
        .Where(o => o.BuildByDefault)
        .Select(o => o.Rid)
        .ToList();

    private static readonly List<string> ServerNotExtraAssemblies = new()
    {
        "JetBrains.Annotations",
    };

    private static readonly HashSet<string> BinSkipFolders = new()
    {
        // Roslyn localization files, screw em.
        "cs",
        "de",
        "es",
        "fr",
        "it",
        "ja",
        "ko",
        "pl",
        "pt-BR",
        "ru",
        "tr",
        "zh-Hans",
        "zh-Hant",
        "server_config.toml" // RT config (use our Content-facing one)
    };

    public static async Task PackageServer(bool skipBuild, bool hybridAcz, bool logBuild, IPackageLogger logger, string configuration, List<string>? platforms = null)
    {
        if (platforms == null)
        {
            platforms ??= PlatformRidsDefault;
        }

        if (hybridAcz)
        {
            // Hybrid ACZ involves a file "Content.Client.zip" in the server executable directory.
            // Rather than hosting the client ZIP on the watchdog or on a separate server,
            //  Hybrid ACZ uses the ACZ hosting functionality to host it as part of the status host,
            //  which means that features such as automatic UPnP forwarding still work properly.
            await ClientPackaging.PackageClient(skipBuild, logBuild, configuration, logger);
        }

        // Good variable naming right here.
        foreach (var platform in Platforms)
        {
            if (!platforms.Contains(platform.Rid))
                continue;

            await BuildPlatform(platform, skipBuild, hybridAcz, logBuild, configuration, logger);
        }
    }

    private static async Task BuildPlatform(PlatformReg platform,
        bool skipBuild,
        bool hybridAcz,
        bool logBuild,
        string configuration,
        IPackageLogger logger)
    {
        logger.Info($"Building project for {platform.TargetOs}...");

        if (!skipBuild)
        {
            // <Trauma> - replaced copypaste with module helper method
            await ModulePackaging.BuildModules("Server", configuration, logBuild, platform.TargetOs);
            // </Trauma>

            await PublishClientServer(platform.Rid, platform.TargetOs, configuration);
        }

        logger.Info($"Packaging {platform.Rid} server...");

        var sw = RStopwatch.StartNew();
        {
            await using var zipFile =
                File.Open(Path.Combine("release", $"SS14.Server_{platform.Rid}.zip"), FileMode.Create, FileAccess.ReadWrite);
            using var zip = new ZipArchive(zipFile, ZipArchiveMode.Update);
            var writer = new AssetPassZipWriter(zip);

            await WriteServerResources(platform, "", writer, logger, hybridAcz, default);
            await writer.FinishedTask;
        }

        logger.Info($"Finished packaging server in {sw.Elapsed}");
    }

    private static async Task PublishClientServer(string runtime, string targetOs, string configuration)
    {
        await ProcessHelpers.RunCheck(new ProcessStartInfo
        {
            FileName = "dotnet",
            ArgumentList =
            {
                "publish",
                "--runtime", runtime,
                "--no-self-contained",
                "-c", configuration,
                $"/p:TargetOs={targetOs}",
                "/p:FullRelease=True",
                "/m",
                "RobustToolbox/Robust.Server/Robust.Server.csproj"
            }
        });
    }

    private static async Task WriteServerResources(
        PlatformReg platform,
        string contentDir,
        AssetPass pass,
        IPackageLogger logger,
        bool hybridAcz,
        CancellationToken cancel)
    {
        var graph = new RobustServerAssetGraph();
        var passes = graph.AllPasses.ToList();

        pass.Dependencies.Add(new AssetPassDependency(graph.Output.Name));

        // Include a TOML config file - include the ss14 one from Resources if possible, using the RT one as a fallback.
        var toml = Path.Combine(contentDir, "Resources", "ConfigPresets", "server_config.toml");
        var robustToml = Path.Combine("RobustToolbox", "bin", "Server", platform.Rid, "publish", "server_config.toml");
        pass.InjectFileFromDisk("server_config.toml", File.Exists(toml) ? toml : robustToml);

        passes.Add(pass);

        AssetGraph.CalculateGraph(passes, logger);

        var inputPassCore = graph.InputCore;
        var inputPassResources = graph.InputResources;

        // Additional assemblies that need to be copied such as EFCore.
        var sourcePath = Path.Combine(contentDir, "bin", "Content.Server");

        // <Trauma> - use helper for all modules not just Content.Server
        var contentAssemblies = ModulePackaging.GetContentAssemblyNamesToCopy(sourcePath, "Server");
        logger.Info($"{contentAssemblies.Count} assemblies packaged:");
        foreach (var name in contentAssemblies)
        {
            logger.Info($"- {name}");
        }
        // </Trauma>

        await RobustSharedPackaging.DoResourceCopy(
            Path.Combine("RobustToolbox", "bin", "Server",
            platform.Rid,
            "publish"),
            inputPassCore,
            BinSkipFolders,
            cancel: cancel);

        await RobustSharedPackaging.WriteContentAssemblies(
            inputPassResources,
            contentDir,
            "Content.Server",
            contentAssemblies,
            cancel: cancel);

        await RobustServerPackaging.WriteServerResources(
            contentDir,
            inputPassResources,
            ServerContentIgnoresResources.Concat(SharedPackaging.AdditionalIgnoredResources).ToHashSet(),
            cancel);

        if (hybridAcz)
        {
            inputPassCore.InjectFileFromDisk("Content.Client.zip", Path.Combine("release", "SS14.Client.zip"));
        }

        inputPassCore.InjectFinished();
        inputPassResources.InjectFinished();
    }

    // Trauma - replaced GetContentAssemblyNamesToCopy with ModulePackaging versions

    private readonly record struct PlatformReg(string Rid, string TargetOs, bool BuildByDefault);
}
