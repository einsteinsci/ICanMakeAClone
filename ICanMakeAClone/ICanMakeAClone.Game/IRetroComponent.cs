using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SiliconStudio.Core.Serialization.Assets;
using SiliconStudio.Xenko.Games;
using SiliconStudio.Xenko.Graphics;
using SiliconStudio.Xenko.Input;

namespace ICanMakeAClone
{
	public interface IRetroComponent
	{
		void LoadContent(ContentManager content);
		void Update(GameTime gameTime, InputManager input);
		void Draw(GameTime gameTime, SpriteBatch sb);
	}
}
