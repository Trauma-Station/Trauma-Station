// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.JumpScare;
using Content.Shared.Electrocution;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Spawners;

namespace Content.Goobstation.Shared.Smites;

public sealed partial class ThunderstrikeSystem : EntitySystem
{
    [Dependency] private IFullScreenImageJumpscare _jumpscare = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPointLightSystem _light = default!;
    [Dependency] private SharedElectrocutionSystem _electrocution = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private static readonly EntProtoId Ash = "Ash";
    private const string Sound = "/Audio/_Goobstation/Effects/Smites/Thunderstrike/thunderstrike.ogg";
    private const string God = "/Textures/_Goobstation/For he does not need no fucking rsi.png";

    public void Smite(Entity<TransformComponent?> ent, bool kill = true, bool predicted = false, EntityUid? user = null)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        CreateLighting(ent.Comp.Coordinates, predicted: predicted, user: user);

        _electrocution.TryDoElectrocution(ent, null, 250, TimeSpan.FromSeconds(1), false, ignoreInsulation: true);

        if (!kill || !_player.TryGetSessionByEntity(ent, out var sesh))
            return;

        var text = new SpriteSpecifier.Texture(new ResPath(God));
        _jumpscare.Jumpscare(text, sesh);

        PredictedQueueDel(ent);
        PredictedSpawnAtPosition(Ash, ent.Comp.Coordinates);
        _popup.PopupEntity(Loc.GetString("admin-smite-turned-ash-other", ("name", ent)), ent, PopupType.LargeCaution);
    }

    public void CreateLighting(EntityCoordinates coordinates, int energy = 125, int radius = 15, bool predicted = false, EntityUid? user = null)
    {
        var ent = PredictedSpawnAtPosition(null, coordinates);
        var comp = _light.EnsureLight(ent);
        _light.SetColor(ent, new Color(255, 255, 255), comp);
        _light.SetEnergy(ent, energy, comp);
        _light.SetRadius(ent, radius, comp);

        var sound = new SoundPathSpecifier(Sound)
        {
            Params = AudioParams.Default.WithVolume(150f)
        };
        if (predicted)
            _audio.PlayPredicted(sound, coordinates, user);
        else
            _audio.PlayPvs(sound, coordinates);

        EnsureComp<TimedDespawnComponent>(ent).Lifetime = 0.125f;
    }
}
