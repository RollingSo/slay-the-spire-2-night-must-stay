extends SceneTree
var checks := 0
func require(value: bool, message: String) -> void:
    checks += 1
    if not value:
        push_error(message)
        quit(1)
        assert(value, message)

func _initialize() -> void:
    var args := OS.get_cmdline_user_args()
    if not args.is_empty():
        require(ProjectSettings.load_resource_pack(args[0]), "Cannot load exported pack")
    call_deferred("run_checks")

func run_checks() -> void:
    for family in ["helen", "frederick", "sebastian"]:
        var sprite := Sprite2D.new()
        root.add_child(sprite)
        var player := Node.new()
        player.set_script(load("res://revenant_assets/families/family_animation.gd"))
        sprite.add_child(player)
        var directory = "res://revenant_assets/families/animations/" + family
        player.configure(sprite, directory)
        var idle: Texture2D = load("res://revenant_assets/families/" + family + ".png")
        require(idle.get_image().get_data() == player.poses["idle"].get_image().get_data(), family + " idle mismatch")
        var bounds := idle.get_image().get_used_rect()
        var ratio := 0.8 if family == "helen" else (1.2 if family == "sebastian" else 1.0)
        sprite.scale = Vector2.ONE * 204.0 * ratio / bounds.size.y
        sprite.position = Vector2(0, -(bounds.end.y - 256) * sprite.scale.y)
        var fixed_scale := sprite.scale
        var fixed_position := sprite.position
        require(absf(bounds.size.y * sprite.scale.y / 204.0 - ratio) < 0.0001, family + " visible ratio")
        require(absf(sprite.position.y + (bounds.end.y - 256) * sprite.scale.y) < 0.0001, family + " ground")
        for pose in player.poses:
            var image: Image = player.poses[pose].get_image()
            require(image.get_size() == Vector2i(512, 512), family + " dimensions " + pose)
            for corner in [Vector2i(0,0), Vector2i(511,0), Vector2i(0,511), Vector2i(511,511)]:
                require(image.get_pixelv(corner).a == 0, family + " corner " + pose)
        player.play_trigger("Attack")
        require(sprite.texture == player.poses["attack_windup"], family + " attack windup")
        player.advance(float(player.attack_times[0]) + 0.001)
        require(sprite.texture == player.poses["attack_impact"], family + " attack impact")
        player.advance(float(player.attack_times[1]))
        require(sprite.texture == player.poses["attack_recover"], family + " attack recovery")
        for source in ["Idle", "Attack", "Hit"]:
            for target in ["Idle", "Attack", "Hit"]:
                player.play_trigger(source)
                player.advance(0.09)
                player.play_trigger(target)
                require(sprite.texture != null, family + " blank transition")
                player.advance(2.0)
                require(player.clip == "idle" and sprite.texture == player.poses["idle"], family + " idle restoration")
                require(sprite.scale == fixed_scale and sprite.position == fixed_position, family + " transform drift")
        player.play_trigger("Hit")
        require(sprite.texture == player.poses["hit_impact"], family + " hit impact")
        player.advance(0.11)
        require(sprite.texture == player.poses["hit_recover"], family + " hit recovery")
        player.play_trigger("Attack")
        sprite.free() # Death, replacement, scene cleanup must destroy in-flight animation.
        require(not is_instance_valid(player), family + " animation survives owner cleanup")
        print("FAMILY_OK ", family, " visible_height=", 204.0 * ratio)
    print("FAMILY_ANIMATION checks=", checks, " failures=0")
    quit(0)
