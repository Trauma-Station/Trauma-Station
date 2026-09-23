// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Antag;
using Content.Shared.Ghost;
using Content.Shared.Localizations;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Content.Trauma.Shared.BloodCult;
using Content.Trauma.Shared.BloodCult.Components;
using Content.Trauma.Shared.BloodCult.Constructs;
using Robust.Shared.Player;
using Robust.Shared.Random;
using static Content.Client.CharacterInfo.CharacterInfoSystem;

namespace Content.Trauma.Client.BloodCult;

public sealed partial class ClientBloodCultSystem : BloodCultSystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private static readonly ProtoId<FactionIconPrototype> CultistIcon = "BloodCultMember";
    private static readonly ProtoId<FactionIconPrototype> LeaderIcon = "BloodCultLeader";

    [SubscribeLocalEvent]
    private void OnPentagramAdded(EntityUid uid, PentagramComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || sprite.LayerMapTryGet(PentagramKey.Key, out _))
            return;

        var adj = sprite.Bounds.Height / 2 + 1.0f / 32 * 10.0f;

        var randomState = _random.Pick(component.States);

        var layer = _sprite.AddLayer((uid, sprite), new SpriteSpecifier.Rsi(component.RsiPath, randomState));

        _sprite.LayerMapSet((uid, sprite), PentagramKey.Key, layer);
        _sprite.LayerSetOffset((uid, sprite), layer, new Vector2(0.0f, adj));
    }

    [SubscribeLocalEvent]
    private void OnPentagramRemoved(EntityUid uid, PentagramComponent component, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !sprite.LayerMapTryGet(PentagramKey.Key, out var layer))
            return;

        _sprite.RemoveLayer((uid, sprite), layer);
    }

    [SubscribeLocalEvent]
    private void OnGetStatusIcons(Entity<BloodCultMemberComponent> ent, ref GetStatusIconsEvent args)
    {
        var id = HasComp<BloodCultLeaderComponent>(ent)
            ? LeaderIcon
            : CultistIcon;
        if (ProtoMan.Resolve(id, out var icon))
            args.StatusIcons.Add(icon);
    }

    [SubscribeLocalEvent]
    private void OnGetCharacterInfoControls(ref GetCharacterInfoControlsEvent args)
    {
        if (_player.LocalEntity is not { } mob ||
            GetRule(mob) is not { } rule)
            return;

        var areasList = new List<string>();
        foreach (var id in rule.Comp.RitualAreas)
        {
            areasList.Add(ProtoMan.Index(id).Name);
        }

        var areas = ContentLocalizationManager.FormatList(areasList);
        var label = new Label()
        {
            Text = $"The veil is thin in these special ritual sites:\n{areas}"
        };
        args.Controls.Add(label);
    }
}
