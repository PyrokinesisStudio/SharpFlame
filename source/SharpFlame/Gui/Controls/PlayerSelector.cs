

using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;

namespace SharpFlame.Gui.Controls
{

	

	public class PlayerSelector : Panel
	{
		readonly List<CustomButton> buttons = new List<CustomButton> ();
		CustomButton selectedButton;

		readonly Color backGroundColor = Color.FromArgb(unchecked ((int)0xFFF3F2EC));
		readonly Color hoverColor = Color.FromArgb(unchecked((int)0xFFB6BDD2));
		readonly Color hoverBorderColor = Color.FromArgb(unchecked((int)0xFF316AC5));

		public virtual string SelectedPlayer {
			get { 
				if (selectedButton == null)
				{
					return null;
				}
				return selectedButton.Text;			
			}
            set {
                var button = buttons.FirstOrDefault(b => b.Text == value);
                SetSelected (button, true);
            }
		}

		public event EventHandler<EventArgs> SelectedPlayerChanged;

		public virtual void OnSelectedPlayerChanged (EventArgs e)
		{
			if (SelectedPlayerChanged != null)
				SelectedPlayerChanged (this, e);
		}

		public PlayerSelector (int players = 10, bool addScavenger = true)
		{
			for (var i = 0; i < players; i++)
			{
				var button = new CustomButton { 
					Text = i.ToString(), 
					BorderWith = new Padding(1, 1), 
					BorderColor = backGroundColor,
					BackGroundColor = backGroundColor, 
					HoverColor = hoverColor,
					HoverBorderColor = hoverBorderColor
				};

				button.Click += delegate {
					SetSelected(button);
				};
				buttons.Add(button);
			}
			if (addScavenger)
			{
				var button = new CustomButton { 
					Text = "S", 
					BorderWith = new Padding(1, 1), 
					BorderColor = backGroundColor,
					BackGroundColor = backGroundColor, 
					HoverColor = hoverColor,
					HoverBorderColor = hoverBorderColor
				};
				button.Click += delegate {
					SetSelected(button);
				};
				buttons.Add (button);
			}

			var columns = buttons.Count / 2;
			var mod = buttons.Count % 2;

			var layout = new DynamicLayout { Spacing = Size.Empty };
			for (var r = 0; r < 2; r++)
			{
				layout.BeginHorizontal ();
				for (var c = 0; c < columns; c++)
				{
					layout.Add (buttons [c + (r * columns)]);
				}
				if (r == 0 && mod == 1)
				{
					layout.Add (null);
				} else if (r == 1 && mod == 1)
				{
					layout.Add (buttons[buttons.Count - 1]);
				}
				layout.EndBeginHorizontal ();
			}

			Content = layout;
		}

		void SetSelected (CustomButton button, bool force = false, bool sendEvent = true) 
		{
			var changed = selectedButton != button;
			if (force || changed)
			{
				selectedButton = button;
				foreach (var r in buttons)
				{
					r.Enabled = !ReferenceEquals (r, button);
				}

				if (sendEvent && changed) 
					OnSelectedPlayerChanged (EventArgs.Empty);
			}
		}
	}
}

