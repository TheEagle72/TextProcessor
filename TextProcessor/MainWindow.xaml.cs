using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LoggerSpace;
using Microsoft.Win32;
using TaskManagerNames;

namespace TextProcessor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, ILoggerObserver
    {
        private bool _isCurrentOutDirValidated;
        private string _currentOutDir = "";

        private static readonly TaskManager TaskManager = new();
        //used for syncing with ListBox to manipulate list of files from UI
        public ObservableCollection<string> Observer { get; } = new();

        public MainWindow()
        {
            InitializeComponent();
            Logger.Instance().AddObserver(this);
            DataContext = this;
        }
        
        private void SyncObserver(IEnumerable<string> collection)
        {
            Observer.Clear();
            foreach (var fromTo in collection)
            {
                Observer.Add(fromTo);
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

            foreach (var readFrom in paths)
            {
                Logger.Instance().TimedMessage(TaskManager.AddFile(readFrom)
                    ? $"Добавлен файл: \"{readFrom}\""
                    : $"Не удалось добавить файл: \"{readFrom}\"");
            }
            SyncObserver(TaskManager.FilesToProcess);
            
            UiButtonProcessFiles.IsEnabled = TaskManager.FilesToProcess.Count > 0;
        }
        
        private void UiButton_RemoveFile_Click(object sender, RoutedEventArgs e)
        {
            var path = TaskManager.FilesToProcess.ElementAt(UiListBoxFiles.SelectedIndex);
            Logger.Instance().TimedMessage($"Удалён файл: \"{path}\"");
            TaskManager.FilesToProcess.Remove(path);
            SyncObserver(TaskManager.FilesToProcess);
            UiButtonProcessFiles.IsEnabled = TaskManager.FilesToProcess.Count > 0;
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
            UiButtonRemoveFile.IsEnabled = UiListBoxFiles.SelectedIndex != -1 && !TaskManager.CurrentlyProcessing;
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

            Logger.Instance().TimedMessage($"Запуск обработки {TaskManager.FilesToProcess.Count} файл(ов)");
            var minLength = uint.Parse(UiTextBoxWordLength.Text);
            var removePunctuation = UiCheckBoxRemovePunctuation.IsChecked == true;
            var removeWhitespaces = UiCheckBoxRemoveWhitespaces.IsChecked == true;

            //disable Ui for the time of processing current file/pack of files
            UiButtonRemoveFile.IsEnabled = false;
            UiButtonAddFile.IsEnabled = false;
            UiButtonProcessFiles.IsEnabled = false;
            
            var task = TaskManager.Process(_currentOutDir, minLength, removePunctuation, removeWhitespaces);
            await task;
            var successfullyProcessedFiles = task.Result;

            Logger.Instance().TimedMessage($"Обработано успешно {successfullyProcessedFiles.Count} файл(ов)");
            successfullyProcessedFiles.Clear();

            UiButtonAddFile.IsEnabled = true;
            UiProcessingProgressBar.Value = 0.0;
            SyncObserver(TaskManager.FilesToProcess);
        }
        public void OnLogUpdated()
        {
            UiTextBoxLogHistory.Text += Logger.Instance().LogHistory.Last();
        }
    }
}
