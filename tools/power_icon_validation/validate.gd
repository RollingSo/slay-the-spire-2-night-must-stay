extends SceneTree

# Run in this resource-free project so a missing PCK texture cannot fall back
# to a PNG or imported texture in the development project.
func _initialize() -> void:
    var args := OS.get_cmdline_user_args()
    if args.size() != 2:
        push_error("Expected PCK and approved icon manifest")
        quit(1)
        return
    var manifest = JSON.parse_string(FileAccess.get_file_as_string(args[1]).trim_prefix("\ufeff"))
    if manifest == null or manifest.checks.size() != manifest.count or manifest.count != 114 or not ProjectSettings.load_resource_pack(args[0], true):
        push_error("Invalid manifest or cannot mount PCK")
        quit(1)
        return
    var failed: Array[String] = []
    var checked := 0
    for entry in manifest.checks:
        var compact_visible_hash := ""
        var paths: Array[String] = [
            "res://images/atlases/power_atlas.sprites/" + entry.id + ".tres",
            "res://images/powers/" + entry.id + ".png",
            "res://powers/" + entry.id + ".png"
        ]
        for resource_path in paths:
            checked += 1
            var tex := ResourceLoader.load(resource_path, "Texture2D", ResourceLoader.CACHE_MODE_IGNORE) as Texture2D
            if tex == null:
                failed.append(resource_path + " missing")
                continue
            var img: Image
            if tex is AtlasTexture:
                img = tex.atlas.get_image()
                if img != null and img.is_compressed():
                    img.decompress()
                if img != null:
                    img = img.get_region(Rect2i(tex.region))
            else:
                img = tex.get_image()
                if img != null and img.is_compressed():
                    img.decompress()
            if img == null or img.get_size() != Vector2i(256, 256):
                failed.append(resource_path + " image/size")
                continue
            img.convert(Image.FORMAT_RGBA8)
            var rgba := img.get_data()
            var visible := rgba.duplicate()
            for i in range(0, rgba.size(), 4):
                # Godot fix_alpha_border may fill RGB below 0.5 alpha. Compare
                # all alpha and opaque RGB to source, and all visible RGBA
                # between the compact/tooltip/flash consumers.
                if rgba[i + 3] < 128:
                    rgba[i] = 0
                    rgba[i + 1] = 0
                    rgba[i + 2] = 0
                if visible[i + 3] == 0:
                    visible[i] = 0
                    visible[i + 1] = 0
                    visible[i + 2] = 0
            var hasher := HashingContext.new()
            hasher.start(HashingContext.HASH_SHA256)
            hasher.update(rgba)
            if hasher.finish().hex_encode().to_upper() != entry.importStableHash:
                failed.append(resource_path + " differs from approved source")
            hasher.start(HashingContext.HASH_SHA256)
            hasher.update(visible)
            var visible_hash := hasher.finish().hex_encode()
            if compact_visible_hash.is_empty():
                compact_visible_hash = visible_hash
            elif compact_visible_hash != visible_hash:
                failed.append(resource_path + " compact/flash pixels differ")
    for error in failed:
        push_error(error)
    print("POWER_PACK_VALIDATION resources=", checked, " powers=", int(manifest.count), " failed=", failed.size())
    quit(0 if failed.is_empty() else 1)
