using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace FFTool
{
    public class SelectionToBrushConverter : IValueConverter
    {
        private Dictionary<string, List<string>> mediaFormats = new Dictionary<string, List<string>>()
        {
            { "视频", new List<string> { "mp4", "avi", "mov", "mkv" } },
            { "音频", new List<string> { "mp3", "wav", "aac", "flac" } },
            { "图片", new List<string> { "jpg", "png", "bmp", "webp" } }
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isSelected = (bool)value;
            return isSelected ? new SolidColorBrush(Color.FromRgb(30, 144, 255)) : new SolidColorBrush(Colors.LightGray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public partial class MainWindow : Window
    {
        private string? selectedFilePath = null;
        private string? selectedOutputPath = null;
        private Process? currentProcess = null;
        public ObservableCollection<MediaTypeItem> MediaTypes { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            // 设置窗口为屏幕分辨率的 60%
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            this.Width = screenWidth * 0.6;
            this.Height = screenHeight * 0.6;
            MediaTypes = new ObservableCollection<MediaTypeItem>
            {
                new MediaTypeItem { Name = "视频" },
                new MediaTypeItem { Name = "音频" },
                new MediaTypeItem { Name = "图片" }
            };
            MediaTypeListBox.ItemsSource = MediaTypes;
        }
        private void MediaTypeListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var item in MediaTypes)
                item.IsSelected = false;

            if (MediaTypeListBox.SelectedItem is MediaTypeItem selected)
                selected.IsSelected = true;
        }
        public class MediaTypeItem
        {
            public string Name { get; set; }
            public bool IsSelected { get; set; }
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new();
            if (openFileDialog.ShowDialog() == true)
            {
                selectedFilePath = openFileDialog.FileName;
                FilePathBox.Text = selectedFilePath;
                StatusText.Text = "已加载文件";
            }
        }

        private void BrowseOutputFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "选择输出目录"
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                selectedOutputPath = dialog.FileName;
                OutputPathBox.Text = selectedOutputPath;
            }
        }

        private void Convert_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(selectedFilePath) || FormatBox.SelectedItem is not ComboBoxItem selectedItem)
            {
                MessageBox.Show("请先选择文件和格式。");
                return;
            }

            string format = selectedItem.Content.ToString();
            string outputDir = string.IsNullOrEmpty(selectedOutputPath)
                ? Path.GetDirectoryName(selectedFilePath)!
                : selectedOutputPath;

            string outputFile = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(selectedFilePath) + "." + format);

            currentProcess = new Process();
            var process = currentProcess;

            process.StartInfo.FileName = "ffmpeg";
            process.StartInfo.Arguments = $"-i \"{selectedFilePath}\" \"{outputFile}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null && e.Data.Contains("time="))
                {
                    Dispatcher.Invoke(() =>
                    {
                        // 模拟进度
                        ProgressBar.Value += 2;
                        if (ProgressBar.Value > 100) ProgressBar.Value = 100;
                    });
                }
            };

            process.Exited += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    StatusText.Text = "转换完成";
                    ProgressBar.Value = 100;
                });
            };

            try
            {
                process.Start();
                process.BeginErrorReadLine();
                StatusText.Text = "正在转换...";
                ProgressBar.Value = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("转换失败: " + ex.Message);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (currentProcess != null && !currentProcess.HasExited)
            {
                try
                {
                    currentProcess.Kill();
                    StatusText.Text = "已中止 ❌";
                    ProgressBar.Value = 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("无法中止进程: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("当前没有正在进行的转换任务。");
            }
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    selectedFilePath = files[0];
                    FilePathBox.Text = selectedFilePath;
                    StatusText.Text = "已加载文件";
                }
            }
        }
    }
}
