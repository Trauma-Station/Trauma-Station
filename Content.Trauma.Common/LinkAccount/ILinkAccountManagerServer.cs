// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Text;
using Robust.Shared.Player;

namespace Content.Trauma.Common.LinkAccount;
public interface ILinkAccountManagerServer
{
    public SharedRMCPatronFull? GetPatron(ICommonSession player);
}
