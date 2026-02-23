using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Weapons.Ranged.Ammo;

[RegisterComponent, NetworkedComponent]
[Access(typeof(BulletholeSystem))]
public sealed partial class BulletholeGeneratorComponent : Component;
