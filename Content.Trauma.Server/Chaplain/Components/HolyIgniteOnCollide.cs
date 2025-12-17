using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.Atmos.EntitySystems;

namespace Content.Trauma.Server.Chaplain.Components;

[RegisterComponent]
public sealed partial class HolyIgniteOnCollideComponent : Component
{
    /// <summary>
    /// How many more times the ignition can be applied.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("count")]
    public int Count = 1;

    [ViewVariables(VVAccess.ReadWrite), DataField("fireStacks")]
    public float FireStacks;

    [ViewVariables(VVAccess.ReadWrite), DataField("fixtureId")]
    public string FixtureId = "ignition";

}
