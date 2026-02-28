switch (args)
{
    case ["repeat-line"]:
        var input = Console.ReadLine();
        Console.WriteLine(input + " again");
        Console.WriteLine(input + " and again");
    break;
    
    case ["input-length"]:
        input = Console.In.ReadToEnd();
        Console.WriteLine(input.Length);
    break;
    
    default:
        Console.WriteLine("Arguments:");
        for (int i = 0; i < args.Length; ++i)
        {
            Console.WriteLine($"[{i}] : {args[i]}");
        }
    break;
}
