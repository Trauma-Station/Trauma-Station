// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Surgery.Tools;

namespace Content.Trauma.Shared.Surgery.Tools;

public sealed partial class TraumaSurgeryToolSystem : EntitySystem
{
    [Dependency] private SurgeryToolExamineSystem _tool = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScrewdriverComponent, SurgeryToolExaminedEvent>(_tool.OnExamined);
    }
}
