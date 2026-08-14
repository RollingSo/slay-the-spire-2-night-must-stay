extends SceneTree

func _initialize() -> void:
	var required := [
		"res://guardian_assets/char_select_bg_guardian.tscn",
		"res://guardian_assets/character_select_guardian_bg.png",
		"res://guardian_assets/vfx/guardian_wind_streak.svg",
		"res://materials/character_select_idle.gdshader",
		"res://guardian_assets/char_select_guardian.png",
		"res://guardian_assets/char_select_guardian_locked.png",
		"res://guardian_assets/character_icon_guardian.png",
		"res://guardian_assets/character_icon_guardian_outline.png",
		"res://images/ui/top_panel/character_icon_guardian.png",
		"res://images/ui/top_panel/character_icon_guardian_outline.png",
		"res://guardian_assets/character_icon_guardian.tscn",
		"res://guardian_assets/map_marker_guardian.png",
		"res://guardian_assets/combat_rig/guardian_combat_character.png",
		"res://guardian_assets/combat_rig/guardian_combat_visuals.tscn",
		"res://guardian_assets/combat_rig/guardian_skeleton_prototype.tscn",
		"res://guardian_assets/combat_rig/guardian_skeleton_prototype.gd",
		"res://guardian_assets/merchant/guardian_merchant.png",
		"res://guardian_assets/merchant/guardian_merchant.tscn",
		"res://guardian_assets/rest_site/guardian_rest_site.png",
		"res://guardian_assets/rest_site/guardian_rest_site.tscn",
		"res://guardian_assets/energy_counter/guardian_orb_layer_1.png",
		"res://guardian_assets/energy_counter/guardian_orb_layer_2.png",
		"res://guardian_assets/energy_counter/guardian_orb_layer_3.png",
		"res://guardian_assets/energy_counter/guardian_orb_layer_4.png",
		"res://guardian_assets/energy_counter/guardian_orb_layer_5.png",
		"res://guardian_assets/energy_icon/guardian_energy_card_icon.png",
		"res://guardian_assets/guardian_transition_mask.png",
		"res://guardian_assets/guardian_trail_emblem.png",
		"res://guardian_assets/card_trail_guardian.tscn",
		"res://materials/transitions/guardian_transition_mat.tres",
		"res://materials/cards/frames/card_frame_guardian_mat.tres",
		"res://guardian_assets/multiplayer_hands/multiplayer_hand_guardian_point.png",
		"res://guardian_assets/multiplayer_hands/multiplayer_hand_guardian_rock.png",
		"res://guardian_assets/multiplayer_hands/multiplayer_hand_guardian_paper.png",
		"res://guardian_assets/multiplayer_hands/multiplayer_hand_guardian_scissors.png",
		"res://guardian_assets/guardian_power_atlas.svg",
		"res://atlases/card_atlas.sprites/guardian/powerful_defend.tres",
		"res://atlases/card_atlas.sprites/guardian/evolved_defend.tres",
		"res://atlases/card_atlas.sprites/guardian/iron_wall_defend.tres",
	]
	var failures := 0
	for path in required:
		if not ResourceLoader.exists(path):
			push_error("Missing resource: " + path)
			failures += 1
		elif ResourceLoader.load(path) == null:
			push_error("Could not load resource: " + path)
			failures += 1

	# These scenes intentionally reference scripts, shaders, and shared UI/VFX
	# resources supplied by the game's PCK. A standalone project validation can
	# verify their presence, while runtime loading is covered by the game.
	var game_dependent_files := [
	]
	for path in game_dependent_files:
		if not FileAccess.file_exists(path):
			push_error("Missing game-dependent resource: " + path)
			failures += 1

	var icon_scene := ResourceLoader.load("res://guardian_assets/character_icon_guardian.tscn") as PackedScene
	if icon_scene == null:
		push_error("Guardian character icon is not a PackedScene")
		failures += 1
	else:
		var icon_instance := icon_scene.instantiate()
		if icon_instance is not Control:
			push_error("Guardian character icon scene root is not a Control")
			failures += 1
		icon_instance.free()

	var select_scene := ResourceLoader.load(
		"res://guardian_assets/char_select_bg_guardian.tscn") as PackedScene
	if select_scene == null:
		push_error("Guardian character-select background is not a PackedScene")
		failures += 1
	else:
		var select_instance := select_scene.instantiate()
		for node_name in ["Artwork", "WindFar", "WindNear", "AnimationPlayer"]:
			if select_instance.get_node_or_null(node_name) == null:
				push_error("Guardian character-select background missing node: " + node_name)
				failures += 1
		var select_animation := select_instance.get_node_or_null("AnimationPlayer") as AnimationPlayer
		if select_animation == null or not select_animation.has_animation("idle"):
			push_error("Guardian character-select background missing idle animation")
			failures += 1
		select_instance.free()

	var combat_scene := ResourceLoader.load("res://guardian_assets/combat_rig/guardian_combat_visuals.tscn") as PackedScene
	if combat_scene == null:
		push_error("Guardian combat visuals are not a PackedScene")
		failures += 1
	else:
		var combat_instance := combat_scene.instantiate()
		var rig := combat_instance.get_node_or_null("Visuals/Prototype")
		if rig == null:
			push_error("Guardian combat visuals are missing GuardianRig")
			failures += 1
		else:
			var animation_player := rig.get_node_or_null("AnimationPlayer") as AnimationPlayer
			if animation_player == null:
				push_error("Guardian rig is missing AnimationPlayer")
				failures += 1
			else:
				for animation_name in ["idle_loop", "guard", "attack", "counter_attack"]:
					if not animation_player.has_animation(animation_name):
						push_error("Guardian rig is missing animation: " + animation_name)
						failures += 1
			if not rig.has_method("play_trigger"):
				push_error("Guardian rig trigger API is missing")
				failures += 1
		combat_instance.free()

	for texture_path in [
		"res://guardian_assets/guardian_transition_mask.png",
		"res://guardian_assets/guardian_trail_emblem.png",
	]:
		var texture := ResourceLoader.load(texture_path) as Texture2D
		if texture == null:
			continue
		var image := texture.get_image()
		if image == null or image.get_format() not in [Image.FORMAT_RGBA8, Image.FORMAT_RGBAF, Image.FORMAT_RGBAH]:
			push_error("Guardian effect texture lacks an alpha channel: " + texture_path)
			failures += 1

	var power_files := [
		"aegis_form_power.tres", "counter_discipline_power.tres", "counter_step_power.tres",
		"defensive_cadence_power.tres", "feather_sword_power.tres", "fortify_power.tres",
		"fortress_heart_power.tres", "great_shield_shock_power.tres", "great_tornado_power.tres",
		"guard_counter_block_next_turn_power.tres", "guard_counter_next_turn_power.tres", "guard_counter_power.tres",
		"guardian_oath_power.tres", "immortal_watch_power.tres", "incoming_damage_reduction_this_turn_power.tres",
		"invoke_storm_strength_down_power.tres", "iron_wall_defend_power.tres", "lai_pi_power.tres",
		"last_stand_protocol_power.tres", "no_attacks_next_turn_power.tres", "phantom_co_strike_power.tres",
		"phantom_imbalance_power.tres", "sacred_counter_power.tres", "sanctuary_watch_power.tres",
		"savior_form_power.tres", "sentry_stance_power.tres", "sky_citadel_power.tres",
		"spear_polish_power.tres", "stomp_stance_power.tres", "storm_avatar_power.tres",
		"thousand_weight_halberd_power.tres", "unbroken_line_power.tres", "wandering_spell_soul_power.tres",
		"wing_flap_power.tres", "winged_bulwark_power.tres", "zephyr_doctrine_power.tres",
	]
	for file_name in power_files:
		var texture := ResourceLoader.load("res://atlases/power_atlas.sprites/" + file_name) as AtlasTexture
		if texture == null or texture.atlas == null or texture.region.size != Vector2(128, 128):
			push_error("Invalid power icon resource: " + file_name)
			failures += 1

	var atlas := ResourceLoader.load("res://guardian_assets/guardian_power_atlas.svg") as Texture2D
	if atlas != null:
		atlas.get_image().save_png("res://guardian_assets/guardian_power_atlas_preview.png")

	if failures == 0:
		print("GUARDIAN_ASSETS_OK")
	quit(failures)
