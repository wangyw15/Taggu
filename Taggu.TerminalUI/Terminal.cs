namespace Taggu.TerminalUI;

public class Terminal
{
    public delegate void CommandHandler(string currentDirectory, string command, string[] arguments);
    
    private readonly Dictionary<string, CommandHandler> _Commands = [];
    private string _CurrentDirectory = Environment.CurrentDirectory;

    public void RegisterCommand(string name, CommandHandler action)
    {
        _Commands[name] = action;
    }

    public static void Error(object? value)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine(value);
        Console.ForegroundColor = ConsoleColor.White;
    }

    public void Run()
    {
        Console.ForegroundColor = ConsoleColor.White;
        while (true)
        {
            Console.Write($"{_CurrentDirectory}>");
            var line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }
            var arguments = (from x in line.Split(' ') select x).ToArray();
            arguments[0] = arguments[0].ToLower(); // ignore command case
            if (arguments[0] == "exit")
            {
                break;
            }
            else if (arguments[0] == "cd")
            {
                if (arguments[1] == ".")
                {
                    // do nothing
                }
                else if (arguments[1] == "..")
                {
                    _CurrentDirectory = Path.GetDirectoryName(_CurrentDirectory) ?? _CurrentDirectory;
                }
                else
                {
                    _CurrentDirectory = string.Join(" ", arguments[1..]);
                }
            }
            else if (arguments[0] == "pwd")
            {
                Console.WriteLine(_CurrentDirectory);
            }
            else if (arguments[0] == "clear")
            {
                Console.Clear();
            }
            else
            {
                if (_Commands.TryGetValue(arguments[0], out var action))
                {
                    action(_CurrentDirectory, arguments[0], arguments[1..]);
                }
                else
                {
                    Error("Invalid command");
                }
            }
            Console.WriteLine();
        }
    }
}
