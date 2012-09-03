using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Diagnostics;
using System.Windows.Interop;

namespace StickyNotes
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private const string cThisAppName = "StickyNotes";
		TimeSpan minimumSaveInterval = TimeSpan.FromMinutes(30);
		DateTime? lastSavedTime = null;

		public MainWindow()
		{
			InitializeComponent();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			mainTextbox.Focus();
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			SaveText(true);
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
			source.AddHook(WndProc);

			var handle = new WindowInteropHelper(Application.Current.MainWindow).Handle;
			if (!Win32Api.RegisterHotKey(handle, Win32Api.Hotkey1, Win32Api.MOD_WIN, (int)System.Windows.Forms.Keys.N))
				UserMessages.ShowWarningMessage(cThisAppName + " could not register hotkey WinKey + N");
		}

		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if (msg == Win32Api.WM_HOTKEY)
			{
				if (wParam == new IntPtr(Win32Api.Hotkey1))
				{
					if (this.Visibility != System.Windows.Visibility.Visible)
						this.ShowThisWindow();
					else
						this.HideThisWindow();
				}
			}
			return IntPtr.Zero;
		}


		private bool skipOneMouseRightUp = false;
		private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (Keyboard.Modifiers == ModifierKeys.Control
				|| Mouse.RightButton == MouseButtonState.Pressed)
			{
				if (Mouse.RightButton == MouseButtonState.Pressed)
					skipOneMouseRightUp = true;
				DragMove();
			}
		}

		private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (skipOneMouseRightUp)
			{
				skipOneMouseRightUp = false;
				e.Handled = true;
			}
			//Does not need to close, rather go to tray icon
			//this.Close();
		}

		private void Window_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Middle)
			{
				HideThisWindow();
			}
		}

		private void ShowThisWindow()
		{
			this.WindowState = System.Windows.WindowState.Normal;
			this.Show();
			this.BringIntoView();
			this.Activate();
			this.mainTextbox.Focus();
			//notificationAreaIcon1.Visibility = Visibility.Collapsed;
		}

		private void HideThisWindow()
		{
			this.Hide();
			//notificationAreaIcon1.Visibility = Visibility.Visible;
			//notificationAreaIcon1.UpdateLayout();
		}

		const int cZoomInterval = 2;
		const int cHorizontalScrollInterval = 10;
		private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (Keyboard.Modifiers == ModifierKeys.Control)//Zoom text
			{
				if (e.Delta < 0)//Rolled down
				{
					if (mainTextbox.FontSize - cZoomInterval >= 8)
						mainTextbox.FontSize -= cZoomInterval;
				}
				else if (e.Delta > 0)//Rolled up
				{
					if (mainTextbox.FontSize + cZoomInterval <= 50)
						mainTextbox.FontSize += cZoomInterval;
				}
			}
			else if (Keyboard.Modifiers == ModifierKeys.Shift)//Scroll horizontally
			{
				e.Handled = true;
				if (e.Delta < 0)//Rolled down
				{
					mainTextbox.ScrollToHorizontalOffset(mainTextbox.HorizontalOffset + cHorizontalScrollInterval);
				}
				else if (e.Delta > 0)//Rolled up
				{
					mainTextbox.ScrollToHorizontalOffset(mainTextbox.HorizontalOffset - cHorizontalScrollInterval);
				}
			}
		}

		private void notificationAreaIcon1_MouseClick(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
			{
				if (this.IsVisible)
					this.HideThisWindow();
				else
					this.ShowThisWindow();
			}
		}

		private void exitMenuItem_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void mainTextbox_TextChanged(object sender, TextChangedEventArgs e)
		{
			SaveText(false);
		}

		private string GetFilePath(string filenameOnly)
		{
			return SettingsInterop.GetFullFilePathInLocalAppdata(filenameOnly, cThisAppName, "SavedTexts");
		}

		private void SaveText(bool forceSave = false)
		{
			DateTime now = DateTime.Now;
			if (!lastSavedTime.HasValue)
				lastSavedTime = now;

			if (forceSave || now.Subtract(lastSavedTime.Value).TotalMilliseconds > minimumSaveInterval.TotalMilliseconds)
			{
				if (mainTextbox.Text.Trim().Length > 0)
				{
					string path = GetFilePath(now.ToString("yyyy_MM_dd HH_mm_ss") + ".txt");
					File.WriteAllText(path, mainTextbox.Text);
					lastSavedTime = now;
				}
			}
		}

		private void opensavedfolderMenuItem_Click(object sender, EventArgs e)
		{
			string dir = System.IO.Path.GetDirectoryName(GetFilePath(""));
			//Process.Start("explorer", "/select,\"" + dir + "\"");
			Process.Start("explorer", dir);
		}

		private int TabSize = 4;
		private void mainTextbox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Tab)
			{
				e.Handled = true;
				String tab = new String(' ', TabSize);
				int oldcaretIndex = mainTextbox.CaretIndex;
				mainTextbox.Text = mainTextbox.Text.Insert(mainTextbox.CaretIndex, tab);
				mainTextbox.CaretIndex = oldcaretIndex + tab.Length;
			}
		}
	}
}
