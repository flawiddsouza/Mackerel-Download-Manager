using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Mackerel_Download_Manager
{
    public static class Downloads
	{
		public static MyBindingList<DownloadEntry> DownloadEntries = new MyBindingList<DownloadEntry>();

		// Deserializes downloads.data and assigns it to DownloadEntries [this method is called only once]
		public static void Deserialize()
		{
			if (File.Exists("downloads.dat"))
			{
				IFormatter formatter = new BinaryFormatter();
				using (FileStream stream = File.OpenRead("downloads.dat"))
				{
                    DownloadEntries = (MyBindingList<DownloadEntry>)formatter.Deserialize(stream);
				}
			}
		}

		// Equivalent to Save (running this method saves the current state of the DownloadEntries object) [this method can be called several times]
		public static void Serialize()
		{
			IFormatter formatter = new BinaryFormatter();
			using (FileStream stream = File.Create("downloads.dat"))
			{
				formatter.Serialize(stream, DownloadEntries);
			}
		}
	}
}
