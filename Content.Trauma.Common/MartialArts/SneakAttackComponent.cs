using System;
using System.Collections.Generic;
using System.Text;
using Robust.Shared.GameStates;

namespace Content.Trauma.Common.MartialArts;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SneakAttackComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IsFound = false;

    [DataField, AutoNetworkedField]
    public int SecondsTillHidden = 2;

    [DataField, AutoNetworkedField]
    public uint FramesTillHidden = 0;
}
