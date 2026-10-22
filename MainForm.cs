using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace E_Key_Monitor;

internal enum DotSizeOption
{
    Small,
    Medium,
    Large
}

internal enum DockPosition
{
    TopRight,
    MiddleRight,
    BottomRight,
    BottomLeft,
    MiddleLeft,
    TopLeft,
    Custom
}

internal sealed class MonitorSettings
{
    public const int DefaultObserveVkCode = 0x45;

    public string Key { get; set; } = "E";
    public int ObserveVkCode { get; set; } = DefaultObserveVkCode;
    public int ActiveTimeMs { get; set; } = 200;
    public double Opacity { get; set; } = 0.8;
    public DotSizeOption DotSize { get; set; } = DotSizeOption.Medium;
    public DockPosition Position { get; set; } = DockPosition.MiddleLeft;
    public int? CustomX { get; set; }
    public int? CustomY { get; set; }
    public bool ClickThrough { get; set; } = true;
    public string? GameDirectory { get; set; }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true
    };

    public static MonitorSettings Load()
    {
        string configPath = GetConfigPath();
        if (!File.Exists(configPath))
        {
            return new MonitorSettings();
        }

        try
        {
            string json = File.ReadAllText(configPath);
            MonitorConfigDto? dto = JsonSerializer.Deserialize<MonitorConfigDto>(json, JsonOptions);
            return Merge(dto);
        }
        catch
        {
            return new MonitorSettings();
        }
    }

    public void Save()
    {
        MonitorConfigDto dto = new()
        {
            Key = Key,
            ActiveTime = ActiveTimeMs,
            Opacity = Opacity,
            DotSize = DotSize.ToString(),
            Position = Position.ToString(),
            CustomX = CustomX,
            CustomY = CustomY,
            ClickThrough = ClickThrough,
            GameDirectory = GameDirectory
        };

        string configPath = GetConfigPath();
        string? directory = Path.GetDirectoryName(configPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(dto, JsonOptions);
        File.WriteAllText(configPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private static string GetConfigPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "config.json");
    }

    /// <summary>
    /// Parses a configured observe key (single A-Z/0-9, case-insensitive)
    /// into its virtual-key code. Returns false for anything else so the
    /// caller can fall back to E.
    /// </summary>
    internal static bool TryParseObserveKey(string? key, out int vkCode, out string normalizedName)
    {
        vkCode = DefaultObserveVkCode;
        normalizedName = "E";

        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        string trimmed = key.Trim();
        if (trimmed.Length != 1)
        {
            return false;
        }

        char candidate = char.ToUpperInvariant(trimmed[0]);
        if ((candidate >= 'A' && candidate <= 'Z') || (candidate >= '0' && candidate <= '9'))
        {
            vkCode = candidate;
            normalizedName = candidate.ToString();
            return true;
        }

        return false;
    }

    private static MonitorSettings Merge(MonitorConfigDto? dto)
    {
        MonitorSettings settings = new();

        if (dto is null)
        {
            return settings;
        }

        if (!string.IsNullOrWhiteSpace(dto.Key))
        {
            settings.Key = dto.Key.Trim();
        }

        // The observe key is user-configurable (A-Z/0-9, case-insensitive).
        // Anything else falls back to E so a typo can never blind the monitor.
        if (!TryParseObserveKey(settings.Key, out int observeVk, out string observeName))
        {
            observeVk = DefaultObserveVkCode;
            observeName = "E";
        }

        settings.Key = observeName;
        settings.ObserveVkCode = observeVk;

        if (dto.ActiveTime is > 0)
        {
            settings.ActiveTimeMs = Math.Clamp(dto.ActiveTime.Value, 1, 10000);
        }

        if (dto.Opacity is > 0)
        {
            settings.Opacity = Math.Clamp(dto.Opacity.Value, 0.1, 1.0);
        }

        if (!string.IsNullOrWhiteSpace(dto.DotSize) &&
            Enum.TryParse(dto.DotSize, true, out DotSizeOption dotSize))
        {
            settings.DotSize = dotSize;
        }

        if (!string.IsNullOrWhiteSpace(dto.Position) &&
            Enum.TryParse(dto.Position, true, out DockPosition position))
        {
            settings.Position = position;
        }

        settings.CustomX = dto.CustomX;
        settings.CustomY = dto.CustomY;

        if (!string.IsNullOrWhiteSpace(dto.GameDirectory))
        {
            settings.GameDirectory = dto.GameDirectory.Trim();
        }

        if (dto.ClickThrough.HasValue)
        {
            settings.ClickThrough = dto.ClickThrough.Value;
        }

        return settings;
    }

    private sealed class MonitorConfigDto
    {
        [JsonPropertyName("key")]
        public string? Key { get; set; }

        [JsonPropertyName("activeTime")]
        public int? ActiveTime { get; set; }

        [JsonPropertyName("opacity")]
        public double? Opacity { get; set; }

        [JsonPropertyName("dotSize")]
        public string? DotSize { get; set; }

        [JsonPropertyName("position")]
        public string? Position { get; set; }

        [JsonPropertyName("customX")]
        public int? CustomX { get; set; }

        [JsonPropertyName("customY")]
        public int? CustomY { get; set; }

        [JsonPropertyName("clickThrough")]
        public bool? ClickThrough { get; set; }

        [JsonPropertyName("gameDirectory")]
        public string? GameDirectory { get; set; }
    }
}

internal sealed class MainForm : Form
{
    private const int VkE = 0x45;
    private const int WmKeyDown = 0x0100;
    private const int WmSysKeyDown = 0x0104;
    private const int WhKeyboardLl = 13;
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExLayered = 0x00080000;
    private const int WsExNoActivate = 0x08000000;
    private const int GwlExStyle = -20;
    private const int PlacementMargin = 16;
    private const int DotMargin = 6;

    // The dot is sized as a share of the screen height, so it occupies the same
    // proportion on every monitor and is not inflated by the Windows scaling
    // percentage (the app is per-monitor DPI aware, so its coordinates are
    // real device pixels).
    private const double SmallDotScreenRatio = 0.020;
    private const double MediumDotScreenRatio = 0.030;
    private const double LargeDotScreenRatio = 0.040;
    private const string FollowedProcessName = "NarakaBladepoint";
    private const uint BiRgb = 0;
    private const uint DibRgbColors = 0;
    private const uint UlwAlpha = 0x00000002;
    private const byte AcSrcOver = 0x00;
    private const byte AcSrcAlpha = 0x01;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpFrameChanged = 0x0020;
    private const long RenderRetryIntervalMs = 1000;
    private const long ShutdownWatchdogMs = 8000;

    private static readonly TimeSpan GameStartTimeout = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan GameExitGracePeriod = TimeSpan.FromSeconds(5);
    private static readonly IntPtr HwndTopMost = new(-1);
    private static readonly IntPtr HgdiError = new(-1);
    private static readonly Color ActiveDotColor = Color.FromArgb(255, 65, 255, 145);
    private static readonly Color InactiveDotColor = Color.FromArgb(255, 235, 235, 235);

    private readonly MonitorSettings _settings;
    private readonly Icon? _applicationIcon;
    private readonly System.Windows.Forms.Timer _statusTimer;
    private readonly System.Windows.Forms.Timer? _gameTimer;
    private readonly NotifyIcon _trayIcon;
    private readonly ContextMenuStrip _dotMenu;
    private readonly ContextMenuStrip _trayMenu;
    private readonly MenuItemSet _dotMenuItems;
    private readonly MenuItemSet _trayMenuItems;
    private readonly bool _followGame;
    private readonly bool _launchGame;
    private readonly bool _watchMode;
    private readonly long _gameStartTick;

