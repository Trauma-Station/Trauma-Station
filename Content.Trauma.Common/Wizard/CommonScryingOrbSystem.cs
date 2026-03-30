using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Trauma.Common.Wizard;

public abstract class CommonScryingOrbSystem : EntitySystem
{
    public abstract bool IsScryingOrbEquipped(EntityUid uid);
}
