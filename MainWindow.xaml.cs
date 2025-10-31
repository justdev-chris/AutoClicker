using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace AutoClicker
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource cts;
        private const int HOTKEY_ID = 9000;
        private bool isRunning = false;
        
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
                ToggleAutomation();
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void ToggleAutomation()
        {
            Dispatcher.Invoke(() =>
            {
                if (isRunning)
                {
                    StopAutomation();
                }
                else
                {
                    StartAutomation();
                }
            });
        }

        private void StopAutomation()
        {
            cts?.Cancel();
            isRunning = false;
            StartBtn.IsEnabled = true;
            StopBtn.IsEnabled = false;
            StatusText.Text = "Stopped by F6 - Press F6 to start";
        }

        private async void StartAutomation()
        {
            try
            {
                StartBtn.IsEnabled = false;
                StopBtn.IsEnabled = true;
                isRunning = true;
                
                cts = new CancellationTokenSource();
                int intervalMs = GetIntervalMs();
                bool mouseClick = MouseClickBox.IsChecked ?? false;
                
                int durationMs = GetDurationMs();
                
                Key selectedKey = KeyBox.Tag as Key? ?? Key.A;
                byte keyCode = (byte)KeyInterop.VirtualKeyFromKey(selectedKey);
                
                StatusText.Text = $"Clicking every {intervalMs}ms for {DurationBox.Text} {((ComboBoxItem)TimeUnitBox.SelectedItem).Content}... Press F6 to stop";
                
                await Task.Run(() => RunAutoClicker(intervalMs, keyCode, mouseClick, durationMs, cts.Token));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
                StopAutomation();
            }
        }

        private void KeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            string keyStr = e.Key.ToString();
            KeyBox.Text = keyStr;
            KeyBox.Tag = e.Key;
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            StartAutomation();
        }

        private int GetIntervalMs()
        {
            return int.Parse(IntervalBox.Text);
        }

        private int GetDurationMs()
        {
            int value = int.Parse(DurationBox.Text);
            string unit = ((ComboBoxItem)TimeUnitBox.SelectedItem).Content.ToString();
            
            return unit switch
            {
                "Milliseconds" => value,
                "Seconds" => value * 1000,
                "Minutes" => value * 60000,
                _ => 10000
            };
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            StopAutomation();
        }

        private void RunAutoClicker(int intervalMs, byte keyCode, bool mouseClick, int durationMs, CancellationToken token)
        {
            var endTime = DateTime.Now.AddMilliseconds(durationMs);
            
            while (!token.IsCancellationRequested && DateTime.Now < endTime)
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
                
                int remainingDelay = intervalMs - 20;
                if (remainingDelay > 0)
                    Thread.Sleep(remainingDelay);
            }
            
            Dispatcher.Invoke(() =>
            {
                if (!token.IsCancellationRequested)
                {
                    StopAutomation();
                    StatusText.Text = "Completed! Press F6 to start again";
                }
            });
        }

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        
        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);
        
        [DllImport("user32.dll")]
        static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        
        [DllImport("user32.dll")]
        static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
        }
    }
}
