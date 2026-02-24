// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Sprite;
using Content.Trauma.Common.Sprite;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Sprite;

/// <summary>
/// Applies random sprite colour to a mob's limbs.
/// </summary>
public sealed class RandomSpriteBodySystem : EntitySystem
{
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomSpriteComponent, RandomSpriteChangedEvent>(OnSpriteChanged);
    }

    private void OnSpriteChanged(Entity<RandomSpriteComponent> ent, ref RandomSpriteChangedEvent args)
    {
        if (GetAnyColor(ent.Comp) is not {} color)
            return;

        DebugTools.Assert(!HasComp<HumanoidProfileComponent>(ent),
            $"{ToPrettyString(ent)} has both RandomSprite and HumanoidProfile, random sprite would conflict with humanoid visuals!");

        _visualBody.ApplyProfile(ent.Owner, new OrganProfileData()
        {
            SkinColor = color
        });
    }

    // jesus christ the carp random color system is overengineered
    private Color? GetAnyColor(RandomSpriteComponent comp)
    {
        foreach (var pair in comp.Selected.Values)
        {
            if (pair.Color is {} color)
                return color;
        }

        return null;
    }
}
