// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityEffects;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Adds a tag to the target entity.
/// </summary>
public sealed partial class AddTag : EntityEffectBase<AddTag>
{
    /// <summary>
    /// Tag to add.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<TagPrototype> Tag;

    /// <summary>
    /// Text to use for the guidebook entry for reagents.
    /// </summary>
    [DataField(required: true)]
    public LocId GuidebookText;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString(GuidebookText, ("chance", Probability));
}

public sealed class AddTagEffectSystem : EntityEffectSystem<TagComponent, AddTag>
{
    [Dependency] private readonly TagSystem _tag = default!;

    protected override void Effect(Entity<TagComponent> ent, ref EntityEffectEvent<AddTag> args)
    {
        _tag.AddTag(ent, args.Effect.Tag);
    }
}
