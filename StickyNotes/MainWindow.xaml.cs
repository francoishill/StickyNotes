﻿using System;
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
using System.IO;
using System.Diagnostics;
using System.Windows.Interop;
using SharedClasses;
using System.Globalization;
using System.Web;

namespace StickyNotes
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public bool wasTextFileLoadedByArguments = false;

		internal const string cThisAppName = "StickyNotes";

		TimeSpan minimumSaveInterval = TimeSpan.FromMinutes(30);
		//DateTime? lastSavedTime = null;

		public MainWindow()
		{
			InitializeComponent();
		}

		private Rect? GetWorkArea(Point pointToDetermineScreen)
		{
			foreach (var screen in System.Windows.Forms.Screen.AllScreens)
				if (screen.WorkingArea.Contains(new System.Drawing.Point((int)pointToDetermineScreen.X, (int)pointToDetermineScreen.Y)))
					return new Rect(new Point(screen.WorkingArea.Left, screen.WorkingArea.Top), new Size(screen.WorkingArea.Width, screen.WorkingArea.Height));
			//If we did not find which screen obtains the point
			return null;
		}
		private Rect WorkArea { get { return GetWorkArea(new Point(this.Left, this.Top)).Value; } }

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			mainTextbox.Focus();
			CleanupZeroByteFiles();
			if (!wasTextFileLoadedByArguments)
			{
				string path = GetFilePath(DateTime.Now.ToString("yyyy_MM_dd HH_mm_ss") + ".txt");
				//if (!File.Exists(path))
				//    File.Create(path).Close();
				mainTextbox.DataContext = new TodoFile(path);
			}
			this.LoadLastWindowPosition(cThisAppName);
		}

		private void CleanupZeroByteFiles()
		{
			string dir = Path.GetDirectoryName(GetFilePath("s.t"));
			var filesWhichAreZeroBytes = Directory.GetFiles(dir, "*" + cFileExtension)
				.Where(fp => new FileInfo(fp).Length == 0);
			foreach (var f in filesWhichAreZeroBytes)
			{
				try
				{
					File.Delete(f);
				}
				catch (Exception exc)
				{
					UserMessages.ShowWarningMessage("Unable to delete empty file '" + f + "': " + exc.Message);
					Process.Start("explorer", "/select,\"" + f + "\"");
				}
			}
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			this.SaveLastWindowPosition(cThisAppName);
			//SaveText(true);
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
			source.AddHook(WndProc);

			var handle = new WindowInteropHelper(Application.Current.MainWindow).Handle;
			if (!Win32Api.RegisterHotKey(handle, Win32Api.Hotkey1, Win32Api.MOD_WIN, (int)System.Windows.Forms.Keys.S))
			{
				var processes = Process.GetProcessesByName(cThisAppName);
				var foundAnotherStickyNotes = false;
				foreach (var proc in processes)
					if (proc.Id != Process.GetCurrentProcess().Id)
						foundAnotherStickyNotes = true;
				if (!foundAnotherStickyNotes)
					UserMessages.ShowWarningMessage(cThisAppName + " could not register hotkey WinKey + S");
			}
			else
			{
				this.ToolTip = "Hotkey WinKey + S";
				notificationAreaIcon1.ToolTip = "Hotkey WinKey + S";
			}
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

		private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (Keyboard.Modifiers == ModifierKeys.Control)//Zoom text
				mainTextbox.ZoomControlBasedOnMouseWheelEvent(ref e);
			else if (Keyboard.Modifiers == ModifierKeys.Shift)//Scroll horizontally
				mainTextbox.ScrollTextboxHorizontallyFromMouseWheelEvent(ref e);
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
			//SaveText(false);
		}

		private string GetFilePath(string filenameOnlyWithExtension)
		{
			return SettingsInterop.GetFullFilePathInLocalAppdata(filenameOnlyWithExtension, cThisAppName, "SavedTexts");
		}

		const string cFileNameDateFormat = "yyyy_MM_dd HH_mm_ss";
		const string cFileExtension = ".txt";
		/*private void SaveText(bool forceSave = false)
		{
			DateTime now = DateTime.Now;
			if (!lastSavedTime.HasValue)
				lastSavedTime = now;

			if (forceSave || now.Subtract(lastSavedTime.Value).TotalMilliseconds > minimumSaveInterval.TotalMilliseconds)
			{
				if (!string.IsNullOrWhiteSpace(mainTextbox.Text))
				{
					string path = GetFilePath(now.ToString(cFileNameDateFormat) + cFileExtension);
					File.WriteAllText(path, mainTextbox.Text);
					lastSavedTime = now;
				}
			}
		}*/

		private string GetLastSavedText()
		{
			string dirForFiles = Path.GetDirectoryName(GetFilePath("s.t"));//Just use temp name to obtain its directory
			var fileNamesWithoutExtensions = Directory.GetFiles(dirForFiles, "*" + cFileExtension)
				.Select(fp => Path.GetFileNameWithoutExtension(fp));
			//string newestDateFileNameWithoutExtension = null;
			//DateTime newestFileDate = DateTime.MinValue;
			Dictionary<string, DateTime> fileNameWithItsParsedDateTime = new Dictionary<string, DateTime>();
			foreach (var fn in fileNamesWithoutExtensions)
			{
				//We get the newest file based on filename and not Modified Stamp as the used might have changed one of the old files
				DateTime tmpdatetime;
				if (DateTime.TryParseExact(fn, cFileNameDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out tmpdatetime))
					fileNameWithItsParsedDateTime.Add(fn, tmpdatetime);
			}

			if (fileNameWithItsParsedDateTime.Count == 0)
				return "";

			DateTime maxDate = fileNameWithItsParsedDateTime.Max(kv => kv.Value);
			string maxFilename = fileNameWithItsParsedDateTime.First(kv => kv.Value.Equals(maxDate)).Key;
			return File.ReadAllText(GetFilePath(maxFilename + cFileExtension));
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

		private void mainTextbox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (DateTime.Now.Subtract(lastWindowActivation).TotalMilliseconds < 50
				&& mainTextbox.SelectionLength > 0)
			{
				e.Handled = true;//So we dont lose a text selection if there was selected text
				lastWindowActivation = DateTime.MinValue;
				Console.WriteLine("T");
			}
		}

		DateTime lastWindowActivation = DateTime.Now;
		private void Window_Activated(object sender, EventArgs e)
		{
			lastWindowActivation = DateTime.Now;
		}

		private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.System
				&& (e.SystemKey == Key.LeftAlt || e.SystemKey == Key.RightAlt
					|| e.SystemKey == Key.LeftCtrl || e.SystemKey == Key.RightCtrl
					|| e.SystemKey == Key.LeftShift || e.SystemKey == Key.RightShift))
				return;//When we actually press the Alt/Ctrl/Shift keys
			if (e.Key == Key.LeftAlt || e.Key == Key.RightAlt
				|| e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl
				|| e.Key == Key.LeftShift || e.Key == Key.RightShift)
				return;//Usually this occurs if multiple SystemKeys (Alt/Ctrl/Shift) pressed

			//Console.WriteLine("Key = " + e.Key + ", SystemKey = " + e.SystemKey + ", Modifiers = " + Keyboard.Modifiers);

			if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
			{
				if (e.Key == Key.S)
				{
					e.Handled = true;
					SaveTextToFile();
					return;
				}
				else if (e.Key == Key.O)
				{
					e.Handled = true;
					LoadTextFromFile();
					return;
				}
			}

			if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
			{//The alt key was down while pressing another key
				WPFHelper.MoveMode moveMode = WPFHelper.MoveMode.Major;
				if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
					moveMode = WPFHelper.MoveMode.Pixel;
				else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
					moveMode = WPFHelper.MoveMode.Minor;

				//Console.WriteLine("moveMode = " + moveMode);

				bool shouldHandleEvent = true;
				bool moveMinor = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
				if ((e.Key == Key.System && e.SystemKey == Key.Up) || (e.Key == Key.Up))
					this.MoveUp(moveMode);
				else if ((e.Key == Key.System && e.SystemKey == Key.Down) || (e.Key == Key.Down))
					this.MoveDown(moveMode);
				else if ((e.Key == Key.System && e.SystemKey == Key.Left) || (e.Key == Key.Left))
					this.MoveLeft(moveMode);
				else if ((e.Key == Key.System && e.SystemKey == Key.Right) || (e.Key == Key.Right))
					this.MoveRight(moveMode);
				else if ((e.Key == Key.System && e.SystemKey == Key.Home) || (e.Key == Key.Home))
					this.DecreaseHeight(moveMode);
				else if ((e.Key == Key.System && e.SystemKey == Key.End) || (e.Key == Key.End))
					this.IncreaseHeight(moveMode);
				else if ((e.Key == Key.System && e.SystemKey == Key.Delete) || (e.Key == Key.Delete))
					this.DecreaseWidth(moveMode);
				else if ((e.Key == Key.System && e.SystemKey == Key.PageDown) || (e.Key == Key.PageDown))
					this.IncreaseWidth(moveMode);
				else
					shouldHandleEvent = false;
				if (shouldHandleEvent)
					e.Handled = true;
			}
		}

		private void SaveTextToFile()
		{
			if (string.IsNullOrWhiteSpace(mainTextbox.Text))
			{
				UserMessages.ShowWarningMessage("The current text is blank or whitespaces, cannot save to file.");
				return;
			}

			var filePath = FileSystemInterop.SelectNewSaveFile(
				"Please select file to save the text to",
				null,
				"Text files (*.txt)|*.txt",
				true);
			if (string.IsNullOrWhiteSpace(filePath))
				return;

			File.WriteAllText(filePath, mainTextbox.Text);
		}

		internal void LoadTextFromFile(string filePath = null)
		{
			if (!string.IsNullOrWhiteSpace(mainTextbox.Text)
				&& !UserMessages.Confirm("By importing another file the current text will be lost, do you want to continue?"))
				return;

			if (filePath == null)
			{
				filePath = FileSystemInterop.SelectFile(
					"Please select file to load",
					null,
					"Text files (*.txt)|*.txt");
				if (string.IsNullOrWhiteSpace(filePath))
					return;
			}

			mainTextbox.DataContext = new TodoFile(filePath);
			//mainTextbox.Text = File.ReadAllText(filePath);
		}

		private void menuitemSaveTextOnline_Click(object sender, RoutedEventArgs e)
		{
			var todofile = mainTextbox.DataContext as TodoFile;
			if (todofile == null)
			{
				UserMessages.ShowErrorMessage("Cannot find the TodoFile object, cannot save text online.");
				return;
			}
			todofile.SaveOnline();
		}

		private void menuitemLoadLastSavedText_Click(object sender, RoutedEventArgs e)
		{
			/*if (!string.IsNullOrEmpty(mainTextbox.Text))
				SaveText(true);*/
			mainTextbox.Text = GetLastSavedText();
		}

		private void menitemOpenWebsite_Click(object sender, RoutedEventArgs e)
		{
			Process.Start(string.Format("http://firepuma.com/journal/filterloggedin_link/{0}",
				EncodeAndDecodeInterop.EncodeStringHex(TodoFile.cLinkText, err => UserMessages.ShowErrorMessage(err))));
		}

		private void menuItemAbout_Click(object sender, RoutedEventArgs e)
		{
			bool origTopmost = this.Topmost;
			this.Topmost = false;
			try
			{
				AboutWindow2.ShowAboutWindow(new System.Collections.ObjectModel.ObservableCollection<DisplayItem>()
				{
					new DisplayItem("Author", "Francois Hill"),
					new DisplayItem("Icon(s) obtained from", null)
				});
			}
			finally
			{
				this.Topmost = origTopmost;
			}
		}

		private void menuitemHide_Click(object sender, RoutedEventArgs e)
		{
			this.HideThisWindow();
		}

		private void menuItemExit_Click(object sender, RoutedEventArgs e)
		{
			//SaveText(true);
			this.Close();
		}

		private void menuitemLoadTextFromFile_Click(object sender, RoutedEventArgs e)
		{
			LoadTextFromFile();
		}

		private void menuitemSaveTextToFile_Click(object sender, RoutedEventArgs e)
		{
			SaveTextToFile();
		}
	}
}
