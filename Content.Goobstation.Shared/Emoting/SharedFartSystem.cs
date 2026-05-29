// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Goobstation.Shared.Emoting;
public abstract partial class SharedFartSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FartComponent, ComponentGetState>(OnGetState);
    }

    private void OnGetState(Entity<FartComponent> ent, ref ComponentGetState args)
    {
        args.State = new FartComponentState(ent.Comp.Emote, ent.Comp.FartTimeout, ent.Comp.FartInhale, ent.Comp.SuperFarted);
    }
}
