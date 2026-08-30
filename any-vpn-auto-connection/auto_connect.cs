using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Automation;

class AutoConnect
{
    [DllImport("kernel32.dll")]
    static extern IntPtr GetCurrentProcess();
    [DllImport("kernel32.dll")]
    static extern bool SetProcessWorkingSetSize(IntPtr proc, IntPtr min, IntPtr max);

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    static extern bool IsIconic(IntPtr hWnd);

    const int SW_RESTORE = 9;
    const int SW_MINIMIZE = 6;

    static void TrimMemory()
    {
        SetProcessWorkingSetSize(GetCurrentProcess(), (IntPtr)(-1), (IntPtr)(-1));
    }

    static void Main(string[] args)
    {
        int intervalSec = 10; // default 10s, safe margin below the ~20s free-timer
        if (args.Length >= 1)
        {
            if (!int.TryParse(args[0], out intervalSec) || intervalSec < 1)
            {
                Console.WriteLine("Usage: AnyVPNAutoConnection.exe [interval-seconds]");
                Console.WriteLine("Example: AnyVPNAutoConnection.exe 10");
                return;
            }
        }

        Console.WriteLine("=== Any VPN auto connection ===");
        Console.WriteLine("Click interval: " + intervalSec + " sec");
        Console.WriteLine("Method: UI Automation Invoke (auto-minimize after click)");
        Console.WriteLine("Press Ctrl+C to quit");
        Console.WriteLine();

        var root = AutomationElement.RootElement;
        var winCond = new PropertyCondition(AutomationElement.NameProperty, "Any VPN");
        var btnCond = new PropertyCondition(AutomationElement.AutomationIdProperty, "KeepFreeAliveButton");

        while (true)
        {
            try
            {
                var win = root.FindFirst(TreeScope.Children, winCond);
                if (win == null)
                {
                    Log("Any VPN window not found");
                }
                else
                {
                    IntPtr winHandle = (IntPtr)win.Current.NativeWindowHandle;

                    // Restore if a previous cycle left it minimized, otherwise Invoke won't reach the button
                    if (winHandle != IntPtr.Zero && IsIconic(winHandle))
                    {
                        ShowWindow(winHandle, SW_RESTORE);
                    }

                    var btn = win.FindFirst(TreeScope.Descendants, btnCond);
                    if (btn == null)
                    {
                        Log("reset button not found (not connected?)");
                        if (winHandle != IntPtr.Zero && IsIconic(winHandle))
                        {
                            ShowWindow(winHandle, SW_MINIMIZE);
                        }
                    }
                    else
                    {
                        var invoke = btn.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                        if (invoke != null)
                        {
                            IntPtr prev = GetForegroundWindow();
                            invoke.Invoke();
                            Log("clicked reset: \"" + btn.Current.Name + "\"");

                            // Any VPN brings its window to the front on click; push it back out of the way
                            if (winHandle != IntPtr.Zero)
                            {
                                ShowWindow(winHandle, SW_MINIMIZE);
                            }
                            if (prev != IntPtr.Zero)
                            {
                                SetForegroundWindow(prev);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log("error: " + e.Message);
            }

            Thread.Sleep(intervalSec * 1000);
            TrimMemory();
        }
    }

    static void Log(string s)
    {
        Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "  " + s);
    }
}
