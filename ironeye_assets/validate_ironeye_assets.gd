extends SceneTree

func _initialize() -> void:
	var required := [
		"res://ironeye_assets/char_select_bg_ironeye.tscn",
		"res://ironeye_assets/character_select_ironeye_bg.png",
		"res://ironeye_assets/vfx/ironeye_dust_mote.svg",
		"res://materials/character_select_idle.gdshader",
		"res://ironeye_assets/char_select_ironeye.png",
		"res://ironeye_assets/char_select_ironeye_locked.png",
		"res://ironeye_assets/character_icon_ironeye.png",
		"res://ironeye_assets/character_icon_ironeye_outline.png",
		"res://ironeye_assets/character_icon_ironeye.tscn",
		"res://ironeye_assets/map_marker_ironeye.png",
		"res://ironeye_assets/combat_rig/ironeye_combat_character.png",
		"res://ironeye_assets/combat_rig/ironeye_attack.png",
		"res://ironeye_assets/combat_rig/ironeye_hit.png",
		"res://ironeye_assets/combat_rig/ironeye_combat_rig.gd",
		"res://ironeye_assets/combat_rig/ironeye_combat_rig.tscn",
		"res://ironeye_assets/combat_rig/ironeye_combat_visuals.tscn",
		"res://ironeye_assets/energy_counter/ironeye_orb_layer_1.png",
		"res://ironeye_assets/energy_counter/ironeye_orb_layer_2.png",
		"res://ironeye_assets/energy_counter/ironeye_orb_layer_3.png",
		"res://ironeye_assets/energy_counter/ironeye_orb_layer_4.png",
		"res://ironeye_assets/energy_counter/ironeye_orb_layer_5.png",
		"res://ironeye_assets/energy_icon/ironeye_energy_card_icon.png",
		"res://images/atlases/ui_atlas.sprites/card/energy_ironeye.tres",
		"res://images/packed/sprite_fonts/ironeye_energy_icon.png",
		"res://materials/transitions/ironeye_transition_mat.tres",
		"res://ironeye_assets/multiplayer_hands/multiplayer_hand_ironeye_point.png",
		"res://ironeye_assets/multiplayer_hands/multiplayer_hand_ironeye_rock.png",
		"res://ironeye_assets/multiplayer_hands/multiplayer_hand_ironeye_paper.png",
		"res://ironeye_assets/multiplayer_hands/multiplayer_hand_ironeye_scissors.png",
		"res://ironeye_assets/relics/cursemark_signet.png",
	]
	var failures := 0
	for path in required:
		if not ResourceLoader.exists(path):
			push_error("Missing resource: " + path)
			failures += 1
		elif ResourceLoader.load(path) == null:
			push_error("Could not load resource: " + path)
			failures += 1

	# These scenes intentionally reference engine resources supplied by the
	# original game PCK. A standalone mod-project validation cannot resolve
	# those dependencies, so verify the authored files here and load them in
	# the merged-game smoke test instead.
	for path in [
		"res://ironeye_assets/energy_counter/ironeye_energy_counter.tscn",
		"res://ironeye_assets/card_trail_ironeye.tscn",
		"res://materials/cards/frames/card_frame_ironeye_mat.tres",
		"res://ironeye_assets/rest_site/ironeye_rest_site.tscn",
		"res://ironeye_assets/merchant/ironeye_merchant.tscn",
	]:
		if not FileAccess.file_exists(path):
			push_error("Missing game-dependent resource: " + path)
			failures += 1

	var icon_scene := ResourceLoader.load(
		"res://ironeye_assets/character_icon_ironeye.tscn") as PackedScene
	if icon_scene == null:
		push_error("Ironeye character icon is not a PackedScene")
		failures += 1
	else:
		var icon_instance := icon_scene.instantiate()
		if icon_instance is not Control:
			push_error("Ironeye character icon scene root is not a Control")
			failures += 1
		icon_instance.free()

	var select_scene := ResourceLoader.load(
		"res://ironeye_assets/char_select_bg_ironeye.tscn") as PackedScene
	if select_scene == null:
		push_error("Ironeye character-select background is not a PackedScene")
		failures += 1
	else:
		var select_instance := select_scene.instantiate()
		for node_name in ["Artwork", "DustFar", "DustNear", "AnimationPlayer"]:
			if select_instance.get_node_or_null(node_name) == null:
				push_error("Ironeye character-select background missing node: " + node_name)
				failures += 1
		var select_animation := select_instance.get_node_or_null("AnimationPlayer") as AnimationPlayer
		if select_animation == null or not select_animation.has_animation("idle"):
			push_error("Ironeye character-select background missing idle animation")
			failures += 1
		select_instance.free()

	var combat_scene := ResourceLoader.load(
		"res://ironeye_assets/combat_rig/ironeye_combat_visuals.tscn") as PackedScene
	if combat_scene == null:
		push_error("Ironeye combat visuals are not a PackedScene")
		failures += 1
	else:
		var combat_instance := combat_scene.instantiate()
		for marker_name in ["Bounds", "CenterPos", "IntentPos", "OrbPos", "TalkPos"]:
			if combat_instance.get_node_or_null("%" + marker_name) == null:
				push_error("Ironeye combat visuals missing unique node: " + marker_name)
				failures += 1
		combat_instance.free()

	var rig_scene := ResourceLoader.load(
		"res://ironeye_assets/combat_rig/ironeye_combat_rig.tscn") as PackedScene
	if rig_scene == null:
		push_error("Ironeye combat rig is not a PackedScene")
		failures += 1
	else:
		var rig_instance := rig_scene.instantiate()
		var animation_player := rig_instance.get_node_or_null("AnimationPlayer") as AnimationPlayer
		for animation_name in ["idle_loop", "attack", "hit", "death"]:
			if animation_player == null or not animation_player.has_animation(animation_name):
				push_error("Ironeye combat rig missing animation: " + animation_name)
				failures += 1
		rig_instance.free()

	if failures == 0:
		print("IRONEYE_ASSETS_OK")
	quit(failures)
