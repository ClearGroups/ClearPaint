using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ClearPaint.Models;
using ClearPaint.Services;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace ClearPaint
{
    public partial class MainWindow : Window
    {
        private readonly LanguageService _lang = LanguageService.I;
        private string? _fp;
        private bool _mod, _drawing;
        private PaintConfig _cfg = new();
        private Polyline? _line;
        private readonly List<Polyline> _lines = new();
        private readonly Stack<Polyline> _undoStack = new();

        private static readonly byte[] _key = SHA256.HashData(Encoding.UTF8.GetBytes("ClearPaint_Secure_Key_2026!"));
        private static readonly List<(string Name, Color Color)> _colors = new()
        {
            ("Чёрный",Colors.Black),("Белый",Colors.White),("Красный",Colors.Red),("Зелёный",Colors.Green),
            ("Синий",Colors.Blue),("Жёлтый",Colors.Yellow),("Оранжевый",Color.FromRgb(255,165,0)),
            ("Фиолетовый",Color.FromRgb(128,0,128)),("Серый",Colors.Gray),("Тёмно-серый",Color.FromRgb(64,64,64)),
            ("Светло-серый",Color.FromRgb(211,211,211)),("Коричневый",Color.FromRgb(139,69,19)),
            ("Розовый",Color.FromRgb(255,192,203)),("Голубой",Color.FromRgb(0,191,255)),
            ("Тёмно-зелёный",Color.FromRgb(0,100,0)),("Тёмно-синий",Color.FromRgb(0,0,139)),
            ("Тёмно-красный",Color.FromRgb(139,0,0)),("Золотой",Color.FromRgb(255,215,0)),
            ("Бежевый",Color.FromRgb(245,245,220)),("Бирюзовый",Color.FromRgb(64,224,208)),
        };

        public MainWindow()
        {
            InitializeComponent();
            Setup();
            ApplyConfig();
            SetLang(true);
        }

        private void Setup()
        {
            BrushSizeBox.ItemsSource = new int[] { 1, 2, 3, 4, 5, 6, 8, 10, 12, 16, 20, 24, 30, 40, 50 };
            BrushSizeBox.SelectedItem = 4;
            BrushSizeBox.SelectionChanged += (_, _) => { if (BrushSizeBox.SelectedItem is int s) _cfg.BrushSize = s; };

            NewMenuItem.Click += (_, _) => NewCanvas();
            OpenMenuItem.Click += (_, _) => Open();
            SaveMenuItem.Click += (_, _) => Save();
            SaveAsMenuItem.Click += (_, _) => SaveAs();
            ExitMenuItem.Click += (_, _) => Close();

            UndoMenuItem.Click += (_, _) => Undo();
            RedoMenuItem.Click += (_, _) => Redo();
            ClearCanvasMenuItem.Click += (_, _) => ClearCanvas();

            GitHubMenuItem.Click += (_, _) => Process.Start(new ProcessStartInfo("https://github.com/ClearGroups") { UseShellExecute = true });
            EnglishMenuItem.Click += (_, _) => SetLang(true);
            RussianMenuItem.Click += (_, _) => SetLang(false);

            DrawColorBtn.Click += (_, _) => ShowColorPicker(true);
            CanvasColorBtn.Click += (_, _) => ShowColorPicker(false);

            Closing += (_, e) =>
            {
                if (_mod)
                {
                    var r = MessageBox.Show(_lang.S("SavePrompt"), "ClearPaint", MessageBoxButton.YesNoCancel);
                    if (r == MessageBoxResult.Yes) Save();
                    else if (r == MessageBoxResult.Cancel) e.Cancel = true;
                }
            };
        }

        private void ApplyConfig()
        {
            PaintCanvas.Background = new SolidColorBrush(_cfg.CanvasColor);
            BrushSizeBox.SelectedItem = _cfg.BrushSize;
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _drawing = true; _undoStack.Clear();
                _line = new Polyline
                {
                    Stroke = new SolidColorBrush(_cfg.DrawColor),
                    StrokeThickness = _cfg.BrushSize,
                    StrokeLineJoin = PenLineJoin.Round,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                };
                _line.Points.Add(e.GetPosition(PaintCanvas));
                PaintCanvas.Children.Add(_line);
                _lines.Add(_line);
                _mod = true; UpdateStar();
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_drawing && _line != null)
                _line.Points.Add(e.GetPosition(PaintCanvas));
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e) { _drawing = false; _line = null; }
        private void Canvas_MouseLeave(object sender, MouseEventArgs e) { _drawing = false; _line = null; }

        private void Undo()
        {
            if (_lines.Count == 0) return;
            var last = _lines[^1]; _lines.RemoveAt(_lines.Count - 1);
            PaintCanvas.Children.Remove(last); _undoStack.Push(last);
            _mod = true; UpdateStar();
        }

        private void Redo()
        {
            if (_undoStack.Count == 0) return;
            var line = _undoStack.Pop(); PaintCanvas.Children.Add(line); _lines.Add(line);
            _mod = true; UpdateStar();
        }

        private void ClearCanvas()
        {
            PaintCanvas.Children.Clear(); _lines.Clear(); _undoStack.Clear();
            _mod = true; UpdateStar();
        }

        private void ShowColorPicker(bool isDraw)
        {
            int cols = 5, rows = 4, btnW = 56, btnH = 44, pad = 8, gap = 4;
            double ww = cols * (btnW + gap) + pad * 2 + 14, wh = rows * (btnH + gap) + pad * 2 + 38;
            var win = new Window { Title = isDraw ? "Цвет рисования" : "Цвет холста", Width = ww, Height = wh, WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = this, ResizeMode = ResizeMode.NoResize };
            var p = new WrapPanel { Width = ww - 16, Margin = new Thickness(pad) };
            foreach (var ci in _colors)
            {
                var bb = (ci.Color == Colors.White || ci.Color == Color.FromRgb(255, 255, 0)) ? Brushes.Gray : Brushes.Transparent;
                var b = new Button { Width = btnW, Height = btnH, Margin = new Thickness(gap / 2), Background = new SolidColorBrush(ci.Color), BorderBrush = bb, BorderThickness = new Thickness(1), ToolTip = ci.Name, Cursor = Cursors.Hand };
                var c = ci.Color;
                b.Click += (_, _) => { if (isDraw) _cfg.DrawColor = c; else { _cfg.CanvasColor = c; PaintCanvas.Background = new SolidColorBrush(c); } _mod = true; UpdateStar(); win.Close(); };
                p.Children.Add(b);
            }
            win.Content = p; win.ShowDialog();
        }

        private string GetConfigDir()
        {
            string dir = string.IsNullOrEmpty(_fp) ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop) : Path.GetDirectoryName(_fp)!;
            string configDir = Path.Combine(dir, "ClearConfig", "ClearPaint");
            Directory.CreateDirectory(configDir);
            return configDir;
        }

        private string GetConfigPath()
        {
            string name = string.IsNullOrEmpty(_fp) ? "untitled" : Path.GetFileNameWithoutExtension(_fp);
            return Path.Combine(GetConfigDir(), name + ".clep.config");
        }

        private void SaveConfig()
        {
            _cfg.Language = _lang.L;
            File.WriteAllText(GetConfigPath(), JsonSerializer.Serialize(_cfg, new JsonSerializerOptions { WriteIndented = true }));
        }

        private void LoadConfig()
        {
            string cp = GetConfigPath();
            if (File.Exists(cp))
            {
                try { _cfg = JsonSerializer.Deserialize<PaintConfig>(File.ReadAllText(cp)) ?? new PaintConfig(); }
                catch { _cfg = new PaintConfig(); }
            }
            else _cfg = new PaintConfig();
            _lang.L = _cfg.Language;
            ApplyConfig();
            SetLang(_lang.L == "en-US");
        }

        private string Encrypt(byte[] data)
        {
            using Aes a = Aes.Create(); a.Key = _key; a.GenerateIV(); byte[] iv = a.IV;
            using var e = a.CreateEncryptor();
            byte[] c = e.TransformFinalBlock(data, 0, data.Length);
            byte[] r = new byte[iv.Length + c.Length];
            Buffer.BlockCopy(iv, 0, r, 0, iv.Length);
            Buffer.BlockCopy(c, 0, r, iv.Length, c.Length);
            return Convert.ToHexString(r);
        }

        private byte[] Decrypt(string hex)
        {
            byte[] d = Convert.FromHexString(hex);
            using Aes a = Aes.Create(); a.Key = _key; a.IV = d[..16];
            using var dec = a.CreateDecryptor();
            return dec.TransformFinalBlock(d[16..], 0, d[16..].Length);
        }

        private void NewCanvas()
        {
            if (_mod) { var r = MessageBox.Show(_lang.S("SavePrompt"), "ClearPaint", MessageBoxButton.YesNoCancel); if (r == MessageBoxResult.Yes) Save(); else if (r == MessageBoxResult.Cancel) return; }
            PaintCanvas.Children.Clear(); _lines.Clear(); _undoStack.Clear();
            _fp = null; _mod = false; _cfg = new PaintConfig();
            ApplyConfig(); UpdateStar();
        }

        private void Save()
        {
            if (_fp == null) { SaveAs(); return; }
            var rtb = new RenderTargetBitmap((int)PaintCanvas.ActualWidth, (int)PaintCanvas.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(PaintCanvas);
            var enc = new PngBitmapEncoder(); enc.Frames.Add(BitmapFrame.Create(rtb));
            using var ms = new MemoryStream(); enc.Save(ms);
            File.WriteAllText(_fp, Encrypt(ms.ToArray()));
            SaveConfig(); _mod = false; UpdateStar();
        }

        private void SaveAs()
        {
            var d = new SaveFileDialog { Filter = "Clear Paint (*.clep)|*.clep", DefaultExt = "clep", FileName = _lang.S("Untitled"), Title = _lang.S("SaveTitle") };
            if (d.ShowDialog() == true) { _fp = d.FileName; Save(); }
        }

        private void Open()
        {
            if (_mod) { var r = MessageBox.Show(_lang.S("SavePrompt"), "ClearPaint", MessageBoxButton.YesNoCancel); if (r == MessageBoxResult.Yes) Save(); else if (r == MessageBoxResult.Cancel) return; }
            var d = new OpenFileDialog { Filter = "Clear Paint (*.clep)|*.clep", Title = _lang.S("OpenTitle") };
            if (d.ShowDialog() == true)
            {
                _fp = d.FileName; LoadConfig();
                byte[] pngData = Decrypt(File.ReadAllText(_fp));
                var img = new Image();
                using (var ms = new MemoryStream(pngData))
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit(); bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = ms; bmp.EndInit();
                    img.Source = bmp;
                    img.Width = bmp.PixelWidth; img.Height = bmp.PixelHeight;
                }
                PaintCanvas.Children.Clear(); _lines.Clear(); _undoStack.Clear();
                PaintCanvas.Children.Add(img);
                _mod = false; UpdateStar();
            }
        }

        private void UpdateStar()
        {
            Title = (_fp == null ? _lang.S("Untitled") : Path.GetFileNameWithoutExtension(_fp)) + ".clep" + (_mod ? "*" : "");
            ModifiedIndicator.Text = _mod ? "●" : "";
        }

        private void SetLang(bool en)
        {
            _lang.L = en ? "en-US" : "ru-RU";
            EnglishMenuItem.IsChecked = en; RussianMenuItem.IsChecked = !en;
            FileMenu.Header = _lang.S("File"); NewMenuItem.Header = _lang.S("New"); OpenMenuItem.Header = _lang.S("Open");
            SaveMenuItem.Header = _lang.S("Save"); SaveAsMenuItem.Header = _lang.S("SaveAs"); ExitMenuItem.Header = _lang.S("Exit");
            EditMenu.Header = _lang.S("Edit"); UndoMenuItem.Header = _lang.S("Undo"); RedoMenuItem.Header = _lang.S("Redo");
            ClearCanvasMenuItem.Header = _lang.S("ClearCanvas");
            SettingsMenu.Header = _lang.S("Settings"); GitHubMenuItem.Header = _lang.S("GitHub");
            LanguageMenu.Header = _lang.S("Language"); EnglishMenuItem.Header = _lang.S("English"); RussianMenuItem.Header = _lang.S("Russian");
            DrawColorBtn.ToolTip = _lang.S("DrawColor"); CanvasColorBtn.ToolTip = _lang.S("CanvasColor");
            LangStatusText.Text = _lang.S("LangStatus");
            UpdateStar();
        }
    }
}