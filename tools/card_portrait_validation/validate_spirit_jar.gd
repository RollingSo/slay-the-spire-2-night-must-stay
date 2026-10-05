extends SceneTree

func _initialize() -> void:
    var args = OS.get_cmdline_user_args()
    if args.size() != 1 or not ProjectSettings.load_resource_pack(args[0], true):
        push_error("Expected exported PCK")
        quit(1)
        return
    var reference: PackedByteArray
    for path in ["res://revenant_assets/potions/spirit_calling_jar.png",
            "res://images/atlases/potion_atlas.sprites/spirit_calling_jar.tres"]:
        var texture = ResourceLoader.load(path, "Texture2D", ResourceLoader.CACHE_MODE_IGNORE) as Texture2D
        if texture == null:
            push_error("Missing Spirit Calling Jar icon: " + path)
            quit(1)
            return
        var img: Image = texture.get_image()
        if img != null and img.is_compressed():
            img.decompress()
        if img == null or img.get_size() != Vector2i(512,512):
            push_error("Invalid Spirit Calling Jar icon dimensions: " + path)
            quit(1)
            return
        img.convert(Image.FORMAT_RGBA8)
        for corner in [Vector2i(0,0), Vector2i(511,0), Vector2i(0,511), Vector2i(511,511)]:
            if img.get_pixelv(corner).a != 0:
                push_error("Spirit Calling Jar icon has opaque corners")
                quit(1)
                return
        var pixels = img.get_data()
        if reference.is_empty():
            reference = pixels
        elif reference != pixels:
            push_error("Spirit Calling Jar atlas pixels differ")
            quit(1)
            return
    print("SPIRIT_JAR_PACK: PNG and atlas imported correctly, 512x512, transparent corners.")
    quit(0)
