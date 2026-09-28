// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.RadialSelector;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.BloodCult.TimedFactory;

/// <summary>
/// Lets you produce a choice of items every <see cref="Cooldown"/> period.
/// Basically an evil vending machine.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(TimedFactorySystem))]
[AutoGenerateComponentPause, AutoGenerateComponentState]
public sealed partial class TimedFactoryComponent : Component
{
    [DataField(required: true)]
    public List<RadialSelectorEntry> Entries = new();

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromMinutes(4);

    /// <summary>
    /// When an item can next be made.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan NextProduce;
}
