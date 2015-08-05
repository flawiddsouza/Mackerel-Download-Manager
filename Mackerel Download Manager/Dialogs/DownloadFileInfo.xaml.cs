using System;
using System.Windows;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Mackerel_Download_Manager.Dialogs
{
	/// <summary>
	/// Interaction logic for DownloadFileInfo.xaml
	/// </summary>
	public partial class DownloadFileInfo : Window
	{
		private Uri DownloadLink;
		private string DownloadDirectory = Environment.ExpandEnvironmentVariables(Properties.Settings.Default.DownloadDirectory);
		private string filename, filenamepath;
		private long filesize;
		private string authUser, authPass, obtainedFrom;

		public DownloadFileInfo(Uri Link, string authUsername = null, string authPassword = null, string LinkObtainedFrom = null)
		{
			InitializeComponent();

			DownloadLink = Link;
			authUser = authUsername; // value will be null if it isn't assigned by the caller of this method
			authPass = authPassword; // same
			obtainedFrom = LinkObtainedFrom; // same

			// fill LinkBox Text with the DownloadLink
			LinkBox.Text = DownloadLink.ToString();

			// populate folders history into the SaveLocationComboBox
			Helper.ComboBoxHistory("foldersHistory.txt", SaveLocationComboBox);

			// if there's a path saved by the user while previously downloading a file, set it as the DownloadDirectory
			if (SaveLocationComboBox.Items.Count > 0)
				DownloadDirectory = SaveLocationComboBox.Items[0].ToString();

			// set a temporary filename before the background process is completed
			filename = System.IO.Path.GetFileName(DownloadLink.ToString());

			// append filename to the SaveLocationComboBox items (downloads paths)
			for (int i = 0; i < SaveLocationComboBox.Items.Count; i++)
			{
				SaveLocationComboBox.Items[i] = SaveLocationComboBox.Items[i] + filename;
			}

			// Example value of filenamepath = "C:\Users\Random\Downloads\filename.ext"
			filenamepath = DownloadDirectory + filename; ;

			// fill SaveLocationComboBox with the path and the filename
			SaveLocationComboBox.Text = filenamepath;

			// set this, so that the ComboBox.Text isn't empty while the thread is working in the background
			SaveLocationComboBox.Text = DownloadDirectory + filename;

			// try to get the FileExtIcon Image Element Source from the filename, if you can
			FileExtIcon.Source = IconTools.GetIconForExtension(filename, ShellIconSize.LargeIcon).IconToImageSource();
		}

		void SaveLocationButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var dlg = new Microsoft.Win32.SaveFileDialog(); // creating a object
				dlg.Filter = "All files (*.*)|*.*"; // All files allowed in the 'Save as type' combo box
				dlg.FileName = System.IO.Path.GetFileName(SaveLocationComboBox.Text); // Assign the filename extracted from the the SaveLocationComboBox
				dlg.InitialDirectory = System.IO.Path.GetDirectoryName(SaveLocationComboBox.Text); // Assign the path extracted from the the SaveLocationComboBox
				dlg.RestoreDirectory = true;

				bool? result = dlg.ShowDialog(); // Show save file dialog box, save result as bool inside var 'result'

				// Process save file dialog box results
				if (result == true)
				{
					SaveLocationComboBox.Text = dlg.FileName; // dlg.FileName, now has both the path and the filename
				}
			}
			// to prevent error when the SaveLocationComboBox.Text is empty when the SaveLocationButton is clicked
			catch (System.ArgumentException)
			{
				var dlg = new Microsoft.Win32.SaveFileDialog(); // creating a object
				dlg.Filter = "All files (*.*)|*.*"; // All files allowed in the 'Save as type' combo box
				dlg.FileName = filename; // Assign the filename extracted from the the SaveLocationComboBox
				dlg.RestoreDirectory = true;

				bool? result = dlg.ShowDialog(); // Show save file dialog box, save result as bool inside var 'result'

				// Process save file dialog box results
				if (result == true)
				{
					SaveLocationComboBox.Text = dlg.FileName; // dlg.FileName, now has both the path and the filename
				}
			}
		}

		void WhenWindowIsLoaded(object sender, RoutedEventArgs e)
		{
			// get filename from the uri & assign it to the string filename | Example value of filename = "filename.ext"
			Task<List<Object>> task = Task.Factory.StartNew(() => Helper.GetFileInfo(DownloadLink, authUser, authPass));
			task.ContinueWith(t =>
			{
				string FileName = (string)t.Result[0];
				long FileSize = (long)t.Result[1];

				if (FileName != null) // when there's no internet connection, the result is always null
				{
					filename = FileName;

					// outside this method, they execute before the result is assigned to the filename

					// append filename to the SaveLocationComboBox items (downloads paths)
					for (int i = 0; i < SaveLocationComboBox.Items.Count; i++)
					{
						SaveLocationComboBox.Items[i] = SaveLocationComboBox.Items[i] + filename;
					}

					// Example value of filenamepath = "C:\Users\Random\Downloads\filename.ext"
					filenamepath = DownloadDirectory + filename;

					// fill SaveLocationComboBox with the path and the filename
					SaveLocationComboBox.Text = filenamepath;

					// get icon for the given filename & put it into the FileExtIcon Image element
					FileExtIcon.Source = IconTools.GetIconForExtension(filename, ShellIconSize.LargeIcon).IconToImageSource();
				}

				if (FileSize != 0) // when there's no internet connection, the result is always 0
				{
					filesize = FileSize;
					FileSizeLabel.Content = Helper.SizeSuffix(filesize);
				}

                // to set the correct redirected url (doesn't do anything when not redirected because the url remains the same)
                Uri newURL = (Uri)t.Result[2];
                if (newURL != null)
                {
                    DownloadLink = newURL;
                    LinkBox.Text = DownloadLink.ToString();
                }

            }, TaskScheduler.FromCurrentSynchronizationContext());
		}

		// button click methods start
		void btnDownloadLater_Click(object sender, RoutedEventArgs e)
		{
			SaveDownload();
			this.Close();
		}
		void btnDownload_Click(object sender, RoutedEventArgs e)
		{
            string currentDownloadID = SaveDownload();
            Mackerel.StartDownload(currentDownloadID);
			this.Close();
		}
		string SaveDownload() // repeated calls, made a function
		{
			Prepender();
            string currentDownloadID = Helper.RandomStringGenerator();
            Downloads.DownloadEntries.Add(new DownloadEntry { DownloadID = currentDownloadID, FileName = filename, DateAdded = DateTime.Now, LastTryDate = DateTime.Now, Size = filesize, Description = Description.Text, SaveTo = SaveLocationComboBox.Text, Q = "Main Download Queue", DownloadLink = DownloadLink, AuthUsername = authUser, AuthPassword = authPass, ObtainedFrom = obtainedFrom });
			Downloads.Serialize(); // Saves added download entry
            return currentDownloadID;
		}
		void btnCancel_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
		// button click methods end

		private void Prepender()
		{
			// Prepending the path to the beginning of the text file, if checkbox to remember path is checked
			// (if the same path already exists, it will be removed before the path is prepended)
			if (RememberPath.IsChecked.Value)
				Helper.PrependTextFile("foldersHistory.txt", System.IO.Path.GetDirectoryName(SaveLocationComboBox.Text) + @"\"); // GetDirectoryName removes the back slash
		}
	}
}
