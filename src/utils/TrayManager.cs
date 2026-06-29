using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows;
using WinForms = System.Windows.Forms;

namespace LiveCaptionsTranslator.utils
{
    /// <summary>
    /// Manages the system tray (notification area) icon and its context menu.
    /// Intended to be created once during application startup and bound to the main window.
    /// </summary>
    public class TrayManager : IDisposable
    {
        private readonly WinForms.NotifyIcon notifyIcon;
        private readonly WinForms.ToolStripMenuItem showItem;
        private readonly WinForms.ToolStripMenuItem pauseItem;
        private readonly Window owner;

        public TrayManager(Window owner)
        {
            this.owner = owner;

            notifyIcon = new WinForms.NotifyIcon
            {
                Icon = LoadAppIcon(),
                Text = "LiveCaptions Translator",
                Visible = true
            };

            var menu = new WinForms.ContextMenuStrip();

            showItem = new WinForms.ToolStripMenuItem("显示主窗口");
            showItem.Click += (s, e) => ShowMainWindow();
            menu.Items.Add(showItem);

            pauseItem = new WinForms.ToolStripMenuItem("暂停翻译（仅记录）");
            pauseItem.Click += (s, e) => TogglePause();
            menu.Items.Add(pauseItem);

            menu.Items.Add(new WinForms.ToolStripSeparator());

            var exitItem = new WinForms.ToolStripMenuItem("退出");
            exitItem.Click += (s, e) => ExitApplication();
            menu.Items.Add(exitItem);

            menu.Opening += (s, e) => RefreshMenuState();

            notifyIcon.ContextMenuStrip = menu;
            notifyIcon.DoubleClick += (s, e) => ShowMainWindow();
        }

        private static Icon LoadAppIcon()
        {
            try
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName
                              ?? Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(exePath) && System.IO.File.Exists(exePath))
                {
                    var icon = Icon.ExtractAssociatedIcon(exePath);
                    if (icon != null)
                        return icon;
                }
            }
            catch
            {
                // fall through to default icon
            }
            return SystemIcons.Application;
        }

        private void RefreshMenuState()
        {
            pauseItem.Text = Translator.LogOnlyFlag ? "恢复翻译" : "暂停翻译（仅记录）";
            showItem.Text = owner.IsVisible ? "隐藏主窗口" : "显示主窗口";
        }

        public void ShowMainWindow()
        {
            if (!owner.Dispatcher.CheckAccess())
            {
                owner.Dispatcher.Invoke(ShowMainWindow);
                return;
            }

            if (owner.IsVisible)
            {
                owner.Hide();
            }
            else
            {
                owner.Show();
                if (owner.WindowState == WindowState.Minimized)
                    owner.WindowState = WindowState.Normal;
                owner.Activate();
                owner.Topmost = Translator.Setting?.MainWindow?.Topmost ?? owner.Topmost;
            }
        }

        private void TogglePause()
        {
            if (!owner.Dispatcher.CheckAccess())
            {
                owner.Dispatcher.Invoke(TogglePause);
                return;
            }

            Translator.LogOnlyFlag = !Translator.LogOnlyFlag;
            Translator.ClearContexts();
        }

        public void ShowBalloon(string title, string message)
        {
            try
            {
                notifyIcon.BalloonTipTitle = title;
                notifyIcon.BalloonTipText = message;
                notifyIcon.ShowBalloonTip(1500);
            }
            catch
            {
                // best effort, ignore
            }
        }

        private void ExitApplication()
        {
            if (!owner.Dispatcher.CheckAccess())
            {
                owner.Dispatcher.Invoke(ExitApplication);
                return;
            }

            if (owner is MainWindow main)
                main.ForceCloseFromTray = true;

            // Make sure window is shown so OnMainWindowClose shutdown mode triggers
            if (!owner.IsVisible)
                owner.Show();
            owner.Close();
        }

        public void Dispose()
        {
            try
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
            }
            catch
            {
                // ignored
            }
        }
    }
}
