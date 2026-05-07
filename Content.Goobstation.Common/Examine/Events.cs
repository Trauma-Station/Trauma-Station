// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Utility;

namespace Content.Goobstation.Common.Examine;

[ByRefEvent]
public record struct ExamineCompletedEvent(
    FormattedMessage Message,
    EntityUid Examined,
    EntityUid Examiner,
    bool IsSecondaryInfo = false);

[ByRefEvent]
public record struct UserExaminedEvent(FormattedMessage Message, EntityUid Examined);

[ByRefEvent]
public record struct GetExamineNameEvent(Entity<MetaDataComponent> Ent, string? Result = null);
