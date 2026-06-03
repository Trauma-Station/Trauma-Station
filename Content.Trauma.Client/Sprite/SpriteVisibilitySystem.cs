using System.Linq;
using Content.Trauma.Common.Sprite;

namespace Content.Trauma.Client.Sprite;

public sealed partial class SpriteVisibilitySystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpriteComponent, UpdateSpriteVisibilityEvent>(OnUpdate);

        SubscribeLocalEvent<SpriteVisibilityComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<SpriteVisibilityComponent> ent, ref ComponentStartup args)
    {
        if (!_spriteQuery.TryComp(ent, out var comp) || comp.Color.A >= 1f)
            return;

        ent.Comp.VisibilityModifiers[nameof(SpriteComponent)] = comp.Color.A;
    }

    private void OnUpdate(Entity<SpriteComponent> ent, ref UpdateSpriteVisibilityEvent args)
    {
        if (args.Alpha >= 1f)
            RemoveVisibilityModifier(ent.AsNullable(), args.Key);
        else
            AddVisibilityModifier(ent, args.Key, args.Alpha);
    }

    private void AddVisibilityModifier(Entity<SpriteComponent> ent, string key, float modifier)
    {
        var comp = EnsureComp<SpriteVisibilityComponent>(ent);
        comp.VisibilityModifiers[key] = MathF.Max(modifier, 0f);
        ReCalculateSpriteVisibility((ent, ent.Comp, comp));
    }

    private void RemoveVisibilityModifier(Entity<SpriteComponent?, SpriteVisibilityComponent?> ent, string key)
    {
        if (!Resolve(ent, ref ent.Comp1))
            return;

        if (!Resolve(ent, ref ent.Comp2, false))
        {
            SetSpriteVisibility(ent!, 1f);
            return;
        }

        ent.Comp2.VisibilityModifiers.Remove(key);
        if (ent.Comp2.VisibilityModifiers.Count == 0 ||
            ent.Comp2.VisibilityModifiers.Count == 1 &&
            ent.Comp2.VisibilityModifiers.ContainsKey(nameof(SpriteComponent)))
        {
            SetSpriteVisibility(ent!, 1f);
            return;
        }

        ReCalculateSpriteVisibility(ent!);
    }

    private void SetSpriteVisibility(Entity<SpriteComponent> ent, float visibility)
    {
        var e = ent.AsNullable();
        visibility = Math.Clamp(visibility, 0f, 1f);
        var visible = visibility > 0f;
        _sprite.SetVisible(e, visible);
        if (visible)
            _sprite.SetColor(e, ent.Comp.Color.WithAlpha(visibility));
    }

    private void ReCalculateSpriteVisibility(Entity<SpriteComponent, SpriteVisibilityComponent> ent)
    {
        var visibility = ent.Comp2.VisibilityModifiers.Values.Aggregate(1f, (x, y) => x * y);
        SetSpriteVisibility(ent, visibility);
    }
}
