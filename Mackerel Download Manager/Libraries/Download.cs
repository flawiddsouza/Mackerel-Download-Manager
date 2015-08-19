using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;

namespace Mackerel_Download_Manager
{
	public class Download
	{
		public event EventHandler<DownloadStatusChangedEventArgs> ResumablityChanged;
        public event EventHandler<DownloadStatusChangedEventArgs> Error;
		public event EventHandler<DownloadProgressChangedEventArgs> ProgressChanged;
		public event EventHandler Completed;

        private bool stop = true;
		public bool Stop
        {
            get
            {
                return stop;
            }
            set
            {
                stop = value;
            }
        }
        private bool paused = false;
        public bool Paused
        {
            get
            {
                return paused;
            }
            set
            {
                paused = value;
            }
        }

        SemaphoreSlim pauseLock = new SemaphoreSlim(1);

		string filename; // TODO: remove this variable and its dependencies

		public void DownloadFile(Uri DownloadLink, string Path)
		{
			filename = System.IO.Path.GetFileName(Path);

			stop = false; // always set this bool to false, everytime this method is called

			var fileInfo = new FileInfo(Path);

			long existingLength = 0;
            if (fileInfo.Exists)
            {
                existingLength = fileInfo.Length;
            }

            while (IsFileLocked(fileInfo))
            {
                Thread.Sleep(1000);
            }

			var request = (HttpWebRequest)HttpWebRequest.Create(DownloadLink);
			request.Proxy = null;
			request.AddRange(existingLength);

			try
			{
				using (var response = (HttpWebResponse)request.GetResponse())
				{
					long fileSize = existingLength + response.ContentLength; //response.ContentLength gives me the size that is remaining to be downloaded
					bool downloadResumable = response.StatusCode == HttpStatusCode.PartialContent;

                    if (!downloadResumable)
                    {
                        if (existingLength > 0 && ResumeUnsupportedWarning() == false)// warn and ask for confirmation to continue if the half downloaded file is unresumable
                        {
                            return;
                        }
                        existingLength = 0;
                        fileSize = response.ContentLength; // reset fileSize to contentlength when the download starts from the beginning
                    }

					OnResumabilityChanged(new DownloadStatusChangedEventArgs(downloadResumable));

                    using (var saveFileStream = fileInfo.Open(downloadResumable ? FileMode.Append : FileMode.Create, FileAccess.Write))
                    {
                        using (var stream = response.GetResponseStream())
                        {
                            byte[] downBuffer = new byte[4096];
                            int byteSize = 0;
                            long totalReceived = byteSize + existingLength;
                            var sw = Stopwatch.StartNew();
                            while (!stop && (byteSize = stream.Read(downBuffer, 0, downBuffer.Length)) > 0)
                            {
                                saveFileStream.Write(downBuffer, 0, byteSize);
                                totalReceived += byteSize;

                                float currentSpeed = totalReceived / (float)sw.Elapsed.TotalSeconds;
                                OnProgressChanged(new DownloadProgressChangedEventArgs(totalReceived, fileSize, (long)currentSpeed));

                                pauseLock.Wait();
                                pauseLock.Release();
                            }
                            sw.Stop();
                        }
                    }
                }
                if (!stop)
                {
                    OnCompleted(EventArgs.Empty);
                }
			}
			catch (WebException e)
			{
                OnError(new DownloadStatusChangedEventArgs(e.Message));
			}
            catch (IOException e)
            {
                OnError(new DownloadStatusChangedEventArgs(e.Message));
            }
		}

		public void Pause()
		{
			if (!paused)
			{
				paused = true;
				// Note this cannot block for more than a moment
				// since the download thread doesn't keep the lock held
				pauseLock.Wait();
			}
		}

		public void Resume()
		{
			if (paused)
			{
				paused = false;
				pauseLock.Release();
			}
		}

		public void StopDownload()
		{
			stop = true;
			this.Resume();  // stop waiting on lock if needed
		}

		public bool ResumeUnsupportedWarning()
		{
			var messageBoxResult = System.Windows.MessageBox.Show("When trying to resume the download , Mackerel got a response from the server that it doesn't support resuming the download. It's possible that it's a temporary error of the server, and you will be able to resume the file at a later time, but at this time Mackerel can download this file from the beginning.\n\nDo you want to download this file from the beginning?", filename, System.Windows.MessageBoxButton.YesNo);
			if (messageBoxResult == System.Windows.MessageBoxResult.Yes)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

        protected virtual bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }

        protected virtual void OnResumabilityChanged(DownloadStatusChangedEventArgs e)
		{
			var handler = ResumablityChanged;
			if (handler != null)
			{
				handler(this, e);
			}
		}

        protected virtual void OnError(DownloadStatusChangedEventArgs e)
        {
            var handler = Error;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnProgressChanged(DownloadProgressChangedEventArgs e)
		{
			var handler = ProgressChanged;
			if (handler != null)
			{
				handler(this, e);
			}
		}

		protected virtual void OnCompleted(EventArgs e)
		{
			var handler = Completed;
			if (handler != null)
			{
				handler(this, e);
			}
		}
	}

	public class DownloadStatusChangedEventArgs : EventArgs
	{
		public DownloadStatusChangedEventArgs(bool canResume) // for ResumablityChanged EventHandler
        {
			ResumeSupported = canResume;
		}

        public DownloadStatusChangedEventArgs(string errorMessage) // for Error EventHandler
        {
            ErrorMessage = errorMessage;
        }

        public bool ResumeSupported { get; private set; }

        public string ErrorMessage { get; private set; }
    }

	public class DownloadProgressChangedEventArgs : EventArgs
	{
		public DownloadProgressChangedEventArgs(long totalReceived, long fileSize, long currentSpeed)
		{
			BytesReceived = totalReceived;
			TotalBytesToReceive = fileSize;
			CurrentSpeed = currentSpeed;
		}

		public long BytesReceived { get; private set; }
		public long TotalBytesToReceive { get; private set; }
		public float ProgressPercentage 
		{ 
			get 
			{ 
				return ((float)BytesReceived / (float)TotalBytesToReceive) * 100; 
			} 
		}
		public float CurrentSpeed { get; private set; } // in bytes
		public TimeSpan TimeLeft
		{
			get
			{
				var bytesRemainingtoBeReceived = TotalBytesToReceive - BytesReceived;
				return TimeSpan.FromSeconds(bytesRemainingtoBeReceived / CurrentSpeed);
			}
		}
	}
}