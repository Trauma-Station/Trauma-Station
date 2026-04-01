namespace Content.Server.Disposal.Tube;

public sealed partial class DisposalTubeComponent
{
    /// <summary>
    /// How fast the item exiting the disposal tube should get thrown at
    /// </summary>
    [DataField]
    public float Speed = 10f;

    /// <summary>
    /// The limit on what upgrade kits can increase the speed to.
    /// </summary>
    [DataField]
    public float MaxUpgradeSpeed = 50f;
}
