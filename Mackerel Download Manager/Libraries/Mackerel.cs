using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Mackerel_Download_Manager
{
	public static class Mackerel
	{
        public static Dictionary<string , Download> currentDownloads = new Dictionary<string, Download>();

		//Main Menu functions
		public static void ResumeDownload(string[] DownloadIDs)
		{
			foreach (var DownloadID in DownloadIDs)
			{
				var itemToResume = Downloads.DownloadEntries.Where(download => download.DownloadID == DownloadID).FirstOrDefault();
				
				if (itemToResume.Running == false)
				{
					itemToResume.Running = true;

                    if (!currentDownloads.ContainsKey(itemToResume.DownloadID))
                    {
                        currentDownloads.Add(itemToResume.DownloadID, new Download());
                    }

                    Task.Factory.StartNew(() => currentDownloads[itemToResume.DownloadID].DownloadFile(itemToResume.DownloadLink, itemToResume.SaveTo));
					var window = new Dialogs.DownloadProgress(itemToResume.DownloadID);
					window.Show();
				}
			}
		}

        public static void StartDownload(string DownloadID)
        {
            var itemToResume = Downloads.DownloadEntries.Where(download => download.DownloadID == DownloadID).FirstOrDefault();

            itemToResume.Running = true;

            currentDownloads.Add(itemToResume.DownloadID, new Download());

            Task.Factory.StartNew(() => currentDownloads[itemToResume.DownloadID].DownloadFile(itemToResume.DownloadLink, itemToResume.SaveTo));
            var window = new Dialogs.DownloadProgress(itemToResume.DownloadID);
            window.Show();
        }

		public static void StopDownload(string[] DownloadIDs)
		{
			foreach (var DownloadID in DownloadIDs)
			{
				var itemToStop = Downloads.DownloadEntries.Where(download => download.DownloadID == DownloadID).FirstOrDefault();
                if (itemToStop.Running == true)
                {
                    itemToStop.Running = false;
                }
			}
		}

        public static void StopDownload(string DownloadID)
        {
            var itemToStop = Downloads.DownloadEntries.Where(download => download.DownloadID == DownloadID).FirstOrDefault();
            if (itemToStop.Running)
            {
                itemToStop.Running = false;
            }
        }

        public static void StopAllDownloads()
		{
            foreach (var itemToStop in Downloads.DownloadEntries.Where(download => download.Running == true))
            {
                itemToStop.Running = false;
            }
		}

		public static void RemoveDownload(string[] DownloadIDs) // this method is able to delete multiple downloads
		{
			foreach (var DownloadID in DownloadIDs)
			{
				// delete from the download list
				var selectedDownload = Downloads.DownloadEntries.Where(download => download.DownloadID == DownloadID).FirstOrDefault();
				var selectedDownloadIndex = Downloads.DownloadEntries.IndexOf(selectedDownload);
				Downloads.DownloadEntries.RemoveAt(selectedDownloadIndex);
                //delete from the harddrive
                if (File.Exists(selectedDownload.SaveTo))
                {
                    File.Delete(selectedDownload.SaveTo);
                }
			}
			Downloads.Serialize(); // save current state of object
		}

		public static void RemoveCompletedDownloads() // this method just removes all completed downloads from Mackerel's download list (it doesn't delete them from the hard drive)
		{
			foreach (var itemToRemove in Downloads.DownloadEntries.Where(download => download.Status == "Complete").ToList())
			{
				Downloads.DownloadEntries.Remove(itemToRemove);
			}
		}

		// Context Menu
		public static void OpenDownloadProperties(string DownloadID) // Open "Download Properties" for the given download ID
		{
			var DownloadProperties = new Dialogs.Context_Menu.DownloadProperties(DownloadID);
			DownloadProperties.Owner = Application.Current.MainWindow; // so that this dialog centers to its parent window, as its window is set to WindowStartupLocation="CenterOwner"
			DownloadProperties.ShowDialog();
		}
	}
}
