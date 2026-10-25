extends SceneTree

func _initialize() -> void:
    for location in ["merchant", "rest_site"]:
        var reference_height := 0.0
        for character in ["duchess", "revenant"]:
            var scene := (load("res://%s_assets/%s/%s_%s.tscn" % [character, location, character, location]) as PackedScene).instantiate()
            var sprite := scene.get_node("Sprite") as Sprite2D
            var used := sprite.texture.get_image().get_used_rect()
            var height := used.size.y * sprite.scale.y
            var bottom := sprite.position.y + (used.end.y - sprite.texture.get_height() * 0.5) * sprite.scale.y
            print(character, " ", location, " used=",used," scale=",sprite.scale.y," height=",height," bottom=",bottom)
            if character == "duchess":
                reference_height = height
            else:
                assert(absf(height / reference_height - 0.9) < 0.00001, "Revenant must be 90% of Duchess visible height")
                var expected_bottom := -63.346595 if location == "merchant" else 64.010993
                assert(absf(bottom - expected_bottom) < 0.001, "Ground alignment changed")
                var player := scene.get_node("AnimationPlayer") as AnimationPlayer
                for name in player.get_animation_list():
                    var animation := player.get_animation(name)
                    for track in range(animation.get_track_count()):
                        if str(animation.track_get_path(track)) == "Sprite:position":
                            assert((animation.track_get_key_value(track, 0) as Vector2).is_equal_approx(sprite.position), "Animation resets old position")
            scene.free()
    print("REVENANT_SOCIAL_SIZE_90_PERCENT_OK")
    quit()
