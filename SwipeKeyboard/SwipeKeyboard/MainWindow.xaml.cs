using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SwipeKeyboard;

public partial class MainWindow : Window
{
    private bool _english;
    private bool _light;
    private bool _swiping;
    private readonly List<string> _trace = new();
    private readonly Dictionary<string, Point> _centers = new();
    private static readonly string[][] Ru = { "ЙЦУКЕНГШЩЗХ".Select(c=>c.ToString()).ToArray(), "ФЫВАПРОЛДЖЭ".Select(c=>c.ToString()).ToArray(), "ЯЧСМИТЬБЮ".Select(c=>c.ToString()).ToArray() };
    private static readonly string[][] En = { "QWERTYUIOP".Select(c=>c.ToString()).ToArray(), "ASDFGHJKL".Select(c=>c.ToString()).ToArray(), "ZXCVBNM".Select(c=>c.ToString()).ToArray() };
    private const int HotkeyId = 0x5151;
    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")] private static extern bool SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
    [StructLayout(LayoutKind.Sequential)] private struct INPUT { public uint type; public InputUnion U; }
    [StructLayout(LayoutKind.Explicit)] private struct InputUnion { [FieldOffset(0)] public KEYBDINPUT ki; }
    [StructLayout(LayoutKind.Sequential)] private struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }

    public MainWindow() { InitializeComponent(); BuildKeys(); SourceInitialized += (_,_) => RegisterHotKey(new System.Windows.Interop.WindowInteropHelper(this).Handle, HotkeyId, 0x0002|0x0004, 0x20); Closed += (_,_) => UnregisterHotKey(new System.Windows.Interop.WindowInteropHelper(this).Handle, HotkeyId); }

    private void BuildKeys()
    {
        KeyboardGrid.Children.Clear(); _centers.Clear();
        var rows = _english ? En : Ru;
        foreach (var row in rows)
        {
            foreach (var key in row)
            {
                var b = new Button { Content=key, FontSize=19, Margin=new Thickness(2), Padding=new Thickness(4), Background=new SolidColorBrush(_light?Color.FromRgb(238,240,244):Color.FromRgb(58,62,69)), Foreground=_light?Brushes.Black:Brushes.White, BorderBrush=new SolidColorBrush(Color.FromRgb(95,100,110)), Tag=key };
                b.PreviewMouseLeftButtonDown += Key_Down; b.PreviewMouseLeftButtonUp += Key_Up; b.MouseEnter += Key_Enter;
                KeyboardGrid.Children.Add(b);
            }
        }
        foreach (var (label,action) in new (string,Action)[]{("123",()=>InsertText("123")),("⌫",Backspace),("Space",()=>InsertText(" ")),("Enter",()=>InsertText("\n")),("Shift",()=>{})})
        { var b=new Button{Content=label,Margin=new Thickness(2),Padding=new Thickness(4)}; b.Click+=(_,_)=>action(); KeyboardGrid.Children.Add(b); }
    }
    private void Key_Down(object sender, MouseButtonEventArgs e) { _swiping=true; _trace.Clear(); AddTrace(sender); ((Button)sender).CaptureMouse(); e.Handled=true; }
    private void Key_Enter(object sender, MouseEventArgs e) { if(_swiping && e.LeftButton==MouseButtonState.Pressed) AddTrace(sender); }
    private void Key_Up(object sender, MouseButtonEventArgs e) { if(!_swiping)return; AddTrace(sender); _swiping=false; ((Button)sender).ReleaseMouseCapture(); var word=string.Concat(_trace.Distinct()); if(word.Length>1) InsertText(word.ToLowerInvariant()+" "); else if(_trace.Count>0) InsertText(_trace[0]); _trace.Clear(); e.Handled=true; }
    private void AddTrace(object sender) { if(sender is Button b && b.Tag is string s && (_trace.Count==0 || _trace[^1]!=s)) _trace.Add(s); }
    private void InsertText(string s) { foreach(char c in s) { if(c=='\n'){Key(0x0D);continue;} if(c==' '){Key(0x20);continue;} if(char.IsLetter(c)) { ushort vk=(ushort)char.ToUpperInvariant(c); Key(vk); } } }
    private void Backspace() => Key(0x08);
    private static void Key(ushort vk) { var input=new INPUT[2]; input[0].type=1; input[0].U.ki.wVk=vk; input[1].type=1; input[1].U.ki.wVk=vk; input[1].U.ki.dwFlags=2; SendInput(2,input,Marshal.SizeOf<INPUT>()); }
    private void Language_Click(object sender,RoutedEventArgs e){_english=!_english;BuildKeys();SuggestionText.Text=_english?"English layout":"Русская раскладка";}
    private void Theme_Click(object sender,RoutedEventArgs e){_light=!_light;Background=new SolidColorBrush(_light?Colors.White:Color.FromRgb(32,33,36));Foreground=_light?Brushes.Black:Brushes.White;BuildKeys();}
    private void Header_MouseLeftButtonDown(object sender,MouseButtonEventArgs e){if(e.ButtonState==MouseButtonState.Pressed)DragMove();}
    protected override void OnSourceInitialized(EventArgs e){base.OnSourceInitialized(e);var source=System.Windows.Interop.HwndSource.FromHwnd(new System.Windows.Interop.WindowInteropHelper(this).Handle);source?.AddHook((IntPtr hwnd,int msg,IntPtr w,IntPtr l,ref bool handled)=>{if(msg==0x0312 && w.ToInt32()==HotkeyId){Visibility=Visibility==Visibility.Visible?Visibility.Hidden:Visibility.Visible;handled=true;}return IntPtr.Zero;});}
}