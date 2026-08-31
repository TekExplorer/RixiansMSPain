extends SceneTree

const SKIP_EXTENSIONS: Array[String] = [
    ".png", ".jpg", ".jpeg", ".webp", ".bmp", ".svg", ".tga",
    ".ogg", ".mp3", ".wav",
]

const REMAP_PATH_KEYS: Array[String] = [
    "path.s3tc_bptc",
    "path.etc2_astc",
    "path",
]

func _init() -> void:
    var args: PackedStringArray = OS.get_cmdline_user_args()
    if args.size() < 2:
        printerr("Error: Missing arguments. Usage: godot -s script.gd -- <output_path> <mod_folder1> [<mod_folder2> ...]")
        quit(1)
        return

    var output_path: String = args[0]
    var mod_folders: PackedStringArray = args.slice(1)

    # 1. First-pass verification: Check if any raw assets are unimported or stale
    var needs_import: bool = false
    for folder in mod_folders:
        var clean_folder: String = _strip_folder_name(folder)
        if _check_directory_needs_import("res://" + clean_folder):
            needs_import = true
            break

    # 2. Trigger automated import if files are dirty/missing
    if needs_import:
        print("⚡ Unimported assets found. Executing Godot editor import...")
        var godot_bin: String = OS.get_executable_path()
        var output: Array = []
        var exit_code: int = OS.execute(godot_bin, ["--headless", "--editor", "--quit"], output, true)
        if exit_code != 0:
            printerr("Godot editor import failed with code: ", exit_code)
            quit(1)
            return

    # 3. Pack files into .pck
    var packer: PCKPacker = PCKPacker.new()
    var err: int = packer.pck_start(output_path, 16)
    if err != OK:
        printerr("Failed to start PCK packer. Error code: ", err)
        quit(1)
        return

    for folder in mod_folders:
        var clean_folder: String = _strip_folder_name(folder)
        print("Packing folder: res://" + clean_folder)
        _pack_folder_recursive(packer, "res://" + clean_folder)

    err = packer.flush(true)
    if err == OK:
        print("Successfully packed: " + output_path)
        quit(0)
    else:
        printerr("Failed to flush PCK file. Error code: ", err)
        quit(1)


func _check_directory_needs_import(path: String) -> bool:
    var dir: DirAccess = DirAccess.open(path)
    if dir == null or dir.file_exists(".gdignore"):
        return false

    dir.list_dir_begin()
    var file_name: String = dir.get_next()

    while file_name != "":
        if file_name == "." or file_name == ".." or file_name == ".godot":
            file_name = dir.get_next()
            continue

        var full_path: String = path + "/" + file_name
        if dir.current_is_dir():
            if _check_directory_needs_import(full_path):
                return true
        else:
            var is_raw: bool = SKIP_EXTENSIONS.any(func(ext: String) -> bool: return file_name.ends_with(ext))
            if is_raw:
                var import_file: String = full_path + ".import"
                if not FileAccess.file_exists(import_file):
                    return true
                
                # Verify .ctex cache exists on disk
                var config: ConfigFile = ConfigFile.new()
                if config.load(import_file) != OK or not config.has_section("remap"):
                    return true
                
                var has_valid_cache: bool = false
                for key in REMAP_PATH_KEYS:
                    var cache_path = config.get_value("remap", key, "")
                    if cache_path is String and cache_path != "" and FileAccess.file_exists(cache_path):
                        has_valid_cache = true
                        break
                if not has_valid_cache:
                    return true

        file_name = dir.get_next()

    return false


func _strip_folder_name(folder: String) -> String:
    var result: String = folder
    if result.ends_with("/"):
        result = result.substr(0, result.length() - 1)
    if result.begins_with("res://"):
        result = result.substr(6, result.length() - 6)
    return result


func _pack_folder_recursive(packer: PCKPacker, path: String) -> void:
    var dir: DirAccess = DirAccess.open(path)
    if dir == null or dir.file_exists(".gdignore"):
        return

    dir.list_dir_begin()
    var file_name: String = dir.get_next()

    while file_name != "":
        if file_name == "." or file_name == ".." or file_name == ".godot":
            file_name = dir.get_next()
            continue

        var full_path: String = path + "/" + file_name
        if dir.current_is_dir():
            _pack_folder_recursive(packer, full_path)
        else:
            var is_raw_image: bool = SKIP_EXTENSIONS.any(func(ext: String) -> bool: return file_name.ends_with(ext))
            if not is_raw_image:
                packer.add_file(full_path, full_path)

            if file_name.ends_with(".import"):
                _pack_imported_dependency(packer, full_path)

        file_name = dir.get_next()


func _pack_imported_dependency(packer: PCKPacker, import_file_path: String) -> void:
    var config: ConfigFile = ConfigFile.new()
    if config.load(import_file_path) != OK or not config.has_section("remap"):
        return

    for key in REMAP_PATH_KEYS:
        var cache_path = config.get_value("remap", key, "")
        if cache_path is String and cache_path != "" and FileAccess.file_exists(cache_path):
            packer.add_file(cache_path, cache_path)
            break