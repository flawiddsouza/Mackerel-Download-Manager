using System;
using System.ComponentModel;

namespace Mackerel_Download_Manager
{
    [Serializable]
	public class DownloadEntry : INotifyPropertyChanged
	{
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		public string DownloadID { get; set; }
		public Uri DownloadLink { get; set; }
		public string FileName { get; set; }
		private long size;
		public long Size
		{
			get { return size; }
			set
			{
				size = value;
				OnPropertyChanged("Size");
			}
		}
		public string SizePretty
		{
			get
			{
				return Helper.SizeSuffix(Size);
			}
			set
			{
				SizePretty = Size.ToString();
			}
		}
		private string timeleft;
		public string TimeLeft
		{
			get { return timeleft; }
			set
			{
				timeleft = value;
				OnPropertyChanged("TimeLeft");
			}
		}
		private string status;
		public string Status
		{
			get { return status; }
			set
			{
				status = value;
				OnPropertyChanged("Status");
			}
		}
        private string transferrate;
		public string TransferRate
        {
            get { return transferrate; }
            set
            {
                transferrate = value;
                OnPropertyChanged("TranferRate");
            }
        }
		public DateTime DateAdded { get; set; }
		public DateTime LastTryDate { get; set; }
		public string SaveTo { get; set; }
		public string Q { get; set; }
		public string Description { get; set; }
		public string AuthUsername { get; set; }
		public string AuthPassword { get; set; }
		public string ObtainedFrom { get; set; } // the web page from where this download was obtained
        [NonSerialized]
        private bool running; // because only fields are non-serializable, properties aren't
        public bool Running 
		{ 
			get { return running; } 
			set 
			{ 
				running = value;
				OnPropertyChanged("Running"); 
			}
		} 
	}
}
