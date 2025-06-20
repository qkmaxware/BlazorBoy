extends Button

signal load_icon(texture: Texture2D)

func _on_pressed() -> void:
	emit_signal("load_icon", self.icon)
	pass # Replace with function body.
