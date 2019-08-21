﻿using System;
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
			PictureBox cabinet = UI.CreatePictureBox(UI.baseImageAddress + "cabinetFrontView.png", new Coordinates(150, 30), 100, 70, parent);
			PictureBox background = UI.CreatePictureBox(UI.baseImageAddress + "roomB.png", new Coordinates(0, 0), 500, 200, parent);
			human = new Player(parent, background);
			background.Controls.Add(cabinet);
			human.playerImage.BringToFront();
		}
	}

	// player functionality
	public class Player
	{
		Position playerPosition = new Position(1, 1, 1, new Coordinates(251, 141));
		Coordinates playerImageCoordinates = new Coordinates(0, 0);
		bool alive = true;
		public PictureBox playerImage;

		public Player(Form1 parent, PictureBox background)
		{
			UpdatePlayerImageCoordinates();
			playerImage = UI.CreatePictureBox(UI.baseImageAddress + "playerWhite.png", playerImageCoordinates, 40, 40, parent);
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
				UpdatePlayerImageCoordinates();
				UI.ChangePictureBoxLocation(playerImage, playerImageCoordinates);
			}
		}

		// updates the coordinates of playerImage when playerPosition changes
		private void UpdatePlayerImageCoordinates()
		{
			playerImageCoordinates.x = playerPosition.floorCoordinates.x - 20;
			playerImageCoordinates.y = playerPosition.floorCoordinates.y - 40;
		}
	}

	// handles furniture behavior
	public class Furniture
	{
		public int type;	// from left (0) to right (5): bookcase, table, coat rack, shelf, microwave, drawer

		public void Lift()
		{

		}

		public void Release()
		{

		}

		// returns whether position is close to a specific type of furniture
		public static bool positionInRangeOfFurniture(int type, Coordinates position)
		{
			switch (type)
			{
				case 0: return position.x > (100 - position.y + 100) && position.x < (120 - position.y + 100) && position.y < 150;	// bookcase
				case 1: return position.x > 130 && position.x < 250 && position.y > 100 && position.y < 120;						// desk
				case 2: return position.x > 150 && position.x < 200 && position.y > 100 && position.y < 120;						// coat rack
				case 3: return position.x > 250 && position.x < 320 && position.y > 100 && position.y < 120;						// shelf
				case 4: return position.x > 285 && position.x < 365 && position.y > 100 && position.y < 120;						// microwave
				case 5: return position.x < (400 + position.y - 100) && position.x > (380 + position.y - 100) && position.y < 140;	// drawer
				default: return false;
			}
		}
	}

	// handles door behavior
	public class Door
	{
		int location;	// possible values 0-3, in the middle of each wall
		int leadsTo;
		bool open;

		public void Open()
		{

		}

		public void Close()
		{

		}

		// returns true if position is in front of a specific door
		public static bool positionInRangeOfDoor(int location, Coordinates position)
		{
			switch (location)
			{
				case 0: return position.x < (110 - position.y + 100) && position.y > 150 && position.y < 180;	// left wall
				case 1: return position.x > 220 && position.x < 280 && position.y > 100 && position.y < 110;    // back wall
				case 2: return position.x > (390 - position.y - 100) && position.y > 150 && position.y < 180;	// right wall
				case 3: return position.x > 220 && position.x < 280 && position.y > 190 && position.y < 200;    // front (invisible) wall
				default: return false;
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

	// handling the position of objects on the floor
	public class Coordinates
	{
		public int x;
		public int y;

		public Coordinates(int x, int y)
		{
			this.x = x;
			this.y = y;
		}

		// returns true if given position is on the floor
		// backgroundX and backgroundY are parameters specifying the position of upper left corner of background
		public static bool CheckIfValidFloorPosition(Coordinates coords, int backgroundX, int backgroundY)
		{
			// for horizontal coordinates, the limit is calculated this way:
			// backgroundX gives the margin from 0, therefore we have to factor it in;
			// the distance of the line on the left/right side of the floor from the respective edge is the same
			// as the distance from the top floor line, which gives us 100-(coords.y-100) and 400+(coords.y-100) respectively
			return (coords.x > backgroundX + 200 - coords.y && coords.x < backgroundX + 300 + coords.y) &&
				// vertically, the player must be between 100 and 200
				(coords.y > backgroundY + 100 && coords.y < backgroundY + 200);				
		}
	}

	public class Room
	{
		Furniture[] furnitures = new Furniture[6];
		Door[] doors = new Door[4];

		// returns the number of furniture next to which the player is standing, -1 if none
		public int furnitureNearby(Coordinates playerPosition)
		{
			for (int i = 0; i < 6; ++i)
			{
				if (furnitures[i] != null && Furniture.positionInRangeOfFurniture(i, playerPosition))
				{
					return i;
				}
			}
			return -1;
		}

		// returns the number of door in front of which the player is standing, -1 if none
		public int doorNearby(Coordinates playerPosition)
		{
			for (int i = 0; i < 4; ++i)
			{
				if (doors[i] != null && Door.positionInRangeOfDoor(i, playerPosition))
				{
					return i;
				}
			}
			return -1;
		}
	}

	// UI functionality
	public class UI
	{
		public static string baseImageAddress = "../../Assets/Images/";

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
