using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SiliconStudio.Xenko.Games;
using UltimateUtil;

namespace ICanMakeAClone
{
	public class CameraOffsetState
	{
		public const float CAMERA_MOVE_SPEED = 110.0f;
		public const float CAMERA_PAUSE_TIME = 2.0f;
		public const float CAMERA_MAX_OFFSET = -220.0f;

		public float CameraOffset
		{ get; private set; }

		public bool CameraGoingLeft
		{ get; private set; }

		public float CameraPauseTime
		{ get; private set; }

		public bool CameraPausing
		{ get; private set; }

		public CameraOffsetState()
		{
			CameraGoingLeft = true;
		}

		public CameraOffsetState(Random rand)
		{
			CameraPausing = rand.NextBool();

			if (CameraPausing)
			{
				CameraPauseTime = (float)rand.NextDouble() * CAMERA_PAUSE_TIME;
			}
			else
			{
				CameraOffset = rand.Next((int)CAMERA_MAX_OFFSET + 1, -1);
				CameraGoingLeft = rand.NextBool();
			}
		}

		public void Update(GameTime gt)
		{
			float elapsed = (float)gt.Elapsed.TotalSeconds;

			if (CameraPausing)
			{
				CameraPauseTime -= elapsed;

				if (CameraPauseTime < 0)
				{
					CameraPauseTime = 0;
					CameraPausing = false;
					CameraGoingLeft = !CameraGoingLeft;
				}
			}
			else
			{
				if (CameraGoingLeft)
				{
					CameraOffset -= elapsed * CAMERA_MOVE_SPEED;
				}
				else
				{
					CameraOffset += elapsed * CAMERA_MOVE_SPEED;
				}

				if (CameraOffset >= 0)
				{
					CameraOffset = 0;
					CameraPausing = true;
					CameraPauseTime = CAMERA_PAUSE_TIME;
				}
				else if (CameraOffset <= CAMERA_MAX_OFFSET)
				{
					CameraOffset = CAMERA_MAX_OFFSET;
					CameraPausing = true;
					CameraPauseTime = CAMERA_PAUSE_TIME;
				}
			}
		}
	}
}
