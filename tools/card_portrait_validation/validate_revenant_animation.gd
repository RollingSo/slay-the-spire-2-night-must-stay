extends SceneTree

var checks := 0
var failures := 0

func _initialize() -> void:
	call_deferred("check_rig")

func require(condition: bool, message: String) -> void:
	checks += 1
	if not condition:
		failures += 1
		push_error(message)

func one_visible(rig: Node) -> bool:
	var count := 0
	for sprite in rig.get_node("PoseRoot").get_children():
		if sprite.visible: count += 1
	return count == 1

func check_rig() -> void:
	var rig = load("res://revenant_assets/combat/revenant_combat_rig.tscn").instantiate()
	root.add_child(rig)
	await process_frame
	var player: AnimationPlayer = rig.get_node("AnimationPlayer")
	var pose: Node2D = rig.get_node("PoseRoot")
	var base_position := pose.position
	var triggers := ["Attack", "Cast", "Prayer", "Call", "Resonance", "Hit", "Idle", "Relaxed"]
	for source in triggers:
		for target in triggers:
			rig.clear_card_motion()
			rig.play_trigger("Idle")
			rig.play_trigger(source)
			player.advance(0.2)
			rig.play_trigger(target)
			require(one_visible(rig), source+" -> "+target+" blank/overlapping immediate frame")
			require(pose.position.is_equal_approx(base_position), source+" -> "+target+" stale position")
			require(is_equal_approx(pose.modulate.a,1.0), "stale alpha")
			player.advance(0.1)
			require(one_visible(rig), "blank/overlapping action frame")
			player.advance(2.0)
			require(one_visible(rig), "blank/overlapping completion frame")
			require(player.current_animation == "idle_loop", "one-shot failed to return idle")
	for motion in ["Attack", "Prayer", "Call", "Resonance"]:
		rig.clear_card_motion()
		rig.play_trigger("Idle")
		rig.play_card_motion(motion)
		player.advance(0.2)
		var current := player.current_animation
		var time := player.current_animation_position
		rig.play_trigger("Attack")
		require(player.current_animation == current, "native ack changed "+motion)
		require(is_equal_approx(player.current_animation_position,time), "native ack restarted "+motion)
		require(one_visible(rig), "native ack hid pose")
		rig.play_trigger("Attack")
		require(player.current_animation == current, "repeated hit changed motion")
		require(is_zero_approx(player.current_animation_position), "repeated hit failed to restart")
		player.advance(2.0)
		rig.play_trigger("Cast")
		require(player.current_animation == current, "delayed hit lost card route")
		rig.clear_card_motion()
		rig.play_trigger("Attack")
		require(player.current_animation == "claw_attack", "card route leaked into next attack")
	for source in triggers:
		rig.play_trigger("Idle")
		rig.play_trigger(source)
		player.advance(0.2)
		rig.play_trigger("Dead")
		require(one_visible(rig), "death entry blank")
		require(pose.position.is_equal_approx(base_position), "death inherited offset")
		player.advance(0.4)
		var time := player.current_animation_position
		for trigger in triggers+["Dead"]:
			rig.play_trigger(trigger)
			require(player.current_animation == "death", "dead rig accepted "+trigger)
			require(is_equal_approx(player.current_animation_position,time), "dead rig restarted")
		player.advance(1.1)
		require(is_zero_approx(pose.modulate.a), "death did not fade")
		rig.play_trigger("Revive")
		require(one_visible(rig), "revive blank/overlapping")
		require(is_equal_approx(pose.modulate.a,1.0), "revive stayed transparent")
		require(pose.position.is_equal_approx(base_position), "revive position drift")
	print("REVENANT_ANIMATION checks=",checks," failures=",failures)
	quit(1 if failures else 0)
