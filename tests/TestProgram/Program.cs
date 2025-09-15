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
        Console.WriteLine($"Wrong input: {string.Join(' ', args)}");
    break;
}