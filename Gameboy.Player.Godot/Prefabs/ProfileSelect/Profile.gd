class_name ProfileButton
extends TextureButton

@export var icon: Texture2D
@export var username: String

@onready var icon_rect: TextureRect = $MarginContainer/VBoxContainer/TextureRect
@onready var username_label: Label = $MarginContainer/VBoxContainer/Label

func _ready() -> void:
	if icon != null:
		icon_rect.texture = icon
	if username != null && len(username) > 0:
		username_label.text = username
