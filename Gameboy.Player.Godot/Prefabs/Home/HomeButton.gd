class_name AnimatedTextureRect extends TextureButton

@export var playing: bool = true
@export var speed_scale: float = 1.0

@export var animation_name: String = "default"
@export_group("Icons")
@export var normal_sprite: SpriteFrames
@export var pressed_sprite: SpriteFrames
@export var hover_sprite: SpriteFrames
@export var disabled_sprite: SpriteFrames
@export var focused_sprite: SpriteFrames
@export var icon_tint: Color

@onready var icon_texture: TextureRect = $IconTexture

func _get_preferred_sprite() -> SpriteFrames:
	if disabled:
		return disabled_sprite
	if is_pressed():
		return pressed_sprite
	if is_hovered():
		return hover_sprite
	if has_focus():
		return focused_sprite
	return normal_sprite
		
func get_sprite() -> SpriteFrames:
	var preferred = _get_preferred_sprite()
	if preferred != null:
		return preferred
	else:
		return normal_sprite

func _ready() -> void:
	icon_texture.self_modulate = icon_tint
	pass

func _process(delta: float) -> void:
	var desired = get_sprite()
	if not desired.has_animation(animation_name):
		playing = false
		return
		
	# Swap to new animation if we need to
	if desired != current_sprite:
		play(desired)
		return
		
	if not playing:
		return 
	
	# Else resume current animation
	_update_anim_data()
	frame_delta += (speed_scale * delta)
	if frame_delta >= refresh_rate/fps:
		var texture = _advance_frame()
		icon_texture.texture = texture
		frame_delta = 0.0
	
	pass

var current_sprite : SpriteFrames
var frame_index : int = 0
var refresh_rate = 1.0
var fps = 30
var frame_delta = 0 

func play(sprites: SpriteFrames) -> void:
	current_sprite = sprites
	frame_index = 0
	frame_delta = 0.0
	_update_anim_data()
	icon_texture.texture = current_sprite.get_frame_texture(animation_name, 0)
	
func _update_anim_data():
	if current_sprite == null:
		return
		
	fps = current_sprite.get_animation_speed(animation_name)
	refresh_rate = current_sprite.get_frame_duration(animation_name, frame_index)
	
func _advance_frame() -> Texture2D:
	frame_index += 1
	var frame_count = current_sprite.get_frame_count(animation_name)
	if frame_index >= frame_count:
		frame_index = 0
		if not current_sprite.get_animation_loop(animation_name):
			playing = false
	_update_anim_data()
	return current_sprite.get_frame_texture(animation_name, frame_index)
	
	
