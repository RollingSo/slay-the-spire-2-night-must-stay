extends SceneTree

# Mechanical delivery downsampling only: no crop, repaint, filtering or composition changes.
func _initialize():
    var args = OS.get_cmdline_user_args()
    if args.size() != 2:
        push_error("Expected generated source PNG and approval output directory")
        quit(1)
        return
    var art = Image.load_from_file(args[0])
    if art == null or abs(float(art.get_width()) / art.get_height() - 1000.0/760.0) > 0.001:
        push_error("Wrong source aspect ratio: must regenerate, not stretch or crop")
        quit(1)
        return
    print("ART_SOURCE_SIZE ", art.get_size())
    DirAccess.make_dir_recursive_absolute(args[1])
    art.resize(1000, 760, Image.INTERPOLATE_LANCZOS)
    if art.save_png(args[1].path_join("recover_gold_candidate.png")) != OK:
        quit(1)
        return
    art.resize(250, 190, Image.INTERPOLATE_LANCZOS)
    art.save_png(args[1].path_join("recover_gold_thumbnail.png"))
    print("ART_DELIVERY 1000x760; thumbnail 250x190; no runtime replacement")
    quit(0)
