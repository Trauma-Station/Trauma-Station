// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Popups;
using Content.Shared.Tag;

namespace Content.Trauma.Shared.Forging;

public abstract class SharedMetalSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly EntityQuery<MetallicComponent> _query = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MetallicPopupsComponent, MetalWorkableChangedEvent>(OnPopupsChanged);
        SubscribeLocalEvent<MetallicTagsComponent, MetalWorkableChangedEvent>(OnTagsChanged);
    }

    private void OnPopupsChanged(Entity<MetallicPopupsComponent> ent, ref MetalWorkableChangedEvent args)
    {
        var loc = args.Workable ? ent.Comp.HeatedPopup : ent.Comp.CooledPopup;
        _popup.PopupEntity(Loc.GetString(loc, ("name", ent.Owner)), ent);
    }

    private void OnTagsChanged(Entity<MetallicTagsComponent> ent, ref MetalWorkableChangedEvent args)
    {
        if (args.Workable)
        {
            _tag.AddTags(ent.Owner, ent.Comp.Workable);
            _tag.RemoveTags(ent.Owner, ent.Comp.Unworkable);
        }
        else
        {
            _tag.AddTags(ent.Owner, ent.Comp.Unworkable);
            _tag.RemoveTags(ent.Owner, ent.Comp.Workable);
        }
    }

    public void SetWorkable(Entity<MetallicComponent> ent, bool workable)
    {
        if (ent.Comp.Workable == workable)
            return;

        ent.Comp.Workable = workable;
        Dirty(ent);
        var ev = new MetalWorkableChangedEvent(workable);
        RaiseLocalEvent(ent, ref ev);
    }

    public bool IsWorkable(EntityUid uid)
        => _query.CompOrNull(uid)?.Workable ?? false;
}
