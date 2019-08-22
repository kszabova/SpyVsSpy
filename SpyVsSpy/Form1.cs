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
		public static Room currentRoom;

		public static void LoadMapFromFile()
		{

		}

		// handles events when key is pressed
		public static void EventOnKeyPress(char key)
		{
			switch (key)
			{
				// movement
				case 'W': human.MovePlayer('U'); break;
				case 'S': human.MovePlayer('D'); break;
				case 'A': human.MovePlayer('L'); break;
				case 'D': human.MovePlayer('R'); break;

				// examining furniture and opening doors
				case 'X':
					int closeFurniture = currentRoom.FurnitureNearby(human.playerPosition.floorCoordinates);
					if (closeFurniture != -1)
					{
						currentRoom.furnitures[closeFurniture].Lift();
						UI.Wait(500);
						currentRoom.furnitures[closeFurniture].Release();
					}
					else
					{
						int closeDoors = currentRoom.DoorNearby(human.playerPosition.floorCoordinates);
						if (closeDoors != -1)
						{
							currentRoom.doors[closeDoors].Switch();
						}
					}
					break;
			}
		}

		// FOR NOW JUST FOR TESTING
		public static void Initialize(Form1 parent)
		{
			PictureBox upperFrame = UI.CreatePictureBox("placeholderBackground.png", new Coordinates(20, 20), 500, 200, parent);
			currentRoom = new Room('Y', upperFrame);
			currentRoom.AddDoor(1, parent);
			currentRoom.AddFurniture(0, parent);
			human = new Player(parent, upperFrame);
			currentRoom.LoadRoom();
		}
	}

	// player functionality
	public class Player
	{
		public Position playerPosition = new Position(1, 1, 1, new Coordinates(251, 141));
		Coordinates playerImageCoordinates = new Coordinates(0, 0);
		bool alive = true;
		public PictureBox playerImage;

		public Player(Form1 parent, PictureBox background)
		{
			UpdatePlayerImageCoordinates();
			playerImage = UI.CreatePictureBox("playerWhite.png", playerImageCoordinates, 40, 40, parent);
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
		public int type;    // from left (0) to right (5): bookcase, table, coat rack, shelf, microwave, drawer
		PictureBox furnitureImage;
		Coordinates imagePosition;
		string filename;

		public Furniture(int type, Form1 parent)
		{
			this.type = type;
			CalculateImagePosition();
			SetFilename();
			furnitureImage = UI.CreatePictureBox(filename, imagePosition, 60, 110, parent);
			furnitureImage.BringToFront();
		}

		public void Lift()
		{
			UI.ChangePictureBoxLocation(furnitureImage, new Coordinates(imagePosition.x, imagePosition.y - 15));
		}

		public void Release()
		{
			UI.ChangePictureBoxLocation(furnitureImage, imagePosition);
		}

		public void Show()
		{
			UI.ChangePictureBoxVisibility(furnitureImage, true);
		}

		public void Hide()
		{
			UI.ChangePictureBoxVisibility(furnitureImage, false);
		}

		// returns whether position is close to a specific type of furniture
		public static bool PositionInRangeOfFurniture(int type, Coordinates position)
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

		// calculates position of the furniture on the screen
		void CalculateImagePosition()
		{
			switch (type)
			{
				case 0: imagePosition = new Coordinates(50, 30); break;		// bookcase
				case 1: imagePosition = new Coordinates(130, 50); break;	// desk
				case 2: imagePosition = new Coordinates(150, 40); break;	// coat rack
				case 3: imagePosition = new Coordinates(250, 60); break;	// shelf
				case 4: imagePosition = new Coordinates(285, 60); break;	// microwave
				case 5: imagePosition = new Coordinates(380, 30); break;	// drawer
			}
			imagePosition.x += UI.upperFrameMargin.X;
			imagePosition.y += UI.upperFrameMargin.Y;
		}

		// sets the variable fileName according to furniture type
		void SetFilename()
		{
			switch (type)
			{
				case 0: filename = "bookcase.png"; break;
				case 1: filename = "desk.png"; break;
				case 2: filename = "coatrack.png"; break;
				case 3: filename = "shelf.png"; break;
				case 4: filename = "microwave.png"; break;
				case 5: filename = "drawer.png"; break;	// TEMPORARY
			}
		}
	}

	// handles door behavior
	public class Door
	{
		int location;	// possible values 0-3, in the middle of each wall
		int leadsTo;
		bool open;
		string openFileName;
		string closedFileName;
		PictureBox doorImage;
		Coordinates imagePosition;

		public Door(int location, Form1 parent)
		{
			this.location = location;
			CalculateImagePosition();
			SetFilename();
			doorImage = UI.CreatePictureBox(closedFileName, imagePosition, 60, 80, parent);
			doorImage.BringToFront();
		}

		// closes the door if open and vice versa
		public void Switch()
		{
			if (open)
				Close();
			else
				Open();
		}

		// makes the door visible
		public void Show()
		{
			UI.ChangePictureBoxVisibility(doorImage, true);
		}

		// makes the door invisible
		public void Hide()
		{
			UI.ChangePictureBoxVisibility(doorImage, false);
		}

		// returns true if position is in front of a specific door
		public static bool PositionInRangeOfDoor(int location, Coordinates position)
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

		// switches the image to that of open door
		void Open()
		{
			UI.ChangeImageInPictureBox(doorImage, openFileName);
			open = true;
		}

		// switches the image to closed
		void Close()
		{
			UI.ChangeImageInPictureBox(doorImage, closedFileName);
			open = false;
		}


		// calculates where the door will be placed on screen
		void CalculateImagePosition()
		{
			switch (location)
			{
				case 0: imagePosition = new Coordinates(20, 70); break;
				case 1: imagePosition = new Coordinates(220, 20); break;
				case 2: imagePosition = new Coordinates(450, 70); break;
				case 3: imagePosition = new Coordinates(210, 195); break;
			}
			imagePosition.x += UI.upperFrameMargin.X;
			imagePosition.y += UI.upperFrameMargin.Y;
		}

		// sets the filename variable depending on type of door
		void SetFilename()
		{
			switch (location)
			{
				case 0:
					closedFileName = "doorLeftClosed.png";
					openFileName = "doorLeftOpen.png";
					break;
				case 1:
					closedFileName = "doorBackClosed.png";
					openFileName = "doorBackOpen.png";
					break;
				case 2:
					closedFileName = "doorRightClosed.png";
					openFileName = "doorRightOpen.png";
					break;
				case 3:
					closedFileName = "doorFrontClosed.png";
					openFileName = "doorFrontOpen.png";
					break;
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

	// keeps track of furniture, doors, traps and objects in the room
	public class Room
	{
		char color;
		PictureBox frame;
		public Furniture[] furnitures = new Furniture[6];
		public Door[] doors = new Door[4];

		public Room(char color, PictureBox frame)
		{
			this.color = color;
			this.frame = frame;
		}

		// loads background, furniture and doors into image
		public void LoadRoom()
		{
			UI.ChangeImageInPictureBox(frame, RoomFilename());
			foreach (Furniture f in furnitures)
			{
				if (f != null)
				{
					f.Show();
				}
			}
			foreach (Door d in doors)
			{
				if (d != null)
				{
					d.Show();
				}
			}
		}

		// hides all furniture and doors
		public void HideRoom()
		{
			foreach (Furniture f in furnitures)
			{
				if (f != null)
				{
					f.Hide();
				}
			}
			foreach (Door d in doors)
			{
				if (d != null)
				{
					d.Hide();
				}
			}
		}

		// returns the number of furniture next to which the player is standing, -1 if none
		public int FurnitureNearby(Coordinates playerPosition)
		{
			for (int i = 0; i < 6; ++i)
			{
				if (furnitures[i] != null && Furniture.PositionInRangeOfFurniture(i, playerPosition))
				{
					return i;
				}
			}
			return -1;
		}

		// returns the number of door in front of which the player is standing, -1 if none
		public int DoorNearby(Coordinates playerPosition)
		{
			for (int i = 0; i < 4; ++i)
			{
				if (doors[i] != null && Door.PositionInRangeOfDoor(i, playerPosition))
				{
					return i;
				}
			}
			return -1;
		}

		// adds a piece of furniture to room  !!! TO BE CHANGED !!!
		public void AddFurniture(int i, Form1 parent)
		{
			furnitures[i] = new Furniture(i, parent);
		}

		// adds a door to room !!! TO BE CHANGED !!!
		public void AddDoor(int i, Form1 parent)
		{
			doors[i] = new Door(i, parent);
		}

		// returns the filename of background image depending on the color of room
		string RoomFilename()
		{
			return "room" + color + ".png";
		}
	}

	// UI functionality
	public class UI
	{
		static string baseImageAddress = "../../Assets/Images/";
		public static Point upperFrameMargin = new Point(20, 20);
		public static Point lowerFrameMargin = new Point(20, 240);

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
		public static PictureBox CreatePictureBox(string filename, Coordinates coords, int width, int height, Form1 parent)
		{
			PictureBox pb = new PictureBox();
			pb.ImageLocation = baseImageAddress + filename;
			pb.Size = new Size(width, height);
			pb.SizeMode = PictureBoxSizeMode.AutoSize;
			pb.Location = new Point(coords.x, coords.y);
			parent.Controls.Add(pb);
			return pb;
		}

		// changes the image in given PictureBox
		public static void ChangeImageInPictureBox(PictureBox pb, string filename)
		{
			pb.ImageLocation = baseImageAddress + filename;
		}
		
		// changes the location of given PictureBox
		public static void ChangePictureBoxLocation(PictureBox pb, Coordinates coords)
		{
			pb.Location = new Point(coords.x, coords.y);
		}

		// makes PictureBox visible or invisible
		public static void ChangePictureBoxVisibility(PictureBox pb, bool visibility)
		{
			if (visibility)
			{
				pb.Visible = true;
			}
			else
			{
				pb.Visible = false;
			}
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
				case Keys.X: Game.EventOnKeyPress('X'); break;
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
