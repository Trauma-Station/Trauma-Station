## no reagent stuff and no trailing "." at the end
guidebook-nested-effect-description =
    {$chance ->
        [1] { $effect }
        *[other] Has a { NATURALPERCENT($chance, 2) } chance to { $effect }
    }{ $conditionCount ->
        [0] {""}
        *[other] {" "}when { $conditions }
    }
