extends Control

@onready var player: AnimationPlayer = $AnimationPlayer

@export_file("*.tscn") var goto: String

func _ready() -> void:
	player.play("Show")

func _on_animation_player_animation_finished(_anim_name: StringName) -> void:
	get_tree().change_scene_to_file(goto)
