extends SceneTree

func _initialize() -> void:
    var args = OS.get_cmdline_user_args()
    if args.size() != 3 or not ProjectSettings.load_resource_pack(args[0], true):
        quit(2)
        return
    var manifest = JSON.parse_string(FileAccess.get_file_as_string(args[1]).trim_prefix("\ufeff"))
    var checked := 0
    var different := 0
    for path in manifest.paths:
        if not path.ends_with(".png"):
            continue
        var source := Image.load_from_file(args[2].path_join(path.trim_prefix("res://")))
        var texture := ResourceLoader.load(path, "Texture2D", ResourceLoader.CACHE_MODE_IGNORE) as Texture2D if ResourceLoader.exists(path) else null
        if source == null or texture == null or texture.get_image() == null:
            print("MISSING ", path)
            different += 1
            continue
        var packed := texture.get_image()
        for frame in [source, packed]:
            if frame.is_compressed():
                frame.decompress()
            frame.convert(Image.FORMAT_RGBA8)
            frame.resize(32, 24, Image.INTERPOLATE_LANCZOS)
        var source_bytes := source.get_data()
        var packed_bytes := packed.get_data()
        var difference := 0
        for index in range(source_bytes.size()):
            difference += absi(int(source_bytes[index]) - int(packed_bytes[index]))
        if float(difference) / source_bytes.size() > 2.0:
            print("DIFFERENT ", path)
            different += 1
        checked += 1
    print("Compared ", checked, " PNG resources; differences or missing: ", different)
    quit(0 if different == 0 else 1)
