using Godot;
using System;
using System.IO;

public partial class SaveSlot : Control
{
    [Export] public Color UnselectedColor {get; set;}
    [Export] public Color SelectedColor {get; set;}
    [Export] public ColorRect SelectedIndicator {get; set;}
    [Export] public Button Button {get; set;}
    private int slotId = -1;
    [Export] public Label SlotID {get; set;}
    [Export] public Control Details {get; set;}
    [Export] public Label EmptyIndicator {get; set;}

    [ExportGroup("Detail Components")]
    [Export] public Label PlayTime {get; set;}

    public void SetSlotId(int id) {
        slotId = id;
        SlotID.Text = id.ToString();
    } 

    public void SetEmpty() {
        this.Details.Visible = false;
        this.EmptyIndicator.Visible = true;
    }

    public void SetFilled(FileInfo file) {
        this.PlayTime.Text = file.LastWriteTime.ToString();

        this.Details.Visible = true;
        this.EmptyIndicator.Visible = false;
    }

    public void SetFocus(int focused) {
        SelectedIndicator.Color = this.slotId == focused ? SelectedColor : UnselectedColor;
    }
}
