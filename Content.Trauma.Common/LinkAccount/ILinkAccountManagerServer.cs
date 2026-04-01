using System;
using System.Collections.Generic;
using System.Text;
using Robust.Shared.Player;

namespace Content.Trauma.Common.LinkAccount;
public interface ILinkAccountManagerServer
{
    public SharedRMCPatronFull? GetPatron(ICommonSession player);
}
