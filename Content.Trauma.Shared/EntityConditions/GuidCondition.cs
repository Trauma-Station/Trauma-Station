// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Robust.Shared.Player;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Checks the target player for a specific GUID.
/// </summary>
public sealed partial class GuidCondition : EntityConditionBase<GuidCondition>
{
    // TODO: validate this shit in linter
    [DataField(required: true)]
    public string Guid;

    internal Guid? _guid;

    public override string EntityConditionGuidebookText(IPrototypeManager proto)
        => string.Empty;
}

public sealed partial class GuidConditionSystem : EntityConditionSystem<ActorComponent, GuidCondition>
{
    protected override void Condition(Entity<ActorComponent> ent, ref EntityConditionEvent<GuidCondition> args)
    {
        var c = args.Condition;
        try
        {
            c._guid ??= new Guid(c.Guid);
        }
        catch
        {
            Log.Error($"GuidCondition has invalid guid {c.Guid}!");
            return;
        }

        args.Result = ent.Comp.PlayerSession.UserId == c._guid;
    }
}
