using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SiliconStudio.Xenko.Audio;

namespace ICanMakeAClone
{
	public class SoundVolumeController
	{
		public Dictionary<SoundEffect, float> Effects
		{ get; private set; }

		public Dictionary<SoundMusic, float> Music
		{ get; private set; }

		public SoundVolumeController()
		{
			Effects = new Dictionary<SoundEffect, float>();
			Music = new Dictionary<SoundMusic, float>();
		}

		public void Register(SoundEffect fx, float volume = 1.0f)
		{
			Effects.Add(fx, volume);
		}

		public void Register(SoundMusic music, float volume = 1.0f)
		{
			Music.Add(music, volume);
		}

		public void SetVolume(float volume)
		{
			foreach (KeyValuePair<SoundEffect, float> kvp in Effects)
			{
				kvp.Key.Reset3D();
				kvp.Key.Volume = volume * kvp.Value;
			}

			foreach (KeyValuePair<SoundMusic, float> kvp in Music)
			{
				kvp.Key.Volume = volume * kvp.Value;
			}
		}
	}
}
