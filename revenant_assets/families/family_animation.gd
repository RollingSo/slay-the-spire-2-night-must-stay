extends Node
## Approved frame animation. The owning manager controls scale and ground alignment.
signal finished
var sprite: Sprite2D
var poses: Dictionary = {}
var clip := "idle"
var elapsed := 0.0
var attack_times := [0.18, 0.11, 0.22]

func configure(target: Sprite2D, directory: String) -> void:
    sprite = target
    poses.clear()
    for pose in ["idle", "attack_windup", "attack_impact", "attack_recover", "hit_impact", "hit_recover"]:
        poses[pose] = load(directory.path_join(pose + ".png")) as Texture2D
        assert(poses[pose] != null, "Missing family frame: " + directory + "/" + pose)
    attack_times = [0.24, 0.12, 0.26] if directory.ends_with("frederick") else [0.18, 0.11, 0.22]
    play_trigger("Idle")

func play_trigger(trigger: String) -> void:
    clip = "attack" if trigger == "Attack" else ("hit" if trigger == "Hit" else "idle")
    elapsed = 0.0
    _sample()

func _process(delta: float) -> void:
    advance(delta)

func advance(delta: float) -> void:
    if clip == "idle" or not is_instance_valid(sprite):
        return
    elapsed += maxf(delta, 0.0)
    _sample()

func _sample() -> void:
    if clip == "idle":
        sprite.texture = poses["idle"]
        return
    var keys = ["attack_windup", "attack_impact", "attack_recover"] if clip == "attack" else ["hit_impact", "hit_recover"]
    var times = attack_times if clip == "attack" else [0.10, 0.20]
    var remaining := elapsed
    for index in range(keys.size()):
        if remaining < float(times[index]):
            sprite.texture = poses[keys[index]]
            return
        remaining -= float(times[index])
    clip = "idle"
    elapsed = 0.0
    sprite.texture = poses["idle"]
    finished.emit()
