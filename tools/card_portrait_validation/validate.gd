extends SceneTree

# Run from this isolated project, never the mod source project: missing packaged
# imports must not silently fall back to source PNGs or local .godot caches.
func _initialize() -> void:
    var args = OS.get_cmdline_user_args()
    if args.size() != 2:
        push_error("Expected exported PCK and absolute card portrait manifest path")
        quit(1)
        return
    var json_text = FileAccess.get_file_as_string(args[1]).trim_prefix("\ufeff")
    var manifest = JSON.parse_string(json_text)
    if not manifest is Dictionary or not manifest.has("paths") or manifest.paths.is_empty():
        push_error("Invalid/empty card portrait manifest")
        quit(1)
        return
    if not ProjectSettings.load_resource_pack(args[0], true):
        push_error("Cannot mount exported mod PCK: " + args[0])
        quit(1)
        return
    var failed: Array[String] = []
    for path in manifest.paths:
        var texture = ResourceLoader.load(path, "Texture2D", ResourceLoader.CACHE_MODE_IGNORE) as Texture2D
        if texture == null:
            failed.append(path)
            continue
        var decoded = texture.get_image()
        if decoded == null or decoded.is_empty() or decoded.get_width() <= 0 or decoded.get_height() <= 0:
            failed.append(path)
    for path in failed:
        push_error("PACKAGED_PORTRAIT_FAILED " + path)
    print("PACKAGED_PORTRAITS: checked=", manifest.paths.size(), " failed=", failed.size(), " (isolated PCK only)")
    quit(0 if failed.is_empty() else 1)
