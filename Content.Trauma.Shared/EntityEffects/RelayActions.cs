// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Whitelist;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// For actions, applies an effect to the action entities that the user has.
/// </summary>
public sealed partial class RelayActions : EntityEffectBase<RelayActions>
{
    /// <summary>
    ///  The effect to apply
    /// </summary>
    [DataField(required: true)]
    public EntityEffect Effect = default!;

    /// <summary>
    /// If non-null, found entities must also match this whitelist.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// If non-null, found entities must also match this blacklist.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;
}

public sealed partial class RelayActionsEffectSystem : EntityEffectSystem<ActionsComponent, RelayActions>
{
    [Dependency] private EffectDataSystem _data = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private SharedActionsSystem _actions = default!;

    protected override void Effect(Entity<ActionsComponent> ent, ref EntityEffectEvent<RelayActions> args)
    {
        var effect = args.Effect;

        var whitelist = effect.Whitelist;
        var blacklist = effect.Blacklist;
        var entEffect = effect.Effect;

        foreach (var action in _actions.GetActions(ent.Owner, ent.Comp))
        {
            if (!_whitelist.CheckBoth(action, whitelist, blacklist))
                continue;

            _data.CopyData(ent, action);
            _effects.TryApplyEffect(action, entEffect, args.Scale, args.User, args.Predicted);
            _data.ClearData(action);
        }
    }
}
