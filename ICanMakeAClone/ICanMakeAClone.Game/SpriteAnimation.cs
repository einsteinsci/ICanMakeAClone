using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Graphics;
using UltimateUtil;

namespace ICanMakeAClone
{
	public class SpriteAnimation
	{
		public class AnimFrame
		{
			public Sprite Sprite
			{ get; set; }

			public Vector2 Offset
			{ get; set; }

			public AnimFrame(Sprite sprite, Vector2 offset)
			{
				Sprite = sprite;
				Offset = offset;
			}

			public void Draw(SpriteBatch sb, Vector2 pos, float rotation = 0)
			{
				Sprite.Draw(sb, pos + Offset, rotation);
			}
		}

		public List<AnimFrame> Frames
		{ get; private set; }

		public AnimFrame this[int index]
		{
			get
			{
				return Frames[index];
			}
		}

		public SpriteAnimation(SpriteSheet sheet, params float[] offsets)
		{
			if (offsets.Length != sheet.Sprites.Count * 2)
			{
				throw new ArgumentException("Offset count ({0}) do not match sheet sprite count ({1}).".Fmt(
					offsets.Length, sheet.Sprites.Count));
			}

			List<Vector2> vecs = new List<Vector2>();
			bool onY = false;
			float x = 0;
			foreach (float f in offsets)
			{
				if (onY)
				{
					vecs.Add(new Vector2(x, f));
				}
				else
				{
					x = f;
				}

				onY = !onY;
			}

			if (vecs.Count != sheet.Sprites.Count)
			{
				throw new ArgumentException("THIS SHOULD NEVER HAPPEN!");
			}

			Frames = new List<AnimFrame>();
			for (int i = 0; i < sheet.Sprites.Count; i++)
			{
				Frames.Add(new AnimFrame(sheet.Sprites[i], vecs[i]));
			}
		}

		public SpriteAnimation(SpriteSheet sheet, params Vector2[] offsets)
		{
			if (offsets.Length != sheet.Sprites.Count)
			{
				throw new ArgumentException("Offset count ({0}) do not match sheet sprite count ({1}).".Fmt(
					offsets.Length, sheet.Sprites.Count));
			}

			Frames = new List<AnimFrame>();
			for (int i = 0; i < sheet.Sprites.Count; i++)
			{
				Frames.Add(new AnimFrame(sheet.Sprites[i], offsets[i]));
			}
		}

		public void Draw(SpriteBatch sb, int frame, Vector2 position, float rotation = 0)
		{
			Frames[frame].Draw(sb, position, rotation);
		}
	}
}
