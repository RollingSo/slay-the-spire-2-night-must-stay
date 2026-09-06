extends SceneTree

func _initialize():
    var args = OS.get_cmdline_user_args()
    if args.size() < 1 or not ProjectSettings.load_resource_pack(args[0], false):
        push_error("Supply original SlayTheSpire2.pck; no audio is copied out.")
        quit(1)
        return
    var failed = 0
    for name in ["glass_orb_passive.mp3", "glass_orb_evoke.mp3", "lightning_orb_evoke.mp3",
        "lightning_orb_passive.mp3", "lightning_orb_channel.mp3", "blunt_attack.mp3", "heavy_attack.mp3",
        "STS_SFX_BurnCard_v1.mp3", "slash_attack.mp3", "dagger_throw.mp3", "dark_orb_evoke.mp3"]:
        var resource = load("res://debug_audio/" + name) as AudioStream
        if resource == null or resource.get_length() <= 0:
            failed += 1
            push_error("Missing/invalid audio: " + name)
        else:
            print("AUDIO_OK ", name, " ", resource.get_length(), "s")
    quit(0 if failed == 0 else 1)
