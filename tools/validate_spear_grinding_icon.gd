extends SceneTree

func _initialize() -> void:
    for prefix in ["res://images/", "res://"]:
        var mapping: String = prefix + "atlases/power_atlas.sprites/spear_grinding_temporary_dexterity_power.tres"
        var icon := load(mapping) as AtlasTexture
        assert(icon != null and icon.region == Rect2(1368, 1096, 256, 256))
        var image := Image.load_from_file(prefix + "powers/spear_grinding_temporary_dexterity_power.png")
        assert(image != null and image.get_size() == Vector2i(256, 256))
        for corner in [Vector2i(0,0),Vector2i(255,0),Vector2i(0,255),Vector2i(255,255)]:
            assert(image.get_pixelv(corner).a == 0.0)
    print("SPEAR_GRINDING_TEMPORARY_DEXTERITY_ICON_OK")
    quit()
