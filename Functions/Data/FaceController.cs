using InControl;

namespace CreativePlayers.Functions.Data
{
    public class FaceController : PlayerActionSet
    {
        public FaceController()
        {
            Up = CreatePlayerAction("Move Up");
            Down = CreatePlayerAction("Move Down");
            Left = CreatePlayerAction("Move Left");
            Right = CreatePlayerAction("Move Right");

            Move = CreateTwoAxisPlayerAction(Left, Right, Down, Up);
            Up.StateThreshold = 0.3f;
            Down.StateThreshold = 0.3f;
            Left.StateThreshold = 0.3f;
            Right.StateThreshold = 0.3f;
        }

		public static FaceController CreateWithBothBindings()
		{
			FaceController myGameActions = new FaceController();
			myGameActions.Up.AddDefaultBinding(InputControlType.RightStickUp);
			myGameActions.Down.AddDefaultBinding(InputControlType.RightStickDown);
			myGameActions.Left.AddDefaultBinding(InputControlType.RightStickLeft);
			myGameActions.Right.AddDefaultBinding(InputControlType.RightStickRight);
			myGameActions.Up.AddDefaultBinding(Key.I);
			myGameActions.Down.AddDefaultBinding(Key.K);
			myGameActions.Left.AddDefaultBinding(Key.J);
			myGameActions.Right.AddDefaultBinding(Key.L);

			return myGameActions;
		}

		public static FaceController CreateWithJoystickBindings()
		{
			FaceController myGameActions = new FaceController();
			myGameActions.Up.AddDefaultBinding(InputControlType.RightStickUp);
			myGameActions.Down.AddDefaultBinding(InputControlType.RightStickDown);
			myGameActions.Left.AddDefaultBinding(InputControlType.RightStickLeft);
			myGameActions.Right.AddDefaultBinding(InputControlType.RightStickRight);

			return myGameActions;
		}

		public static FaceController CreateWithKeyboardBindings(int _playerIndex = -1)
		{
			FaceController myGameActions = new FaceController();
			if (_playerIndex == -1 || _playerIndex == 0)
			{
				myGameActions.Up.AddDefaultBinding(Key.I);
				myGameActions.Down.AddDefaultBinding(Key.K);
				myGameActions.Left.AddDefaultBinding(Key.J);
				myGameActions.Right.AddDefaultBinding(Key.L);
			}
			return myGameActions;
		}

		public PlayerAction Up;

        public PlayerAction Down;

        public PlayerAction Left;

        public PlayerAction Right;

        public PlayerTwoAxisAction Move;
    }
}
