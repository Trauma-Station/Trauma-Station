entity-effect-guidebook-delete-entity = {$chance ->
    [1] deletes
    *[other] delete
} the target
entity-effect-guidebook-relay-implanted = for the implant's user, {$effect}
entity-effect-guidebook-add-killsign = gives the target a KILL sign
entity-effect-guidebook-add-accents = gives the target some funny accents
entity-effect-guidebook-force-equip-clothing = force {$chance ->
    [1] equips
    *[other] equip
} {A($name)} to the target's {$slot}

entity-effect-guidebook-speak = Causes involuntary speech

entity-effect-guidebook-increases-reach = Increases reach
entity-effect-guidebook-decreases-reach = Decreases reach

entity-effect-guidebook-scale-entity = Scales the target's size by ({$x}, {y})

entity-effect-guidebook-attack-self = {$chance ->
    [1] makes
    *[other] make
} the target {$canUse ->
    [true] attack
    *[false] punch
} itself
entity-effect-guidebook-attack-others = {$chance ->
    [1] makes
    *[other] make
} the target attack a random nearby thing

entity-effect-popup-seizure = {CAPITALIZE($entity)} starts having a seizure!
entity-effect-popup-acidic-flesh-bubbles = Your acid flesh bubbles...
entity-effect-popup-acidic-flesh-pops = {$entity}'s skin bubbles and pops!

entity-effect-popup-spasm-leg = Your leg spasms!
entity-effect-popup-spasm-fingers = Your fingers spasm!
entity-effect-popup-spasm-arm = Your arm spasms!

entity-effect-popup-feet-trip = You trip over your own feet.
