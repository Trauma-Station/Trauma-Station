// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Goobstation.Shared.Blob.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class BlobbernautComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<BlobChemPrototype> CurrentChem = "ReactiveSpines";

    [DataField]
    public TimeSpan DamageDelay = TimeSpan.FromSeconds(5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextDamage;

    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new()
        {
            { "Piercing", 25 },
        }
    };

    [DataField]
    public EntityUid? Factory;

    /// <summary>
    /// Scale used with the chemical's attack effects.
    /// </summary>
    [DataField]
    public float AttackEffectsScale = 4f;
}
