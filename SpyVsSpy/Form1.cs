using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace SpyVsSpy
{
	// control of the game
	public class Game
	{
		public static Player human;
		public static Room currentRoom;
		public static Room[,,] levelMap;
		

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

		// loads next room given door in current room
		public static void LoadRoomByDoor(int door)
		{
			Triplet leadsTo = currentRoom.doors[door].leadsTo;
			Room nextRoom = levelMap[leadsTo.x, leadsTo.y, leadsTo.z];
			currentRoom.HideRoom();
			nextRoom.LoadRoom(UI.upperFrame);
			currentRoom = nextRoom;
		}

		// FOR NOW JUST FOR TESTING
		public static void Initialize(Form1 parent)
		{
			UI.parentForm = parent;
			UI.upperFrame = UI.CreatePictureBox("placeholderBackground.png", new Coordinates(20, 20), 500, 200);
			UI.trapulatorUp = UI.CreatePictureBox("trapulatorPlaceholder.png", new Coordinates(540, 20), 200, 200);
			Item.InitializeItems();
			Triplet firstRoomCoords = UI.LoadLevel(1, parent);
			currentRoom = levelMap[firstRoomCoords.x, firstRoomCoords.y, firstRoomCoords.z];
			human = new Player(0);
			currentRoom.LoadRoom(UI.upperFrame);
		}
	}

	// player functionality
	public class Player
	{
		public Position playerPosition = new Position(1, 1, 1, new Coordinates(251, 141));
		public PictureBox playerImage;
		Coordinates playerImageCoordinates = new Coordinates(0, 0);

		int type;		// 0 for human, 1 for computer
		bool alive = true;
		bool[] items = new bool[5];		// 0-passport, 1-key, 2-money, 3-secret plans, 4-suitcase

		public Player(int type)
		{
			UpdatePlayerImageCoordinates();
			playerImage = UI.CreatePictureBox("playerWhite.png", playerImageCoordinates, 40, 40);
			// the following two lines are necessary so that the player has a transparent background
			if (type == 0)
			{
				UI.upperFrame.Controls.Add(playerImage);
				UI.upperFrame.BackColor = Color.Transparent;
			}
			else
			{
				UI.lowerFrame.Controls.Add(playerImage);
				UI.lowerFrame.BackColor = Color.Transparent;
			}
		}

		// moves player in given direction and updates his position on the screen
		public void MovePlayer(char direction)
		{
			Coordinates newCoords = new Coordinates(playerPosition.floorCoordinates.x, playerPosition.floorCoordinates.y);
			int doorCrossed = -1;

			switch (direction)
			{
				case 'U': newCoords.y -= 5; break;
				case 'D': newCoords.y += 5; break;
				case 'L': newCoords.x -= 5; break;
				case 'R': newCoords.x += 5; break;
			}

			// check if player is trying to cross a door
			SetDoorBeingCrossed(newCoords, ref doorCrossed);

			// check if player is within the limits of the floor, updates coordinates if so
			if (Coordinates.CheckIfValidFloorPosition(newCoords))
			{
				playerPosition.floorCoordinates = newCoords;
				UpdatePlayerImageCoordinates();
				UI.ChangePictureBoxLocation(playerImage, playerImageCoordinates);
			}

			// if player is crossing a door, loads the new room
			if (doorCrossed != -1 && ValidateDoorCrossing(direction, doorCrossed))
			{
				// update player's position
				Coordinates newPosition = CalculatePositionAfterCrossingDoor(doorCrossed, playerPosition.floorCoordinates);
				playerPosition.floorCoordinates = newPosition;
				UpdatePlayerImageCoordinates();										// v
				UI.ChangePictureBoxLocation(playerImage, playerImageCoordinates);   // make these two lines into a new function (Refresh?)
				// load new room
				Game.LoadRoomByDoor(doorCrossed);
			}
		}

		// pick up item in furniture; return what item is now in furniture (-1 for none)
		public int PickUpItem(int item)
		{
			// furniture contained suitcase -> player now has suitcase and everything in it
			if (item == 4)
			{
				items[4] = true;
				for (int i = 0; i < 4; ++i)
				{
					items[i] = Suitcase.contents[i];
					if (items[i])
					{
						Item.ShowOnTrapulator(i, type);
					}
				}
				return -1;
			}
			// player has suitcase -> add the item to it
			else if (items[4])
			{
				Suitcase.AddItem(item);
				items[item] = true;
				Item.ShowOnTrapulator(item, type);
				return -1;
			}
			// no suitcase
			else
			{
				int itemInPosession = ItemInPosession();
				// player has some item -> swap the two
				if (itemInPosession > -1)
				{
					items[item] = true;
					items[itemInPosession] = false;
					Item.ShowOnTrapulator(item, type);
					Item.HideFromTrapulator(itemInPosession, type);
					return itemInPosession;
				}
				// player has no item -> simply pick up the one in the furniture
				else
				{
					items[item] = true;
					Item.ShowOnTrapulator(item, type);
					return itemInPosession;
				}
			}
		}

		public void DropItem(int item)
		{
			items[item] = false;
		}

		// updates the coordinates of playerImage when playerPosition changes
		void UpdatePlayerImageCoordinates()
		{
			playerImageCoordinates.x = playerPosition.floorCoordinates.x - 20;
			playerImageCoordinates.y = playerPosition.floorCoordinates.y - 40;
		}

		// checks if player is crossing a door; if so, sets the variable doorCrossed to the number of that door
		void SetDoorBeingCrossed(Coordinates pos, ref int doorCrossed)
		{
			for (int i = 0; i < 4; ++i)
			{
				if (Game.currentRoom.doors[i] != null && Door.PositionInDoor(i, pos) && Game.currentRoom.doors[i].open)
				{
					doorCrossed = i;
				}
			}
		}

		// checks whether the door player wants to cross is correct according to key press
		bool ValidateDoorCrossing(char direction, int door)
		{
			return (direction == 'L' && door == 0) || (direction == 'U' && door == 1) ||
				(direction == 'R' && door == 2) || (direction == 'D' && door == 3);
		}

		// calculates new position of player after crossing door
		Coordinates CalculatePositionAfterCrossingDoor(int door, Coordinates curCoords)
		{
			Coordinates newCoords = new Coordinates(curCoords.x, curCoords.y);		// set the position to the same as current, only update the wrong coordinate
			switch (door)
			{
				case 0: newCoords.x = 295 + curCoords.y; break; // puts player 1px away from the wall
				case 1: newCoords.y = 195; break;               // player appears at the bottom of the screen
				case 2: newCoords.x = 205 - curCoords.y; break; // 1px away from the wall
				case 3: newCoords.y = 101; break;				// directly in front of the top door
			}
			return newCoords;
		}

		// returns number of item that player already has
		int ItemInPosession()
		{
			for (int i = 1; i < 5; ++i)
			{
				if (items[i])
				{
					return i;
				}
			}
			return -1;
		}
	}

	// handles furniture behavior
	public class Furniture
	{
		public int type;    // from left (0) to right (5): bookcase, table, coat rack, shelf, microwave, drawer
		public int item;
		PictureBox furnitureImage;
		Coordinates imagePosition;
		string filename;

		public Furniture(int type, int item)
		{
			this.type = type;
			this.item = item;
			CalculateImagePosition();
			SetFilename();
			furnitureImage = UI.CreatePictureBox(filename, imagePosition, 60, 110);
			furnitureImage.Hide();
			furnitureImage.BringToFront();
		}

		// puts the furniture higher in the air
		public void Lift()
		{
			UI.ChangePictureBoxLocation(furnitureImage, new Coordinates(imagePosition.x, imagePosition.y - 15));
			if (item != -1)
			{
				int newItem = Game.human.PickUpItem(item);
				item = newItem;
			}
		}

		// puts the furniture back in its original position
		public void Release()
		{
			UI.ChangePictureBoxLocation(furnitureImage, imagePosition);
		}

		// makes furniture visible
		public void Show()
		{
			UI.ChangePictureBoxVisibility(furnitureImage, true);
		}

		// makes furniture invisible
		public void Hide()
		{
			UI.ChangePictureBoxVisibility(furnitureImage, false);
		}

		// returns whether position is close to a specific type of furniture
		public static bool PositionInRangeOfFurniture(int type, Coordinates position)
		{
			switch (type)
			{
				case 0: return position.x < Coordinates.CalculateFloorLimit(position.y, 30, 'l') && position.y < 150;	// bookcase
				case 1: return position.x > 130 && position.x < 250 && position.y > 100 && position.y < 120;			// desk
				case 2: return position.x > 150 && position.x < 200 && position.y > 100 && position.y < 120;			// coat rack
				case 3: return position.x > 250 && position.x < 320 && position.y > 100 && position.y < 120;			// shelf
				case 4: return position.x > 285 && position.x < 365 && position.y > 100 && position.y < 120;			// microwave
				case 5: return position.x > Coordinates.CalculateFloorLimit(position.y, 30, 'r') && position.y < 140;	// drawer
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
		public Triplet leadsTo;
		public bool open;
		string openFileName;
		string closedFileName;
		PictureBox doorImage;
		Coordinates imagePosition;

		public Door(int location, Triplet leadsTo)
		{
			this.location = location;
			this.leadsTo = leadsTo;
			CalculateImagePosition();
			SetFilename();
			doorImage = UI.CreatePictureBox(closedFileName, imagePosition, 60, 80);
			doorImage.Hide();
			doorImage.BringToFront();
		}

		// closes the door if open and vice versa
		public void Switch()
		{
			int oppositeDoor = GetCorrespondingDoor(location);
			Room adjacentRoom = Game.levelMap[leadsTo.x, leadsTo.y, leadsTo.z];
			if (open)
			{
				Close();
				adjacentRoom.doors[oppositeDoor].Close();
			}
			else
			{
				Open();
				adjacentRoom.doors[oppositeDoor].Open();
			}
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
				case 0: return position.x <= Coordinates.CalculateFloorLimit(position.y, 10, 'l') && position.y >= 150 && position.y <= 180;	// left wall
				case 1: return position.x >= 220 && position.x <= 280 && position.y >= 100 && position.y <= 110;								// back wall
				case 2: return position.x >= Coordinates.CalculateFloorLimit(position.y, 10, 'r') && position.y >= 150 && position.y <= 180;	// right wall
				case 3: return position.x >= 210 && position.x <= 290 && position.y >= 190 && position.y <= 200;								// front (invisible) wall
				default: return false;
			}
		}

		// returns true if position is directly inside the door
		public static bool PositionInDoor(int location, Coordinates position)
		{
			switch (location)
			{
				case 0: return position.x <= Coordinates.CalculateFloorLimit(position.y, 3, 'l') && position.y >= 150 && position.y <= 180;
				case 1: return position.x >= 220 && position.x <= 280 && position.y <= 103;
				case 2: return position.x >= Coordinates.CalculateFloorLimit(position.y, 3, 'r') && position.y >= 150 && position.y <= 180;
				case 3: return position.x >= 220 && position.x <= 280 && position.y >= 192;
				default: return false;
			}
		}

		// returns the number of the door on the opposite side of the wall
		static int GetCorrespondingDoor(int location)
		{
			switch (location)
			{
				case 0: return 2;
				case 1: return 3;
				case 2: return 0;
				case 3: return 1;
				default: return -1;
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

	public class Item
	{
		static PictureBox[] itemsUp = new PictureBox[4];

		static string placeholderImage = "placeholderItem.png";

		// creates space for item images on screen
		public static void InitializeItems()
		{
			for (int i = 0; i < 4; ++i)
			{
				itemsUp[i] = UI.CreatePictureBox(placeholderImage, CalculatePositionOnTrapulator(i), 50, 50);
				UI.trapulatorUp.Controls.Add(itemsUp[i]);
				UI.trapulatorUp.BackColor = Color.Transparent;
			}
		}

		// displays item's image on trapulator
		public static void ShowOnTrapulator(int item, int player)
		{
			if (player == 0)
			{
				UI.ChangeImageInPictureBox(itemsUp[item], GetFilename(item));
			}
		}

		public static void HideFromTrapulator(int item, int player)
		{

		}

		// returns filename of image of given item
		static string GetFilename(int item)
		{
			string filename = "";
			switch (item)
			{
				case 0: filename = "passport"; break;
				case 1: filename = "money"; break;
				case 2: filename = "key"; break;
				case 3: filename = "secretplans"; break;
			}
			return filename + ".png";
		}

		// returns coordinates where item image should appear on trapulator
		static Coordinates CalculatePositionOnTrapulator(int item)
		{
			Coordinates coords = new Coordinates(0, 100);
			switch (item)
			{
				case 0:	coords.x = 0; break;
				case 1: coords.x = 50; break;
				case 2: coords.x = 100; break;
				case 3: coords.x = 150; break;
			}
			return coords;
		}
	}

	// keeps track of what is inside the suitcase
	public class Suitcase
	{
		public static bool[] contents = new bool[4];

		// initialize suitcase with only one item
		public Suitcase(int initialItem)
		{
			for (int i = 0; i < 4; ++i)
			{
				contents[i] = false;
			}
			contents[initialItem] = true;
		}

		public static void AddItem(int item)
		{
			contents[item] = true;
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
		public static bool CheckIfValidFloorPosition(Coordinates coords)
		{
			return (coords.x >= CalculateFloorLimit(coords.y, 0, 'l') && coords.x <= CalculateFloorLimit(coords.y, 0, 'r')) &&
				(coords.y >= 100 && coords.y <= 200);				
		}

		// calculates the x-coordinate of floor limit at y-coordinate distanceFromTop either on [l]eft or [r]ight side
		public static int CalculateFloorLimit(int distanceFromTop, int margin, char side)
		{
			// the value is calculated as follows: the distance from the center horizontal line to floor line is the same as
			// its distance from the 100th coordinate on the left or 400th on the right, respectively, and the edge;
			// then the margin pushes the imaginary line towards the center, therefore we add to left and substract from right
			switch (side)
			{
				case 'l': return 200 - distanceFromTop + margin;
				case 'r': return 300 + distanceFromTop - margin;
				default: return 0;
			}
		}
	}

	// holds three integer values
	public class Triplet
	{
		public int x, y, z;
		public Triplet(int x, int y, int z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}
	}

	// keeps track of furniture, doors, traps and objects in the room
	public class Room
	{
		char color;
		public Furniture[] furnitures = new Furniture[6];
		public Door[] doors = new Door[4];

		public Room(char color)
		{
			this.color = color;
		}

		// loads background, furniture and doors into image
		public void LoadRoom(PictureBox frame)
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

		// adds a piece of furniture to room
		public void AddFurniture(int type, int item)
		{
			// item values larger than 3 mean it is suitcase with something inside
			if (item > 3)
			{
				Suitcase.AddItem(item % 4);
				// the furniture wil seemingly contain suitcase only
				furnitures[type] = new Furniture(type, 4);
			}
			// otherwise it contains at most one item
			else
			{
				furnitures[type] = new Furniture(type, item);
			}
		}

		// adds a door to room
		public void AddDoor(int i, Triplet leadsTo)
		{
			doors[i] = new Door(i, leadsTo);
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
		public static Form1 parentForm;

		static string baseImageAddress = "../../Assets/Images/";
		static string baseMapAddress = "../../Assets/LevelMaps/";

		public static PictureBox upperFrame;
		public static PictureBox lowerFrame;
		public static PictureBox trapulatorUp;
		public static PictureBox trapulatorDown;

		public static Point upperFrameMargin = new Point(20, 20);	// offset from (0, 0)
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
		public static PictureBox CreatePictureBox(string filename, Coordinates coords, int width, int height)
		{
			PictureBox pb = new PictureBox();
			pb.ImageLocation = baseImageAddress + filename;
			pb.Size = new Size(width, height);
			pb.SizeMode = PictureBoxSizeMode.AutoSize;
			pb.Location = new Point(coords.x, coords.y);
			parentForm.Controls.Add(pb);
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

		// loads map of the rooms from file, returns starting room
		public static Triplet LoadLevel(int level, Form1 parent)
		{
			string filename = baseMapAddress + "level" + level.ToString() + ".txt";
			Debug.WriteLine(filename);
			StreamReader sr;
			try
			{
				sr = new StreamReader(filename);	
			}
			catch
			{
				throw new Exception();
			}
			string currentLine = sr.ReadLine();
			string[] lineSplit = currentLine.Split();

			int floors = Convert.ToInt32(lineSplit[0]);
			int rows = Convert.ToInt32(lineSplit[1]);
			int cols = Convert.ToInt32(lineSplit[2]);

			Game.levelMap = new Room[floors, rows, cols];

			// load individual rooms
			for (int f = 0; f < floors; ++f)
				for (int r = 0; r < rows; ++r)
					for (int c = 0; c < cols; ++c)
					{
						char color = sr.ReadLine()[0];
						if (color == 'X')		// if room doesn't exist, skip the rest of the code
						{
							continue;
						}
						Game.levelMap[f, r, c] = new Room(color);
						Room currentRoom = Game.levelMap[f, r, c];

						// load furnitures
						int noFurnitures = Convert.ToInt32(sr.ReadLine());
						string[] furnitures = sr.ReadLine().Split();
						for (int i = 0; i < noFurnitures * 2; i += 2)
						{
							int type = Convert.ToInt32(furnitures[i]);
							int item = Convert.ToInt32(furnitures[i + 1]);
							currentRoom.AddFurniture(type, item);
						}

						int noDoors = Convert.ToInt32(sr.ReadLine());
						string[] doors = sr.ReadLine().Split();
						for (int i = 0; i < noDoors * 2; i += 2)
						{
							string[] leadsToSplit = doors[i + 1].Split(',');
							Triplet leadsTo = new Triplet(Convert.ToInt32(leadsToSplit[0]), Convert.ToInt32(leadsToSplit[1]), Convert.ToInt32(leadsToSplit[2]));
							currentRoom.AddDoor(Convert.ToInt32(doors[i]), leadsTo);
						}
					}
			string[] firstRoomSplit = sr.ReadLine().Split(',');
			return new Triplet(Convert.ToInt32(firstRoomSplit[0]), Convert.ToInt32(firstRoomSplit[1]), Convert.ToInt32(firstRoomSplit[2]));
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
			this.Size = new Size(800, 500);
			Game.Initialize(this);
		}
	}
}
