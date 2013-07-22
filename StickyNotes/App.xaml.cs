using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using SharedClasses;

namespace StickyNotes
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			SharedClasses.AutoUpdating.CheckForUpdates_ExceptionHandler();
			//SharedClasses.AutoUpdating.CheckForUpdates(null, null, true);

			StickyNotes.MainWindow mw = new MainWindow();
			if (e.Args.Length > 0)
			{
				var filePath = e.Args[0];
				if (e.Args.Length > 1)
					UserMessages.ShowWarningMessage("Only one argument is supported by " + StickyNotes.MainWindow.cThisAppName);
				else if (!File.Exists(e.Args[0]))
					UserMessages.ShowWarningMessage("Cannot open file with " + StickyNotes.MainWindow.cThisAppName + ", file does not exist: " + filePath);
				else
				{
					mw.LoadTextFromFile(filePath);
					mw.wasTextFileLoadedByArguments = true;
				}
			}
			mw.ShowDialog();
		}
	}
}
