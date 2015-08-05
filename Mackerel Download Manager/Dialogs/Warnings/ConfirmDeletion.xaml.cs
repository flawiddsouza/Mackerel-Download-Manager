using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Mackerel_Download_Manager.Dialogs.Warnings
{
	/// <summary>
	/// Interaction logic for ConfirmDeletion.xaml
	/// </summary>
	public partial class ConfirmDeletion : Window
	{
		public bool Yes, DontShowCheckbox;

		public ConfirmDeletion(string Message = "Sample Message", string BoxTitle = "Sample Title")
		{
			InitializeComponent();
			this.Title = BoxTitle;
			MessageText.Text = Message;
		}

		private void btnYes_Click(object sender, RoutedEventArgs e)
		{
			DontShowCheckbox = DontShow.IsChecked.Value;
			Yes = true;
			this.Close();
		}

		private void btnNo_Click(object sender, RoutedEventArgs e)
		{
			Yes = false;
			this.Close();
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			HideWindowIcon.RemoveIcon(this);
		}
	}
}
