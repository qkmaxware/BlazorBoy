using Godot;
using Qkmaxware.Emulators.Gameboy.Player;
using System;
using System.IO;

public partial class SaveManager : Window
{
    [Export] public int NumberOfSlots = 100;
    [Export] public Control Container;
    [Export(PropertyHint.File, hintString: "*.tscn")] public PackedScene SaveSlot;
    public int SlotId = 0;

    [Export] public Home HomeMenu;

    public override void _Ready() {
        for (var i = 0; i < Math.Max(1, NumberOfSlots); i++) {
            var number = i;
            var slot = SaveSlot.Instantiate<SaveSlot>();
            slot.SetSlotId(i);
            slot.Button.Pressed += () => {
                SetSlotId(number);
            };
            slot.SetEmpty();
            slot.SetFocus(this.SlotId);
            Container.AddChild(slot);
        }
        RefreshFocus();
    }

    public void SetSlotId(int id) {
        this.SlotId = Math.Max(0, id);
        RefreshFocus();
    }

    public void RefreshListingAndShow() {
        this.RefreshListing();
        this.Show();
    }

    public void RefreshFocus() {
        var children = Container.GetChildren();
        foreach (var child in children) {
            if (child is not SaveSlot slot) {
                continue;
            }

            slot.SetFocus(this.SlotId);
        }
    }

    public void RefreshListing() {
        var profile = ProfileManager.Instance.GetProfile();
        var children = Container.GetChildren();
        var rom_name = Path.GetFileName(HomeMenu.LoadedCartPath) ?? "rom.gb";
        var i = 0;
        foreach (var child in children) {
            if (child is not SaveSlot slot) {
                continue;
            }
            
            if (profile is null) {
                slot.SetEmpty();
                continue;
            }

            slot.SetFocus(this.SlotId);

            var full_path = profile.SavePath + "/" + rom_name + ".sav" + (i++);
            var path = new FileInfo(full_path); 
            if (!path.Exists) {
                slot.SetEmpty();
                continue;
            }

            slot.SetFilled(path);
        }
    }
}
