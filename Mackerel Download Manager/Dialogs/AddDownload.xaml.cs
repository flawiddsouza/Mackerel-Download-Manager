using System;
using System.Windows;
using System.Linq;
using System.Windows.Data;

namespace Mackerel_Download_Manager.Dialogs
{
	/// <summary>
	/// Interaction logic for AddDownload.xaml
	/// </summary>
	public partial class AddDownload : Window
	{
		private Uri Link;

		public AddDownload()
		{
			InitializeComponent();

			// populate url history into the DownloadLinkComboBox
			Helper.ComboBoxHistory("UrlHistory.txt", DownloadLinkComboBox);
		}
		
		void OK_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Link = new Uri(DownloadLinkComboBox.Text);
			}

			catch (UriFormatException)
			{
				MessageBox.Show("Invalid link!");
				return;
			}

			// Prepending the new download link to the beginning of the text file 
			// (if the same link already exists, it will be removed before the link is prepended)
			Helper.PrependTextFile("UrlHistory.txt", DownloadLinkComboBox.Text);

			bool duplicateDownload = Downloads.DownloadEntries.Any(download => download.DownloadLink == Link);
			if (duplicateDownload != true)
			{
				DownloadFileInfo DownBox;
				if (UseAuth.IsChecked.Value)
					DownBox = new DownloadFileInfo(Link, AuthUser.Text, AuthPass.Password);
				else
					DownBox = new DownloadFileInfo(Link);
				DownBox.Show();
			}
			else
			{
				var duplicate_diag = new Dialogs.Warnings.DuplicateDownloadLink();
				duplicate_diag.Owner = Application.Current.MainWindow;
				duplicate_diag.DownloadLink.Text = Link.ToString();
				duplicate_diag.Show();
			}
			this.Close();
		}

		void Cancel_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}

	/// <summary>
	/// Converts a boolean to its opposite value
	/// </summary>
	[ValueConversion(typeof(bool), typeof(bool))]
	public class InverseBooleanConverter : IValueConverter
	{
		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter,
			System.Globalization.CultureInfo culture)
		{
			if (targetType != typeof(bool))
				throw new InvalidOperationException("The target must be a boolean");

			return (bool)value;
		}

		public object ConvertBack(object value, Type targetType, object parameter,
			System.Globalization.CultureInfo culture)
		{
			throw new NotSupportedException();
		}

		#endregion
	}
}
