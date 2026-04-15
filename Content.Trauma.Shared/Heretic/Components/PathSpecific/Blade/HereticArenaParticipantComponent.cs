using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Blade;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class HereticArenaParticipantComponent : BaseSpriteOverlayComponent
{
    [DataField]
    public EntityUid? Weapon;

    [DataField, AutoNetworkedField]
    public bool IsInsideArena;

    [DataField]
    public EntityUid? Mind;

    [DataField]
    public EntProtoId WeaponProto = "HereticBladeArena";

    [DataField]
    public EntProtoId RoleProto = "MindRoleArenaParticipant";

    [DataField]
    public FixedPoint2 HereticHealPerCrit = 60;

    [DataField, AutoNetworkedField]
    public bool IsVictor;

    [DataField]
    public DamageSpecifier DamageOnCrit = new()
    {
        DamageDict =
        {
            { "Slash", 50 },
        }
    };

    [DataField]
    public DamageModifierSet ModifierSet = new()
    {
        Coefficients =
        {
            { "Radiation", 0f },
        },
        IgnoreArmorPierceFlags = (int) PartialArmorPierceFlags.All,
    };

    [DataField]
    public bool WasPressureImmune;

    [DataField]
    public bool WasBreathingImmune;

    [DataField]
    public bool WasIgnoringGravity;

    [DataField]
    public string FighterState = "arena_fighter";

    [DataField]
    public string VictorState = "arena_victor";

    [DataField]
    public override SpriteSpecifier? Sprite { get; set; } =
        new SpriteSpecifier.Rsi(new ResPath("_Goobstation/Heretic/Effects/crown.rsi"), "arena_fighter");

    public override Vector2 Offset { get; set; } = new (0f, 2f / 3f);

    public override Enum Key { get; set; } = HereticArenaKey.Key;

    public override bool Unshaded { get; set; } = false;
}

public enum HereticArenaKey : byte
{
    Key
}
