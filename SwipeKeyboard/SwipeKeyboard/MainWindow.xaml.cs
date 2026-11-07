using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace SwipeKeyboard;

public partial class MainWindow : Window
{
    private bool _english, _light, _swiping;
    private readonly List<string> _trace = new();
    private const int HotkeyId = 0x5151;
    private const int GwlExStyle = -20;
    private const int WsExNoActivate = 0x08000000;
    private const uint KeyUpFlag = 0x0002, UnicodeFlag = 0x0004;
    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")] private static extern uint SendInput(uint count, INPUT[] inputs, int size);
    [DllImport("user32.dll", EntryPoint="GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int index);
    [DllImport("user32.dll", EntryPoint="SetWindowLongPtrW")] private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int index, IntPtr value);
    [StructLayout(LayoutKind.Sequential)] private struct INPUT { public uint type; public InputUnion U; }
    [StructLayout(LayoutKind.Explicit)] private struct InputUnion { [FieldOffset(0)] public KEYBDINPUT ki; }
    [StructLayout(LayoutKind.Sequential)] private struct KEYBDINPUT { public ushort wVk, wScan; public uint dwFlags, time; public IntPtr dwExtraInfo; }

    private static readonly string[][] Ru = { "ЙЦУКЕНГШЩЗХ".Select(c=>c.ToString()).ToArray(), "ФЫВАПРОЛДЖЭ".Select(c=>c.ToString()).ToArray(), "ЯЧСМИТЬБЮ".Select(c=>c.ToString()).ToArray() };
    private static readonly string[][] En = { "QWERTYUIOP".Select(c=>c.ToString()).ToArray(), "ASDFGHJKL".Select(c=>c.ToString()).ToArray(), "ZXCVBNM".Select(c=>c.ToString()).ToArray() };

    public MainWindow()
    {
        InitializeComponent();
        BuildKeys();
        SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLongPtr(hwnd, GwlExStyle, new IntPtr(GetWindowLongPtr(hwnd, GwlExStyle).ToInt64() | WsExNoActivate));
            RegisterHotKey(hwnd, HotkeyId, 0x0002 | 0x0004, 0x20);
            HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);
        };
        Closed += (_, _) => UnregisterHotKey(new WindowInteropHelper(this).Handle, HotkeyId);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == 0x0312 && wParam.ToInt32() == HotkeyId)
        {
            Visibility = Visibility == System.Windows.Visibility.Visible ? System.Windows.Visibility.Hidden : System.Windows.Visibility.Visible;
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void BuildKeys()
    {
        KeyboardGrid.Children.Clear();
        foreach (var row in (_english ? En : Ru))
        foreach (var key in row)
        {
            var b = new Button
            {
                Content = key, Tag = key, FontSize = 19, Margin = new Thickness(2), Padding = new Thickness(4),
                Background = new SolidColorBrush(_light ? Color.FromRgb(238,240,244) : Color.FromRgb(58,62,69)),
                Foreground = _light ? Brushes.Black : Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(95,100,110)), Focusable = false
            };
            b.PreviewMouseLeftButtonDown += Key_Down;
            b.PreviewMouseLeftButtonUp += Key_Up;
            b.MouseEnter += Key_Enter;
            KeyboardGrid.Children.Add(b);
        }
        foreach (var item in new (string label, Action action)[] {
            ("123",()=>InsertText("123")), ("⌫",Backspace), ("Space",()=>InsertText(" ")),
            ("Enter",()=>PressVirtualKey(0x0D)), ("Shift",()=>PressVirtualKey(0x10))
        })
        {
            var b = new Button { Content=item.label, Margin=new Thickness(2), Padding=new Thickness(4), Focusable=false };
            b.Click += (_,_) => item.action();
            KeyboardGrid.Children.Add(b);
        }
    }

    private void Key_Down(object sender, MouseButtonEventArgs e)
    {
        _swiping = true; _trace.Clear(); AddTrace(sender);
        if (sender is Button b) b.CaptureMouse();
        e.Handled = true;
    }
    private void Key_Enter(object sender, MouseEventArgs e)
    {
        if (_swiping && e.LeftButton == MouseButtonState.Pressed) AddTrace(sender);
    }
    private void Key_Up(object sender, MouseButtonEventArgs e)
    {
        if (!_swiping) return;
        AddTrace(sender); _swiping = false;
        if (sender is Button b) b.ReleaseMouseCapture();
        var word = string.Concat(_trace.Distinct());
        if (word.Length > 1) InsertText(word.ToLowerInvariant() + " ");
        else if (_trace.Count > 0) InsertText(_trace[0]);
        _trace.Clear(); e.Handled = true;
    }
    private void AddTrace(object sender)
    {
        if (sender is Button b && b.Tag is string s && (_trace.Count == 0 || _trace[^1] != s)) _trace.Add(s);
    }
    private void InsertText(string value)
    {
        foreach (char c in value)
        {
            if (c == '\n') { PressVirtualKey(0x0D); continue; }
            if (c == '\b') { PressVirtualKey(0x08); continue; }
            if (c == '\t') { PressVirtualKey(0x09); continue; }
            if (c == ' ') { PressVirtualKey(0x20); continue; }
            SendUnicode(c);
        }
    }
    private static void SendUnicode(char c)
    {
        var inputs = new INPUT[2];
        inputs[0].type = 1; inputs[0].U.ki.wScan = c; inputs[0].U.ki.dwFlags = UnicodeFlag;
        inputs[1].type = 1; inputs[1].U.ki.wScan = c; inputs[1].U.ki.dwFlags = UnicodeFlag | KeyUpFlag;
        SendInput(2, inputs, Marshal.SizeOf<INPUT>());
    }
    private static void PressVirtualKey(ushort vk)
    {
        var inputs = new INPUT[2];
        inputs[0].type = 1; inputs[0].U.ki.wVk = vk;
        inputs[1].type = 1; inputs[1].U.ki.wVk = vk; inputs[1].U.ki.dwFlags = KeyUpFlag;
        SendInput(2, inputs, Marshal.SizeOf<INPUT>());
    }
    private void Backspace() => PressVirtualKey(0x08);
    private void Language_Click(object sender, RoutedEventArgs e) { _english = !_english; BuildKeys(); SuggestionText.Text = _english ? "English layout" : "Русская раскладка"; }
    private void Theme_Click(object sender, RoutedEventArgs e) { _light = !_light; Background = new SolidColorBrush(_light ? Colors.White : Color.FromRgb(32,33,36)); Foreground = _light ? Brushes.Black : Brushes.White; BuildKeys(); }
    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); }
}