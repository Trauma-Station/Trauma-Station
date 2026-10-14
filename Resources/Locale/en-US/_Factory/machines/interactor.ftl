# Constructor / Interactor

signal-port-name-machine-start = Start
signal-port-description-machine-start = Signal port to start a machine once.

signal-port-name-machine-repeating = Repeating
signal-port-description-machine-repeating = Signal port to control starting after completing automatically.

signal-port-name-machine-started = Started
signal-port-description-machine-started = Signal port that gets pulsed after a machine starts.

signal-port-name-machine-completed = Completed
signal-port-description-machine-completed = Signal port that gets pulsed after a machine completes its work.

signal-port-name-machine-failed = Failed
signal-port-description-machine-failed = Signal port that gets pulsed after a machine fails to start.

# Interactor

signal-port-name-automation-slot-tool = Item: Tool
signal-port-description-automation-slot-tool = An automation slot for an interactor's held tool.

signal-port-name-alt-interact = Alt Interact Mode
signal-port-description-alt-interact = Signal port to toggle alt interact mode, or set it to a HIGH/LOW value.

signal-port-name-use-in-hand = Use In Hand Mode
signal-port-description-use-in-hand = Signal port to toggle use in hand mode, or set it to a HIGH/LOW value. This will ignore targets and use Z or Alt+Z on the held tool.

signal-port-name-harm-mode = Harm Mode
signal-port-description-harm-mode = Signal port to toggle harm mode, or set it to a HIGH/LOW value. This will hit the target with the held tool like the interactor is in harm mode.

signal-port-name-tool-pickup-locked = Tool Pickup Locked
signal-port-description-tool-pickup-locked = Signal port to toggle the tool pickup lock, or set it to a HIGH/LOW value. Prevents picking up a tool by any means while locked.

signal-port-name-tool-drop-locked = Tool Drop Locked
signal-port-description-tool-drop-locked = Signal port to toggle the tool drop lock, or set it to a HIGH/LOW value. Prevents dropping the tool by any means while locked.
