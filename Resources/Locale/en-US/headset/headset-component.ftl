# Chat window radio wrap (prefix and postfix)
# Einstein Engines - Languages begin (change text color based on language color set in handler)
chat-radio-message-wrap = [color={$color}]{$channel} [bold]{$name}[/bold] {$verb}, { chat-manager-speech-double-quote-begin }[/color][font="{$fontType}" size={$fontSize}][color={$languageColor}]{$message}[/color][/font][color={$color}]{ chat-manager-speech-double-quote-end }[/color]
chat-radio-message-wrap-bold = [color={$color}]{$channel} [bold]{$name}[/bold] {$verb}, { chat-manager-speech-double-quote-begin }[/color][color={$languageColor}][font="{$fontType}" size={$fontSize}][bold]{$message}[/bold][/font][/color][color={$color}]{ chat-manager-speech-double-quote-end }[/color]
# Einstein Engines - Languages end

examine-headset-default-channel = Use {$prefix} for the default channel ([color={$color}]{$channel}[/color]).

chat-radio-common = Common
chat-radio-centcom = CentComm
chat-radio-command = Command
chat-radio-engineering = Engineering
chat-radio-medical = Medical
chat-radio-science = Science
chat-radio-security = Security
chat-radio-service = Service
chat-radio-supply = Supply
chat-radio-syndicate = Syndicate
chat-radio-freelance = Freelance

# not headset but whatever
chat-radio-handheld = Handheld
chat-radio-binary = Binary
chat-radio-xenoborg = Xenoborg
chat-radio-mothership = Mothership