    private LowLevelKeyboardProc? _keyboardProc;
    private IntPtr _hookHandle = IntPtr.Zero;
    private long _lastTriggerTick = -1_000_000;
    private bool _isActive;
    private bool _clickThrough;
    private bool _shuttingDown;
    private bool _placementInitialized;
    private bool _resourcesDisposed;
    private bool _initialRenderFailureShown;
    private bool _isDragging;
    private bool _dragMoved;
    private bool _gameSeen;
    private bool _gameStartTimeoutNotified;
    private long? _gameMissingSinceTick;
    private string? _lastDiagnosticLogPath;
    private bool _renderDirty;
    private long _lastRenderRetryTick;
    private System.Threading.Timer? _shutdownWatchdog;
    private int _observeVkCode = VkE;
    private string _observeKeyName = "E";

    private int _activeTimeMs;
    private double _windowOpacity;
    private DotSizeOption _dotSize;
    private DockPosition _position;
    private Point _dragStartCursor;
    private Point _dragStartLocation;

    public MainForm(bool followGame, bool launchGame, bool watchMode)
    {
        _followGame = followGame;
        _launchGame = launchGame;
        _watchMode = watchMode;
        _gameStartTick = Environment.TickCount64;
        _settings = MonitorSettings.Load();
        _activeTimeMs = _settings.ActiveTimeMs;
        _windowOpacity = _settings.Opacity;
        _dotSize = _settings.DotSize;
        _position = _settings.Position;
        _clickThrough = _settings.ClickThrough;
        _observeVkCode = _settings.ObserveVkCode;
        _observeKeyName = _settings.Key;

        if (_observeVkCode != VkE)
        {
            LogDiagnostic($"Observe key configured: {_observeKeyName} (VK=0x{_observeVkCode:X})", 0);
        }

        Text = "E键监控工具";
        AutoScaleMode = AutoScaleMode.None;
        StartPosition = FormStartPosition.Manual;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        ApplyDotMetrics();

        _statusTimer = new System.Windows.Forms.Timer
        {
            Interval = 50
        };
        _statusTimer.Tick += (_, _) => UpdateActivationState();

        if (_followGame)
        {
            _gameTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000
            };
            _gameTimer.Tick += (_, _) => UpdateGameFollowState();
        }

        _dotMenu = BuildSettingsMenu(out _dotMenuItems);
        _trayMenu = BuildSettingsMenu(out _trayMenuItems);

        _applicationIcon = LoadApplicationIcon();
        _trayIcon = new NotifyIcon
        {
            Icon = _applicationIcon ?? SystemIcons.Application,
            Visible = true,
            Text = $"E键监控工具({_observeKeyName})",
            ContextMenuStrip = _trayMenu
        };

        MouseDown += HandleMouseDown;
        MouseMove += HandleMouseMove;
        MouseUp += HandleMouseUp;
        MouseCaptureChanged += HandleMouseCaptureChanged;
        SizeChanged += (_, _) =>
        {
            Location = ClampLocationToWorkingArea(Location, Screen.FromRectangle(Bounds).WorkingArea);
            if (Visible)
            {
                MarkRenderDirtyOnFailure(RenderLayeredWindow(), Environment.TickCount64);
            }
        };

        ApplyPlacement(_position);
        _placementInitialized = true;
        UpdateMenuChecks();
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= WsExToolWindow | WsExNoActivate | WsExLayered;
            if (_clickThrough)
            {
                cp.ExStyle |= WsExTransparent;
            }

            return cp;
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        try
        {
            if (!EnsureGameDirectoryConfigured())
            {
                RequestShutdown();
                return;
            }

            if (_launchGame && !StartConfiguredGameLauncher())
            {
                RequestShutdown();
                return;
            }

            InstallKeyboardHook();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"无法安装全局 E 键监听。\n\n错误信息：{ex.Message}\n\n程序将安全退出。",
                "E键监控工具",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            BeginInvoke(RequestShutdown);
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        if (_watchMode)
        {
            // Watch mode stays in the tray until the game shows up, so the dot
            // is never painted at startup.
            StartTimers();
            Hide();
            return;
        }

        // Run after the first visible message so the layered window has a live handle.
        BeginInvoke(InitializeVisibleWindow);
    }

    protected override void WndProc(ref Message m)
    {
        const int WmDpiChanged = 0x02E0;
        if (m.Msg == WmDpiChanged)
        {
            base.WndProc(ref m);

            // The window moved to a monitor with a different scale: the dot has
            // to be recomputed for the new screen, then redrawn.
            ApplyDotMetrics();
            if (Visible)
            {
                MarkRenderDirtyOnFailure(RenderLayeredWindow(), Environment.TickCount64);
            }

            return;
        }

        base.WndProc(ref m);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // The visual surface is supplied by UpdateLayeredWindow.
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        // Avoid normal WinForms painting so the layered bitmap owns all pixels.
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        CleanupResources();
        base.OnFormClosed(e);
    }

    private ContextMenuStrip BuildSettingsMenu(out MenuItemSet items)
    {
        ContextMenuStrip menu = new();
        ToolStripMenuItem gameDirectoryItem = new("设置游戏目录...", null, (_, _) => SelectGameDirectory());
        ToolStripMenuItem createShortcutsItem = new("生成桌面快捷方式", null, (_, _) => CreateDesktopShortcuts());
        ToolStripMenuItem autoStartItem = new("开机自启", null, (_, _) => ToggleAutoStart());

        ToolStripMenuItem clickThroughItem = new("鼠标穿透", null, (_, _) => SetClickThrough(!_clickThrough));

        ToolStripMenuItem sizeMenu = new("切换大小");
        ToolStripMenuItem smallSizeItem = new("小", null, (_, _) => SetDotSize(DotSizeOption.Small));
        ToolStripMenuItem mediumSizeItem = new("中", null, (_, _) => SetDotSize(DotSizeOption.Medium));
        ToolStripMenuItem largeSizeItem = new("大", null, (_, _) => SetDotSize(DotSizeOption.Large));
        sizeMenu.DropDownItems.AddRange(new ToolStripItem[]
        {
            smallSizeItem,
            mediumSizeItem,
            largeSizeItem
        });

        ToolStripMenuItem positionMenu = new("切换位置");
        ToolStripMenuItem topRightItem = new("右上", null, (_, _) => SetDockPosition(DockPosition.TopRight));
        ToolStripMenuItem middleRightItem = new("右中", null, (_, _) => SetDockPosition(DockPosition.MiddleRight));
        ToolStripMenuItem bottomRightItem = new("右下", null, (_, _) => SetDockPosition(DockPosition.BottomRight));
        ToolStripMenuItem bottomLeftItem = new("左下", null, (_, _) => SetDockPosition(DockPosition.BottomLeft));
        ToolStripMenuItem middleLeftItem = new("左中", null, (_, _) => SetDockPosition(DockPosition.MiddleLeft));
        ToolStripMenuItem topLeftItem = new("左上", null, (_, _) => SetDockPosition(DockPosition.TopLeft));
        positionMenu.DropDownItems.AddRange(new ToolStripItem[]
        {
            topRightItem,
            middleRightItem,
            bottomRightItem,
            bottomLeftItem,
            middleLeftItem,
            topLeftItem
        });

        ToolStripMenuItem exitItem = new("退出", null, (_, _) => RequestShutdown());

        menu.Items.Add(clickThroughItem);
        menu.Items.Add(gameDirectoryItem);
        menu.Items.Add(createShortcutsItem);
        menu.Items.Add(autoStartItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(sizeMenu);
        menu.Items.Add(positionMenu);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);
        menu.Opening += (_, _) => UpdateMenuChecks();

        items = new MenuItemSet(
            clickThroughItem,
            gameDirectoryItem,
            smallSizeItem,
            mediumSizeItem,
            largeSizeItem,
            topRightItem,
            middleRightItem,
            bottomRightItem,
            bottomLeftItem,
            middleLeftItem,
            topLeftItem,
            autoStartItem);

        return menu;
    }

