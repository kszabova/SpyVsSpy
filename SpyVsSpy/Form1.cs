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
	}

	public class Player
	{
		Position playerPosition;
		bool alive;
	}

	public class Trap
	{

	}

	public class Position
	{
		int floor;
		int roomX;
		int roomY;
		int posX;
		int posY;
		
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
		int x;
		int y;

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
		public static PictureBox CreatePictureBox(string path, int xPos, int yPos, int width, int height, Form1 parent)
		{
			PictureBox pb = new PictureBox();
			pb.ImageLocation = path;
			pb.Size = new Size(width, height);
			pb.Location = new Point(xPos, yPos);
			parent.Controls.Add(pb);
			return pb;
		}

		// changes the image in given PictureBox
		public static void ChangeImageInPictureBox(PictureBox pb, string path)
		{
			pb.ImageLocation = path;
		}

		public static void ChangePictureBoxLocation(PictureBox pb, int xNew, int yNew)
		{
			pb.Location = new Point(xNew, yNew);
		}
	}

	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			PictureBox player = UI.CreatePictureBox("../../Assets/Images/placeholderPlayer.png", 100, 100, 50, 100, this);
			PictureBox background = UI.CreatePictureBox("../../Assets/Images/roomO.png", 0, 0, 500, 200, this);
			UI.ChangePictureBoxLocation(player, 200, 100);
		}
	}
}
