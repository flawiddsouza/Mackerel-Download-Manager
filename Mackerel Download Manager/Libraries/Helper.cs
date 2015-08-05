using System;
using System.IO;
using System.Windows.Controls; // to be able use ComboBox as datatype in the method arguments
using System.Net;
using System.Linq;
using System.Collections.Generic; // used for RandomStringGenerator()

namespace Mackerel_Download_Manager
{
	/// <summary>
	/// This helper class serves as a helper, or in other words a function library, for repeatedly used important functions)
	/// </summary>
	public static class Helper
	{
		// Convert Bytes to KB, MB, GB... Ex: 9894(bytes) wil be converted "9.894 KB"
		public static readonly string[] SizeSuffixes = 
                   { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
		
		public static string SizeSuffix(Int64 value, int decimalPoints = 2)
		{
			if (value < 0) { return "-" + SizeSuffix(-value); } 
			if (value == 0) { return null; }
		
			int mag = (int)Math.Log(value, 1024);
			decimal adjustedSize = (decimal)value / (1L << (mag * 10));

			return string.Format("{0:n" + decimalPoints + "} {1}", adjustedSize, SizeSuffixes[mag]);
		}
		
		// Use the given text file to populate the given ComboBox
		public static void ComboBoxHistory(String TextFileName, ComboBox ComboBoxName) // Ex: Helper.ComboBoxHistory("UrlHistory.txt", DownloadLinkComboBox);
		{
			using (var sr = new StreamReader(new FileStream(TextFileName, FileMode.OpenOrCreate)))
			{
				string line = sr.ReadLine();
				
				while(line != null)
				{
					ComboBoxName.Items.Add(line);
					line = sr.ReadLine();
				}
			}
		}
		
		// This converts the type System.Drawing.Icon to an ImageSource that can used by the WPF Image element as its source
		public static System.Windows.Media.ImageSource IconToImageSource(this System.Drawing.Icon icon)
		{
	        var imageSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
	            icon.Handle,
	            System.Windows.Int32Rect.Empty,
	            System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
	
	        return imageSource;
	    }

		// Simulate prepending text to the beginning of the text file by faking it (if the same text already exists, it's removed before prepending)
		public static void PrependTextFile(string filename, string TextToPrepend)
		{
			string str;
			using (StreamReader sreader = new StreamReader(filename))
			{
				str = sreader.ReadToEnd();
				// before the prepend, this checks and removes the text from the file/string, if it already exists
				if (str.Contains(TextToPrepend))
				{
					str = str.Replace(TextToPrepend + Environment.NewLine, "");
				}
			}

			File.Delete(filename);

			using (StreamWriter sw = new StreamWriter(filename, false))
			{
				str = TextToPrepend + Environment.NewLine + str;
				sw.Write(str);
			}
		}

		// Consume link and get download file info
		public static List<object> GetFileInfo(Uri DownloadLink, string authUsername = null, string authPassword = null) // is this the best place for this method?
		{
			string DownloadLinkString = Convert.ToString(DownloadLink);

			var request = (HttpWebRequest)WebRequest.Create(DownloadLink);
			request.Proxy = null; // adding this line makes the http request 6 times faster
			if (authUsername != null)
				Helper.SetBasicAuthHeader(request, authUsername, authPassword); // if auth info exists then it's added to the request header
			try
			{
				var res = (HttpWebResponse)request.GetResponse();
				using (Stream rstream = res.GetResponseStream())
				{
					string FileName = res.Headers["Content-Disposition"] != null && res.Headers["Content-Disposition"] != "attachment" ?
						System.Uri.UnescapeDataString(res.Headers["Content-Disposition"].Replace("attachment; filename=", "").Replace("\"", "")) :
						res.Headers["Location"] != null ? Path.GetFileName(res.Headers["Location"]) :
						Path.GetFileName(DownloadLinkString).Contains(Convert.ToString('?')) || Path.GetFileName(DownloadLinkString).Contains(Convert.ToString('=')) ?
						Path.GetFileName(res.ResponseUri.ToString()) : Path.GetFileName(DownloadLinkString);

					long FileSize = long.TryParse(res.Headers["Content-Length"], out FileSize) ? FileSize : 0;

                    Uri URL = res.ResponseUri;

					return new List<object> { FileName, FileSize, URL };
				}
			}
			catch (WebException)
			{
				return new List<object> { null, (long)0, null };
			}
		}

		// Method name says it all
		public static string RandomStringGenerator()
		{
			var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			var random = new Random();
			return new string(
				Enumerable.Repeat(chars, 8)
						  .Select(s => s[random.Next(s.Length)])
						  .ToArray());
		}

		public static void SetBasicAuthHeader(WebRequest request, String userName, String userPassword)
		{
			string authInfo = userName + ":" + userPassword;
			authInfo = Convert.ToBase64String(System.Text.Encoding.Default.GetBytes(authInfo));
			request.Headers["Authorization"] = "Basic " + authInfo;
		}
	}
}
