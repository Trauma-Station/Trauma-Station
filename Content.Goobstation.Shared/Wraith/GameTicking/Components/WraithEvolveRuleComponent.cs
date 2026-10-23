namespace Content.Goobstation.Shared.Wraith.GameTicking.Components;

[RegisterComponent]
public sealed partial class WraithEvolveRuleComponent : Component
{
    /// <summary>
    /// The game rule used for the Wraith ERT Chaplain autocall.
    /// </summary>
    [DataField]
    public EntProtoId ERTChaplainEvent = "SpawnERTChaplain";
}
