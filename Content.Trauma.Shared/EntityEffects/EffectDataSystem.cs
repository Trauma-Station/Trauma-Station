// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// API for passing extra data to effects via storing a temporary component on the target entity.
/// When setting data make sure to remove it after the effect has ran via passing null.
/// </summary>
/// <remarks>
/// The only reason this exists is because "ECS" entity effects are half baked and provide no way to extend the arguments like the original did.
/// TheShuEd my goat.
/// </remarks>
public sealed class EffectDataSystem : EntitySystem
{
    private EntityQuery<EntityEffectUserComponent> _userQuery;
    private EntityQuery<EntityEffectToolComponent> _toolQuery;

    public override void Initialize()
    {
        base.Initialize();

        _userQuery = GetEntityQuery<EntityEffectUserComponent>();
        _toolQuery = GetEntityQuery<EntityEffectToolComponent>();
    }

    public EntityUid? GetUser(EntityUid target)
        => _userQuery.CompOrNull(target)?.User;

    public EntityUid? GetTool(EntityUid target)
        => _toolQuery.CompOrNull(target)?.Tool;

    public void SetUser(EntityUid target, EntityUid user)
    {
        // don't add components to entities being deleted, 0.001% edge case but still
        if (TerminatingOrDeleted(target))
            return;

        EnsureComp<EntityEffectUserComponent>(target).User = user;
    }

    public void ClearUser(EntityUid target)
        => RemComp<EntityEffectUserComponent>(target);

    public void SetTool(EntityUid target, EntityUid tool)
    {
        // don't add components to entities being deleted, 0.001% edge case but still
        if (TerminatingOrDeleted(target))
            return;

        EnsureComp<EntityEffectToolComponent>(target).Tool = tool;
    }

    public void ClearTool(EntityUid target)
        => RemComp<EntityEffectToolComponent>(target);

    public void CopyData(EntityUid src, EntityUid target)
    {
        // TODO: if you add any more data change these 2 methods to raise events
        if (GetUser(src) is {} user)
            SetUser(target, user);
        if (GetTool(src) is {} tool)
            SetTool(target, tool);
    }

    public void ClearData(EntityUid target)
    {
        ClearUser(target);
        ClearTool(target);
    }
}

/// <summary>
/// Temporary data component that stores the user that caused an effect to occur.
/// </summary>
[RegisterComponent, Access(typeof(EffectDataSystem))]
public sealed partial class EntityEffectUserComponent : Component
{
    [DataField]
    public EntityUid User;
}

/// <summary>
/// Temporary data component that stores the tool used to cause an effect to occur.
/// </summary>
[RegisterComponent, Access(typeof(EffectDataSystem))]
public sealed partial class EntityEffectToolComponent : Component
{
    [DataField]
    public EntityUid Tool;
}
