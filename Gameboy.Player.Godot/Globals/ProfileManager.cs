using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using Qkmaxware.Emulators.Gameboy.Hardware;
using System.IO;

namespace Qkmaxware.Emulators.Gameboy.Player;

public class Profile
{
    public string Path { get; private set; }
    public string Username { get; private set; }

    private string icon_path;
    private string setting_path;
    private string state_path;
    public string SavePath { get; private set; }
    private string screenshot_path;

    public Profile(string path)
    {
        this.Path = path;
        this.Username = System.IO.Path.GetFileName(path);
        this.icon_path = path + "/" + "icon.png";
        this.setting_path = path + "/" + "user_config.cfg";

        this.state_path = path + "/" + "states";
        this.SavePath = path + "/" + "saves";
        this.screenshot_path = path + "/" + "screenshots";
    }

    public void MakeDirs()
    {
        Directory.CreateDirectory(Path);

        Directory.CreateDirectory(state_path);
        Directory.CreateDirectory(SavePath);
        Directory.CreateDirectory(screenshot_path);
    }

    public bool TryGetIcon(out Texture2D icon)
    {
        try
        {
            var image = new Image();
            image.Load(icon_path);
            var texture = ImageTexture.CreateFromImage(image);
            icon = texture;
            return texture != null;
        }
        catch
        {
            icon = null;
            return false;
        }
    }

    public string ConfigPath => setting_path;

    public bool TryLoadConfig(out ConfigFile file)
    {
        // Create config
        var config = new ConfigFile();

        Error err = config.Load(setting_path);
        if (err != Error.Ok)
        {
            file = null;
            return false;
        }

        file = config;
        return true;
    }
}

public partial class ProfileManager : Node
{
    public static ProfileManager Instance;
    private Profile active;

    public override void _Ready()
    {
        base._Ready();
        Instance = this;
    }

    public bool HasProfile() => active != null;

    public Profile GetProfile()
    {
        return active;
    }

    public void SetProfile(Profile profile)
    {
        active = profile;
    }

    public void SetProfileFromPath(string path)
    {
        active = new Profile(path);
        GD.Print($"Set active profile to: '{path}'");
    }
}