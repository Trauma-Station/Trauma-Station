// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Popups;

namespace Content.Trauma.Shared.Forging;

/// <summary>
/// Shared API for working with metallic objects.
/// </summary>
public abstract partial class SharedMetalSystem : EntitySystem
{
    [Dependency] protected IPrototypeManager Proto = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private EntityQuery<MetallicComponent> _query = default!;

    /// <summary>
    /// Cache of each metal prototype.
    /// </summary>
    public List<MetalPrototype> AllMetals = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MetallicComponent, MetalChangedEvent>(OnMetalChanged);

        SubscribeLocalEvent<MetallicPopupsComponent, MetalWorkableChangedEvent>(OnPopupsChanged);
        SubscribeLocalEvent<MetallicTagsComponent, MetalWorkableChangedEvent>(OnTagsChanged);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        LoadPrototypes();
    }

    private void OnMetalChanged(Entity<MetallicComponent> ent, ref MetalChangedEvent args)
    {
        // this logic is for procedurally generated items
        if (ent.Comp.MinTemp != 0)
            return;

        ent.Comp.MinTemp = args.Metal.MinTemp;
        ent.Comp.IdealTemp = args.Metal.WorkingTemp;
        DirtyField(ent, ent.Comp, nameof(MetallicComponent.MinTemp));
        DirtyField(ent, ent.Comp, nameof(MetallicComponent.IdealTemp));
    }

    private void OnPopupsChanged(Entity<MetallicPopupsComponent> ent, ref MetalWorkableChangedEvent args)
    {
        var loc = args.Workable ? ent.Comp.HeatedPopup : ent.Comp.CooledPopup;
        _popup.PopupEntity(Loc.GetString(loc, ("name", ent.Owner)), ent);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<MetalPrototype>())
            LoadPrototypes();
    }

    private void LoadPrototypes()
    {
        AllMetals.Clear();
        foreach (var metal in Proto.EnumeratePrototypes<MetalPrototype>())
        {
            AllMetals.Add(metal);
        }
        AllMetals.Sort((a, b) => a.Name.CompareTo(b.Name));
    }

    public void SetWorkable(Entity<MetallicComponent> ent, bool workable)
    {
        if (ent.Comp.Workable == workable)
            return;

        ent.Comp.Workable = workable;
        DirtyField(ent, ent.Comp, nameof(MetallicComponent.Workable));
        var ev = new MetalWorkableChangedEvent(workable);
        RaiseLocalEvent(ent, ref ev);
    }

    public bool IsWorkable(EntityUid uid)
        => _query.CompOrNull(uid)?.Workable ?? false;

    public void SetMetal(Entity<MetallicComponent?> ent, [ForbidLiteral] ProtoId<MetalPrototype> metal)
    {
        if (!Resolve(ent, ref ent.Comp) || ent.Comp.Metal == metal || !Proto.Resolve(metal, out var proto))
            return;

        ent.Comp.Metal = metal;
        DirtyField(ent, ent.Comp, nameof(MetallicComponent.Metal));
        // let sprite update clientside and temperatures serverside
        var ev = new MetalChangedEvent(proto);
        RaiseLocalEvent(ent, ref ev);
    }

    /// <summary>
    /// Get the metal of an item, throwing if it isn't metallic or hasn't been procedurally set yet.
    /// </summary>
    public ProtoId<MetalPrototype> GetMetalOrThrow(EntityUid uid)
        => _query.Comp(uid).Metal!.Value;
}
