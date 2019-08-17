using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpyVsSpy
{
	// control of the game
	public class Game
	{
		public static Player human;

		public static void LoadMapFromFile()
		{

		}

		// translates position on the 2D floor plan to real coordinates of the picture
		public static Coordinates FloorCoordsToPictureCoords(int x, int y)
		{
			int new_x = (100 - y) + (300 + 2*y)/100 * x;		// floor starts at offset 100-y from left edge, width of floor at that position is 300+2*y
			int new_y = 100 + y;								// height of floor in the view is 100 and its farthes edge is coordinate 100
			return new Coordinates(new_x, new_y);
		}

		public static void EventOnKeyPress(char key)
		{
			switch (key)
			{
				case 'W': human.MovePlayer('U'); break;
			}
		}

		public static void Initialize(Form1 parent)
		{

		}
	}

	public class Player
	{
		Position playerPosition = new Position();
		bool alive = true;
		PictureBox pictureBox;

		// moves player in given direction and updates his position on the screen
		public void MovePlayer(char direction)
		{
			switch (direction)
			{
				case 'U': playerPosition.posX -= 5; break;
				case 'D': playerPosition.posX += 5; break;
				case 'L': playerPosition.posY -= 5; break;
				case 'R': playerPosition.posY += 5; break;
			}

			Coordinates screenCoords = Game.FloorCoordsToPictureCoords(playerPosition.posX, playerPosition.posY);
			UI.ChangePictureBoxLocation(pictureBox, screenCoords);
		}
	}

	public class Trap
	{

	}

	public class Position
	{
		public int floor;
		public int roomX;
		public int roomY;
		public int posX;
		public int posY;
		
		// compares two positions and compares them
		// parameter type specifies the type of comparison: 
		// 'f' returns true if positions are on the same [f]loor,
		// 'r' returns true if positions are in the same [r]oom,
		// 'e' returns true if p1 and p2 are [e]qual
		public static bool ArePositionsEqual(char type, Position p1, Position p2)
		{
			switch (type)
			{
				case 'f': return p1.floor == p2.floor;
				case 'r': return p1.floor == p2.floor && p1.roomX == p2.roomX && p1.roomY == p2.roomY;
				case 'e': return p1.floor == p2.floor && p1.roomX == p2.roomX && p1.roomY == p2.roomY
						&& p1.posX == p2.posX && p1.posY == p2.posY;
				default: return false;
			}
		}


	}

	public class Coordinates
	{
		public int x;
		public int y;

		public Coordinates(int x, int y)
		{
			this.x = x;
			this.y = y;
		}
	}

	public class Room
	{
		int[,] floorPlan = new int[10, 10];
		char trapSet;
	}

	public class UI
	{
		// stops the application for the given amount of miliseconds
		public static void Wait(int miliseconds)
		{
			Timer t = new Timer()
			{
				Interval = miliseconds,
				Enabled = true
			};
			t.Start();
			t.Tick += (s, e) =>
			{
				t.Enabled = false;
				t.Stop();
			};
			while (t.Enabled)
			{
				Application.DoEvents();
			}
		}

		// creates a new PictureBox in the specified position and returns the PictureBox instance
		public static PictureBox CreatePictureBox(string path, Coordinates coords, int width, int height, Form1 parent)
		{
			PictureBox pb = new PictureBox();
			pb.ImageLocation = path;
			pb.Size = new Size(width, height);
			pb.Location = new Point(coords.x, coords.y);
			parent.Controls.Add(pb);
			return pb;
		}

		// changes the image in given PictureBox
		public static void ChangeImageInPictureBox(PictureBox pb, string path)
		{
			pb.ImageLocation = path;
		}
		
		// changes the location of given PictureBox
		public static void ChangePictureBoxLocation(PictureBox pb, Coordinates coords)
		{
			pb.Location = new Point(coords.x, coords.y);
		}
	}

	public partial class Form1 : Form
	{
		// handles behavior after key press
		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			switch (keyData)
			{
				case Keys.Up: case Keys.W: Game.EventOnKeyPress('W'); break;
				case Keys.Down: case Keys.S: Game.EventOnKeyPress('S'); break;
				case Keys.Left: case Keys.A: Game.EventOnKeyPress('A'); break;
				case Keys.Right: case Keys.D: Game.EventOnKeyPress('D'); break;
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}

		public Form1()
		{
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			//PictureBox player = UI.CreatePictureBox("../../Assets/Images/placeholderPlayer.png", new Coordinates(100, 100), 50, 100, this);
			//PictureBox background = UI.CreatePictureBox("../../Assets/Images/roomO.png", new Coordinates(0, 0), 500, 200, this);
			//UI.ChangePictureBoxLocation(player, new Coordinates(200, 100);
		}
	}
}
