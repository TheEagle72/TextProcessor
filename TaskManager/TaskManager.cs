using LoggerSpace;
using TextProcessorCoreLib;

namespace TaskManagerNames
{
    public class TaskManager  // may be reasonable writeTo better implement IEnumerator or another - more suitable interface?
    {
        private readonly HashSet<string> _files = new();
        public bool CurrentlyProcessing { get; private set; }
        private readonly List<string> _successfullyProcessedFiles = new();
        public ICollection<string> FilesToProcess => _files;

        private void Log(string message)
        {
            Logger.Instance().TimedMessage(message);
        }

        public bool AddFile(string readFrom)
        {
            return _files.Add(readFrom);
        }
        public bool RemoveFile(string path)
        {
            return _files.Remove(path);
        }

        public async Task<List<string>> Process(string writeToDir, uint minLength, bool removePunctuation, bool removeWhitespaces)
        {
            CurrentlyProcessing = true;


            foreach (var readFrom in FilesToProcess)
            {
                var writeTo = writeToDir + @"\" + Path.GetFileName(readFrom);
                if (readFrom != writeTo)
                {
                    Log($"Обработка файла: \"{readFrom}\" началась");
                    await ProcessFile(readFrom, writeTo, minLength, removePunctuation, removeWhitespaces);
                    Log($"Обработка файла: \"{readFrom}\" успешно завершена");
                }
                else
                {
                    Log($"Обработка файла: \"{readFrom}\" отменена! Конечный и исходный путь совпадают.");
                }
            }

            FilesToProcess.Clear();

            CurrentlyProcessing = false;
            return _successfullyProcessedFiles;
        }

        async Task ProcessFile(string readFrom, string writeTo, uint minLength, bool removePunctuation, bool removeWhitespaces)
        {
            //offload cpu bound task so ui does not freeze
            await Task.Run((() =>
            {
                //disposed at the end of scope
                using var reader = new StreamReader(readFrom);
                using var writer = new StreamWriter(writeTo);
                new ChunkTextProcessor(new ChunkTextFilter(minLength, removePunctuation, removeWhitespaces)).ProcessText(reader, writer);
            }));
            _successfullyProcessedFiles.Add(readFrom);
        }
    }
}
