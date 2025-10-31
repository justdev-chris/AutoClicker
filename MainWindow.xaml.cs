using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace AutoClicker
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource cts;
        private const int HOTKEY_ID = 9000;
        
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var helper = new WindowInteropHelper(this);
            var source = HwndSource.FromHwnd(helper.Handle);
            source.AddHook(HwndHook);
            
            if (!RegisterHotKey(helper.Handle, HOTKEY_ID, 0, 0x75))
            {
                MessageBox.Show("Failed to register F6 hotkey");
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, HOTKEY_ID);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0312 && wParam.ToInt32() == HOTKEY_ID)
            {
                StopAutomation();
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void StopAutomation()
        {
            Dispatcher.Invoke(() =>
            {
                cts?.Cancel();
                StartBtn.IsEnabled = true;
                StopBtn.IsEnabled = false;
                StatusText.Text = "Stopped by F6";
            });
        }

        private void KeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            string keyStr = e.Key.ToString();
            KeyBox.Text = keyStr;
            KeyBox.Tag = e.Key;
        }

        private async void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StartBtn.IsEnabled = false;
                StopBtn.IsEnabled = true;
                StatusText.Text = "Running... Press F6 to stop";
                
                cts = new CancellationTokenSource();
                int cps = int.Parse(CpsBox.Text);
                bool mouseClick = MouseClickBox.IsChecked ?? false;
                
                Key selectedKey = KeyBox.Tag as Key? ?? Key.A;
                byte keyCode = (byte)KeyInterop.VirtualKeyFromKey(selectedKey);
                
                await Task.Run(() => RunAutoClicker(cps, keyCode, mouseClick, cts.Token));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
                StartBtn.IsEnabled = true;
                StopBtn.IsEnabled = false;
            }
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            StopAutomation();
        }

        private void RunAutoClicker(int cps, byte keyCode, bool mouseClick, CancellationToken token)
        {
            int delayMs = 1000 / cps;
            
            while (!token.IsCancellationRequested)
            {
                keybd_event(keyCode, 0, 0, UIntPtr.Zero);
                Thread.Sleep(10);
                keybd_event(keyCode, 0, 2, UIntPtr.Zero);
                
                if (mouseClick)
                {
                    mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero);
                    Thread.Sleep(10);
                    mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero);
                }
                
                Thread.Sleep(delayMs - 20);
            }
        }

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        
        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);
        
        [DllImport("user32.dll")]
        static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        
        [DllImport("user32.dll")]
        static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
