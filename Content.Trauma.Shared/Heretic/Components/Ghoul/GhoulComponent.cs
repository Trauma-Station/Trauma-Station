// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Heretic.Components.Ghoul;

[RegisterComponent, NetworkedComponent]
public sealed partial class GhoulComponent : Component
{
    /// <summary>
    ///     Total health for ghouls.
    /// </summary>
    [DataField]
    public FixedPoint2 TotalHealth = 50;

    [DataField]
    public GhoulDeathBehavior DeathBehavior = GhoulDeathBehavior.GibOrgans;

    [DataField]
    public EntProtoId? SpawnOnDeathPrototype;

    /// <summary>
    ///     Whether ghoul should be given a bloody blade
    /// </summary>
    [DataField]
    public bool GiveBlade;

    [DataField]
    public bool ChangeHumanoidProfile = true;

    [DataField]
    public LocId? ExamineMessage = "examine-system-cant-see-entity";

    [DataField]
    public EntityUid? BoundWeapon;

    [DataField]
    public EntProtoId BladeProto = "HereticBladeFleshGhoul";

    [DataField]
    public SoundSpecifier? BladeDeleteSound = new SoundCollectionSpecifier("gib");

    [DataField]
    public LocId GhostRoleName = "ghostrole-ghoul-name";

    [DataField]
    public LocId GhostRoleDesc = "ghostrole-ghoul-desc";

    [DataField]
    public LocId GhostRoleRules = "ghostrole-ghoul-rules";

    [DataField]
    public Color? OldSkinColor;

    [DataField]
    public Color? OldEyeColor;

    [DataField]
    public HashSet<ProtoId<NpcFactionPrototype>> OldFactions = new();
}

public enum GhoulDeathBehavior : byte
{
    GibOrgans, // Gibs into organs
    Gib, // Gibs without organs
    NoGib, // Doesn't gib
    Deconvert, // Doesn't gib, deconverts automatically
}
