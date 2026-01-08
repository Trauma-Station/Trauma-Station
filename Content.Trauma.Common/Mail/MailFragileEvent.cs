namespace Content.Trauma.Common.Mail;

/// <summary>
/// Raised on mail to check if it is fragile.
/// TODO MAIL: remove with nyano mail removal
/// </summary>
[ByRefEvent]
public record struct MailFragileEvent(bool Fragile = false);