    private bool EnsureGameDirectoryConfigured()
    {
        if (_watchMode)
        {
            // Watch mode only watches the process name; it never launches the
            // game, so it must not ask for the game directory - a sign-in would
            // pop a modal folder picker on every boot.
            return true;
        }

        if (TryNormalizeAndValidateDirectory(_settings.GameDirectory, out string? normalizedDirectory, out _))
        {
            _settings.GameDirectory = normalizedDirectory;
            return true;
        }

        // Pure standalone mode must not force game directory selection:
        // the user may only want the E-key dot without the game installed.
        // Follow/launch modes still require it and cancel means exit.
        bool directoryRequired = _followGame || _launchGame;

        while (!_shuttingDown)
        {
            using FolderBrowserDialog dialog = new()
            {
                Description = directoryRequired
                    ? "请选择同时包含 StartGame.exe 和 NarakaBladepoint.exe 的游戏目录。"
                    : "请选择同时包含 StartGame.exe 和 NarakaBladepoint.exe 的游戏目录（仅独立监控可跳过，以后可从托盘“设置游戏目录...”补选）。",
                ShowNewFolderButton = false,
                SelectedPath = Directory.Exists(_settings.GameDirectory) ? _settings.GameDirectory : string.Empty
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                if (directoryRequired)
                {
                    return false;
                }

                DialogResult skipChoice = MessageBox.Show(
                    this,
                    "尚未设置游戏目录。\n\n选择“是”跳过并仅运行独立监控（以后可从托盘补选）；\n选择“否”重新选择目录；\n选择“取消”退出程序。",
                    "E键监控工具",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1);
                if (skipChoice == DialogResult.Yes)
                {
                    LogDiagnostic("Skip game directory in standalone mode", 0);
                    return true;
                }

                if (skipChoice == DialogResult.Cancel)
                {
                    return false;
                }

                continue;
            }

            if (!TryNormalizeAndValidateDirectory(dialog.SelectedPath, out normalizedDirectory, out string error))
            {
                MessageBox.Show(this, error, "E键监控工具", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                continue;
            }

            string? previousDirectory = _settings.GameDirectory;
            _settings.GameDirectory = normalizedDirectory;
            try
            {
                _settings.Save();
                return true;
            }
            catch (Exception ex)
            {
                _settings.GameDirectory = previousDirectory;
                LogDiagnostic("Save game directory", 0, ex);
                MessageBox.Show(this, $"游戏目录保存失败，已保留之前的设置。\n\n{ex.Message}", "E键监控工具", MessageBoxButtons.OK, MessageBoxIcon.Error);
                continue;
            }
        }

        return false;
    }

    private void SelectGameDirectory()
    {
        string? previousDirectory = _settings.GameDirectory;
        using FolderBrowserDialog dialog = new()
        {
            Description = "请选择同时包含 StartGame.exe 和 NarakaBladepoint.exe 的游戏目录。",
            ShowNewFolderButton = false,
            SelectedPath = Directory.Exists(previousDirectory) ? previousDirectory : string.Empty
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (!TryNormalizeAndValidateDirectory(dialog.SelectedPath, out string? normalizedDirectory, out string error))
        {
            MessageBox.Show(this, error, "E键监控工具", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _settings.GameDirectory = normalizedDirectory;
        try
        {
            _settings.Save();
        }
        catch (Exception ex)
        {
            _settings.GameDirectory = previousDirectory;
            LogDiagnostic("Save game directory", 0, ex);
            MessageBox.Show(this, $"游戏目录保存失败，已保留之前的设置。\n\n{ex.Message}", "E键监控工具", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private bool StartConfiguredGameLauncher()
    {
        if (string.IsNullOrWhiteSpace(_settings.GameDirectory))
        {
            MessageBox.Show(
                this,
                "尚未设置游戏目录，请先选择游戏目录。",
                "E键监控工具",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }

        string launcherPath = Path.Combine(_settings.GameDirectory, "StartGame.exe");
        if (!IsRegularFile(launcherPath))
        {
            MessageBox.Show(
                this,
                $"找不到游戏启动器：\n{launcherPath}",
                "E键监控工具",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return false;
        }

        try
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = launcherPath,
                WorkingDirectory = _settings.GameDirectory,
                UseShellExecute = true
            };

            Process? launcherProcess = Process.Start(startInfo);
            if (launcherProcess is null)
            {
                throw new InvalidOperationException("Windows 没有返回启动器进程。");
            }

            launcherProcess.Dispose();
            return true;
        }
        catch (Exception ex)
        {
            LogDiagnostic("启动游戏启动器", 0, ex);
            MessageBox.Show(
                this,
                $"启动游戏启动器失败。\n\n{ex.Message}",
                "E键监控工具",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return false;
        }
    }

    private void CreateDesktopShortcuts()
    {
        try
        {
            string desktopDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (string.IsNullOrWhiteSpace(desktopDirectory))
            {
                throw new InvalidOperationException("无法确定当前用户的桌面路径。");
            }

            string executablePath = Path.Combine(AppContext.BaseDirectory, "E_Key_Monitor.exe");
            if (!IsRegularFile(executablePath))
            {
                throw new FileNotFoundException("找不到当前监控工具 EXE。", executablePath);
            }

            Type shellType = Type.GetTypeFromProgID("WScript.Shell")
                ?? throw new InvalidOperationException("系统不支持创建 Windows 快捷方式。");
            object shellObject = Activator.CreateInstance(shellType)
                ?? throw new InvalidOperationException("无法创建 Windows 快捷方式组件。");

            try
            {
                dynamic shell = shellObject;
                string workingDirectory = Path.GetDirectoryName(executablePath) ?? AppContext.BaseDirectory;
                SaveShortcut(
                    shell,
                    Path.Combine(desktopDirectory, "E键监控工具.lnk"),
                    executablePath,
                    string.Empty,
                    workingDirectory,
                    "E键监控工具");
                SaveShortcut(
                    shell,
                    Path.Combine(desktopDirectory, "启动StartGame和监控.lnk"),
                    executablePath,
                    "--launch-game",
                    workingDirectory,
                    "启动 StartGame 和 E 键监控工具");
            }
            finally
            {
                ReleaseComObject(shellObject);
            }

            MessageBox.Show(
                this,
                "已在桌面生成两个快捷方式：\n\nE键监控工具\n启动StartGame和监控",
                "E键监控工具",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            LogDiagnostic("生成桌面快捷方式", 0, ex);
            MessageBox.Show(
                this,
                $"生成桌面快捷方式失败。\n\n{ex.Message}",
                "E键监控工具",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static void SaveShortcut(
        dynamic shell,
        string shortcutPath,
        string executablePath,
        string arguments,
        string workingDirectory,
        string description)
    {
        object shortcutObject = shell.CreateShortcut(shortcutPath);
        try
        {
            dynamic shortcut = shortcutObject;
            shortcut.TargetPath = executablePath;
            shortcut.Arguments = arguments;
            shortcut.WorkingDirectory = workingDirectory;
            shortcut.IconLocation = $"{executablePath},0";
            shortcut.Description = description;
            shortcut.Save();
        }
        finally
        {
            ReleaseComObject(shortcutObject);
        }
    }

    private static void ReleaseComObject(object value)
    {
        if (Marshal.IsComObject(value))
        {
            Marshal.FinalReleaseComObject(value);
        }
    }

    private static bool TryNormalizeAndValidateDirectory(string? directoryPath, out string? normalizedDirectory, out string error)
    {
        normalizedDirectory = null;
        error = "请选择同时包含 StartGame.exe 和 NarakaBladepoint.exe 的游戏目录。";

        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return false;
        }

        try
        {
            normalizedDirectory = Path.GetFullPath(directoryPath.Trim());
            if (!Directory.Exists(normalizedDirectory))
            {
                error = "选择的路径不是现有文件夹。";
                normalizedDirectory = null;
                return false;
            }

            if (!IsRegularFile(Path.Combine(normalizedDirectory, "StartGame.exe")) ||
                !IsRegularFile(Path.Combine(normalizedDirectory, "NarakaBladepoint.exe")))
            {
                error = "选择的文件夹必须包含 StartGame.exe 和 NarakaBladepoint.exe 文件。";
                normalizedDirectory = null;
                return false;
            }

            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            error = $"选择的路径无效。\n\n{ex.Message}";
            return false;
        }
    }

    private static bool IsRegularFile(string path)
    {
        try
        {
            return File.Exists(path) && (File.GetAttributes(path) & FileAttributes.Directory) == 0;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static Icon? LoadApplicationIcon()
    {
        try
        {
            return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }
        catch
        {
            return null;
        }
    }

    private void InstallKeyboardHook()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            return;
        }

        _keyboardProc = HookCallback;
        IntPtr moduleHandle = GetCurrentModuleHandle();
        _hookHandle = SetWindowsHookEx(WhKeyboardLl, _keyboardProc, moduleHandle, 0);

        if (_hookHandle == IntPtr.Zero)
        {
            int error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"SetWindowsHookEx(WH_KEYBOARD_LL) 失败，Win32Error={error}。");
        }
    }

    private static IntPtr GetCurrentModuleHandle()
    {
        using Process currentProcess = Process.GetCurrentProcess();
        string? moduleName = currentProcess.MainModule?.ModuleName;
        return GetModuleHandle(moduleName);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode >= 0)
            {
                int message = wParam.ToInt32();
                if (message is WmKeyDown or WmSysKeyDown)
                {
                    int vkCode = Marshal.ReadInt32(lParam);
                    if (vkCode == _observeVkCode)
                    {
                        _lastTriggerTick = Environment.TickCount64;
                    }
                }
            }
        }
        catch
        {
            // Always keep the low-level keyboard hook chain moving.
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private void UpdateActivationState()
    {
        long now = Environment.TickCount64;
        bool shouldBeActive = now - _lastTriggerTick <= _activeTimeMs;

        if (shouldBeActive != _isActive)
        {
            _isActive = shouldBeActive;
            if (Visible)
            {
                MarkRenderDirtyOnFailure(RenderLayeredWindow(), now);
            }
        }
        else if (_renderDirty && Visible && now - _lastRenderRetryTick >= RenderRetryIntervalMs)
        {
            // Transient render failures self-heal here at ~1 retry/second,
            // throttled so the diagnostic log cannot be flooded.
            _lastRenderRetryTick = now;
            if (RenderLayeredWindow())
            {
                _renderDirty = false;
            }
        }
    }

    private void MarkRenderDirtyOnFailure(bool renderSucceeded, long nowTick)
    {
        if (renderSucceeded)
        {
            _renderDirty = false;
            return;
        }

        _renderDirty = true;
        _lastRenderRetryTick = nowTick;
    }

    private void InitializeVisibleWindow()
    {
        if (_shuttingDown || _resourcesDisposed || IsDisposed)
        {
            return;
        }

        ApplyDotMetrics();

        // The first visible render keeps the hard exit as a last resort, but
        // arms the self-heal flag so later ticks can recover transient faults.
        if (!ApplyClickThroughStyle() || !RenderLayeredWindow())
        {
            _renderDirty = true;
            _lastRenderRetryTick = Environment.TickCount64;
            ShowInitialRenderFailureAndExit();
            return;
        }

        _renderDirty = false;
        StartTimers();
    }

    private void StartTimers()
    {
        _statusTimer.Start();
        _gameTimer?.Start();
    }

    private void ShowDot()
    {
        if (Visible || _shuttingDown || _resourcesDisposed)
        {
            return;
        }

        ApplyDotMetrics();

        // The layered render refuses to paint a hidden window, so the window has
        // to be up before the dot is drawn.
        Visible = true;

        MarkRenderDirtyOnFailure(RenderLayeredWindow(), Environment.TickCount64);
    }

    private void HideDot()
    {
        if (!Visible || _shuttingDown || _resourcesDisposed)
        {
            return;
        }

        Visible = false;
    }

    private bool RenderLayeredWindow()
    {
        if (_resourcesDisposed)
        {
            return LogRenderFailure(
                "Validate window resources",
                0,
                new ObjectDisposedException(nameof(MainForm)));
        }

        IntPtr windowHandle = IsHandleCreated ? Handle : IntPtr.Zero;
        if (windowHandle == IntPtr.Zero || !Visible)
        {
            return LogRenderFailure(
                "Validate visible window handle",
                0,
                new InvalidOperationException(
                    $"IsHandleCreated={IsHandleCreated}, Handle=0x{windowHandle.ToInt64():X}, Visible={Visible}."));
        }

        int width = ClientSize.Width;
        int height = ClientSize.Height;
        if (width <= 0 || height <= 0)
        {
            return LogRenderFailure(
                "Validate window size",
                0,
                new InvalidOperationException($"Invalid client size: {width}x{height}."));
        }

        Rectangle windowBounds = new(Left, Top, width, height);
        Rectangle workingArea = Screen.FromRectangle(windowBounds).WorkingArea;
        if (!workingArea.Contains(windowBounds))
        {
            // Contain instead of failing: clamp a partially-off-screen window
            // back into the working area and keep rendering.
            Point contained = ClampLocationToWorkingArea(Location, workingArea);
            if (contained != Location)
            {
                Location = contained;
                windowBounds = new(contained.X, contained.Y, width, height);
            }

            if (!workingArea.Contains(windowBounds))
            {
                return LogRenderFailure(
                    "Validate window coordinates",
                    0,
                    new InvalidOperationException(
                        $"Window bounds {windowBounds} are outside working area {workingArea}."));
            }
        }

        long requiredStyles = WsExLayered | WsExNoActivate | WsExToolWindow;
        Marshal.SetLastPInvokeError(0);
        long currentStyles = GetWindowLongPtrCompat(windowHandle, GwlExStyle).ToInt64();
        int getStyleError = Marshal.GetLastWin32Error();
        if (currentStyles == 0 && getStyleError != 0)
        {
            return LogRenderFailure("GetWindowLongPtr(validate GWL_EXSTYLE)", getStyleError);
        }

        if ((currentStyles & requiredStyles) != requiredStyles)
        {
            return LogRenderFailure(
                "Validate layered window style",
                0,
                new InvalidOperationException($"Extended style 0x{currentStyles:X} does not include 0x{requiredStyles:X}."));
        }

        int diameter = GetEffectiveDotDiameter();
        int maxDiameter = Math.Min(width, height) - DotMargin;
        if (maxDiameter <= 0)
        {
            return LogRenderFailure(
                "Validate dot diameter",
                0,
                new InvalidOperationException(
                    $"Diameter={diameter}, maximum={maxDiameter}, client size={width}x{height}."));
        }

        if (diameter <= 0 || diameter > maxDiameter)
        {
            // Contain instead of failing: a stale window size must never take
            // the whole tool down, the dot is clamped to what actually fits.
            LogDiagnostic($"Clamp dot diameter to {maxDiameter} (requested {diameter})", 0);
            diameter = maxDiameter;
        }

        int x = (width - diameter) / 2;
        int y = (height - diameter) / 2;
        if (x < 0 || y < 0 || x + diameter > width || y + diameter > height)
        {
            return LogRenderFailure(
                "Validate dot coordinates",
                0,
                new InvalidOperationException(
                    $"Dot bounds ({x}, {y}, {diameter}, {diameter}) exceed client size {width}x{height}."));
        }

        byte sourceConstantAlpha = GetLayerOpacityByte();
        if (sourceConstantAlpha == 0)
        {
            return LogRenderFailure(
                "Validate source alpha",
                0,
                new InvalidOperationException("SourceConstantAlpha must be greater than zero."));
        }

        bool renderSucceeded = false;
        bool cleanupSucceeded = true;
        IntPtr screenDc = IntPtr.Zero;
        IntPtr memoryDc = IntPtr.Zero;
        IntPtr hBitmap = IntPtr.Zero;
        IntPtr oldBitmap = IntPtr.Zero;
        string currentStep = "Initialize layered render";

        try
        {
            do
            {
                currentStep = "GetDC";
                Marshal.SetLastPInvokeError(0);
                screenDc = GetDC(IntPtr.Zero);
                if (screenDc == IntPtr.Zero)
                {
                    LogRenderFailure(currentStep, Marshal.GetLastWin32Error());
                    break;
                }

                currentStep = "CreateCompatibleDC";
                Marshal.SetLastPInvokeError(0);
                memoryDc = CreateCompatibleDC(screenDc);
                if (memoryDc == IntPtr.Zero)
                {
                    LogRenderFailure(currentStep, Marshal.GetLastWin32Error());
                    break;
                }

                int stride = checked(width * 4);
                BitmapInfo bitmapInfo = new()
                {
                    Header = new BitmapInfoHeader
                    {
                        Size = (uint)Marshal.SizeOf<BitmapInfoHeader>(),
                        Width = width,
                        Height = -height,
                        Planes = 1,
                        BitCount = 32,
                        Compression = BiRgb,
                        SizeImage = checked((uint)(stride * height))
                    }
                };

                currentStep = "CreateDIBSection";
                Marshal.SetLastPInvokeError(0);
                hBitmap = CreateDIBSection(
                    screenDc,
                    ref bitmapInfo,
                    DibRgbColors,
                    out IntPtr pixelBuffer,
                    IntPtr.Zero,
                    0);
                if (hBitmap == IntPtr.Zero || pixelBuffer == IntPtr.Zero)
                {
                    LogRenderFailure(currentStep, Marshal.GetLastWin32Error());
                    break;
                }

                currentStep = "SelectObject(layered bitmap)";
                Marshal.SetLastPInvokeError(0);
                oldBitmap = SelectObject(memoryDc, hBitmap);
                if (!IsValidGdiObject(oldBitmap))
                {
                    LogRenderFailure(currentStep, Marshal.GetLastWin32Error());
                    break;
                }

                currentStep = "Draw premultiplied alpha bitmap";
                using (Bitmap bitmap = new(width, height, stride, PixelFormat.Format32bppPArgb, pixelBuffer))
                using (Graphics graphics = Graphics.FromImage(bitmap))
                using (SolidBrush brush = new(GetCurrentDotColor()))
                {
                    graphics.Clear(Color.Transparent);
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.FillEllipse(brush, new Rectangle(x, y, diameter, diameter));
                    graphics.Flush();
                }

                int backgroundAlpha = Marshal.ReadByte(pixelBuffer, 3);
                int centerPixelOffset = ((y + diameter / 2) * stride) + ((x + diameter / 2) * 4) + 3;
                int centerAlpha = Marshal.ReadByte(pixelBuffer, centerPixelOffset);
                if (backgroundAlpha != 0 || centerAlpha == 0)
                {
                    LogRenderFailure(
                        "Validate DIB alpha pixels",
                        0,
                        new InvalidOperationException(
                            $"Background alpha={backgroundAlpha}, center alpha={centerAlpha}."));
                    break;
                }

                PointStruct topPosition = new(Left, Top);
                SizeStruct size = new(width, height);
                PointStruct sourcePosition = new(0, 0);
                BlendFunction blend = new()
                {
                    BlendOp = AcSrcOver,
                    BlendFlags = 0,
                    SourceConstantAlpha = sourceConstantAlpha,
                    AlphaFormat = AcSrcAlpha
                };

                currentStep = "UpdateLayeredWindow";
                Marshal.SetLastPInvokeError(0);
                renderSucceeded = UpdateLayeredWindow(
                    windowHandle,
                    screenDc,
                    ref topPosition,
                    ref size,
                    memoryDc,
                    ref sourcePosition,
                    0,
                    ref blend,
                    UlwAlpha);

                if (!renderSucceeded)
                {
                    LogRenderFailure(currentStep, Marshal.GetLastWin32Error());
                }
            } while (false);
        }
        catch (Exception ex)
        {
            LogRenderFailure(currentStep, 0, ex);
        }
        finally
        {
            if (IsValidGdiObject(oldBitmap) && memoryDc != IntPtr.Zero)
            {
                Marshal.SetLastPInvokeError(0);
                IntPtr restoreResult = SelectObject(memoryDc, oldBitmap);
                if (!IsValidGdiObject(restoreResult))
                {
                    cleanupSucceeded = false;
                    LogRenderFailure("SelectObject(restore previous bitmap)", Marshal.GetLastWin32Error());
                }
            }

            if (memoryDc != IntPtr.Zero)
            {
                Marshal.SetLastPInvokeError(0);
                if (!DeleteDC(memoryDc))
                {
                    cleanupSucceeded = false;
                    LogRenderFailure("DeleteDC", Marshal.GetLastWin32Error());
                }
            }

            if (hBitmap != IntPtr.Zero)
            {
                Marshal.SetLastPInvokeError(0);
                if (!DeleteObject(hBitmap))
                {
                    cleanupSucceeded = false;
                    LogRenderFailure("DeleteObject(layered bitmap)", Marshal.GetLastWin32Error());
                }
            }

            if (screenDc != IntPtr.Zero)
            {
                Marshal.SetLastPInvokeError(0);
                if (ReleaseDC(IntPtr.Zero, screenDc) == 0)
                {
                    cleanupSucceeded = false;
                    LogRenderFailure("ReleaseDC", Marshal.GetLastWin32Error());
                }
            }
        }

        return renderSucceeded && cleanupSucceeded;
    }

    private bool LogRenderFailure(string step, int win32Error, Exception? exception = null)
    {
        LogDiagnostic(step, win32Error, exception);
        return false;
    }

    private void LogDiagnostic(string step, int win32Error = 0, Exception? exception = null)
    {
        try
        {
            exception ??= win32Error != 0
                ? new System.ComponentModel.Win32Exception(win32Error)
                : null;

            string entry =
                $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] " +
                $"Step={step}; Win32Error={win32Error}; " +
                $"Exception={(exception is null ? "none" : exception)}{Environment.NewLine}";

            foreach (string path in GetDiagnosticLogPaths())
            {
                try
                {
                    string? directory = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.AppendAllText(path, entry);
                    _lastDiagnosticLogPath = path;
                    break;
                }
                catch
                {
                    // Try the next writable diagnostic location.
                }
            }
        }
        catch
        {
            // Diagnostics must never crash the monitor.
        }
    }

    private static IEnumerable<string> GetDiagnosticLogPaths()
    {
        yield return Path.Combine(Environment.CurrentDirectory, "E_Key_Monitor.log");
        yield return Path.Combine(AppContext.BaseDirectory, "E_Key_Monitor.log");

        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrEmpty(localAppData))
        {
            yield return Path.Combine(localAppData, "E_Key_Monitor", "E_Key_Monitor.log");
        }
    }

    private void ShowInitialRenderFailureAndExit()
    {
        if (_initialRenderFailureShown || _shuttingDown)
        {
            return;
        }

        _initialRenderFailureShown = true;
        string logLocation = _lastDiagnosticLogPath ?? "日志写入失败，请检查目录写入权限";
        MessageBox.Show(
            this,
            $"状态圆点首次绘制失败。\n\n诊断信息：\n{logLocation}\n\n程序将安全退出。",
            "E键监控工具",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
        RequestShutdown();
    }

    private void UpdateGameFollowState()
    {
        if (!_followGame || _shuttingDown || _resourcesDisposed)
        {
            return;
        }

        long now = Environment.TickCount64;
        bool gameRunning;

        try
        {
            gameRunning = IsFollowedGameRunning();
        }
        catch (Exception ex)
        {
            LogDiagnostic("Check followed game process", 0, ex);
            if (_gameSeen)
            {
                _gameMissingSinceTick = null;
            }

            return;
        }

        if (gameRunning)
        {
            _gameSeen = true;
            _gameMissingSinceTick = null;
            if (_watchMode)
            {
                ShowDot();
            }

            return;
        }

        if (!_gameSeen)
        {
            if (_watchMode)
            {
                // Watch mode waits for as long as it takes: the game may be
                // started hours after the machine boots.
                return;
            }

            if (!_gameStartTimeoutNotified &&
                HasElapsed(_gameStartTick, now, GameStartTimeout))
            {
                _gameStartTimeoutNotified = true;
                NotifyGameStartTimeoutAndExit();
            }

            return;
        }

        _gameMissingSinceTick ??= now;
        if (HasElapsed(_gameMissingSinceTick.Value, now, GameExitGracePeriod))
        {
            if (_watchMode)
            {
                HideDot();
            }
            else
            {
                RequestShutdown();
            }
        }
    }

    private static bool IsFollowedGameRunning()
    {
        Process[] processes = Process.GetProcessesByName(FollowedProcessName);
        try
        {
            return processes.Length > 0;
        }
        finally
        {
            foreach (Process process in processes)
            {
                process.Dispose();
            }
        }
    }

    private void NotifyGameStartTimeoutAndExit()
    {
        try
        {
            _trayIcon.ShowBalloonTip(
                5000,
                "E键监控工具",
                "等待 NarakaBladepoint 启动超过 10 分钟，工具将退出。",
                ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            LogDiagnostic("Show game start timeout notification", 0, ex);
        }

        RequestShutdown();
    }

    private static bool HasElapsed(long startTick, long nowTick, TimeSpan duration)
    {
        return nowTick - startTick >= (long)duration.TotalMilliseconds;
    }

    private static bool IsValidGdiObject(IntPtr value)
    {
        return value != IntPtr.Zero && value != HgdiError;
    }

    private byte GetLayerOpacityByte()
    {
        int alpha = (int)Math.Round(Math.Clamp(_windowOpacity, 0.1, 1.0) * 255);
        return (byte)Math.Clamp(alpha, 1, 255);
    }

    private void SetDotSize(DotSizeOption dotSize)
    {
        if (_dotSize == dotSize)
        {
            return;
        }

        _dotSize = dotSize;
        _settings.DotSize = dotSize;
        SaveSettingsSafely();
        UpdateMenuChecks();
        ApplyDotMetrics();
        MarkRenderDirtyOnFailure(RenderLayeredWindow(), Environment.TickCount64);
    }

    private void SetDockPosition(DockPosition position)
    {
        if (_position == position)
        {
            return;
        }

        _position = position;
        _settings.Position = position;
        ApplyDotMetrics();
        SaveSettingsSafely();
        UpdateMenuChecks();
        MarkRenderDirtyOnFailure(RenderLayeredWindow(), Environment.TickCount64);
    }

    private void SetClickThrough(bool clickThrough)
    {
        if (_clickThrough == clickThrough)
        {
            UpdateMenuChecks();
            return;
        }

        if (clickThrough)
        {
            CancelDrag();
        }

        _clickThrough = clickThrough;
        _settings.ClickThrough = clickThrough;
        ApplyClickThroughStyle();
        SaveSettingsSafely();
        UpdateMenuChecks();
    }

    private bool ApplyClickThroughStyle()
    {
        if (!IsHandleCreated || _resourcesDisposed)
        {
            return LogRenderFailure(
                "Apply window styles",
                0,
                new InvalidOperationException(
                    $"IsHandleCreated={IsHandleCreated}, ResourcesDisposed={_resourcesDisposed}."));
        }

        Marshal.SetLastPInvokeError(0);
        IntPtr currentStyle = GetWindowLongPtrCompat(Handle, GwlExStyle);
        int getStyleError = Marshal.GetLastWin32Error();
        if (currentStyle == IntPtr.Zero && getStyleError != 0)
        {
            return LogRenderFailure("GetWindowLongPtr(GWL_EXSTYLE)", getStyleError);
        }

        long styleValue = currentStyle.ToInt64();
        long mandatoryStyles = WsExToolWindow | WsExNoActivate | WsExLayered;
        long updatedValue = _clickThrough
            ? styleValue | mandatoryStyles | WsExTransparent
            : (styleValue | mandatoryStyles) & ~WsExTransparent;

        if (updatedValue != styleValue)
        {
            Marshal.SetLastPInvokeError(0);
            IntPtr previousStyle = SetWindowLongPtrCompat(Handle, GwlExStyle, new IntPtr(updatedValue));
            int setStyleError = Marshal.GetLastWin32Error();
            if (previousStyle == IntPtr.Zero && setStyleError != 0)
            {
                return LogRenderFailure("SetWindowLongPtr(GWL_EXSTYLE)", setStyleError);
            }
        }

        Marshal.SetLastPInvokeError(0);
        if (!SetWindowPos(
            Handle,
            HwndTopMost,
            0,
            0,
            0,
            0,
            SwpNoMove | SwpNoSize | SwpNoActivate | SwpFrameChanged))
        {
            return LogRenderFailure("SetWindowPos(apply layered styles)", Marshal.GetLastWin32Error());
        }

        Marshal.SetLastPInvokeError(0);
        long appliedStyles = GetWindowLongPtrCompat(Handle, GwlExStyle).ToInt64();
        int verifyStyleError = Marshal.GetLastWin32Error();
        if (appliedStyles == 0 && verifyStyleError != 0)
        {
            return LogRenderFailure("GetWindowLongPtr(verify GWL_EXSTYLE)", verifyStyleError);
        }

        long expectedStyles = mandatoryStyles | (_clickThrough ? (long)WsExTransparent : 0L);
        if ((appliedStyles & expectedStyles) != expectedStyles ||
            (!_clickThrough && (appliedStyles & WsExTransparent) != 0))
        {
            return LogRenderFailure(
                "Verify layered window styles",
                0,
                new InvalidOperationException(
                    $"Applied style 0x{appliedStyles:X}, expected flags 0x{expectedStyles:X}."));
        }

        return true;
    }

    private void ApplyPlacement(DockPosition position)
    {
        Point targetLocation = position == DockPosition.Custom
            ? GetCustomOrFallbackLocation()
            : GetPresetLocation(position, GetPlacementScreen().WorkingArea);

        Location = targetLocation;
    }

    private Point GetCustomOrFallbackLocation()
    {
        if (_settings.CustomX.HasValue && _settings.CustomY.HasValue)
        {
            Point desired = new(_settings.CustomX.Value, _settings.CustomY.Value);
            if (TryClampVisibleLocation(desired, out Point customLocation))
            {
                return customLocation;
            }
        }

        // Fallback matches the default position (MiddleLeft) so a lost
        // custom coordinate never surprises the user on another screen.
        Screen primaryScreen = Screen.PrimaryScreen ?? Screen.FromPoint(Cursor.Position);
        return GetPresetLocation(DockPosition.MiddleLeft, primaryScreen.WorkingArea);
    }

    private bool TryClampVisibleLocation(Point desiredLocation, out Point clampedLocation)
    {
        Rectangle desiredBounds = new(desiredLocation, ClientSize);
        Screen? bestScreen = null;
        int bestIntersectionArea = 0;

        foreach (Screen screen in Screen.AllScreens)
        {
            Rectangle intersection = Rectangle.Intersect(screen.WorkingArea, desiredBounds);
            int intersectionArea = intersection.Width * intersection.Height;
            if (intersectionArea > bestIntersectionArea)
            {
                bestIntersectionArea = intersectionArea;
                bestScreen = screen;
            }
        }

        if (bestScreen is null)
        {
            clampedLocation = Point.Empty;
            return false;
        }

        clampedLocation = ClampLocationToWorkingArea(desiredLocation, bestScreen.WorkingArea);
        return true;
    }

    private Point GetPresetLocation(DockPosition position, Rectangle area)
    {
        int x = area.Right - ClientSize.Width - PlacementMargin;
        int y = area.Top + PlacementMargin;

        switch (position)
        {
            case DockPosition.MiddleRight:
                x = area.Right - ClientSize.Width - PlacementMargin;
                y = area.Top + (area.Height - ClientSize.Height) / 2;
                break;
            case DockPosition.BottomRight:
                x = area.Right - ClientSize.Width - PlacementMargin;
                y = area.Bottom - ClientSize.Height - PlacementMargin;
                break;
            case DockPosition.BottomLeft:
                x = area.Left + PlacementMargin;
                y = area.Bottom - ClientSize.Height - PlacementMargin;
                break;
            case DockPosition.MiddleLeft:
                x = area.Left + PlacementMargin;
                y = area.Top + (area.Height - ClientSize.Height) / 2;
                break;
            case DockPosition.TopLeft:
                x = area.Left + PlacementMargin;
                y = area.Top + PlacementMargin;
                break;
        }

        return ClampLocationToWorkingArea(new Point(x, y), area);
    }

    private Screen GetPlacementScreen()
    {
        if (!_placementInitialized)
        {
            return Screen.PrimaryScreen ?? Screen.FromPoint(Cursor.Position);
        }

        Point center = new(Location.X + ClientSize.Width / 2, Location.Y + ClientSize.Height / 2);
        return Screen.FromPoint(center);
    }

    private Point ClampLocationToWorkingArea(Point location, Rectangle area)
    {
        int maxX = Math.Max(area.Left, area.Right - ClientSize.Width);
        int maxY = Math.Max(area.Top, area.Bottom - ClientSize.Height);
        int x = Math.Max(area.Left, Math.Min(location.X, maxX));
        int y = Math.Max(area.Top, Math.Min(location.Y, maxY));
        return new Point(x, y);
    }

    private int GetEffectiveDotDiameter()
    {
        Screen screen = GetPlacementScreen();
        int deviceHeight = Math.Max(1, screen.Bounds.Height);
        double ratio = _dotSize switch
        {
            DotSizeOption.Small => SmallDotScreenRatio,
            DotSizeOption.Medium => MediumDotScreenRatio,
            DotSizeOption.Large => LargeDotScreenRatio,
            _ => MediumDotScreenRatio
        };

        int diameter = (int)Math.Round(deviceHeight * ratio);
        if (diameter < 12)
        {
            diameter = 12;
        }

        return diameter % 2 == 0 ? diameter : diameter - 1;
    }

    /// <summary>
    /// Keeps the window sized around the dot and re-applies the placement.
    /// The window follows the dot, never the other way round: a bigger dot on a
    /// bigger screen must still fit inside the layered window.
    /// </summary>
    private void ApplyDotMetrics()
    {
        // Place first, then measure: the size has to be derived from the screen
        // the window actually ends up on. Measuring first would size the dot for
        // the primary screen while the window sits on another one.
        ApplyPlacement(_position);
        _placementInitialized = true;

        int diameter = GetEffectiveDotDiameter();
        Size target = new(diameter + DotMargin * 2, diameter + DotMargin * 2);
        if (ClientSize != target)
        {
            ClientSize = target;
        }

        // Re-clamp: the new size can push the window off the working area.
        ApplyPlacement(_position);
    }

    private Color GetCurrentDotColor()
    {
        return _isActive ? ActiveDotColor : InactiveDotColor;
    }

    private void UpdateMenuChecks()
    {
        UpdateMenuChecks(_dotMenuItems);
        UpdateMenuChecks(_trayMenuItems);
    }

    private void UpdateMenuChecks(MenuItemSet items)
    {
        items.ClickThroughItem.Checked = _clickThrough;

        items.SmallSizeItem.Checked = _dotSize == DotSizeOption.Small;
        items.MediumSizeItem.Checked = _dotSize == DotSizeOption.Medium;
        items.LargeSizeItem.Checked = _dotSize == DotSizeOption.Large;

        items.TopRightItem.Checked = _position == DockPosition.TopRight;
        items.MiddleRightItem.Checked = _position == DockPosition.MiddleRight;
        items.BottomRightItem.Checked = _position == DockPosition.BottomRight;
        items.BottomLeftItem.Checked = _position == DockPosition.BottomLeft;
        items.MiddleLeftItem.Checked = _position == DockPosition.MiddleLeft;
        items.TopLeftItem.Checked = _position == DockPosition.TopLeft;
        items.AutoStartItem.Checked = AutoStartManager.IsEnabledForCurrentExecutable();
    }

    /// <summary>
    /// Turns the optional start-with-Windows entry on or off. The entry is only
    /// written when the user asks for it, and points at the running executable
    /// in watch mode so the dot appears as soon as the game starts.
    /// </summary>
    private void ToggleAutoStart()
    {
        try
        {
            if (AutoStartManager.IsEnabledForCurrentExecutable())
            {
                AutoStartManager.Disable();
                _trayIcon.ShowBalloonTip(
                    3000,
                    "E键监控工具",
                    "已关闭开机自启。",
                    ToolTipIcon.Info);
            }
            else
            {
                bool replacedStale = AutoStartManager.IsEnabled();
                AutoStartManager.Enable(AutoStartManager.GetExecutablePath(), "--watch");
                _trayIcon.ShowBalloonTip(
                    3000,
                    "E键监控工具",
                    replacedStale
                        ? "已开启开机自启（旧的失效路径已替换为当前 EXE）。"
                        : "已开启开机自启：开机后自动在托盘待命，游戏启动时显示圆点。",
                    ToolTipIcon.Info);
            }
        }
        catch (Exception ex)
        {
            LogDiagnostic("Toggle auto start", 0, ex);
            MessageBox.Show(
                this,
                $"设置开机自启失败：\n{ex.Message}",
                "E键监控工具",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        UpdateMenuChecks();
    }

    private void HandleMouseDown(object? sender, MouseEventArgs e)
    {
        if (_clickThrough || e.Button != MouseButtons.Left)
        {
            return;
        }

        _isDragging = true;
        _dragMoved = false;
        _dragStartCursor = Cursor.Position;
        _dragStartLocation = Location;
        Capture = true;
    }

    private void HandleMouseMove(object? sender, MouseEventArgs e)
    {
        if (!_isDragging)
        {
            return;
        }

        if (_clickThrough || (Control.MouseButtons & MouseButtons.Left) == 0)
        {
            CancelDrag();
            return;
        }

        Point currentCursor = Cursor.Position;
        int deltaX = currentCursor.X - _dragStartCursor.X;
        int deltaY = currentCursor.Y - _dragStartCursor.Y;
        Point proposedLocation = new(_dragStartLocation.X + deltaX, _dragStartLocation.Y + deltaY);
        Point clampedLocation = ClampLocationToWorkingArea(
            proposedLocation,
            Screen.FromPoint(currentCursor).WorkingArea);

        if (Location != clampedLocation)
        {
            Location = clampedLocation;
            _dragMoved = true;
        }
    }

    private void HandleMouseUp(object? sender, MouseEventArgs e)
    {
        if (!_clickThrough && e.Button == MouseButtons.Left)
        {
            FinishDrag(saveCustomLocation: true);
            return;
        }

        if (!_clickThrough && e.Button == MouseButtons.Right)
        {
            _dotMenu.Show(PointToScreen(e.Location));
        }
    }

    private void HandleMouseCaptureChanged(object? sender, EventArgs e)
    {
        if (_isDragging && !Capture)
        {
            CancelDrag();
        }
    }

    private void FinishDrag(bool saveCustomLocation)
    {
        if (!_isDragging)
        {
            return;
        }

        bool shouldSaveCustomLocation = saveCustomLocation && _dragMoved;
        _isDragging = false;
        _dragMoved = false;
        _dragStartCursor = Point.Empty;
        _dragStartLocation = Point.Empty;

        if (Capture)
        {
            Capture = false;
        }

        if (shouldSaveCustomLocation)
        {
            _position = DockPosition.Custom;
            _settings.Position = DockPosition.Custom;
            _settings.CustomX = Location.X;
            _settings.CustomY = Location.Y;
            SaveSettingsSafely();
            UpdateMenuChecks();
        }

        // The dot may have been dropped on a screen with a different
        // resolution: the size has to follow the screen it now lives on.
        ApplyDotMetrics();
        MarkRenderDirtyOnFailure(RenderLayeredWindow(), Environment.TickCount64);
    }

    private void CancelDrag()
    {
        if (!_isDragging)
        {
            return;
        }

        _isDragging = false;
        _dragMoved = false;
        _dragStartCursor = Point.Empty;
        _dragStartLocation = Point.Empty;

        if (Capture)
        {
            Capture = false;
        }
    }

    private void SaveSettingsSafely()
    {
        try
        {
            _settings.Save();
        }
        catch (Exception ex)
        {
            LogDiagnostic("Save settings", 0, ex);
        }
    }

    private void RequestShutdown()
    {
        if (_shuttingDown)
        {
            return;
        }

        _shuttingDown = true;
        _trayIcon.Visible = false;

        // Watchdog against zombie processes: if graceful close hangs
        // (known 30s non-exit case), force-terminate after a timeout.
        System.Threading.Timer? watchdog = null;
        watchdog = new System.Threading.Timer(
            _ =>
            {
                try
                {
                    LogDiagnostic("Shutdown watchdog timeout, forcing exit", 0);
                }
                finally
                {
                    Environment.Exit(1);
                }
            },
            null,
            ShutdownWatchdogMs,
            System.Threading.Timeout.Infinite);
        _shutdownWatchdog = watchdog;

        Close();
    }

    private void CleanupResources()
    {
        if (_resourcesDisposed)
        {
            return;
        }

        _resourcesDisposed = true;
        CancelDrag();

        // Graceful close won the race: disarm the shutdown watchdog.
        System.Threading.Timer? watchdog = System.Threading.Interlocked.Exchange(ref _shutdownWatchdog, null);
        watchdog?.Dispose();

        _statusTimer.Stop();
        _gameTimer?.Stop();

        if (_hookHandle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }

        _keyboardProc = null;
        _trayIcon.Visible = false;
        _trayIcon.ContextMenuStrip = null;
        _trayIcon.Dispose();
        _applicationIcon?.Dispose();
        _dotMenu.Dispose();
        _trayMenu.Dispose();
        _statusTimer.Dispose();
        _gameTimer?.Dispose();
    }

    private static IntPtr GetWindowLongPtrCompat(IntPtr hWnd, int nIndex)
    {
        return IntPtr.Size == 8
            ? GetWindowLongPtr64(hWnd, nIndex)
            : new IntPtr(GetWindowLong32(hWnd, nIndex));
    }

    private static IntPtr SetWindowLongPtrCompat(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        return IntPtr.Size == 8
            ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
            : new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateDIBSection(
        IntPtr hdc,
        ref BitmapInfo pbmi,
        uint usage,
        out IntPtr bits,
        IntPtr section,
        uint offset);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hObject);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UpdateLayeredWindow(
        IntPtr hwnd,
        IntPtr hdcDst,
        ref PointStruct pptDst,
        ref SizeStruct psize,
        IntPtr hdcSrc,
        ref PointStruct pptSrc,
        uint crKey,
        ref BlendFunction pblend,
        uint dwFlags);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private sealed record MenuItemSet(
        ToolStripMenuItem ClickThroughItem,
        ToolStripMenuItem GameDirectoryItem,
        ToolStripMenuItem SmallSizeItem,
        ToolStripMenuItem MediumSizeItem,
        ToolStripMenuItem LargeSizeItem,
        ToolStripMenuItem TopRightItem,
        ToolStripMenuItem MiddleRightItem,
        ToolStripMenuItem BottomRightItem,
        ToolStripMenuItem BottomLeftItem,
        ToolStripMenuItem MiddleLeftItem,
        ToolStripMenuItem TopLeftItem,
        ToolStripMenuItem AutoStartItem);

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfoHeader
    {
        public uint Size;
        public int Width;
        public int Height;
        public ushort Planes;
        public ushort BitCount;
        public uint Compression;
        public uint SizeImage;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public uint ColorsUsed;
        public uint ColorsImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfo
    {
        public BitmapInfoHeader Header;
        public uint Colors;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PointStruct
    {
        public int X;
        public int Y;

        public PointStruct(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SizeStruct
    {
        public int Width;
        public int Height;

        public SizeStruct(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct BlendFunction
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;
    }
}
