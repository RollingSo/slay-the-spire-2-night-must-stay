extends SceneTree

func _initialize() -> void:
    var args := OS.get_cmdline_user_args()
    if args.size() > 1 or (args.size() == 1 and not ProjectSettings.load_resource_pack(args[0], true)):
        push_error("Cannot mount mod pack")
        quit(1)
        return
    var failed := false
    for id in ["fearless_strength_power", "fearless_dexterity_down_power"]:
        var reference: Image
        for path in ["res://images/atlases/power_atlas.sprites/" + id + ".tres",
                "res://images/powers/" + id + ".png", "res://powers/" + id + ".png"]:
            var texture := load(path) as Texture2D
            if texture == null:
                push_error("Missing " + path)
                failed = true
                continue
            var image: Image
            if texture is AtlasTexture:
                image = texture.atlas.get_image()
                if image.is_compressed():
                    image.decompress()
                image = image.get_region(Rect2i(texture.region))
            else:
                image = texture.get_image()
                if image.is_compressed():
                    image.decompress()
            if image.get_size() != Vector2i(256, 256):
                push_error("Wrong dimensions: " + path)
                failed = true
                continue
            for corner in [Vector2i(0, 0), Vector2i(255, 0), Vector2i(0, 255), Vector2i(255, 255)]:
                if image.get_pixelv(corner).a != 0.0:
                    push_error("Non-transparent corner: " + path)
                    failed = true
            if reference == null:
                reference = image
            else:
                for y in range(256):
                    for x in range(256):
                        var actual := image.get_pixel(x, y)
                        var expected := reference.get_pixel(x, y)
                        if actual.a != expected.a or (actual.a > 0.0 and actual != expected):
                            push_error("Compact and flash icon mismatch: " + path)
                            failed = true
                            break
                    if failed:
                        break
        print("FEARLESS_ICON_CHECK ", id)
    quit(1 if failed else 0)
