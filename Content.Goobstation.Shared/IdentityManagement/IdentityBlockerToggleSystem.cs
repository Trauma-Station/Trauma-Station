using Content.Shared.IdentityManagement;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Inventory;

namespace Content.Goobstation.Shared.IdentityManagement;

/// <summary>
/// Updates your identity when you toggle a mask up or down.
/// </summary>
public sealed class IdentityBlockerToggleSystem : EntitySystem
{
    [Dependency] private readonly IdentitySystem _identity = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IdentityBlockerComponent, ComponentInit>(BlockerUpdateIdentity);
        SubscribeLocalEvent<IdentityBlockerComponent, ComponentRemove>(BlockerUpdateIdentity);
    }

    private void BlockerUpdateIdentity(EntityUid uid, IdentityBlockerComponent component, EntityEventArgs args)
    {
        var target = uid;

        if (_inventory.TryGetContainingEntity(uid, out var containing))
            target = containing.Value;

        _identity.QueueIdentityUpdate(target);
    }
}
