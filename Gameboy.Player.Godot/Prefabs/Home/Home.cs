using Godot;
using Godot.NativeInterop;
using Qkmaxware.Emulators.Gameboy;
using Qkmaxware.Emulators.Gameboy.Hardware;
using Qkmaxware.Emulators.Gameboy.Player;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;

public partial class Home : Control
{
    [ExportGroup("Ui")]
    [Export] public Control Root { get; set; }

    [ExportGroup("Ui/Profile")]
    [Export] public Texture2D DefaultProfileIcon { get; set; }
    [Export] public TextureRect ProfileIcon { get; set; }
    Profile activeProfile;

    [ExportGroup("Ui/Game Details")]
    [Export] public Control DetailsRoot { get; set; }
    [Export] public Label Title { get; set; }
    [Export] public Texture2D DefaultBoxart { get; set; }
    [Export] public TextureRect Boxart { get; set; }
    [Export] public HttpRequest BoxartDownloader { get; set; }
    private string[] BoxartDownloaderHeaders = [];
    [Export] public Label Publisher { get; set; }
    [Export] public Label Year { get; set; }
    [Export] public Label Region { get; set; }
    [Export] public RichTextLabel Description { get; set; }
    [Export] public Label Genres { get; set; }

    [ExportGroup("Player")]
    [Export] public GodotBoy GodotBoy { get; set; }

    public override void _Ready()
    {
        base._Ready();
        Stop();
        DetailsRoot.Visible = false;
        SetProfileInfo(ProfileManager.Instance.GetProfile());
    }

    public override void _ExitTree()
    {
        base._ExitTree();
    }

    public void SetProfileInfo(Profile profile)
    {
        activeProfile = null;
        ProfileIcon.Texture = DefaultProfileIcon;
        if (profile is null)
        {
            return;
        }

        activeProfile = profile;
        activeProfile.MakeDirs();
        if (profile.TryGetIcon(out var icon))
        {
            ProfileIcon.Texture = icon;
        }

        if (GodotBoy?.Options is not null)
        {
            GodotBoy.Options.SetConfigPath(activeProfile.ConfigPath ?? Options.DefaultFilePath);
            GodotBoy.Options.LoadConfig();
        }
    }

    private string cartPath;
    public void LoadCart(string path)
    {
        try
        {
            cartPath = path;
            var cart = new Cartridge(File.ReadAllBytes(path));

            var db = GameDatabase.Instance();
            var details = db.Where(x => x.CartTitle == cart.Info.title).FirstOrDefault();

            Title.Text = details?.Name ?? cart.Info.title;
            Boxart.Texture = DefaultBoxart;
            if (details?.BoxArtUrl is not null && BoxartDownloader is not null)
            {
                BoxartDownloader.CancelRequest();
                BoxartDownloader.RequestRaw(
                    details.BoxArtUrl,
                    BoxartDownloaderHeaders,
                    HttpClient.Method.Get
                );
            }
            Publisher.Text = details.PublisherName ?? cart.Info.licencee.ToString();
            Year.Text = details?.ReleaseYear.ToString() ?? "?";
            Region.Text = cart.Info.region.ToString();
            Description.Text = details?.Description ?? string.Empty;
            Genres.Text = string.Join(", ", details?.Genres ?? Enumerable.Empty<string>());

            GodotBoy?.LoadCart(cart);
            DetailsRoot.Visible = true;
        }
        catch (Exception e)
        {
            GD.PushError(e);
        }
    }

    public void OnBoxartDownloaded(int result, int code, string[] headers, byte[] body)
    {
        if (code != 200)
            return;

        try
        {
            Image image = new Image();
            image.LoadPngFromBuffer(body);
            Texture2D texture = ImageTexture.CreateFromImage(image);
            Boxart.Texture = texture;
        }
        catch { }
    }

    public void Stop()
    {
        this.Root.Visible = true;
        if (this.GodotBoy is not null && this.GodotBoy.IsPlaying)
        {
            this.GodotBoy.Stop();
        }
    }

    private string getSavePathForCart(int save_index)
    {
        var cart = GodotBoy.Console.GetCartridge();
        if (cart is null || cartPath is null)
            return null;

        if (activeProfile is null)
        {
            return cartPath + ".sav" + save_index; // Just a default save for the cart
        }

        activeProfile.MakeDirs();
        return activeProfile.SavePath + "/" + System.IO.Path.GetFileName(cartPath) + ".sav" + save_index;
    }

    public bool Play()
    {
        if (GodotBoy is null)
            return false;

        if (!GodotBoy.IsCartLoaded())
            return false;

        var save_path = getSavePathForCart(0);
        if (save_path is not null)
        {
            GodotBoy.LoadSaveFromFile(save_path);
        }

        this.Root.Visible = false;
        this.GodotBoy.Play();
        return true;
    }

    [ExportGroup("LinkedScenes")]
    [Export(PropertyHint.File, hintString: "*.tscn")] public string ProfileSelectScene;
    public void ReturnToProfileSelect()
    {
        if (ProfileSelectScene is null)
            return;

        GetTree().ChangeSceneToFile(ProfileSelectScene);
    }

    public void OpenSettings()
    {
        GodotBoy?.Options?.Show();
    }
}
