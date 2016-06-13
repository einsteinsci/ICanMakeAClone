using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SiliconStudio.Core.IO;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Core.Serialization.Assets;
using SiliconStudio.Xenko.Audio;
using SiliconStudio.Xenko.Graphics;
using SiliconStudio.Xenko.Input;

namespace ICanMakeAClone
{
	internal static class Util
	{
		public static void Draw(this Texture texture, SpriteBatch spritebatch, Vector2 position, 
			float rotation = 0, SpriteEffects spriteEffects = SpriteEffects.None)
		{
			spritebatch.Draw(texture, position, Color.White, rotation, Vector2.Zero, 1, spriteEffects);
		}

		public static void Draw(this Sprite sprite, SpriteBatch sb, Vector2 position, RectangleF? sourceRect,
			Color color, float rotation = 0, SpriteEffects fx = SpriteEffects.None, float scale = 1.0f)
		{
			Texture t = sprite.Texture;
			if (sourceRect.HasValue)
			{
				RectangleF destRect = new RectangleF(position.X, position.Y, sourceRect.Value.Width, sourceRect.Value.Height);
				RectangleF sourceRectCorrected;
				switch (sprite.Orientation)
				{
				case ImageOrientation.Rotated90:
					sourceRectCorrected = new RectangleF(sprite.Region.X, sprite.Region.Y, sourceRect.Value.Height, sourceRect.Value.Width);
					break;
				default:
					sourceRectCorrected = new RectangleF(sprite.Region.X, sprite.Region.Y, sourceRect.Value.Width, sourceRect.Value.Height);
					break;
				}

				float sourceRot = sprite.Orientation == ImageOrientation.Rotated90 ? 90.0f : 0;
				sb.Draw(t, position, sourceRectCorrected, color, rotation - sourceRot.ToRadians(), Vector2.Zero, scale, fx);
			}
			else
			{
				sprite.Draw(sb, position, color, Vector2.One * scale, rotation, spriteEffects: fx);
				//spriteBatch.Draw(t, position, color, rotation, Vector2.Zero, scale, spriteEffects);
			}
		}

		public static Vector2 GetMousePosPx(this InputManager input, Vector2 windowSize)
		{
			return new Vector2(input.MousePosition.X * windowSize.X, input.MousePosition.Y * windowSize.Y);
		}

		public static Int2 ToInt2(this Vector2 vec)
		{
			return new Int2((int)vec.X, (int)vec.Y);
		}

		public static Vector2 ToVector2(this Int2 n)
		{
			return new Vector2(n.X, n.Y);
		}

		public static float ToRadians(this float degrees)
		{
			return degrees * (float)Math.PI * (1.0f / 180.0f);
		}

		public static string ReadAllText(string path)
		{
			List<byte> list = new List<byte>();
			using (Stream stream = VirtualFileSystem.OpenStream(path, VirtualFileMode.Open, VirtualFileAccess.Read))
			{
				int res = 0;
				do
				{
					res = stream.ReadByte();

					if (res >= 0)
					{
						list.Add((byte)res);
					}
				} while (res != -1);
			}

			byte[] arr = list.ToArray();
			return Encoding.UTF8.GetString(arr, 0, arr.Length);
		}

		public static void WriteAllText(string path, string text)
		{
			byte[] arr = Encoding.UTF8.GetBytes(text);

			using (Stream stream = VirtualFileSystem.OpenStream(path, VirtualFileMode.Create, VirtualFileAccess.Write))
			{
				stream.Write(arr, 0, arr.Length);
			}
		}

		public static ONAF2.VentState Not(this ONAF2.VentState vent)
		{
			switch (vent)
			{
			case ONAF2.VentState.Left:
				return ONAF2.VentState.Right;
			case ONAF2.VentState.Right:
				return ONAF2.VentState.Left;
			default:
				return ONAF2.VentState.Left;
			}
		}

		public static bool IsInGame(this ONAF2.UIState state)
		{
			return state == ONAF2.UIState.Office || state == ONAF2.UIState.Laptop;
		}

		public static Color MakeTransparency(byte alpha)
		{
			return new Color(255, 255, 255, alpha);
		}
		public static Color MakeTransparency(float alpha)
		{
			return new Color(1.0f, 1.0f, 1.0f, alpha);
		}

		public static SoundEffect LoadSoundEffect(this ContentManager content, ONAF2.Level level, 
			string path, float volume = 1.0f, bool looped = false)
		{
			SoundEffect res = content.Load<SoundEffect>(path);

			foreach (KeyValuePair<SoundEffect, float> kvp in level.VolumeController.Effects)
			{
				if (kvp.Key == res)
				{
					content.Unload(res);
					return kvp.Key;
				}
			}

			res.Volume = volume * level.Main.Volume;
			res.IsLooped = looped;
			level.VolumeController.Register(res, volume);

			return res;
		}

		public static SoundMusic LoadMusic(this ContentManager content, ONAF2.Level level, 
			string path, float volume = 1.0f, bool looped = true)
		{
			SoundMusic res = content.Load<SoundMusic>(path);

			foreach (var kvp in level.VolumeController.Music)
			{
				if (kvp.Key == res)
				{
					content.Unload(res);
					return kvp.Key;
				}
			}

			res.Volume = volume * level.Main.Volume;
			res.IsLooped = looped;
			level.VolumeController.Register(res, volume);

			return res;
		}
	}
}
