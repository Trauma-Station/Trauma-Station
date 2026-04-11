// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.StationEvents.Events;
using Content.Trauma.Server.StationEvents.Components;

namespace Content.Trauma.Server.StationEvents.Events;

// amazing neglect of ecs wizden
public sealed class RadiationStormRule : StationEventSystem<RadiationStormRuleComponent>;
