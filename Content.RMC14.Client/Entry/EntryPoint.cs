using Content.RMC14.Client.LinkAccount;
using Content.Shitcode.Common.LinkAccount;
using Robust.Shared.ContentPack;

namespace Content.RMC14.Client.Entry;

internal class EntryPoint : GameClient
{
    public override void Init()
    {
        base.Init();

        IoCManager.Register<ILinkAccountManager, LinkAccountManager>();
    }
}
