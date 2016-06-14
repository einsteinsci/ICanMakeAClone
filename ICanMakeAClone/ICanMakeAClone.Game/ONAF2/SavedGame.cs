using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SiliconStudio.Core.IO;

namespace ICanMakeAClone.ONAF2
{
	[JsonObject(MemberSerialization.OptIn)]
	public class SavedGame
	{
		public const string PATH = "/roaming/ONAF2.json";

		[JsonProperty]
		public float Volume
		{ get; set; }

		[JsonProperty]
		public bool HasWon
		{ get; set; }

		[JsonProperty]
		public bool HasWonHardboiled
		{ get; set; }

		public SavedGame()
		{
			Volume = 1;
		}

		public void Save()
		{
			string json = JsonConvert.SerializeObject(this, Formatting.Indented);
			Util.WriteAllText(PATH, json);
		}

		public static SavedGame Load()
		{
			if (!VirtualFileSystem.FileExists(PATH))
			{
				return new SavedGame();
			}

			string json = Util.ReadAllText(PATH);
			try
			{
				SavedGame res = JsonConvert.DeserializeObject<SavedGame>(json);
				return res;
			}
			catch (JsonException)
			{
				return new SavedGame();
			}
		}
	}
}
