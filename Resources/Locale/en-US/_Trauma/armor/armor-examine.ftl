-protection = damage { $protect ->
    [true] reduced
    *[false] [color=red]increased[/color]
} by [color=lightblue]{TOSTRING($value, "F1")}%[/color].

armor-coefficient-value-trauma = - [color=yellow]{$type}[/color] { -protection(protect: $protect, value: $value) }

stamina-resistance-coefficient-value-trauma = - [color=lightyellow]Stamina[/color] { -protection(protect: $protect, value: $value) }
