// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.StationRadio.Components;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Utility;
using Content.Shared.Audio.Jukebox;
using Robust.Client.Audio;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.IoC;
using Robust.Shared.Utility;
using System.Collections.Generic;

namespace Content.IntegrationTests.Tests._Trauma;

/// <summary>
/// Checks that sounds which are guaranteed to always be played positionally have 1 channel.
/// Currently this is jukebox audio and station radio vinyls.
/// </summary>
public sealed class MonoSoundsTests : GameTest
{
    private static string[] _vinyls = GameDataScrounger.EntitiesWithComponent("Vinyl");

    [Test]
    public async Task AllPositionalSoundsMono()
    {
        // NFI why i have to do this but sure, loading resources completely fails without it
        IoCManager.InitThread(Client.InstanceDependencyCollection);
        var proto = Client.ProtoMan;
        var cache = Client.ResolveDependency<IResourceCache>();
        var failed = new List<string>();
        void AssertMono(ResPath path)
        {
            if (!cache.TryGetResource<AudioResource>(path, out var audio))
            {
                failed.Add($"Failed to read sound file from {path}");
                return;
            }

            if (audio.AudioStream.ChannelCount > 1)
                failed.Add($"Found stereo positional sound file at {path}, convert it to mono");
        }

        foreach (var jukebox in proto.EnumeratePrototypes<JukeboxPrototype>())
        {
            AssertMono(jukebox.Path.Path);
        }

        foreach (var id in _vinyls)
        {
            var ent = proto.Index(id);
            ent.TryGetComponent<VinylComponent>("Vinyl", out var vinyl); // scrounger shouldnt return it if it's missing
            if (vinyl?.Song is SoundPathSpecifier sound)
                AssertMono(sound.Path);
            else
                failed.Add($"Non-path specifier {vinyl?.Song} for vinyl {id} is not supported by this test!");
        }

        if (failed.Count > 0)
            Assert.Fail(string.Join('\n', failed));
    }
}
