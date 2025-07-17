using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace FFTool
{
    public partial class MainWindow : Window
    {
        private string? selectedFilePath = null;
        private string? selectedOutputPath = null;
        private Process? currentProcess = null;
        public ObservableCollection<MediaTypeItem> MediaTypes { get; set; }

        // 定义各种媒体类型对应的格式
        private readonly Dictionary<string, List<string>> mediaFormats = new()
        {
            { "视频", new List<string> { "mp4", "avi", "mkv", "mov", "wmv", "flv", "webm" } },
            { "音频", new List<string> { "mp3", "wav", "flac", "aac", "ogg", "m4a", "wma" } },
            { "图片", new List<string> { "jpg", "png", "gif", "bmp", "webp", "tiff", "ico" } }
        };

        public MainWindow()
        {
            InitializeComponent();

            // 设置窗口初始大小
            this.Width = 1000;
            this.Height = 700;

            MediaTypes = new ObservableCollection<MediaTypeItem>
            {
                new MediaTypeItem { Name = "视频" },
                new MediaTypeItem { Name = "音频" },
                new MediaTypeItem { Name = "图片" }
            };
            MediaTypeListBox.ItemsSource = MediaTypes;

            // 默认选择视频类型
            MediaTypes[0].IsSelected = true;
            MediaTypeListBox.SelectedIndex = 0;
            UpdateFormatOptions("视频");

            // 设置默认状态文本
            StatusText.Text = "准备就绪";
        }

        private void MediaTypeListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var item in MediaTypes)
                item.IsSelected = false;

            if (MediaTypeListBox.SelectedItem is MediaTypeItem selected)
            {
                selected.IsSelected = true;
                UpdateFormatOptions(selected.Name);
            }
        }

        private void UpdateFormatOptions(string mediaType)
        {
            FormatBox.Items.Clear();

            if (mediaFormats.ContainsKey(mediaType))
            {
                foreach (string format in mediaFormats[mediaType])
                {
                    ComboBoxItem item = new ComboBoxItem { Content = format };
                    FormatBox.Items.Add(item);
                }

                // 默认选择第一个格式
                if (FormatBox.Items.Count > 0)
                {
                    FormatBox.SelectedIndex = 0;
                }
            }
        }

        public class MediaTypeItem : INotifyPropertyChanged
        {
            private string name = "";
            private bool isSelected;

            public string Name
            {
                get => name;
                set
                {
                    name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }

            public bool IsSelected
            {
                get => isSelected;
                set
                {
                    isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new();
            if (openFileDialog.ShowDialog() == true)
            {
                selectedFilePath = openFileDialog.FileName;
                FilePathBox.Text = selectedFilePath;
                StatusText.Text = "文件已选择";
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
                StatusText.Text = "输出目录已设置";
            }
        }

        private void Convert_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(selectedFilePath) || FormatBox.SelectedItem is not ComboBoxItem selectedItem)
            {
                MessageBox.Show("请先选择文件和格式。");
                return;
            }

            string? format = selectedItem.Content?.ToString();
            if (string.IsNullOrEmpty(format))
            {
                MessageBox.Show("请选择有效的格式。");
                return;
            }

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
                    StatusText.Text = "✅ 转换完成";
                    ProgressBar.Value = 100;
                });
            };

            try
            {
                process.Start();
                process.BeginErrorReadLine();
                StatusText.Text = "🔄 正在转换...";
                ProgressBar.Value = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("转换失败: " + ex.Message);
                StatusText.Text = "❌ 转换失败";
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (currentProcess != null && !currentProcess.HasExited)
            {
                try
                {
                    currentProcess.Kill();
                    StatusText.Text = "⏹️ 已中止";
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
                    StatusText.Text = "文件已选择";
                }
            }
        }
    }
}