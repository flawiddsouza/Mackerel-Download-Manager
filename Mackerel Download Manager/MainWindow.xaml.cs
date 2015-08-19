using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Wpf.Util;

namespace Mackerel_Download_Manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
		public List<GridViewColumnHeader> headers;
        public static RoutedCommand TrayIconClick = new RoutedCommand();
        public static RoutedCommand DeleteSelectedFilesCommand = new RoutedCommand(); // to be able to delete selected downloads by using the delete key
        
        public MainWindow()
        {
			InitializeComponent();

            Downloads.Deserialize();

			DownloadList_MainDownloadQueue(); // this needs to be called because it can't be done automatically by the Selected event hander of the isSelected treeviewitem.
			Downloads.DownloadEntries.ListChanged += delegate { DownloadList_MainDownloadQueue(); }; // for the listview to be updated everytime the collection changes (objects returned by a LINQ to Objects query do not provide ListChanged events)

			EnableDisableButtonsOnCondition(); // this method must be run here on window initialization, or the buttons won't be disabled when the window starts

			DeleteSelectedFilesCommand.InputGestures.Add(new KeyGesture(Key.Delete)); // to be being able to delete selected downloads using the delete key
		}

		// Button Click Methods
        void AddURL_Click(object sender, RoutedEventArgs e)
        {	
			var AddURL = new Dialogs.AddDownload();
			AddURL.ShowDialog();
        }
        void ResumeSelectedDownloads_Click(object sender, RoutedEventArgs e)
        {
			var SelectedDownloads = (dynamic)DownloadList.SelectedItems;
			List<string> DownloadIDs = new List<string>();
			for (int i = 0; i < SelectedDownloads.Count; i++)
			{
				var selectedItem = (dynamic)DownloadList.SelectedItems[i];
				DownloadIDs.Add(selectedItem.DownloadID);
			}
			Mackerel.ResumeDownload(DownloadIDs.ToArray()); //send a string of DownloadIDs for resuming

			EnableDisableButtonsOnCondition();
        }
        void StopSelectedDownloads_Click(object sender, RoutedEventArgs e)
        {
			var SelectedDownloads = (dynamic)DownloadList.SelectedItems;
			List<string> DownloadIDs = new List<string>();
			for (int i = 0; i < SelectedDownloads.Count; i++)
			{
				var selectedItem = (dynamic)DownloadList.SelectedItems[i];
				DownloadIDs.Add(selectedItem.DownloadID);
			}
			Mackerel.StopDownload(DownloadIDs.ToArray()); //send a string of DownloadIDs for stopping

			EnableDisableButtonsOnCondition();
        }
        void StopAllDownloads_Click(object sender, RoutedEventArgs e)
        {
			Mackerel.StopAllDownloads();

			EnableDisableButtonsOnCondition();
        }
        void DeleteSelectedDownloads_Click(object sender, RoutedEventArgs e)
        {
			var SelectedDownloads = (dynamic)DownloadList.SelectedItems;
			var SelectedDownloadsCount = SelectedDownloads.Count;
			if (Properties.Settings.Default.DisplayDialogforCompletedDeletions == true)
			{
				Dialogs.Warnings.ConfirmDeletion window; //variable creation
				if (SelectedDownloadsCount == 1)
					window = new Dialogs.Warnings.ConfirmDeletion("The selected download is not complete. Are you sure you want to delete it from the list of downloads?", "Confirm deletion of downloads");
				else
					window = new Dialogs.Warnings.ConfirmDeletion("The selected downloads are not complete. Are you sure you want to delete them from the list of downloads?", "Confirm deletion of downloads");
				window.Owner = this;
				window.ShowDialog(); //show dialog
				if (window.Yes == true)
				{
					if (window.DontShowCheckbox) // if don't show checkbox is checked, then set this property to false
					{
						Properties.Settings.Default.DisplayDialogforCompletedDeletions = false;
						Properties.Settings.Default.Save();// save preference immediately
					}
					List<string> DownloadIDs = new List<string>();
					for (int i = 0; i < SelectedDownloadsCount; i++)
					{
						var selectedItem = (dynamic)DownloadList.SelectedItems[i];
						DownloadIDs.Add(selectedItem.DownloadID);
					}
					Mackerel.RemoveDownload(DownloadIDs.ToArray()); //send a string of DownloadIDs for deletion
				}
			}
			else
			{
				List<string> DownloadIDs = new List<string>();
				for (int i = 0; i < SelectedDownloadsCount; i++)
				{
					var selectedItem = (dynamic)DownloadList.SelectedItems[i];
					DownloadIDs.Add(selectedItem.DownloadID);
				}
				Mackerel.RemoveDownload(DownloadIDs.ToArray()); //send a string of DownloadIDs for deletion
			}
        }
        void DeleteCompletedDownloads_Click(object sender, RoutedEventArgs e)
        {
            var messageBoxResult = MessageBox.Show("Are you sure you want to delete all completed dowloads from Mackerel's download list?", "Confirm deletion of downloads", MessageBoxButton.YesNo);
			if (messageBoxResult == MessageBoxResult.Yes)
				Mackerel.RemoveCompletedDownloads();
        }
        void StartDefaultDownloadQueue_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Main download queue was started");
        }
        void StopDefaultDownloadQueue_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Main download queue was stopped");
        }

		// enable disable buttons start
		private void EnableDisableButtonsOnCondition() // What have I done? :P
		{
			var SelectedDownloads = (dynamic)DownloadList.SelectedItems;

			// enable disable context menu items based on whether singular item or items are selected or not
			if (SelectedDownloads.Count == 0)
			{
				ResumeSelectedDownloads.IsEnabled = false;
				StopSelectedDownloads.IsEnabled = false;
				DeleteSelectedDownloads.IsEnabled = false;

				ResumeSelectedDownloads_MenuItem.IsEnabled = false;
				StopSelectedDownloads_MenuItem.IsEnabled = false;
				DeleteSelectedDownloads_MenuItem.IsEnabled = false;

				Open_ContextMenuItem.IsEnabled = false;
				OpenWith_ContextMenuItem.IsEnabled = false;
				OpenContainingFolder_ContextMenuItem.IsEnabled = false;
				ResumeSelectedDownloads_ContextMenuItem.IsEnabled = false;
				StopSelectedDownloads_ContextMenuItem.IsEnabled = false;
				DeleteSelectedDownloads_ContextMenuItem.IsEnabled = false;
				OpenDownloadProperties_ContextMenuItem.IsEnabled = false;
			}
			else
			{
				Open_ContextMenuItem.IsEnabled = true;
				OpenWith_ContextMenuItem.IsEnabled = false; // Keep it false until it is implemented
				OpenContainingFolder_ContextMenuItem.IsEnabled = true;
				OpenDownloadProperties_ContextMenuItem.IsEnabled = true;
			}

			// enable disable context menu items, based on whether the selected download is running or not
			for (int i = 0; i < SelectedDownloads.Count; i++)
			{
				var selectedItem = (DownloadEntry)DownloadList.SelectedItems[i];
				if (selectedItem.Running)
				{
					ResumeSelectedDownloads.IsEnabled = false;
					DeleteSelectedDownloads.IsEnabled = false;
					StopSelectedDownloads.IsEnabled = true;

					ResumeSelectedDownloads_MenuItem.IsEnabled = false;
					DeleteSelectedDownloads_MenuItem.IsEnabled = false;
					StopSelectedDownloads_MenuItem.IsEnabled = true;

					ResumeSelectedDownloads_ContextMenuItem.IsEnabled = false;
					DeleteSelectedDownloads_ContextMenuItem.IsEnabled = false;
					StopSelectedDownloads_ContextMenuItem.IsEnabled = true;
				}
				else // when the download is not running
				{
					ResumeSelectedDownloads.IsEnabled = true;
					DeleteSelectedDownloads.IsEnabled = true;
					StopSelectedDownloads.IsEnabled = false;

					ResumeSelectedDownloads_MenuItem.IsEnabled = true;
					DeleteSelectedDownloads_MenuItem.IsEnabled = true;
					StopSelectedDownloads_MenuItem.IsEnabled = false;

					ResumeSelectedDownloads_ContextMenuItem.IsEnabled = true;
					DeleteSelectedDownloads_ContextMenuItem.IsEnabled = true;
					StopSelectedDownloads_ContextMenuItem.IsEnabled = false;
				}
			}

			// enable and disable the 'stop all downloads' option by checking if any downloads are running or not
			bool running = false;
			foreach (DownloadEntry item in DownloadList.Items)
			{
				if (item.Running)
					running = true;
			}
			if (running)
			{
				StopAllDownloads.IsEnabled = true;
				StopAllDownloads_MenuItem.IsEnabled = true;
			}
			else
			{
				StopAllDownloads.IsEnabled = false;
				StopAllDownloads_MenuItem.IsEnabled = false;
			}

            Downloads.DownloadEntries.ListChanged += delegate
            {
                // enable disable context menu items, based on whether the selected download is running or not
                for (int i = 0; i < SelectedDownloads.Count; i++)
                {
                    var selectedItem = (DownloadEntry)DownloadList.SelectedItems[i];
                    if (selectedItem.Running)
                    {
                        ResumeSelectedDownloads.IsEnabled = false;
                        DeleteSelectedDownloads.IsEnabled = false;
                        StopSelectedDownloads.IsEnabled = true;

                        ResumeSelectedDownloads_MenuItem.IsEnabled = false;
                        DeleteSelectedDownloads_MenuItem.IsEnabled = false;
                        StopSelectedDownloads_MenuItem.IsEnabled = true;

                        ResumeSelectedDownloads_ContextMenuItem.IsEnabled = false;
                        DeleteSelectedDownloads_ContextMenuItem.IsEnabled = false;
                        StopSelectedDownloads_ContextMenuItem.IsEnabled = true;
                    }
                    else // when the download is not running
                    {
                        ResumeSelectedDownloads.IsEnabled = true;
                        DeleteSelectedDownloads.IsEnabled = true;
                        StopSelectedDownloads.IsEnabled = false;

                        ResumeSelectedDownloads_MenuItem.IsEnabled = true;
                        DeleteSelectedDownloads_MenuItem.IsEnabled = true;
                        StopSelectedDownloads_MenuItem.IsEnabled = false;

                        ResumeSelectedDownloads_ContextMenuItem.IsEnabled = true;
                        DeleteSelectedDownloads_ContextMenuItem.IsEnabled = true;
                        StopSelectedDownloads_ContextMenuItem.IsEnabled = false;
                    }
                }

                // enable and disable the 'stop all downloads' option by checking if any downloads are running or not
                running = false;
                foreach (DownloadEntry item in DownloadList.Items)
                {
                    if (item.Running)
                        running = true;
                }
                if (running)
                {
                    StopAllDownloads.IsEnabled = true;
                    StopAllDownloads_MenuItem.IsEnabled = true;
                }
                else
                {
                    StopAllDownloads.IsEnabled = false;
                    StopAllDownloads_MenuItem.IsEnabled = false;
                }
            };

			// disable context menu items, based on whether the selected download is completed or not
			for (int i = 0; i < SelectedDownloads.Count; i++)
			{
				var selectedItem = (DownloadEntry)DownloadList.SelectedItems[i];
				if (selectedItem.Status == "Complete")
				{
					ResumeSelectedDownloads.IsEnabled = false;
					DeleteSelectedDownloads.IsEnabled = true;
					StopSelectedDownloads.IsEnabled = false;

					ResumeSelectedDownloads_MenuItem.IsEnabled = false;
					DeleteSelectedDownloads_MenuItem.IsEnabled = true;
					StopSelectedDownloads_MenuItem.IsEnabled = false;

					ResumeSelectedDownloads_ContextMenuItem.IsEnabled = false;
					DeleteSelectedDownloads_ContextMenuItem.IsEnabled = true;
					StopSelectedDownloads_ContextMenuItem.IsEnabled = false;
				}
			}
		}

		private void DownloadList_SelectionChanged(object sender, SelectionChangedEventArgs e) //so that buttons are re-enabled once the user selects an item from the list and vice versa
		{
			EnableDisableButtonsOnCondition();
		}
		// enable disable buttons end

		// Menu Items
		private void MenuItemAboutMackerel_Click(object sender, RoutedEventArgs e)
		{
			var AboutMackerel = new Dialogs.Menu.AboutMackerel();
			AboutMackerel.Owner = this; // so that this dialog centers to its parent window, as its window is set to WindowStartupLocation="CenterOwner"
			AboutMackerel.Show();
		}

		private void MenuItemOptions_Click(object sender, RoutedEventArgs e)
		{
			var Options = new Dialogs.Menu.Options();
			Options.Owner = this; // so that this dialog centers to its parent window, as its window is set to WindowStartupLocation="CenterOwner"
			Options.Show();
		}

		private void ExitApplication(object sender, RoutedEventArgs e)
		{
            Application.Current.Shutdown();
		}
		// End of Menu Items

		// Context Menu Items start
		private void Open_Click(object sender, RoutedEventArgs e)
		{
			var selectedItem = (dynamic)DownloadList.SelectedItem;
			if (System.IO.File.Exists(selectedItem.SaveTo))
				System.Diagnostics.Process.Start(selectedItem.SaveTo);
			else //if the file doesn't exist just open the folder where it should be
				System.Diagnostics.Process.Start(System.IO.Path.GetDirectoryName(selectedItem.SaveTo));
		}

		private void OpenWith_Click(object sender, RoutedEventArgs e)
		{
			var selectedItem = (dynamic)DownloadList.SelectedItem;
		}

		private void OpenContainingFolder_Click(object sender, RoutedEventArgs e) // this method takes only one selection at a time
		{
			var selectedItem = (dynamic)DownloadList.SelectedItem;
			if (System.IO.File.Exists(selectedItem.SaveTo))
				System.Diagnostics.Process.Start("explorer.exe", @"/select, " + selectedItem.SaveTo);
			else //if the file doesn't exist just open the folder where it should be
				System.Diagnostics.Process.Start(System.IO.Path.GetDirectoryName(selectedItem.SaveTo));
		}

		// Right Click - Open Download Properties for the selected download
		private void OpenDownloadProperties_Click(object sender, RoutedEventArgs e)
		{
			var selectedItem = (dynamic)DownloadList.SelectedItem;
			if(selectedItem != null)
				Mackerel.OpenDownloadProperties(selectedItem.DownloadID);
		}
		
		// Double Click - Open Download Properties for the selected download
		private void DownloadList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			var selectedItem = (dynamic)DownloadList.SelectedItem;
			if (selectedItem != null)
				Mackerel.OpenDownloadProperties(selectedItem.DownloadID);
		}
		// Context Menu Items end

		// listview initial sort
		private void Window_Activated(object sender, EventArgs e)
		{
			headers = GetVisualChildren<GridViewColumnHeader>(DownloadList).ToList(); // 'headers' is a public List<GridViewColumnHeader> property of this class
			ApplySort("LastTryDate", "Last Try Date", "Descending");
		}

		private void ApplySort(string columnPropertyName, string ColumnName, string SortDirection) // Usage: AppySort("ColumnHeaderProperty", "Column Header Text", "Descending");
		{
			if (SortDirection == "Ascending")
			{
				GridViewSort.ApplySort(DownloadList.Items, columnPropertyName, DownloadList, headers.Where(h => h.Content != null && h.Content.ToString() == ColumnName).FirstOrDefault());
			}
			else if (SortDirection == "Descending")
			{
				GridViewSort.ApplySort(DownloadList.Items, columnPropertyName, DownloadList, headers.Where(h => h.Content != null && h.Content.ToString() == ColumnName).FirstOrDefault()); // first one sorts asc
				GridViewSort.ApplySort(DownloadList.Items, columnPropertyName, DownloadList, headers.Where(h => h.Content != null && h.Content.ToString() == ColumnName).FirstOrDefault()); // second one sorts the first to desc
			}
		}

		public static IEnumerable<T> GetVisualChildren<T>(DependencyObject parent) where T : DependencyObject
		{
			int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
			for (int i = 0; i < childrenCount; i++)
			{
				DependencyObject child = VisualTreeHelper.GetChild(parent, i);
				if (child is T)
					yield return (T)child;

				foreach (var descendant in GetVisualChildren<T>(child))
					yield return descendant;
			}
		}
		// end of listview initial sort

		// TreeViewItem Selected property = these methods
		private void DownloadList_All(object sender, RoutedEventArgs e)
		{
			DownloadList.ItemsSource = Downloads.DownloadEntries;
		}

		private void DownloadList_Unfinished(object sender, RoutedEventArgs e)
		{
			e.Handled = true; // so that the parent TreeViewItem Selected event isn't fired when the sub TreeViewItem is Selected

			try
			{
				DownloadList.ItemsSource = Downloads.DownloadEntries.Where(download => download.Status != "Complete");
			}
			catch (NullReferenceException)
			{
				// do nothing
			}
			Downloads.DownloadEntries.ListChanged += delegate { DownloadList.ItemsSource = Downloads.DownloadEntries.Where(download => download.Status != "Complete"); }; // for the listview to be updated everytime the collection changes (objects returned by a LINQ to Objects query do not provide ListChanged events)
		}
	
		private void DownloadList_Finished(object sender, RoutedEventArgs e)
		{
			e.Handled = true;

			DownloadList.ItemsSource = Downloads.DownloadEntries.Where(download => download.Status == "Complete");
			Downloads.DownloadEntries.ListChanged += delegate { DownloadList.ItemsSource = Downloads.DownloadEntries.Where(download => download.Status == "Complete"); }; // for the listview to be updated everytime the collection changes (objects returned by a LINQ to Objects query do not provide ListChanged events)
		}

		private void DownloadList_MainDownloadQueue(object sender, RoutedEventArgs e)
		{
			e.Handled = true;

			try
			{
				DownloadList_MainDownloadQueue();
			}
			catch(NullReferenceException)
			{
				// do nothing
			}
			Downloads.DownloadEntries.ListChanged += delegate { DownloadList_MainDownloadQueue(); }; // for the listview to be updated everytime the collection changes (objects returned by a LINQ to Objects query do not provide ListChanged events)
		}

		private void DownloadList_MainDownloadQueue() // need to reuse it, this is better
		{
			DownloadList.ItemsSource = Downloads.DownloadEntries.Where(download => download.Q == "Main Download Queue" && download.Status != "Complete");
		}

		// list view drag drop things
		private void DownloadList_DragDrop(object sender, DragEventArgs e)
		{

		}

        private void ShowApplication()
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }
            else if (Visibility == Visibility.Collapsed)
            {
                Visibility = Visibility.Visible;
            }
            else if (Visibility == Visibility.Visible || WindowState == WindowState.Normal)
            {
                Topmost = true; // bring to top
                Topmost = false; // reset property        
            }
        }

        private void TrayIcon_Click(object sender, RoutedEventArgs e)
        {
            ShowApplication();
        }

        private void ShowApplication(object sender, RoutedEventArgs e)
        {
            ShowApplication();
        }

        // Save any changes made by the user to the Mackerel.settings file
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
            e.Cancel = true;
            this.Visibility = Visibility.Collapsed;
        }


        // Methods related to controlling the visible behavior of the single instance of the application
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_SHOWME)
            {
                ShowApplication();
            }

            return IntPtr.Zero;
        }
    }
}