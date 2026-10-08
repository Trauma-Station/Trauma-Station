// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DeviceLinking;

namespace Content.Factory.Common.DeviceLinking;

[DataRecord]
public partial record struct LogicIntPayload(int Value) : ISignalNetworkPayload;

[DataRecord]
public partial record struct LogicStringPayload(string Value) : ISignalNetworkPayload;
