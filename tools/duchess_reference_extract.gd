extends SceneTree

func _initialize() -> void:
	var output = ProjectSettings.globalize_path("res://.tmp/duchess_references")
	if not ProjectSettings.load_resource_pack("D:/Steam/steamapps/common/Slay the Spire 2/SlayTheSpire2.pck", false):
		quit(1)
		return
	DirAccess.make_dir_recursive_absolute(output)
	for card in ["strike_silent", "footwork", "grand_finale"]:
		var path = "res://images/packed/card_portraits/silent/%s.png" % card
		var texture = load(path) as Texture2D
		if texture:
			texture.get_image().save_png(output.path_join("%s.png" % card))
	quit()
