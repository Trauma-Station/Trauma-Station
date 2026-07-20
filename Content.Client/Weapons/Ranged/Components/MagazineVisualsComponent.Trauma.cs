namespace Content.Client.Weapons.Ranged.Components;

public sealed partial class MagazineVisualsComponent
{
    /// <summary>
    /// Whether should only set zero step when there is no ammo left.
    /// </summary>
    [DataField]
    public bool ZeroNoAmmo;
}
