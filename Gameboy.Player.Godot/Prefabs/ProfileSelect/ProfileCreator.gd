extends Window

@onready var username_input: LineEdit = $ScrollContainer/MarginContainer/VBoxContainer/TextEdit
@onready var texture_input: TextureRect = $ScrollContainer/MarginContainer/VBoxContainer/TextureRect

signal profile_created()

func _ready() -> void:
	visible = false # just incase

func _on_create_profile_pressed() -> void:
	var uname = username_input.text
	if uname == null or len(uname) < 0:
		return
		
	var icon = texture_input.texture
	
	var dir_path = OS.get_user_data_dir() + "/" + "profiles"
	if not DirAccess.dir_exists_absolute(dir_path):
		DirAccess.make_dir_absolute(dir_path)
	
	# Create profile folder
	var user_dir_path = dir_path + "/" + uname
	if not DirAccess.dir_exists_absolute(user_dir_path):
		DirAccess.make_dir_absolute(user_dir_path)
	else:
		return # Profile already exists... don't do anything
	
	# Create profile subdirs
	var user_save_path = user_dir_path + "/saves" 
	if not DirAccess.dir_exists_absolute(user_save_path):
		DirAccess.make_dir_absolute(user_save_path)
	var user_state_path = user_dir_path + "/states" 
	if not DirAccess.dir_exists_absolute(user_state_path):
		DirAccess.make_dir_absolute(user_state_path)
	var user_screenshot_path = user_dir_path + "/screenshots" 
	if not DirAccess.dir_exists_absolute(user_screenshot_path):
		DirAccess.make_dir_absolute(user_screenshot_path)
	
	# Copy icon to dir
	var user_icon_path = user_dir_path + "/icon.png"
	var image = icon.get_image()
	image.save_png(user_icon_path)
		
	# Create profile settings
		
	print("created: " + dir_path)
	emit_signal("profile_created")

func _on_image_file_dialog_file_selected(path: String) -> void:
	var image = Image.new()
	image.load(path)
	var texture = ImageTexture.create_from_image(image)
	if texture_input != null:
		texture_input.texture = texture
