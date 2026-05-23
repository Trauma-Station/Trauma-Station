// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Shared.Redial;

public abstract partial class SharedRedialManager : IPostInjectInit
{
    [Dependency] protected INetManager _netManager = default!;

    public void PostInject()
    {
        Initialize();
    }

    public virtual void Initialize()
    {
    }
}
