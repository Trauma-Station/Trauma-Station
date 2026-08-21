using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Trauma.Common.Bank;

/// <summary>
/// Raised by-ref on the vending machine before vending. Cancel to deny the sale.
/// </summary>
[ByRefEvent]
public record struct VendingMachineVendAttemptEvent(string ItemId, bool Cancelled = false, string Reason = "");
