extends Label

func _ready() -> void:
	_on_timer_timeout()

func _on_timer_timeout() -> void:
	var x = Time.get_datetime_dict_from_system()
	var hours = str(x["hour"]).lpad(2, "0")
	var minutes = str(x["minute"]).lpad(2, "0")
	text = hours + ":" + minutes
