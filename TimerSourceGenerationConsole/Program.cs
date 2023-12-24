using TimerSourceGeneration;

namespace TimerSourceGenerationConsole;

partial class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Starting Application");
        TimerTest();
        HelloFrom("Generated Code");
        Console.WriteLine("Ending Application");
    }

    static partial void HelloFrom(string name);

    [Timer]
    static partial void TimerTest();
}