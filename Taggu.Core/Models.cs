namespace Taggu.Core;

public class FileData(string path)
{
    /// <summary>
    /// For LiteDB
    /// </summary>
    public int ID { get; set; }

    public string Path { get; set; } = path;

    public string Identifier { get; set; } = "";
}

public class ImageTags(string identifier)
{
    /// <summary>
    /// For LiteDB
    /// </summary>
    public int ID { get; set; }

    public string Identifier { get; set; } = identifier;

    public DateTime LastUpdate { get; set; }
    
    public Dictionary<string, float> Tags { get; set; } = [];
}
