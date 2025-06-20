extends Control

@onready var container: Control = $ColorRect/MarginContainer/ScrollContainer/HBoxContainer
@onready var profile_prefab: PackedScene = preload("res://Prefabs/ProfileSelect/Profile.tscn")

@export_file("*.tscn") var goto: String

func _ready() -> void:
	reload_profile_list()
	
	var children = container.get_children()
	if len(children) > 0:
		children[0].grab_focus()

func clear_profile_list() -> void:
	var children = container.get_children()
	var child_count = len(children)
	for i in range(child_count - 1):
		var child = children[i]
		child.queue_free()

func reload_profile_list() -> void:
	clear_profile_list()
	
	var dir_path = OS.get_user_data_dir() + "/" + "profiles"
	var dir = DirAccess.open(dir_path)
	if not dir:
		return
		
	var subdirs = dir.get_directories()
	subdirs.reverse()
	for subdir in subdirs:
		var full_path = dir_path + "/" + subdir
		
		# Set username
		var instance = profile_prefab.instantiate() as ProfileButton
		instance.username = subdir
		var callback = func():
			ProfileManager.SetProfileFromPath(full_path)
			get_tree().change_scene_to_file(goto)
		instance.pressed.connect(callback)
		
		# Set icon
		var icon_path = full_path + "/icon.png"
		var icon_exists = FileAccess.file_exists(icon_path)
		if icon_exists:
			var image = Image.new()
			image.load(icon_path)
			var texture = ImageTexture.create_from_image(image)
			instance.icon = texture
			
		container.add_child(instance)
		container.move_child(instance, 0)
		
	return

func load_profile():
	pass
