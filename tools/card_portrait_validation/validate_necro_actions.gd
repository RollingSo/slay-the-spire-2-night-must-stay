extends SceneTree

func _initialize() -> void:
    var args = OS.get_cmdline_user_args()
    if args.size() != 1 or not ProjectSettings.load_resource_pack(args[0], true):
        push_error("Expected exported PCK")
        quit(1)
        return
    var failures: Array[String] = []
    for id in ["necro_attack_power", "necro_protect_power"]:
        var reference: PackedByteArray
        for path in ["res://revenant_assets/powers/" + id + ".png",
                "res://images/powers/" + id + ".png", "res://powers/" + id + ".png",
                "res://images/atlases/power_atlas.sprites/" + id + ".tres"]:
            var texture = ResourceLoader.load(path, "Texture2D", ResourceLoader.CACHE_MODE_IGNORE) as Texture2D
            if texture == null:
                failures.append(path + " missing")
                continue
            var img: Image = texture.get_image()
            if img != null and img.is_compressed():
                img.decompress()
            if img == null or img.get_size() != Vector2i(256, 256):
                failures.append(path + " invalid dimensions")
                continue
            img.convert(Image.FORMAT_RGBA8)
            for corner in [Vector2i(0,0), Vector2i(255,0), Vector2i(0,255), Vector2i(255,255)]:
                if img.get_pixelv(corner).a != 0:
                    failures.append(path + " opaque corner")
            var pixels = img.get_data()
            for i in range(0, pixels.size(), 4):
                if pixels[i+3] == 0:
                    pixels[i] = 0
                    pixels[i+1] = 0
                    pixels[i+2] = 0
            if reference.is_empty():
                reference = pixels
            elif reference != pixels:
                failures.append(path + " compact/flash pixels differ")
    for failure in failures:
        push_error(failure)
    print("NECRO_ACTION_PACK: 8 textures, failures=", failures.size())
    quit(0 if failures.is_empty() else 1)
