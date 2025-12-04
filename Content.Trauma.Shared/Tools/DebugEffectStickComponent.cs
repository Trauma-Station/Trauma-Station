// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Trauma.Shared.EntityEffects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Tools;

/// <summary>
/// Debug component that lets you set any entity effect prototype to use on clicked entities.
/// Can only be configured by admins and, by default, only used by admins.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(DebugEffectStickSystem))]
[AutoGenerateComponentState]
[EntityCategory("Debug", "DoNotMap")]
public sealed partial class DebugEffectStickComponent : Component
{
    /// <summary>
    /// The user-configurable effect to apply.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<EntityEffectPrototype>? Effect;

    /// <summary>
    /// VV this to allow non-admins to use (but not edit) the effect.
    /// God help us all.
    /// </summary>
    [DataField]
    public bool Unsafe = true;
}

[Serializable, NetSerializable]
public enum DebugEffectStickUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class DebugStickSetEffectMessage(string? effect) : BoundUserInterfaceMessage
{
    public readonly ProtoId<EntityEffectPrototype>? Effect = effect;
}
