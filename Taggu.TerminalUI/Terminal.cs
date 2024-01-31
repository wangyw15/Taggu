namespace Taggu.TerminalUI;

public class Terminal
{
    private readonly Dictionary<string, Action<string[]>> _Commands = [];

    public void RegisterCommand(string name, Action<string[]> action)
    {
        _Commands[name] = action;
    }

    public void Run()
    {
        Console.ForegroundColor = ConsoleColor.White;
        while (true)
        {
            Console.Write("> ");
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
            else if (arguments[0] == "clear")
            {
                Console.Clear();
            }
            else
            {
                if (_Commands.TryGetValue(arguments[0], out var action))
                {
                    action(arguments);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid command");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
        }
    }
}
