using Taggu.TerminalUI;
using Taggu.Core;
using LiteDB;
using System.Security.Cryptography;

var deepdanbooru = new DeepDanbooru("model/model.onnx", "model/tags.txt", true);
Console.WriteLine("Taggu");

var terminal = new Terminal();
var pwd = Environment.CurrentDirectory;

using (var db = new LiteDatabase("data.db"))
{
    var galleries = db.GetCollection<Gallery>();
    var images = db.GetCollection<ImageEntry>();
    
    terminal.RegisterCommand("cd", args =>
    {
        pwd = string.Join(" ", args[1..]);
    });
    terminal.RegisterCommand("pwd", args =>
    {
        Console.WriteLine(pwd);
    });
    terminal.RegisterCommand("tag", args =>
    {
        var tags = deepdanbooru.Evaluate(string.Join(" ", args[1..]));
        foreach (var tag in tags)
        {
            Console.WriteLine($"{tag.Key}: {tag.Value}");
        }
    });
    terminal.RegisterCommand("tagall", args =>
    {
        // add entry if not exists
        if (!galleries.Exists(x => x.Folder == pwd))
        {
            galleries.Insert(new Gallery(pwd));
        }

        var gallery = galleries.FindOne(x => x.Folder == pwd);
        var md5 = MD5.Create();
        var files = Directory.GetFiles(pwd);
        for (var i = 0; i < files.Length; i++)
        {
            var name = Path.GetFileName(files[i]);
            Console.WriteLine($"{i+1}/{files.Length} {name}");
            
            if (!gallery.Images.ContainsKey(name))
            {
                var digest = BitConverter.ToString(md5.ComputeHash(File.ReadAllBytes(files[i])))
                             .Replace("-", "")
                             .ToLower();
                var tags = deepdanbooru.Evaluate(files[i]);

                images.Upsert(new ImageEntry(digest)
                {
                    LastUpdate = DateTime.Now,
                    Tags = tags,
                });
                gallery.Images.Add(name, digest);
            }
        }
        galleries.Update(gallery);
        db.Commit();
    });
    terminal.RegisterCommand("statistics", args =>
    {
        if (galleries.Exists(x => x.Folder == pwd))
        {
            var result = new Dictionary<string, int>();
            var gallery = galleries.FindOne(x => x.Folder == pwd);
            var count = gallery.Images.Count;
            var currentIndex = 0;
            foreach (var image in gallery.Images)
            {
                Console.WriteLine($"{currentIndex}/{count}");
                var target = images.FindOne(x => x.Name == image.Value);
                foreach (var tag in target.Tags.Keys)
                {
                    if (!result.ContainsKey(tag))
                    {
                        result.Add(tag, 0);
                    }
                    result[tag] += 1;
                }
                currentIndex++;
            }
            using (var writer = new StreamWriter("out.csv"))
            {
                writer.WriteLine("tag,count");
                foreach (var i in result)
                {
                    writer.WriteLine($"{i.Key},{i.Value}");
                }
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Not scanned");
            Console.ForegroundColor = ConsoleColor.White;
        }
    });
    terminal.Run();
}
