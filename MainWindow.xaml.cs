using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace FFTool
{
    public partial class MainWindow : Window
    {
        private bool isNvidiaAvailable = false;
        private string? selectedFilePath = null;
        private string? selectedOutputPath = null;
        private Process? currentProcess = null;
        private bool isUpdatingSlider = false;
        private bool isUpdatingTextBox = false;

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
            this.Width = 1200;
            this.Height = 800;

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

            // 检测英伟达硬件加速支持
            CheckNvidiaAcceleration();

            // 设置默认状态文本
            StatusText.Text = "准备就绪";
        }

        private void CheckNvidiaAcceleration()
        {
            try
            {
                // 检测是否支持英伟达硬件加速
                var process = new Process();
                process.StartInfo.FileName = "ffmpeg";
                process.StartInfo.Arguments = "-encoders";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // 检查是否包含英伟达编码器
                isNvidiaAvailable = output.Contains("h264_nvenc") || output.Contains("hevc_nvenc");

                if (!isNvidiaAvailable)
                {
                    NvidiaAccelerationCheckBox.IsEnabled = false;
                    NvidiaAccelerationCheckBox.Content = "英伟达硬件加速不可用 (未检测到支持的显卡或驱动)";
                }
            }
            catch (Exception)
            {
                isNvidiaAvailable = false;
                NvidiaAccelerationCheckBox.IsEnabled = false;
                NvidiaAccelerationCheckBox.Content = "英伟达硬件加速不可用 (FFmpeg未安装或配置错误)";
            }
        }

        private void MediaTypeListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var item in MediaTypes)
                item.IsSelected = false;

            if (MediaTypeListBox.SelectedItem is MediaTypeItem selected)
            {
                selected.IsSelected = true;
                UpdateFormatOptions(selected.Name);

                // 显示或隐藏视频参数面板
                VideoParametersPanel.Visibility = selected.Name == "视频"
                    ? Visibility.Visible
                    : Visibility.Collapsed;
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

        // 码率滑块值改变事件
        private void BitrateSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isUpdatingTextBox) return;

            isUpdatingSlider = true;
            if (BitrateTextBox != null)
            {
                BitrateTextBox.Text = ((int)e.NewValue).ToString();
            }
            isUpdatingSlider = false;
        }

        // 码率文本框改变事件
        private void BitrateTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdatingSlider) return;

            if (int.TryParse(BitrateTextBox.Text, out int value))
            {
                isUpdatingTextBox = true;
                if (value >= BitrateSlider.Minimum && value <= BitrateSlider.Maximum)
                {
                    BitrateSlider.Value = value;
                }
                else if (value < BitrateSlider.Minimum)
                {
                    BitrateSlider.Value = BitrateSlider.Minimum;
                    BitrateTextBox.Text = BitrateSlider.Minimum.ToString();
                }
                else if (value > BitrateSlider.Maximum)
                {
                    BitrateSlider.Value = BitrateSlider.Maximum;
                    BitrateTextBox.Text = BitrateSlider.Maximum.ToString();
                }
                isUpdatingTextBox = false;
            }
        }

        // 帧率滑块值改变事件
        private void FramerateSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isUpdatingTextBox) return;

            isUpdatingSlider = true;
            if (FramerateTextBox != null)
            {
                FramerateTextBox.Text = ((int)e.NewValue).ToString();
            }
            isUpdatingSlider = false;
        }

        // 帧率文本框改变事件
        private void FramerateTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdatingSlider) return;

            if (int.TryParse(FramerateTextBox.Text, out int value))
            {
                isUpdatingTextBox = true;
                if (value >= FramerateSlider.Minimum && value <= FramerateSlider.Maximum)
                {
                    FramerateSlider.Value = value;
                }
                else if (value < FramerateSlider.Minimum)
                {
                    FramerateSlider.Value = FramerateSlider.Minimum;
                    FramerateTextBox.Text = FramerateSlider.Minimum.ToString();
                }
                else if (value > FramerateSlider.Maximum)
                {
                    FramerateSlider.Value = FramerateSlider.Maximum;
                    FramerateTextBox.Text = FramerateSlider.Maximum.ToString();
                }
                isUpdatingTextBox = false;
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

                // 自动分析码率
                int recommendedBitrate = AnalyzeVideoBitrate(selectedFilePath);
                BitrateTextBox.Text = recommendedBitrate.ToString();
                BitrateSlider.Value = recommendedBitrate;

                // 自动分析帧率
                int recommendedFramerate = AnalyzeVideoFramerate(selectedFilePath);
                FramerateTextBox.Text = recommendedFramerate.ToString();
                FramerateSlider.Value = recommendedFramerate;
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

        private async void Convert_Click(object sender, RoutedEventArgs e)
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

            string inputExt = Path.GetExtension(selectedFilePath).TrimStart('.').ToLower();
            string outputExt = format.ToLower();

            string outputFileName = Path.GetFileNameWithoutExtension(selectedFilePath);

            // 如果原格式和目标格式相同，自动加后缀
            if (inputExt == outputExt)
            {
                outputFileName += "_converted";
            }

            string outputFile = Path.Combine(outputDir, outputFileName + "." + format);

            string ffmpegArgs = BuildFFmpegArguments(selectedFilePath, outputFile);

            StatusText.Text = "🔄 正在转换...";
            ProgressBar.Value = 0;

            await Task.Run(() =>
            {
                try
                {
                    using var process = new Process();
                    process.StartInfo.FileName = "ffmpeg";
                    process.StartInfo.Arguments = ffmpegArgs;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;

                    process.Start();

                    string? line;
                    while ((line = process.StandardError.ReadLine()) != null)
                    {
                        if (line.Contains("time="))
                        {
                            Dispatcher.Invoke(() =>
                            {
                                ProgressBar.Value += 2;
                                if (ProgressBar.Value > 100) ProgressBar.Value = 100;
                            });
                        }
                    }

                    process.WaitForExit();

                    Dispatcher.Invoke(() =>
                    {
                        StatusText.Text = "✅ 转换完成";
                        ProgressBar.Value = 100;
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("转换失败: " + ex.Message);
                        StatusText.Text = "❌ 转换失败";
                    });
                }
            });
        }

        private string BuildFFmpegArguments(string inputFile, string outputFile)
        {
            var args = new StringBuilder();
            args.Append($"-i \"{inputFile}\"");

            // 获取当前选择的媒体类型
            var selectedMediaType = MediaTypeListBox.SelectedItem as MediaTypeItem;

            if (selectedMediaType?.Name == "视频")
            {
                // 硬件加速设置
                bool useNvidiaAcceleration = NvidiaAccelerationCheckBox.IsChecked == true && isNvidiaAvailable;

                if (useNvidiaAcceleration)
                {
                    // 添加硬件解码
                    args.Insert(0, "-hwaccel cuda -hwaccel_output_format cuda ");

                    // 获取输出格式
                    string outputFormat = Path.GetExtension(outputFile).ToLower();

                    // 根据输出格式选择合适的英伟达编码器
                    switch (outputFormat)
                    {
                        case ".mp4":
                        case ".mkv":
                        case ".avi":
                            args.Append(" -c:v h264_nvenc");
                            break;
                        case ".webm":
                            // WebM格式，如果支持VP9硬件编码则使用，否则回退到软件编码
                            args.Append(" -c:v libvpx-vp9");
                            break;
                        default:
                            args.Append(" -c:v h264_nvenc");
                            break;
                    }

                    // 英伟达编码器特定参数
                    args.Append(" -preset fast");
                    args.Append(" -rc vbr");
                }
                else
                {
                    // 软件编码
                    args.Append(" -c:v libx264");
                }

                // 码率设置
                int bitrate = (int)BitrateSlider.Value;
                if (useNvidiaAcceleration)
                {
                    args.Append($" -b:v {bitrate}k -maxrate {bitrate * 2}k -bufsize {bitrate * 2}k");
                }
                else
                {
                    args.Append($" -b:v {bitrate}k");
                }

                // 帧率设置
                int framerate = (int)FramerateSlider.Value;
                args.Append($" -r {framerate}");

                // 视频翻转设置
                var videoFilters = new List<string>();

                if (HorizontalFlipCheckBox.IsChecked == true)
                {
                    videoFilters.Add("hflip");
                }

                if (VerticalFlipCheckBox.IsChecked == true)
                {
                    videoFilters.Add("vflip");
                }

                if (videoFilters.Count > 0)
                {
                    if (useNvidiaAcceleration)
                    {
                        // 硬件加速时需要在GPU上进行滤镜处理
                        args.Append($" -vf \"hwdownload,format=nv12,{string.Join(",", videoFilters)},hwupload\"");
                    }
                    else
                    {
                        args.Append($" -vf \"{string.Join(",", videoFilters)}\"");
                    }
                }

                // 音频编码设置
                args.Append(" -c:a aac");
            }
            else if (selectedMediaType?.Name == "音频")
            {
                // 音频转换不需要硬件加速
                args.Append(" -c:a libmp3lame");
            }

            args.Append($" \"{outputFile}\"");
            return args.ToString();
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
                    string errorMessage = "转换失败: " + ex.Message;
                    if (NvidiaAccelerationCheckBox.IsChecked == true && ex.Message.Contains("cuda"))
                    {
                        errorMessage += "\n\n建议：尝试取消英伟达硬件加速选项后重试。";
                    }
                    MessageBox.Show(errorMessage);
                    StatusText.Text = "❌ 转换失败";
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

                    // 自动分析码率
                    int recommendedBitrate = AnalyzeVideoBitrate(selectedFilePath);
                    BitrateTextBox.Text = recommendedBitrate.ToString();
                    BitrateSlider.Value = recommendedBitrate;

                    // 自动分析帧率
                    int recommendedFramerate = AnalyzeVideoFramerate(selectedFilePath);
                    FramerateTextBox.Text = recommendedFramerate.ToString();
                    FramerateSlider.Value = recommendedFramerate;
                }
            }
        }

        private int AnalyzeVideoBitrate(string filePath)
        {
            try
            {
                var process = new Process();
                process.StartInfo.FileName = "ffmpeg";
                process.StartInfo.Arguments = $"-i \"{filePath}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                string ffmpegOutput = process.StandardError.ReadToEnd();
                process.WaitForExit();

                // 解析 "bitrate: XXXX kb/s"
                var match = System.Text.RegularExpressions.Regex.Match(ffmpegOutput, @"bitrate:\s*(\d+)\s*kb/s");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int bitrate))
                {
                    // 限制在滑块范围内
                    bitrate = Math.Max((int)BitrateSlider.Minimum, Math.Min((int)BitrateSlider.Maximum, bitrate));
                    return bitrate;
                }
            }
            catch { }
            return 2000; // 默认值
        }

        private int AnalyzeVideoFramerate(string filePath)
        {
            try
            {
                var process = new Process();
                process.StartInfo.FileName = "ffmpeg";
                process.StartInfo.Arguments = $"-i \"{filePath}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                string ffmpegOutput = process.StandardError.ReadToEnd();
                process.WaitForExit();

                // 解析 "fps" 或 "tbr"
                var match = System.Text.RegularExpressions.Regex.Match(ffmpegOutput, @"(\d+(?:\.\d+)?)\s*fps");
                if (match.Success && double.TryParse(match.Groups[1].Value, out double fps))
                {
                    int framerate = (int)Math.Round(fps);
                    framerate = Math.Max((int)FramerateSlider.Minimum, Math.Min((int)FramerateSlider.Maximum, framerate));
                    return framerate;
                }
                // 兼容 tbr 字段
                match = System.Text.RegularExpressions.Regex.Match(ffmpegOutput, @"(\d+(?:\.\d+)?)\s*tbr");
                if (match.Success && double.TryParse(match.Groups[1].Value, out double tbr))
                {
                    int framerate = (int)Math.Round(tbr);
                    framerate = Math.Max((int)FramerateSlider.Minimum, Math.Min((int)FramerateSlider.Maximum, framerate));
                    return framerate;
                }
            }
            catch { }
            return 30; // 默认值
        }
    }
}