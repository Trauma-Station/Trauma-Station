using Content.Server._Orion.Gravity.Systems;

namespace Content.Server._Orion.Gravity.Components;

[RegisterComponent]
[Access(typeof(GravitySourceSystem))]
public sealed partial class GravitySourceComponent : Component
{
    [ViewVariables]
    public bool Active;
}
