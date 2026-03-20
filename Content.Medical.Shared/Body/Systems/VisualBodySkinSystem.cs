// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.Body;

/// <summary>
/// Makes e.g. lizard tails and snouts follow skin color properly.
/// </summary>
public sealed class VisualBodySkinSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VisualOrganMarkingsComponent, BodyRelayedEvent<ApplyOrganProfileDataEvent>>(OnApplyOrganProfile);
    }

    private void OnApplyOrganProfile(Entity<VisualOrganMarkingsComponent> ent, ref BodyRelayedEvent<ApplyOrganProfileDataEvent> args)
    {
        if (args.Args.Base?.SkinColor is not {} color)
            return;

        var appearances = _proto.Index(ent.Comp.MarkingData.Group).Appearances;

        var markings = ent.Comp.Markings;
        foreach (var (layer, list) in markings)
        {
            if (!appearances.TryGetValue(layer, out var app) || !app.MatchSkin)
                continue;

            // set all markings that have to match skin to the skin color
            for (int i = 0; i < list.Count; i++)
            {
                list[i] = list[i].WithColor(color);
            }
        }
        DirtyField(ent, ent.Comp, nameof(VisualOrganMarkingsComponent.Markings));
    }
}
