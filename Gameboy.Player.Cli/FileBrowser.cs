using System.Linq;

namespace Qkmaxware.Emulators.Gameboy.Players;

/// <summary>
/// Command line file browser
/// </summary>
public class FileBrowser {
    private DirectoryInfo active;

    private int selected = -1;
    private List<DirectoryInfo> subdirs = new List<DirectoryInfo>();
    private List<FileInfo> subfiles = new List<FileInfo>();

    private IEnumerable<FileSystemInfo> children => subdirs.Cast<FileSystemInfo>().Concat(subfiles.Cast<FileSystemInfo>());
    private int count => subdirs.Count + subfiles.Count;

    /// <summary>
    /// Create a new file browser to the current directory
    /// </summary>
    public FileBrowser() : this(new DirectoryInfo(System.Environment.CurrentDirectory)) {}

    /// <summary>
    /// Create a new file browser to an existing directory
    /// </summary>
    /// <param name="initial">directory</param>
    public FileBrowser(DirectoryInfo initial) {
        this.active = initial;
        Refresh();
    }

    /// <summary>
    /// Refresh file listing
    /// </summary>
    public void Refresh() {
        selected = -1;
        this.subdirs.Clear();
        this.subdirs.AddRange(this.active.GetDirectories());

        this.subfiles.Clear();
        this.subfiles.AddRange(this.active.GetFiles().Where(file => file.Extension == ".gb"));
    }

    /// <summary>
    /// Navigate to the previous entry
    /// </summary>
    public void PrevEntry() {
        if (selected > -1)
            selected --;
    }

    /// <summary>
    /// Navigate to the next entry
    /// </summary>
    public void NextEntry() {
        if (selected < count)
            selected++;
    }

    /// <summary>
    /// Select a file or folder. Returns the file if it is selected
    /// </summary>
    /// <returns>file if selected, null otherwise</returns>
    public FileInfo? Accept() {
        if (selected == -1 && active.Parent is not null) {
            active = active.Parent;
            Refresh();
            return null;
        }

        if (selected < 0 || selected >= count) {
            return null;
        }

        if (selected < subdirs.Count) {
            this.active = subdirs[selected];
            Refresh();
            return null;
        } else {
            var file = subfiles[selected - subdirs.Count];
            return file;
        }
    }   

    /// <summary>
    /// Print the current directory to file
    /// </summary>
    public void ToConsole() {
        if (active.Parent is not null) {
            if (selected == -1) {
                Console.Write('>');
            } else {
                Console.Write(' ');
            }
            Console.Write("DIR ");
            Console.WriteLine("..");
        }

        int i = 0;
        foreach (var subdir in subdirs) {
            if (selected == i) {
                Console.Write('>');
            } else {
                Console.Write(' ');
            }
            Console.Write("DIR ");
            Console.WriteLine(subdir.Name);
            i++;
        }
        foreach (var subfile in subfiles) {
            if (selected == i) {
                Console.Write('>');
            } else {
                Console.Write(' ');
            }
            Console.WriteLine(subfile.Name);
            i++;
        }
    }
}