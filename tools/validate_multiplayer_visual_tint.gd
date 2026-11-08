extends SceneTree

func _initialize() -> void:
    var creature := Node2D.new()
    var visuals := Node2D.new()
    visuals.name = "Visuals"
    creature.add_child(visuals)
    var family := Sprite2D.new()
    creature.add_child(family)
    var controller := Node.new()
    controller.set_script(load("res://revenant_assets/families/family_animation.gd"))
    family.add_child(controller)
    controller.set("sprite", family)
    for tint in [Color.WHITE, Color(0.5, 0.5, 0.5), Color(0.5, 0.5, 0.5, 0.2)]:
        visuals.modulate = tint
        controller.call("sync_native_visual_layer")
        assert(family.modulate == tint, "Family must inherit native rear-row tint/fade")
    var duchess := (load("res://duchess_assets/combat_rig/duchess_combat_visuals.tscn") as PackedScene).instantiate()
    var body := duchess.get_node("Visuals")
    var idle := body.get_node("Prototype/PoseRoot/Idle") as CanvasItem
    assert(body.is_ancestor_of(idle), "Duchess artwork must inherit native Visuals modulation")
    assert(not idle.top_level, "Duchess must not escape native canvas ordering")
    duchess.free()
    creature.free()
    print("MULTIPLAYER_VISUAL_TINT_OK")
    quit()
