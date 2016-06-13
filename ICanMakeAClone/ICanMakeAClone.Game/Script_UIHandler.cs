using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Input;
using SiliconStudio.Xenko.Engine;
using SiliconStudio.Xenko.Graphics;
using SiliconStudio.Xenko.UI;
using SiliconStudio.Xenko.UI.Controls;
using SiliconStudio.Xenko.UI.Panels;

namespace ICanMakeAClone
{
	public class Script_UIHandler : SyncScript
	{
		public SpriteSheet UIImages;

		public UIElement CreateLayout()
		{
			StackPanel root = new StackPanel();

			ImageElement map = new ImageElement {
				Source = UIImages[0],
				HorizontalAlignment = HorizontalAlignment.Right,
				Width = 300.0f,
				Height = 400.0f,
				Margin = new Thickness(0, 0, 0, 20, 20, 0),
			};
			//root.Children.Add(map);

			return root;
		}

		public override void Start()
		{
			base.Start();

			Game.Window.Title = "I can make a FNAF clone.";

			UIComponent uiComponent = Entity.Get<UIComponent>();
			uiComponent.RootElement = CreateLayout();
		}

		public override void Update()
		{
			
		}
	}
}