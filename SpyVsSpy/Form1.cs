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

		// handles events when key is pressed
		public static void EventOnKeyPress(char key)
		{
			switch (key)
			{
				case 'W': human.MovePlayer('U'); break;
				case 'S': human.MovePlayer('D'); break;
				case 'A': human.MovePlayer('L'); break;
				case 'D': human.MovePlayer('R'); break;
			}
		}

		public static void Initialize(Form1 parent)
		{
			PictureBox background = UI.CreatePictureBox(UI.baseImageAddress + "roomB.png", new Coordinates(0, 0), 500, 200, parent);
			human = new Player(parent, background);
		}
	}

	public class Player
	{
		Position playerPosition = new Position(1, 1, 1, new Coordinates(251, 101));
		bool alive = true;
		PictureBox playerImage;

		public Player(Form1 parent, PictureBox background)
		{
			playerImage = UI.CreatePictureBox(UI.baseImageAddress + "playerWhite.png", playerPosition.floorCoordinates, 40, 40, parent);
			// the following two lines are necessary so that the player has a transparent background
			background.Controls.Add(playerImage);
			background.BackColor = Color.Transparent;
		}

		// moves player in given direction and updates his position on the screen
		public void MovePlayer(char direction)
		{
			Coordinates newCoords = new Coordinates(playerPosition.floorCoordinates.x, playerPosition.floorCoordinates.y);
			switch (direction)
			{
				case 'U': newCoords.y -= 5; break;
				case 'D': newCoords.y += 5; break;
				case 'L': newCoords.x -= 5; break;
				case 'R': newCoords.x += 5; break;
			}

			if (Coordinates.CheckIfValidFloorPosition(newCoords, 0, 0))
			{
				playerPosition.floorCoordinates = newCoords;
				UI.ChangePictureBoxLocation(playerImage, playerPosition.floorCoordinates);
			}
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
		public Coordinates floorCoordinates;
		
		public Position(int floor, int roomX, int roomY, Coordinates floorCoordinates)
		{
			this.floor = floor;
			this.roomX = roomX;
			this.roomY = roomY;
			this.floorCoordinates = floorCoordinates;
		}

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
						&& p1.floorCoordinates == p2.floorCoordinates;
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

		// TODO player coordinates will always include the starting point of background AND take into account height&width
		// returns true if given position is on the floor
		// backgroundX and backgroundY are parameters specifying the position of upper left corner of background
		public static bool CheckIfValidFloorPosition(Coordinates coords, int backgroundX, int backgroundY)
		{
			return (coords.x > 100-coords.y+100 && coords.x < 400 + coords.y-100) && (coords.y + backgroundY > 100 && coords.y + backgroundY < 200);
		}
	}

	public class Room
	{
		int[,] floorPlan = new int[10, 10];
		char trapSet;
	}

	public class UI
	{
		public static string baseImageAddress = "../../Assets/Images/";
		public static Image transparentBackground = Image.FromFile(baseImageAddress + "transparentPlayerBackground.png");
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
			pb.SizeMode = PictureBoxSizeMode.AutoSize;
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
			//this.Size = new Size(500, 250);
			Game.Initialize(this);
		}
	}
}
