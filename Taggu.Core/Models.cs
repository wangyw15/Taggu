namespace Taggu.Core;

public class Gallery
{
    public int ID { get; set; }
    public string Folder { get; set; } = "";
    public Dictionary<string, string> Images { get; set; } = []; // filename md5

    public Gallery(string folder)
    {
        Folder = folder;
    }
}

public class ImageEntry
{
    public int ID { get; set; }
    public string Name { get; set; } = ""; // md5
    public DateTime LastUpdate { get; set; }
    public Dictionary<string, float> Tags { get; set; } = [];

    public ImageEntry(string name)
    {
        Name = name;
    }
}
