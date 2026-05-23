// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Tag;

namespace Content.Trauma.Shared.Forging;

public abstract partial class SharedMetalSystem
{
    [Dependency] private TagSystem _tag = default!;

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

    /// <summary>
    /// Adds a tag to a metallic object while it is workable.
    /// If it's already workable, adds the tag immediately too.
    /// </summary>
    public bool AddWorkableTag(Entity<MetallicTagsComponent?> ent, [ForbidLiteral] ProtoId<TagPrototype> tag)
    {
        ent.Comp ??= EnsureComp<MetallicTagsComponent>(ent);
        if (ent.Comp.Workable.Contains(tag))
            return false;

        ent.Comp.Workable.Add(tag);
        Dirty(ent);
        if (IsWorkable(ent))
            _tag.AddTag(ent.Owner, tag);
        return true;
    }

    /// <summary>
    /// Adds a tag to a metallic object while it is unworkable.
    /// If it's already unworkable, adds the tag immediately too.
    /// </summary>
    public bool AddUnworkableTag(Entity<MetallicTagsComponent?> ent, [ForbidLiteral] ProtoId<TagPrototype> tag)
    {
        ent.Comp ??= EnsureComp<MetallicTagsComponent>(ent);
        if (ent.Comp.Unworkable.Contains(tag))
            return false;

        ent.Comp.Unworkable.Add(tag);
        Dirty(ent);
        if (!IsWorkable(ent))
            _tag.AddTag(ent.Owner, tag);
        return true;
    }
}
