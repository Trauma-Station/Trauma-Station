using Content.Trauma.Server.Gravity.Systems;

namespace Content.Trauma.Server.Gravity.Components;

[RegisterComponent]
[Access(typeof(GravitySourceSystem))]
public sealed partial class GravitySourceComponent : Component
{
    [ViewVariables]
    public bool Active;
}
