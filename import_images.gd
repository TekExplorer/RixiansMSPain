@tool
extends EditorScript

# Define paths relative to the project root
const IMAGE_INPUT_DIR = "res://artist_assets/"
const TRES_OUTPUT_DIR = "res://images/atlases/card_atlas.sprites/"

func _run() -> void:
	print("🧹 Clearing destination directory to ensure full synchronization...")
	_clear_directory_recursive(TRES_OUTPUT_DIR)
	
	print("🚀 Starting recursive .tres asset conversion pipeline...")
	
	# 1. Establish structural roots
	if not DirAccess.dir_exists_absolute(IMAGE_INPUT_DIR):
		DirAccess.make_dir_recursive_absolute(IMAGE_INPUT_DIR)
		print("ℹ️ Input directory created. Put nested artist assets inside: ", IMAGE_INPUT_DIR)
		return

	# Ensure destination folder is re-created after being wiped
	if not DirAccess.dir_exists_absolute(TRES_OUTPUT_DIR):
		DirAccess.make_dir_recursive_absolute(TRES_OUTPUT_DIR)

	# 2. Force the Editor to scan for new raw loose images
	var editor_fs = EditorInterface.get_resource_filesystem()
	editor_fs.scan()
	
	while editor_fs.is_scanning():
		OS.delay_msec(100)

	# 3. Recursively convert files starting from the root folder
	var total_processed = _process_directory_recursive(IMAGE_INPUT_DIR)
	print("🎉 Conversion completed successfully! Processed %d total images." % total_processed)


# New Helper: Recursively deletes all files and folders inside a given directory path
func _clear_directory_recursive(dir_path: String) -> void:
	if not DirAccess.dir_exists_absolute(dir_path):
		return
		
	var dir = DirAccess.open(dir_path)
	if dir == null:
		return
		
	dir.list_dir_begin()
	var item_name = dir.get_next()
	
	while item_name != "":
		# Skip native navigation links
		if item_name == "." or item_name == "..":
			item_name = dir.get_next()
			continue
			
		var item_full_path = dir_path.get_basename() + "/" + item_name
		
		if dir.current_is_dir():
			# Recurse down to clear subfolders first
			_clear_directory_recursive(item_full_path + "/")
			# Remove the empty subfolder from disk
			DirAccess.remove_absolute(item_full_path)
		else:
			# Remove file from disk
			DirAccess.remove_absolute(item_full_path)
			
		item_name = dir.get_next()
		
	dir.list_dir_end()


# Recursive worker function
func _process_directory_recursive(current_dir_path: String) -> int:
	var images_processed_count = 0
	var dir = DirAccess.open(current_dir_path)
	if dir == null:
		printerr("❌ Could not open folder: ", current_dir_path)
		return 0
		
	dir.list_dir_begin()
	var item_name = dir.get_next()
	while item_name != "":
		# Skip native navigation links explicitly to fix path mirroring behavior
		if item_name == "." or item_name == "..":
			item_name = dir.get_next()
			continue
			
		var item_full_path = current_dir_path.get_basename() + "/" + item_name
		
		if dir.current_is_dir():
			# Found a sub-folder! Recurse deep into it
			images_processed_count += _process_directory_recursive(item_full_path + "/")
		else:
			# Found a loose file. Verify extension match
			var ext = item_name.get_extension().to_lower()
			if ext in ["png", "jpg", "jpeg", "webp"]:
				var relative_sub_path = current_dir_path.replace(IMAGE_INPUT_DIR, "")
				var destination_folder = TRES_OUTPUT_DIR + relative_sub_path
				var destination_tres_path = destination_folder + item_name.get_basename() + ".tres"
				
				# Ensure structural sub-folders exist before serialization
				if not DirAccess.dir_exists_absolute(destination_folder):
					DirAccess.make_dir_recursive_absolute(destination_folder)
				
				# Load the texture via Godot's asset engine cache
				var texture_resource = load(item_full_path)
				if texture_resource is Texture2D:
					var error = ResourceSaver.save(texture_resource, destination_tres_path)
					if error == OK:
						print("✅ Generated resource: ", destination_tres_path)
						images_processed_count += 1
					else:
						printerr("❌ Serialization failed for ", item_name, " - Code: ", error)
				else:
					printerr("⚠️ File skipped: Cannot parse ", item_full_path, " into Texture2D.")
					
		item_name = dir.get_next()
		
	dir.list_dir_end()
	return images_processed_count
