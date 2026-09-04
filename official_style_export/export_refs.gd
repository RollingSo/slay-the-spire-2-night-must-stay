extends SceneTree

const PCK_PATH := "D:/Steam/steamapps/common/Slay the Spire 2/SlayTheSpire2.pck"
const OUTPUT_DIR := "C:/Users/17857/Documents/ChatGPT/黑夜君临mod/sts2_official_style_reference"

const REFERENCES := [
	"images/packed/card_portraits/colorless/apotheosis.png",
	"images/packed/card_portraits/colorless/apparition.png",
	"images/packed/card_portraits/ironclad/strike_ironclad.png",
	"images/packed/card_portraits/ironclad/defend_ironclad.png",
	"images/packed/card_portraits/ironclad/barricade.png",
	"images/packed/card_portraits/ironclad/inflame.png",
	"images/packed/card_portraits/ironclad/demon_form.png",
	"images/packed/card_portraits/ironclad/fiend_fire.png",
	"images/packed/card_portraits/ironclad/corruption.png",
	"images/packed/card_portraits/silent/strike_silent.png",
	"images/packed/card_portraits/silent/defend_silent.png",
	"images/packed/card_portraits/silent/footwork.png",
	"images/packed/card_portraits/silent/grand_finale.png",
	"images/packed/card_portraits/silent/nightmare.png",
	"images/packed/card_portraits/silent/phantom_blades.png",
	"images/packed/card_portraits/defect/strike_defect.png",
	"images/packed/card_portraits/defect/defend_defect.png",
	"images/packed/card_portraits/defect/meteor_strike.png",
	"images/packed/card_portraits/defect/echo_form.png",
	"images/packed/card_portraits/defect/creative_ai.png",
	"images/packed/card_portraits/defect/biased_cognition.png",
	"images/packed/card_portraits/defect/quadcast.png",
	"images/packed/card_portraits/necrobinder/strike_necrobinder.png",
	"images/packed/card_portraits/necrobinder/reaper_form.png",
	"images/packed/card_portraits/necrobinder/grave_warden.png",
	"images/packed/card_portraits/regent/strike_regent.png",
	"images/packed/card_portraits/regent/big_bang.png",
	"images/packed/card_portraits/regent/the_smith.png",
	"images/packed/card_portraits/regent/void_form.png",
	"images/packed/card_portraits/regent/tyranny.png",
	"images/packed/card_portraits/regent/the_sealed_throne.png",
	"images/packed/card_portraits/silent/wraith_form.png"
]

func _initialize() -> void:
	DirAccess.make_dir_recursive_absolute(OUTPUT_DIR)
	if not ProjectSettings.load_resource_pack(PCK_PATH, true):
		push_error("Could not mount PCK: " + PCK_PATH)
		quit(1)
		return
	var exported := 0
	for relative_path: String in REFERENCES:
		var resource_path: String = "res://" + relative_path
		var texture := ResourceLoader.load(resource_path) as Texture2D
		if texture == null:
			push_warning("Missing texture: " + resource_path)
			continue
		var file_name: String = relative_path.replace("/", "__")
		var output_path: String = OUTPUT_DIR.path_join(file_name)
		var result := texture.get_image().save_png(output_path)
		if result != OK:
			push_warning("Save failed: " + output_path + " code=" + str(result))
			continue
		exported += 1
		print("EXPORTED ", relative_path, " -> ", output_path)
	print("EXPORTED_COUNT=", exported)
	quit(0)
