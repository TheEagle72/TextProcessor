namespace TextProcessor;

public class Logger
{
    private Logger() { }
    private static Logger instance;

    public static Logger Instance()
    {
        if (instance == null)
        {
            instance = new Logger();
        }
        return instance;
    }
    public List<string> LogHistory { get; } = new();

    public void Clear()
    {
        LogHistory.Clear();
    }
    public void Message(string message)
    {
        LogHistory.Add(message);
    }
}