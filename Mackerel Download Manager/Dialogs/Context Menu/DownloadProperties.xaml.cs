using System.Collections.Generic;
using System.Windows;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Mackerel_Download_Manager.Dialogs.Context_Menu
{
	/// <summary>
	/// Interaction logic for DownloadProperties.xaml
	/// </summary>
	public partial class DownloadProperties : Window
	{
		private DownloadEntry properties;

		public DownloadProperties(string DownloadID)
		{
			InitializeComponent();

			properties = Downloads.DownloadEntries.Where(download => download.DownloadID == DownloadID).FirstOrDefault();

			FileIcon.Source = IconTools.GetIconForExtension(properties.FileName, ShellIconSize.LargeIcon).IconToImageSource();
			FileName.Text = properties.FileName;

			FileType.Text = SHGetFileInfo_Wrapper.GetFileTypeDescription(properties.FileName);
			Status.Text = properties.Status != null ? properties.Status : "Unknown";
			Size.Text = properties.SizePretty != null ? properties.SizePretty : "Unknown";

			SaveTo_txtBox.Text = properties.SaveTo;

			Address.Text = properties.DownloadLink.ToString();
			Description.Text = properties.Description;

			authUser.Text = properties.AuthUsername;
			authPass.Password = properties.AuthPassword;

			if (properties.Status == "Complete")
			{
				SaveTo_button.Content = "Move";
				btnOpen.IsEnabled = true;
                Address.IsReadOnly = true;
            }

			SaveTo_txtBox.SelectAll(); // select all text in this box on window load (to show that the box is selectable)

			this.PreviewKeyDown += new KeyEventHandler(HandleEsc); // Close window when escape is hit
		}

		private void SaveTo_button_Click(object sender, RoutedEventArgs e)
		{
			var dlg = new Microsoft.Win32.SaveFileDialog(); // creating a object
			dlg.Filter = "All files (*.*)|*.*"; // All files allowed in the 'Save as type' combo box
			dlg.FileName = System.IO.Path.GetFileName(SaveTo_txtBox.Text); // Assign the filename extracted from the the SaveTo_txtBox
			dlg.InitialDirectory = System.IO.Path.GetDirectoryName(SaveTo_txtBox.Text); // Assign the path extracted from the the SaveTo_txtBox
			dlg.RestoreDirectory = true;

			bool? result = dlg.ShowDialog(); // Show save file dialog box, save result as bool inside var 'result'

			// Process save file dialog box results
			if (result == true)
			{
				SaveTo_txtBox.Text = dlg.FileName; // dlg.FileName, now has both the path and the filename
			}
		}

		private void btnOpen_Click(object sender, RoutedEventArgs e)
		{
			System.Diagnostics.Process.Start(SaveTo_txtBox.Text);
		}

		private void btnOK_Click(object sender, RoutedEventArgs e)
		{
			properties.SaveTo = SaveTo_txtBox.Text;
			properties.DownloadLink = new System.Uri(Address.Text);
			properties.Description = Description.Text;
			properties.AuthUsername = authUser.Text;
			properties.AuthPassword = authPass.Password;
			Downloads.Serialize();
			this.Close();
		}

		private void HandleEsc(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
				Close();
		}
	}
}
