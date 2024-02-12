using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using TextProcessorCoreLib;
using Path = System.IO.Path;

namespace TextProcessor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isCurrentOutDirValidated = false;
        private readonly HashSet<string> _filesQueue = new();
        private string _currentOutDir = "";
        //used for syncing with ListBox to manipulate list of files from UI
        public ObservableCollection<string> Observer { get; } = new();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }
        private void Log(string msg)
        {
            var time = DateTime.Now;
            var timeStampedMessage = $"{time.Hour:D2}:{time.Minute:D2}:{time.Second:D2} {msg}\n";
            Logger.Instance().Message(timeStampedMessage);
            UiTextBoxLogHistory.Text += timeStampedMessage;
        }

        private void SyncObserver(IEnumerable<string> collection)
        {
            Observer.Clear();
            foreach (var file in collection)
            {
                Observer.Add(file);
            }
        }
        
        private void UiButton_AddFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                FileName = "file.txt",
                Filter = "Text files (*.txt)|*.txt",
                Title = "Выберите файл",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() != true) return;
            var paths = openFileDialog.FileNames;
            _filesQueue.UnionWith(paths);
            SyncObserver(_filesQueue);
            
            foreach (var file in paths)
            {
                Log($"Добавлен файл: \"{file}\"");
            }
            UiButtonProcessFiles.IsEnabled = _filesQueue.Count > 0;
        }
        
        private void UiButton_RemoveFile_Click(object sender, RoutedEventArgs e)
        {
            Log($"Удалён файл: \"{_filesQueue.Remove(Observer[UiListBoxFiles.SelectedIndex])}\"");

            _filesQueue.Remove(_filesQueue.ElementAt(UiListBoxFiles.SelectedIndex));
            SyncObserver(_filesQueue);
            UiButtonProcessFiles.IsEnabled = _filesQueue.Count > 0;
        }
        
        private void UiTextBox_WordLength_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        
        private void UiButton_SelectOutDirectory_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new OpenFolderDialog()
            {
                Title = "Выберите выходной каталог"
            };

            if (saveFileDialog.ShowDialog() != true) return;
            _currentOutDir = saveFileDialog.FolderName;
            if (IsOutDirectoryValid())
            {
                UiTextBoxOutDirectory.Text = _currentOutDir;
            }
        }
        
        private void UiTextBox_OutDirectory_TextChanged(object sender, TextChangedEventArgs e)
        {
            _isCurrentOutDirValidated = false;
            _currentOutDir = UiTextBoxOutDirectory.Text;
        }
        private void UiListBox_Files_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UiListBoxFiles.SelectedIndex == -1)
            {
                UiRemoveFile.IsEnabled = false;
            }
        }
        private bool IsOutDirectoryValid()
        {
            if (!Directory.Exists(_currentOutDir))
            {
                MessageBox.Show($"\"{_currentOutDir}\" не является корректным выходным каталогом", "Ошибка!");

                return false;
            }
            _isCurrentOutDirValidated = true;
            return _isCurrentOutDirValidated;
        }
        
        private async void UiButton_ProcessFiles_Click(object sender, RoutedEventArgs e)
        {
            if (!IsOutDirectoryValid())
            {
                return;    
            }
            //Make "snapshot" of current file queue and all of processing parameters
            var filesQueueCopy = new HashSet<string>(_filesQueue);
            Log($"Запуск обработки {filesQueueCopy.Count} файл(ов)");
            var minLength = uint.Parse(UiTextBoxWordLength.Text);
            var removePunctuation = UiCheckBoxRemovePunctuation.IsChecked == true;
            var removeWhitespaces = UiCheckBoxRemoveWhitespaces.IsChecked == true;


            _filesQueue.Clear(); //never used anymore, can be cleared so its open for insert of new files

            //disable Ui for the time of processing current file/pack of files
            UiRemoveFile.IsEnabled = false;
            UiButtonAddFile.IsEnabled = false;
            UiButtonProcessFiles.IsEnabled = false;

            var successfullyProcessedFiles = new List<string>();
            var skippedFiles = new List<string>();

            async Task ProcessFile(string readPath)
            {
                
                var writePath = _currentOutDir + '\\' + Path.GetFileName(readPath);
                if (readPath == writePath)
                {
                    Log($"Пропущен файл: {readPath} путь к входному и выходному файлу совпадают");
                    skippedFiles.Add(writePath);
                    return;
                }

                Log($"Обработка файла: \"{readPath}\" началась");
                //offload cpu bound task so ui does not freeze
                await Task.Run((() =>
                {
                    //disposed at the end of scope
                    using var reader = new StreamReader(readPath);
                    using var writer = new StreamWriter(writePath);
                    new ChunkTextProcessor(new TextFilter(minLength, removePunctuation, removeWhitespaces)).ProcessText(reader, writer);
                }));
                Log($"Обработка файла: \"{readPath}\" успешно завершена");
                successfullyProcessedFiles.Add(readPath);
            }

            uint i = 0;
            foreach (var readPath in filesQueueCopy)
            {
                SyncObserver(filesQueueCopy);
                await ProcessFile(readPath);
                UiProcessingProgressBar.Value = (double)++i * 100 / filesQueueCopy.Count; //can be replaced with IProgress
            }

            Log($"Обработано успешно {successfullyProcessedFiles.Count} файл(ов)");
            if (skippedFiles.Count>0)
            {
                Log($"Пропущено {skippedFiles.Count} файл(ов)");
            }

            UiButtonAddFile.IsEnabled = true;
            UiProcessingProgressBar.Value = 0.0;

            successfullyProcessedFiles.Clear();
            skippedFiles.Clear();
            filesQueueCopy.Clear();
            SyncObserver(_filesQueue);
        }
    }
}
