// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.EntityEffects;
using Robust.Shared.Toolshed;

namespace Content.Trauma.Server.Commands;

/// <summary>
/// Applies an entity effect prototype to input entities.
/// Debug effect stick in command form.
/// </summary>
[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class EffectCommand : ToolshedCommand
{
    private SharedEntityEffectsSystem? _effects;
    private SharedEntityEffectsSystem Effects
    {
        get
        {
            _effects ??= GetSys<SharedEntityEffectsSystem>();
            return _effects;
        }
    }

    [CommandImplementation]
    public void Effect(
        [PipedArgument] EntityUid uid,
        [CommandArgument] ProtoId<EntityEffectPrototype> id,
        [CommandArgument] float scale = 1f)
    {
        // TODO: predicted: false
        // TODO: pass user
        Effects.TryApplyEffect(uid, id, scale);
    }
}
