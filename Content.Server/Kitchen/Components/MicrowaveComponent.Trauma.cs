using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server.Kitchen.Components;
public sealed partial class MicrowaveComponent
{
    /// <summary>
    /// Stores the last user of the microwave for xp delivery.
    /// </summary>
    [DataField]
    public EntityUid? LastKnownKnowledgeHolder;
}
