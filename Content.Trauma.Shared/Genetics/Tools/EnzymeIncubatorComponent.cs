// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Genetics.Tools;

/// <summary>
/// Replaces the clicked mob's enzymes with stored ones which get removed when used.
/// This is printed in the genetics console.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(EnzymeIncubatorSystem))]
[AutoGenerateComponentState]
public sealed partial class EnzymeIncubatorComponent : Component
{
    /// <summary>
    /// The enzymes to apply, or null if it's spent.
    /// </summary>
    [DataField, AutoNetworkedField]
    public UniqueEnzymes? Enzymes;

    /// <summary>
    /// How long the doafter takes when used on yourself.
    /// Doubled when used on someone else.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(6);

    /// <summary>
    /// How long the target will jitter for after enzymes are changed.
    /// </summary>
    [DataField]
    public TimeSpan JitterTime = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Damage done to the target after enzymes are changed.
    /// </summary>
    [DataField]
    public DamageSpecifier? Damage = new DamageSpecifier()
    {
        DamageDict = new()
        {
            { "Cellular", 5 }
        }
    };

    /// <summary>
    /// Keep the enzymes after use.
    /// </summary>
    /// <remarks>
    /// Just for admin abuse.
    /// </remarks>
    [DataField]
    public bool Infinite;
}
