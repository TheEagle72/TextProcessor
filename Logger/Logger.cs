namespace LoggerSpace
{
    public interface ILoggerObserver
    {
        void OnLogUpdated();
    }

    public class Logger
    {
        private Logger() { }
        private static Logger _instance;

        public static Logger Instance()
        {
            return _instance ?? (_instance = new Logger());
        }
        public List<string> LogHistory { get; } = new();
        private readonly List<ILoggerObserver> _observers = new();

        public void Clear()
        {
            LogHistory.Clear();
        }

        public void AddObserver(ILoggerObserver observer)
        {
            _observers.Add(observer);
        }
        public void RemoveObserver(ILoggerObserver observer)
        {
            _observers.Remove(observer);
        }

        public static string AddCurrentTime(string message)
        {
            var time = DateTime.Now;
            var timeStampedMessage = $"{time.Hour:D2}:{time.Minute:D2}:{time.Second:D2} {message}\n";
            return timeStampedMessage;
        }
        
        public void TimedMessage(string message)
        {
            lock (_instance) //bad too
            {
                Message(AddCurrentTime(message));
            }
        }

        public void Message(string message)
        {
            lock (_instance)
            {
                LogHistory.Add(message);
                NotifyObservers();
            }
        }
        private void NotifyObservers()
        {
            foreach (var observer in _observers)
            {
                observer.OnLogUpdated();
            }
        }
    }
}
