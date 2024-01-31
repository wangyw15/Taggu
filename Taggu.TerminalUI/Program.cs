using Taggu.TerminalUI;
using Taggu.Core;
using LiteDB;
using System.Security.Cryptography;

var deepdanbooru = new DeepDanbooru("model/model.onnx", "model/tags.txt", true);
Console.WriteLine("Taggu terminal\n");

var terminal = new Terminal();
var pwd = Environment.CurrentDirectory;

using (var db = new LiteDatabase("data.db"))
{
    var images = db.GetCollection<ImageTags>();
    var fileData = db.GetCollection<FileData>();

    terminal.RegisterCommand("tag", (pwd, command, args) =>
    {
        var tags = deepdanbooru.Evaluate(Path.Combine(pwd, string.Join(" ", args)));
        foreach (var tag in tags)
        {
            Console.WriteLine($"{tag.Key}: {tag.Value}");
        }
    });
    
    terminal.RegisterCommand("tagall", (pwd, command, args) =>
    {
        var files = Directory.GetFiles(pwd);
        for (var i = 0; i < files.Length; i++)
        {
            var path = files[i];
            var name = Path.GetFileName(path);

            Console.Write($"{i + 1}/{files.Length} {name}");
            var (Left, Top) = Console.GetCursorPosition();
            Console.Write(new string(' ', Console.BufferWidth - Left));
            Console.SetCursorPosition(0, Top);

            var digest = BitConverter.ToString(MD5.HashData(File.ReadAllBytes(path)))
                         .Replace("-", "")
                         .ToLower();

            if (!fileData.Exists(x => x.Path == path))
            {
                fileData.Insert(new FileData(path)
                {
                    Identifier = digest,
                });
            }

            if (!images.Exists(x => x.Identifier == digest))
            {
                var tags = deepdanbooru.Evaluate(path);

                images.Insert(new ImageTags(digest)
                {
                    LastUpdate = DateTime.Now,
                    Tags = tags,
                });
            }
        }
        db.Commit();
        Console.WriteLine();
    });
    
    terminal.RegisterCommand("statistics", (pwd, command, args) =>
    {
        var result = new Dictionary<string, Dictionary<string, int>>(); // yyyy-MM, tag, count
        var files = Directory.GetFiles(pwd);
        var imageCount = new Dictionary<string, int>(); // yyyy-MM, count
        for (var i = 0; i < files.Length; i++)
        {
            var path = files[i];
            var name = Path.GetFileName(path);

            Console.Write($"{i + 1}/{files.Length} {name}");
            var (Left, Top) = Console.GetCursorPosition();
            Console.Write(new string(' ', Console.BufferWidth - Left));
            Console.SetCursorPosition(0, Top);

            if (!fileData.Exists(x => x.Path == path))
            {
                var digest = BitConverter.ToString(MD5.HashData(File.ReadAllBytes(path)))
                             .Replace("-", "")
                             .ToLower();
                
                fileData.Insert(new FileData(path)
                {
                    Identifier = digest,
                });

                if (!images.Exists(x => x.Identifier == digest))
                {
                    var tags = deepdanbooru.Evaluate(path);

                    images.Upsert(new ImageTags(digest)
                    {
                        LastUpdate = DateTime.Now,
                        Tags = tags,
                    });
                }

                db.Commit();
            }

            var identifier = fileData.FindOne(x => x.Path == path).Identifier;
            var image = images.FindOne(x => x.Identifier == identifier);

            var info = new FileInfo(path);
            var date = DateTime.Now.ToString("yyyy-MM");
            if (info.CreationTime < info.LastWriteTime)
            {
                date = info.CreationTime.ToString("yyyy-MM");
            }
            else
            {
                date = info.LastWriteTime.ToString("yyyy-MM");
            }

            result.TryAdd(date, []);
            foreach (var tag in image.Tags.Keys)
            {
                result[date].TryAdd(tag, 0);
                result[date][tag] += 1;
            }

            imageCount.TryAdd(date, 0);
            imageCount[date]++;
        }
        using (var writer = new StreamWriter("out.csv"))
        {
            writer.WriteLine("date,tag,count,month_total_image_count");
            foreach (var monthResult in result)
            {
                foreach (var tag in monthResult.Value)
                {
                    writer.WriteLine($"{monthResult.Key},{tag.Key},{tag.Value},{imageCount[monthResult.Key]}");
                }
            }
            var total = 0;
            foreach (var count in imageCount)
            {
                total += count.Value;
            }
            writer.WriteLine($",total_image_count,{total},");
        }
        Console.WriteLine();
    });
    
    terminal.RegisterCommand("search", (pwd, command, args) =>
    {
        var keyword = string.Join(' ', args);
        foreach (var image in images.FindAll())
        {
            if (image.Tags.ContainsKey(keyword))
            {
                Console.WriteLine(fileData.FindOne(x => x.Identifier == image.Identifier).Path);
            }
        }
    });
    terminal.Run();
}
