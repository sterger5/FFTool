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
        private string? selectedSubtitlePath = null;
        // 音频参数相关字段
        private bool isUpdatingAudioVolumeSlider = false;
        private bool isUpdatingAudioVolumeTextBox = false;
        private bool isUpdatingAudioBitrateSlider = false;
        private bool isUpdatingAudioBitrateTextBox = false;

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

            // 由于默认选择视频类型，所以初始化时显示视频参数和字幕面板
            VideoAndSubtitlePanel.Visibility = Visibility.Visible;
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

                // 根据媒体类型显示对应的参数面板
                VideoAndSubtitlePanel.Visibility = selected.Name == "视频"
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                AudioParametersPanel.Visibility = selected.Name == "音频"
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

        // 音量滑块值改变事件
        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isUpdatingTextBox) return;

            isUpdatingSlider = true;
            if (VolumeTextBox != null)
            {
                VolumeTextBox.Text = ((int)e.NewValue).ToString();
            }
            isUpdatingSlider = false;
        }

        // 音量文本框改变事件
        private void VolumeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdatingSlider) return;

            if (int.TryParse(VolumeTextBox.Text, out int value))
            {
                isUpdatingTextBox = true;
                if (value >= VolumeSlider.Minimum && value <= VolumeSlider.Maximum)
                {
                    VolumeSlider.Value = value;
                }
                else if (value < VolumeSlider.Minimum)
                {
                    VolumeSlider.Value = VolumeSlider.Minimum;
                    VolumeTextBox.Text = VolumeSlider.Minimum.ToString();
                }
                else if (value > VolumeSlider.Maximum)
                {
                    VolumeSlider.Value = VolumeSlider.Maximum;
                    VolumeTextBox.Text = VolumeSlider.Maximum.ToString();
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

                // 如果是音频类型，自动填入真实信息
                if ((MediaTypeListBox.SelectedItem as MediaTypeItem)?.Name == "音频")
                {
                    var (br, sr) = AnalyzeAudioInfo(selectedFilePath);

                    Dispatcher.Invoke(() =>
                    {
                        // 比特率（限制到滑块范围）
                        br = Math.Clamp(br, (int)AudioBitrateSlider.Minimum, (int)AudioBitrateSlider.Maximum);
                        AudioBitrateSlider.Value = br;
                        AudioBitrateTextBox.Text = br.ToString();

                        // 采样率（找到对应 ComboBoxItem 并选中）
                        foreach (ComboBoxItem item in AudioSampleRateComboBox.Items)
                        {
                            if (item.Content?.ToString() == sr.ToString())
                            {
                                AudioSampleRateComboBox.SelectedItem = item;
                                break;
                            }
                        }
                    });
                }

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
            if (inputExt == outputExt)
                outputFileName += "_converted";

            string outputFile = Path.Combine(outputDir, outputFileName + "." + format);
            string ffmpegArgs = BuildFFmpegArguments(selectedFilePath, outputFile);

            StatusText.Text = "🔄 正在转换...";
            ProgressBar.Value = 0;

            bool doSplit = SplitVideoAudioCheckBox.IsChecked == true;

            await Task.Run(() =>
            {
                try
                {
                    if (doSplit)
                    {
                        string outDir = string.IsNullOrEmpty(selectedOutputPath)
                            ? Path.GetDirectoryName(selectedFilePath)!
                            : selectedOutputPath;

                        string fileNoExt = Path.GetFileNameWithoutExtension(selectedFilePath);

                        string videoOnlyFile = Path.Combine(outDir, fileNoExt + "_silent" + Path.GetExtension(selectedFilePath));
                        RunFFmpeg($"-i \"{selectedFilePath}\" -c:v copy -an -y \"{videoOnlyFile}\"");

                        string audioOnlyFile = Path.Combine(outDir, fileNoExt + "_audio.aac");
                        RunFFmpeg($"-i \"{selectedFilePath}\" -c:a copy -vn -y \"{audioOnlyFile}\"");

                        Dispatcher.Invoke(() =>
                        {
                            StatusText.Text = "✅ 分离完成";
                            ProgressBar.Value = 100;
                        });
                    }
                    else
                    {
                        currentProcess = new Process();
                        var process = currentProcess;
                        process.StartInfo.FileName = "ffmpeg";
                        process.StartInfo.Arguments = ffmpegArgs;
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardError = true;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.CreateNoWindow = true;

                        process.Start();

                        // 先获取总时长（秒）
                        double totalSeconds = 0;
                        var durMatch = System.Text.RegularExpressions.Regex.Match(ffmpegArgs, @"-i ""([^""]+)""");
                        if (durMatch.Success)
                        {
                            var durProcess = new Process();
                            durProcess.StartInfo.FileName = "ffmpeg";
                            durProcess.StartInfo.Arguments = $"-i \"{durMatch.Groups[1].Value}\"";
                            durProcess.StartInfo.UseShellExecute = false;
                            durProcess.StartInfo.RedirectStandardError = true;
                            durProcess.StartInfo.CreateNoWindow = true;
                            durProcess.Start();
                            string durOutput = durProcess.StandardError.ReadToEnd();
                            durProcess.WaitForExit();

                            var durRegex = System.Text.RegularExpressions.Regex.Match(durOutput, @"Duration:\s*(\d{2}):(\d{2}):(\d{2}\.\d{2})");
                            if (durRegex.Success)
                            {
                                totalSeconds = TimeSpan.Parse(durRegex.Value.Replace("Duration: ", "")).TotalSeconds;
                            }
                        }

                        string log = "";
                        string line;
                        while ((line = process.StandardError.ReadLine() ?? "") != "")
                        {
                            log += line + "\n";

                            var timeMatch = System.Text.RegularExpressions.Regex.Match(line, @"time=(\d{2}):(\d{2}):(\d{2}\.\d{2})");
                            if (timeMatch.Success && totalSeconds > 0)
                            {
                                double currentSeconds = TimeSpan.Parse(timeMatch.Value.Replace("time=", "")).TotalSeconds;
                                int percent = (int)(currentSeconds / totalSeconds * 100);
                                Dispatcher.Invoke(() => ProgressBar.Value = Math.Min(percent, 100));
                            }
                        }

                        process.WaitForExit();

                        Dispatcher.Invoke(() =>
                        {
                            // 用户点了“中止”后 currentProcess 会被设为 null
                            if (currentProcess == null)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    StatusText.Text = "⏹️ 已中止";
                                });
                            }
                            else if (process.ExitCode != 0)
                            {
                                string logFile = Path.Combine(Path.GetDirectoryName(selectedFilePath)!, "ffmpeg_error.log");
                                File.WriteAllText(logFile, log);
                                MessageBox.Show($"转换失败！\n\n错误日志已保存到：\n{logFile}\n\n请打开这个文件查看详细错误。", "转换失败", MessageBoxButton.OK, MessageBoxImage.Error);
                                StatusText.Text = "❌ 转换失败";
                            }
                            else
                            {
                                StatusText.Text = "✅ 转换完成";
                                ProgressBar.Value = 100;
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("转换失败: " + ex.Message);
                        StatusText.Text = "❌ 失败";
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
                // 运行时硬件能力检测
                bool useNvidiaAcceleration = false;
                string nvEncoder = GetNvEncoderName();   // 获取用户想用的 NVENC 编码器

                if (NvidiaAccelerationCheckBox.IsChecked == true && isNvidiaAvailable)
                {
                    string filePath = selectedFilePath ?? throw new InvalidOperationException("未选择任何文件");
                    if (CanUseNvenc(selectedFilePath, nvEncoder))
                    {
                        useNvidiaAcceleration = true;
                        args.Append($" -c:v {nvEncoder}");
                    }
                    else
                    {
                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(
                                $"当前硬件或驱动不支持使用 {nvEncoder} 处理此文件，已自动改用软件编码。",
                                "硬件加速提示",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                            NvidiaAccelerationCheckBox.IsChecked = false; // 去掉勾选
                        });
                    }
                }

                if (!useNvidiaAcceleration)
                {
                    // 软件编码分支
                    string selectedCodec = (VideoCodecComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "H.264 (AVC)";
                    args.Append(selectedCodec switch
                    {
                        "H.264 (AVC)" => " -c:v libx264",
                        "H.265 (HEVC)" => " -c:v libx265",
                        "VP9" => " -c:v libvpx-vp9",
                        "AV1" => " -c:v libaom-av1",
                        _ => " -c:v libx264"
                    });
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

                // 字幕处理
                if (!string.IsNullOrEmpty(selectedSubtitlePath))
                {
                    string subtitleFilter = $"subtitles='{selectedSubtitlePath.Replace("\\", "/").Replace("'", "\\'")}'";
                    videoFilters.Add(subtitleFilter);
                }

                if (videoFilters.Count > 0)
                {
                    if (useNvidiaAcceleration && !videoFilters.Any(f => f.Contains("subtitles")))
                    {
                        // 硬件加速时需要在GPU上进行滤镜处理（字幕除外）
                        args.Append($" -vf \"hwdownload,format=nv12,{string.Join(",", videoFilters)},hwupload\"");
                    }
                    else
                    {
                        // 有字幕时使用软件滤镜
                        args.Append($" -vf \"{string.Join(",", videoFilters)}\"");
                    }
                }

                // 音频编码设置
                args.Append(" -c:a aac");

                // 音量调节
                double volume = VolumeSlider.Value / 100.0; // 转换为小数形式
                args.Append($" -filter:a volume={volume:F2}");
            }
            else if (selectedMediaType?.Name == "音频")
            {
                // 获取输出格式
                string outputFormat = Path.GetExtension(outputFile).ToLower();

                // 根据输出格式选择音频编码器
                switch (outputFormat)
                {
                    case ".mp3":
                        args.Append(" -c:a libmp3lame");
                        break;
                    case ".aac":
                    case ".m4a":
                        args.Append(" -c:a aac");
                        break;
                    case ".flac":
                        args.Append(" -c:a flac");
                        break;
                    case ".ogg":
                        args.Append(" -c:a libvorbis");
                        break;
                    case ".wav":
                        args.Append(" -c:a pcm_s16le");
                        break;
                    case ".wma":
                        args.Append(" -c:a wmav2");
                        break;
                    default:
                        args.Append(" -c:a libmp3lame");
                        break;
                }

                // 音频比特率设置
                int audioBitrate = (int)AudioBitrateSlider.Value;
                if (outputFormat != ".flac" && outputFormat != ".wav") // 无损格式不需要比特率设置
                {
                    args.Append($" -b:a {audioBitrate}k");
                }

                // 采样率设置
                if (AudioSampleRateComboBox.SelectedItem is ComboBoxItem sampleRateItem)
                {
                    string sampleRate = sampleRateItem.Content?.ToString() ?? "44100";
                    args.Append($" -ar {sampleRate}");
                }

                // 声道设置
                if (MonoAudioCheckBox.IsChecked == true)
                {
                    args.Append(" -ac 1"); // 单声道
                }

                // 音量调节
                double audioVolume = AudioVolumeSlider.Value / 100.0;
                if (Math.Abs(audioVolume - 1.0) > 0.01) // 只有当音量不是100%时才添加滤镜
                {
                    args.Append($" -filter:a volume={audioVolume:F2}");
                }

                // 音频标准化
                if (NormalizeAudioCheckBox.IsChecked == true)
                {
                    if (args.ToString().Contains("-filter:a"))
                    {
                        // 如果已经有音频滤镜，则组合使用
                        args.Replace("-filter:a volume=", "-filter:a loudnorm,volume=");
                    }
                    else
                    {
                        args.Append(" -filter:a loudnorm");
                    }
                }
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
                    currentProcess = null;  // 关键：把变量设成 null，表示“用户主动中止”
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
        private void BrowseSubtitle_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new();
            openFileDialog.Filter = " 字幕文件 (*.srt;*.ass;*.ssa;*.vtt)|*.srt;*.ass;*.ssa;*.vtt|所有文件 (*.*)|*.*";
            openFileDialog.Title = "选择字幕文件";

            if (openFileDialog.ShowDialog() == true)
            {
                selectedSubtitlePath = openFileDialog.FileName;
                SubtitlePathBox.Text = selectedSubtitlePath;
                StatusText.Text = "字幕文件已选择";
            }
        }

        private void ClearSubtitle_Click(object sender, RoutedEventArgs e)
        {
            selectedSubtitlePath = null;
            SubtitlePathBox.Text = "";
            StatusText.Text = "字幕文件已清除";
        }
        // 音频音量滑块值改变事件
        private void AudioVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isUpdatingAudioVolumeTextBox) return;

            isUpdatingAudioVolumeSlider = true;
            if (AudioVolumeTextBox != null)
            {
                AudioVolumeTextBox.Text = ((int)e.NewValue).ToString();
            }
            isUpdatingAudioVolumeSlider = false;
        }

        // 音频音量文本框改变事件
        private void AudioVolumeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdatingAudioVolumeSlider) return;

            if (int.TryParse(AudioVolumeTextBox.Text, out int value))
            {
                isUpdatingAudioVolumeTextBox = true;
                if (value >= AudioVolumeSlider.Minimum && value <= AudioVolumeSlider.Maximum)
                {
                    AudioVolumeSlider.Value = value;
                }
                else if (value < AudioVolumeSlider.Minimum)
                {
                    AudioVolumeSlider.Value = AudioVolumeSlider.Minimum;
                    AudioVolumeTextBox.Text = AudioVolumeSlider.Minimum.ToString();
                }
                else if (value > AudioVolumeSlider.Maximum)
                {
                    AudioVolumeSlider.Value = AudioVolumeSlider.Maximum;
                    AudioVolumeTextBox.Text = AudioVolumeSlider.Maximum.ToString();
                }
                isUpdatingAudioVolumeTextBox = false;
            }
        }

        // 音频比特率滑块值改变事件
        private void AudioBitrateSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isUpdatingAudioBitrateTextBox) return;

            isUpdatingAudioBitrateSlider = true;
            if (AudioBitrateTextBox != null)
            {
                AudioBitrateTextBox.Text = ((int)e.NewValue).ToString();
            }
            isUpdatingAudioBitrateSlider = false;
        }

        // 音频比特率文本框改变事件
        private void AudioBitrateTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdatingAudioBitrateSlider) return;

            if (int.TryParse(AudioBitrateTextBox.Text, out int value))
            {
                isUpdatingAudioBitrateTextBox = true;
                if (value >= AudioBitrateSlider.Minimum && value <= AudioBitrateSlider.Maximum)
                {
                    AudioBitrateSlider.Value = value;
                }
                else if (value < AudioBitrateSlider.Minimum)
                {
                    AudioBitrateSlider.Value = AudioBitrateSlider.Minimum;
                    AudioBitrateTextBox.Text = AudioBitrateSlider.Minimum.ToString();
                }
                else if (value > AudioBitrateSlider.Maximum)
                {
                    AudioBitrateSlider.Value = AudioBitrateSlider.Maximum;
                    AudioBitrateTextBox.Text = AudioBitrateSlider.Maximum.ToString();
                }
                isUpdatingAudioBitrateTextBox = false;
            }
        }
        private void RunFFmpeg(string arguments)
        {
            try
            {
                using var p = new Process();
                p.StartInfo.FileName = "ffmpeg";
                p.StartInfo.Arguments = arguments;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = true;

                p.Start();

                // 读取所有错误输出
                string error = p.StandardError.ReadToEnd();
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();

                // 如果出错了，弹窗显示
                if (p.ExitCode != 0 || error.Contains("Error") || error.Contains("Invalid"))
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("FFmpeg 报错：\n\n" + error, "转换失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("运行 FFmpeg 出错：\n\n" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private (int bitrate, int sampleRate) AnalyzeAudioInfo(string filePath)
        {
            try
            {
                var p = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = $"-i \"{filePath}\"",
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                p.Start();
                string output = p.StandardError.ReadToEnd();
                p.WaitForExit();

                // 解析比特率
                var brMatch = System.Text.RegularExpressions.Regex.Match(output, @"bitrate:\s*(\d+)\s*kb/s");
                int bitrate = brMatch.Success ? int.Parse(brMatch.Groups[1].Value) : 128;

                // 解析采样率（优先 Audio 行，回退 Stream 行）
                var srMatch = System.Text.RegularExpressions.Regex.Match(output, @"Audio:.*?\s(\d+)\s*Hz");
                if (!srMatch.Success)
                    srMatch = System.Text.RegularExpressions.Regex.Match(output, @"Stream.*Audio:.*?\s(\d+)\s*Hz");
                int sampleRate = srMatch.Success ? int.Parse(srMatch.Groups[1].Value) : 44100;

                return (bitrate, sampleRate);
            }
            catch
            {
                return (128, 44100); // 默认值
            }
        }

        /// <summary>
        /// 快速检测「这张卡 + 这份文件 + 指定编码器」是否真的能用 NVENC。
        /// 只跑 5 帧，耗时 1~2 秒。
        /// </summary>
        private bool CanUseNvenc(string filePath, string nvEncoder)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-hwaccel cuda -i \"{filePath}\" -c:v {nvEncoder} -frames:v 5 -f null -",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var p = Process.Start(psi);
                if (p == null) return false;

                string err = p.StandardError.ReadToEnd();
                p.WaitForExit();

                return p.ExitCode == 0 &&
                       !err.Contains("not supported", StringComparison.OrdinalIgnoreCase) &&
                       !err.Contains("no capable device", StringComparison.OrdinalIgnoreCase) &&
                       !err.Contains("invalid", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 根据用户界面选中的编码器名称，返回对应的 NVENC 编码器字符串
        /// </summary>
        private string GetNvEncoderName()
        {
            if (VideoCodecComboBox.SelectedItem is ComboBoxItem item)
            {
                return item.Content?.ToString() switch
                {
                    "H.264 (AVC)" => "h264_nvenc",
                    "H.265 (HEVC)" => "hevc_nvenc",
                    "AV1" => "av1_nvenc",
                    _ => "h264_nvenc"
                };
            }
            return "h264_nvenc";
        }
    }
}