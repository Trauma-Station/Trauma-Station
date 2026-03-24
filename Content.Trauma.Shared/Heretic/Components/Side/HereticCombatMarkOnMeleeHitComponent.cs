using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Heretic.Components.Side;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HereticCombatMarkOnMeleeHitComponent : Component
{
    [DataField, AutoNetworkedField]
    public string NextPath = "Ash";

    [DataField]
    public List<string> Paths = new()
    {
        "Ash",
        "Void",
        "Flesh",
        "Blade",
        "Rust",
        "Cosmos",
    };
}
