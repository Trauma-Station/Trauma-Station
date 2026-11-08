// <Trauma>
using Content.Trauma.Common.Inventory;
// </Trauma>
using Content.Shared.Explosion;
using Content.Shared.Inventory;

namespace Content.Server.Inventory
{
    public sealed partial class ServerInventorySystem : InventorySystem
    {
        // Trauma - save inventory items in a dictionary before transferring to not lose items in child slots like suitstorage, pda, pockets
        private readonly Dictionary<EntityUid, SlotDefinition> _toTransfer = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<InventoryComponent, BeforeExplodeEvent>(OnExploded);
        }

        private void OnExploded(Entity<InventoryComponent> ent, ref BeforeExplodeEvent args)
        {
            // explode each item in their inventory too
            var slots = new InventorySlotEnumerator(ent);
            while (slots.MoveNext(out var slot))
            {
                if (slot.ContainedEntity != null)
                    args.Contents.Add(slot.ContainedEntity.Value);
            }
        }

        public void TransferEntityInventories(Entity<InventoryComponent?> source, Entity<InventoryComponent?> target)
        {
            if (!Resolve(source.Owner, ref source.Comp, false) || !Resolve(target.Owner, ref target.Comp, false)) // Trauma - don't log missing
                return;

            var enumerator = new InventorySlotEnumerator(source.Comp);
            // <Trauma>
            _toTransfer.Clear();
            while (enumerator.NextItem(out var item, out var slot))
            {
                _toTransfer[item] = slot;
            }
            foreach (var (item, slot) in _toTransfer)
            {
                var ev = new InventoryItemTransferEvent(source, target, slot.Name);
                RaiseLocalEvent(item, ref ev);
                if (ev.Handled)
                    continue;

                TryUnequip(source, slot.Name, true, true, inventory: source.Comp, triggerHandContact: true);
                TryEquip(target, item, slot.Name, true, true, inventory: target.Comp, triggerHandContact: true);
            }
            // </Trauma>
        }
    }
}
