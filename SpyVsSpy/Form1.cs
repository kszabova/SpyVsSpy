﻿using System;
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
		public static Player[] players = new Player[2];
		public static Room currentRoom;
		public static Room[,,] levelMap;
		

		// handles events when key is pressed
		public static void EventOnKeyPress(char key)
		{
			if (players[0].alive)
			{
				switch (key)
				{
					// movement
					case 'W': players[0].MovePlayer('U'); break;
					case 'S': players[0].MovePlayer('D'); break;
					case 'A': players[0].MovePlayer('L'); break;
					case 'D': players[0].MovePlayer('R'); break;

					// examining furniture and opening doors
					case 'X':
						int closeFurniture = currentRoom.FurnitureNearby(players[0].playerPosition.floorCoordinates);
						// player is standing in front of a furniture
						if (closeFurniture != -1)
						{
							currentRoom.furnitures[closeFurniture].Lift(0);
							UI.Wait(500);
							currentRoom.furnitures[closeFurniture].Release();
						}
						else
						{
							int closeDoors = currentRoom.DoorNearby(players[0].playerPosition.floorCoordinates);
							// player is standing in front of a door
							if (closeDoors != -1)
							{
								currentRoom.doors[closeDoors].Switch();
							}
							// no furniture and no door => drop item in a random furniture in the room
							else
							{
								int furniture = currentRoom.GetRandomFurniture();
								players[0].DropItemToFurniture(furniture);
							}
						}
						break;
				}
			}
		}

		// loads next room given door in current room
		public static void LoadRoomByDoor(int door)
		{
			Triplet leadsTo = currentRoom.doors[door].leadsTo;
			Room nextRoom = levelMap[leadsTo.x, leadsTo.y, leadsTo.z];
			//currentRoom.HideRoom();
			currentRoom.images.Remove(players[0].image);
			nextRoom.LoadRoom(UI.roomViewUp);
			currentRoom = nextRoom;
		}

		// FOR NOW JUST FOR TESTING
		public static void Initialize(Form1 parent)
		{
			UI.parentForm = parent;
			UI.roomViewUp = UI.CreateImage("placeholderBackground.png", new Coordinates(20, 20), new Size(500, 200), parent);
			UI.sidePanelUp = UI.CreateImage("trapulatorPlaceholder.png", new Coordinates(540, 20), new Size(500, 200), parent);
			Item.InitializeItems();
			Triplet firstRoomCoords = UI.LoadLevel(1);
			currentRoom = levelMap[firstRoomCoords.x, firstRoomCoords.y, firstRoomCoords.z];
			players[0] = new Player(0);
			currentRoom.LoadRoom(UI.roomViewUp);
			UI.Countdown();
		}
	}

	// player functionality
	public class Player
	{
		public Position playerPosition = new Position(1, 1, 1, new Coordinates(251, 141));
		public TransparentPanel playerImage;
		Size imageSize = new Size(40, 40);
		Coordinates playerImageCoordinates = new Coordinates(0, 0);
		public ImageContainer image;
		public bool alive = true;

		int type;		// 0 for human, 1 for computer
		bool[] items = new bool[5];     // 0-passport, 1-key, 2-money, 3-secret plans, 4-suitcase
		string aliveImage;
		string deadImage;

		// !! TEMPORARY !! - will depend on type of player, reduce repeating code etc
		public Player(int type)
		{
			aliveImage = "playerWhite.png";
			deadImage = "playerWhiteDead.png";
			//playerImage = UI.CreateImage(aliveImage, playerImageCoordinates, imageSize, UI.roomViewUp);
			image = new ImageContainer(aliveImage, playerImageCoordinates.ToPoint(), imageSize);
			UpdatePlayerImageCoordinates();
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
				UI.UpdatePlayerOnScreen(UI.roomViewUp, 0);
			}

			// if player is crossing a door, loads the new room
			if (doorCrossed != -1 && ValidateDoorCrossing(direction, doorCrossed))
			{
				// update player's position
				Coordinates newPosition = CalculatePositionAfterCrossingDoor(doorCrossed, playerPosition.floorCoordinates);
				playerPosition.floorCoordinates = newPosition;
				UpdatePlayerImageCoordinates();
				UI.UpdatePlayerOnScreen(UI.roomViewUp, 0);
				// load new room
				try
				{
					Game.LoadRoomByDoor(doorCrossed);
				}
				catch
				{
					Game.players[0].Die();
				}
			}
		}

		// please dont
		public void Die()
		{
			alive = false;
			UI.secondsLeft -= 15;
			UI.ChangeImageInPanel(playerImage, deadImage);
			UI.FadeAway(playerImage);
			// after a while, player appears at the same place where he died
			UI.Wait(3000);
			UI.ChangeImageInPanel(playerImage, aliveImage);
			UI.UpdatePlayerOnScreen(UI.roomViewUp, 0);
			UI.ChangePanelVisibility(playerImage, true);
			alive = true;
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

		// puts any item the player is holding inside a furniture
		public void DropItemToFurniture(int furniture)
		{
			int item = ItemInPosession();
			Game.currentRoom.furnitures[furniture].item = item;
			// set all items to false; if player only had one item, it will get dropped; if they had a suitcase, they will lose all items
			for (int i = 0; i < 4; ++i)
			{
				items[i] = false;
				Item.HideFromTrapulator(i, type);
			}
			items[4] = false;
		}

		// updates the coordinates of playerImage when playerPosition changes
		void UpdatePlayerImageCoordinates()
		{
			playerImageCoordinates.x = playerPosition.floorCoordinates.x - 20;
			playerImageCoordinates.y = playerPosition.floorCoordinates.y - 40;
			image.location = playerImageCoordinates.ToPoint();
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
			// first check if player has the suitcase
			if (items[4])
			{
				return 4;
			}
			// otherwise check other items
			for (int i = 1; i < 4; ++i)
			{
				if (items[i])
				{
					return i;
				}
			}
			// if player doesn' have any item, return -1
			return -1;
		}
	}

	// handles furniture behavior
	public class Furniture
	{
		public int type;    // from left (0) to right (5): bookcase, table, coat rack, shelf, microwave, drawer
		public int item;
		public Size imageSize;
		public Coordinates imagePosition;
		public TransparentPanel furnitureImage;
		public ImageContainer image;
		string filename;

		public Furniture(int type, int item)
		{
			this.type = type;
			this.item = item;
			CalculateImageSize();
			CalculateImagePosition();
			SetFilename();
			//furnitureImage = UI.CreateImage(filename, imagePosition, imageSize, UI.roomViewUp);
			//furnitureImage.Hide();
			//furnitureImage.BringToFront();
			image = new ImageContainer(filename, imagePosition.ToPoint(), imageSize);
		}

		// puts the furniture higher in the air
		public void Lift(int player)
		{
			//UI.ChangePanelLocation(furnitureImage, new Coordinates(imagePosition.x, imagePosition.y - 15));
			image.location = new Point(imagePosition.x, imagePosition.y - 15);
			UI.UpdateFurnitureOnScreen(UI.roomViewUp, this);
			// if furniture contained an item, pick it up
			if (item != -1)
			{
				int newItem = Game.players[player].PickUpItem(item);
				item = newItem;
			}
			// otherwise put any object player is carrying inside that furniture
			else
			{
				Game.players[player].DropItemToFurniture(type);
			}
		}

		// puts the furniture back in its original position
		public void Release()
		{
			//UI.ChangePanelLocation(furnitureImage, imagePosition);
			image.location = imagePosition.ToPoint();
			UI.UpdateFurnitureOnScreen(UI.roomViewUp, this);
		}

		// makes furniture visible
		public void Show()
		{
			UI.ChangePanelVisibility(furnitureImage, true);
		}

		// makes furniture invisible
		public void Hide()
		{
			UI.ChangePanelVisibility(furnitureImage, false);
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
		}

		// calculates size of image depending on type
		void CalculateImageSize()
		{
			switch (type)
			{
				case 0: imageSize = new Size(70, 120); break;
				case 1: imageSize = new Size(120, 60); break;
				case 2: imageSize = new Size(50, 70); break;
				case 3: imageSize = new Size(70, 50); break;
				case 4: imageSize = new Size(80, 50); break;
				case 5: imageSize = new Size(60, 110); break;
				default: imageSize = new Size(0, 0); break;
			}
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
		public Size imageSize;
		public Coordinates imagePosition;
		public TransparentPanel doorImage;
		public ImageContainer image;

		public Door(int location, Triplet leadsTo)
		{
			this.location = location;
			this.leadsTo = leadsTo;
			CalculateImageSize();
			CalculateImagePosition();
			SetFilename();
			//doorImage = UI.CreateImage(closedFileName, imagePosition, imageSize, UI.roomViewUp);
			//doorImage.Hide();
			//doorImage.BringToFront();
			image = new ImageContainer(closedFileName, imagePosition.ToPoint(), imageSize);
		}

		// closes the door if open and vice versa
		public void Switch()
		{
			int oppositeDoor = GetCorrespondingDoor(location);
			try			// remove
			{			// remove
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
			catch		// remove
			{
				Game.players[0].Die();//remove
			}	//remove
		}

		// makes the door visible
		public void Show()
		{
			UI.ChangePanelVisibility(doorImage, true);
		}

		// makes the door invisible
		public void Hide()
		{
			UI.ChangePanelVisibility(doorImage, false);
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
			//UI.ChangeImageInPanel(doorImage, openFileName);
			image.filename = closedFileName;
			open = true;
		}

		// switches the image to closed
		void Close()
		{
			//UI.ChangeImageInPanel(doorImage, closedFileName);
			image.filename = openFileName;
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
		}

		// sets the size of door image depending on location
		void CalculateImageSize()
		{
			switch (location)
			{
				case 0: case 2: imageSize = new Size(30, 110); break;
				case 1: imageSize = new Size(60, 80); break;
				case 3: imageSize = new Size(80, 5); break;
				default: imageSize = new Size(0, 0); break;
			}
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

	// takes care mostly of trapulator images; most item logic is part of Player and Furniture class
	public class Item
	{
		static TransparentPanel[] itemsUp = new TransparentPanel[4];

		static string placeholderImage = "placeholderItem.png";
		static Size imageSize = new Size(50, 50);

		// creates space for item images on screen
		public static void InitializeItems()
		{
			for (int i = 0; i < 4; ++i)
			{
				itemsUp[i] = UI.CreateImage(placeholderImage, CalculatePositionOnTrapulator(i), imageSize, UI.sidePanelUp);
			}
		}

		// displays item's image on trapulator
		public static void ShowOnTrapulator(int item, int player)
		{
			if (player == 0)
			{
				UI.ChangeImageInPanel(itemsUp[item], GetFilename(item));
			}
		}

		// hides image from trapulator
		public static void HideFromTrapulator(int item, int player)
		{
			if (player == 0)
			{
				UI.ChangeImageInPanel(itemsUp[item], placeholderImage);
			}
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

		// converts Coordinates to Point
		public Point ToPoint()
		{
			return new Point(x, y);
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

		public List<int> furnituresPresent = new List<int> { };     // list of all pieces of furniture by number present in the room
		public List<int> doorsPresent = new List<int> { };

		public List<ImageContainer> images = new List<ImageContainer> { };

		public Room(char color)
		{
			this.color = color;
			images.Add(new ImageContainer(RoomFilename(), new Point(0, 0), new Size(500, 200)));
		}

		// loads background, furniture and doors into image
		public void LoadRoom(TransparentPanel frame)
		{
			//UI.ChangeImageInPanel(frame, RoomFilename());
			frame.images = images;
			images.Add(Game.players[0].image);
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
			furnituresPresent.Add(type);
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
			images.Add(furnitures[type].image);
		}

		// adds a door to room
		public void AddDoor(int i, Triplet leadsTo)
		{
			doorsPresent.Add(i);
			doors[i] = new Door(i, leadsTo);
			images.Add(doors[i].image);
		}

		// returns the number of a random furniture in the room
		public int GetRandomFurniture()
		{
			Random random = new Random();
			int index = random.Next(furnituresPresent.Count);
			return furnituresPresent[index];
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

		public static string baseImageAddress = "../../Assets/Images/";
		static string baseMapAddress = "../../Assets/LevelMaps/";
		
		public static TransparentPanel roomViewUp;
		public static TransparentPanel roomViewDown;
		public static TransparentPanel sidePanelUp;
		public static TransparentPanel sidePanelDown;
		public static TextPanel countdownUp;
		public static TextPanel countdownDown;

		public static int secondsLeft = 120;
		//static TextBox timer;

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

		// keeps track of time player has left
		public static void Countdown()
		{
			//timer = new TextBox();
			//timer.Location = new Point(540, 240);
			//timer.ReadOnly = true;
			//parentForm.Controls.Add(timer);
			Timer countdown = new Timer()
			{
				Interval = 1000,
				Enabled = true
			};
			countdown.Start();
			countdown.Tick += (s, e) =>
			{
				secondsLeft--;
				UpdateTimer();
				if (secondsLeft == 0)
				{
					Game.players[0].Die();
					countdown.Stop();
				}
			};
		}
		
		// creates a new panel with specified parameters and returns the TransparentPanel instance
		public static TransparentPanel CreateImage(string filename, Coordinates coords, Size size, Control parent)
		{
			TransparentPanel panel = new TransparentPanel();
			panel.Size = size;
			panel.Location = new Point(coords.x, coords.y);
			panel.ChangeImage(baseImageAddress + filename, size);
			parent.Controls.Add(panel);
			return panel;
		}
		
		// changes the image in given panel
		public static void ChangeImageInPanel(TransparentPanel panel, string filename)
		{
			panel.ChangeImage(baseImageAddress + filename, panel.Size);
		}
		
		// changes the location of given panel
		public static void ChangePanelLocation(TransparentPanel panel, Coordinates coords)
		{
			panel.Location = new Point(coords.x, coords.y);
			RedrawRoom('u');
		}
		
		// redraws the area with furniture
		public static void UpdateFurnitureOnScreen(TransparentPanel panel, Furniture furniture)
		{
			Rectangle areaToUpdate = GetRectangleWithMargin(furniture.image.location, furniture.image.size, 15);
			panel.Invalidate(areaToUpdate);
			UpdatePlayerOnScreen(panel, 0);
		}

		// redraws player
		public static void UpdatePlayerOnScreen(TransparentPanel panel, int player)
		{
			Rectangle areaToUpdate = GetRectangleWithMargin(Game.players[player].image.location, Game.players[player].image.size, 5);
			panel.Invalidate(areaToUpdate);
		}

		// makes panel visible if visibility==true, otherwise makes it invisible
		public static void ChangePanelVisibility(TransparentPanel panel, bool visibility)
		{
			if (visibility)
			{
				panel.Visible = true;
			}
			else
			{
				panel.Visible = false;
			}
		}
		
		// incrementally puts image higher and higher until it eventually disappears
		public static void FadeAway(TransparentPanel panel)
		{
			for (int i = 0; i < 10; ++i)
			{
				ChangePanelLocation(panel, new Coordinates(panel.Location.X, panel.Location.Y - 10));
				Wait(200);
			}
			ChangePanelVisibility(panel, false);
		}

		// loads map of the rooms from file, returns starting room
		public static Triplet LoadLevel(int level)
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

		// updates text on timer
		static void UpdateTimer()
		{
			int minutes = secondsLeft / 60;
			int seconds = secondsLeft % 60;
			string timeText = Convert.ToString(minutes) + ":" + Convert.ToString(seconds);
			countdownUp.UpdateText(timeText);
		}

		// loads main parts of the UI
		public static void LoadUI(Form1 form)
		{
			parentForm = form;
			roomViewUp = new TransparentPanel();
			roomViewUp.Location = new Point(20, 20);
			roomViewUp.Size = new Size(500, 200);
			parentForm.Controls.Add(roomViewUp);

			sidePanelUp = new TransparentPanel();
			sidePanelUp.Location = new Point(540, 20);
			sidePanelUp.Size = new Size(200, 200);
			parentForm.Controls.Add(sidePanelUp);

			
			countdownUp = new TextPanel();
			countdownUp.Location = new Point(0, 0);
			countdownUp.Size = new Size(200, 100);
			sidePanelUp.Controls.Add(countdownUp);

			roomViewDown = new TransparentPanel();
			roomViewDown.Location = new Point(20, 240);
			roomViewDown.Size = new Size(500, 200);
			parentForm.Controls.Add(roomViewDown);

			sidePanelDown = new TransparentPanel();
			sidePanelDown.Location = new Point(540, 240);
			sidePanelDown.Size = new Size(200, 200);
			parentForm.Controls.Add(sidePanelDown);

			countdownDown = new TextPanel();
			countdownDown.Location = new Point(0, 0);
			countdownDown.Size = new Size(200, 100);
			sidePanelDown.Controls.Add(countdownDown);
		}

		// this causes buffer - find a better way?
		public static void RedrawRoom(char frame)
		{
			//roomViewUp.Invalidate();
			TransparentPanel roomView;
			if (frame == 'u')
				roomView = roomViewUp;
			else
				roomView = roomViewDown;

			/**
			int nearbyFurniture = Game.currentRoom.FurnitureNearby(Game.players[0].playerPosition.floorCoordinates);
			foreach (int i in Game.currentRoom.furnituresPresent)
			{
				if (i == nearbyFurniture)
				{
					//roomView.Invalidate(new Rectangle(Game.currentRoom.furnitures[i].imagePosition.ToPoint(), Game.currentRoom.furnitures[i].imageSize));
					//Game.currentRoom.furnitures[i].furnitureImage.Invalidate();
					roomView.Invalidate(GetRectangleWithMargin(Game.currentRoom.furnitures[i].imagePosition.ToPoint(), Game.currentRoom.furnitures[i].imageSize, 10));
				}
			}
			int nearbyDoor = Game.currentRoom.DoorNearby(Game.players[0].playerPosition.floorCoordinates);
			foreach (int i in Game.currentRoom.doorsPresent)
			{
				if (i == nearbyDoor)
				{
					//roomView.Invalidate(new Rectangle(Game.currentRoom.doors[i].imagePosition.ToPoint(), Game.currentRoom.doors[i].imageSize));
					//Game.currentRoom.doors[i].doorImage.Invalidate();
					roomView.Invalidate(GetRectangleWithMargin(Game.currentRoom.doors[i].imagePosition.ToPoint(), Game.currentRoom.doors[i].imageSize, 0));
				}
			}
			//roomView.Invalidate(new Rectangle(Game.players[0].playerImage.Location, Game.players[0].playerImage.Size));
			//Game.players[0].playerImage.Invalidate();
			**/
			roomView.Invalidate(GetRectangleWithMargin(Game.players[0].playerImage.Location, Game.players[0].playerImage.Size, 5));
			Game.players[0].playerImage.Invalidate();
		}

		// returns Rectangle with a given margin around a given area
		static Rectangle GetRectangleWithMargin(Point startingPoint, Size startingSize, int margin)
		{
			Point newPoint = new Point(startingPoint.X - margin, startingPoint.Y - margin);
			Size newSize = new Size(startingSize.Width + 2 * margin, startingSize.Height + 2 * margin);
			return new Rectangle(newPoint, newSize);
		}
		
	}

	// transparent control for graphics
	public class TransparentPanel : Panel
	{
		string imageFilename = "../../Assets/Images/placeholderBackground.png";
		public List<ImageContainer> images = new List<ImageContainer> { };

		[Browsable(false)]
		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams cp = base.CreateParams;
				cp.ExStyle |= 0x20;
				return cp;
			}
		}

		protected override void OnPaintBackground(PaintEventArgs e)
		{
			// do nothing
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			foreach (ImageContainer image in images)
			{
				g.DrawImage(Image.FromFile(UI.baseImageAddress + image.filename), image.location.X, image.location.Y, image.size.Width, image.size.Height);
			}
			base.OnPaint(e);
		}

		public void ChangeImage(string filename, Size size)
		{
			imageFilename = filename;
			Size = size;
			Invalidate();
		}
	}

	// panel that can display dynamically updated text
	public class TextPanel : Panel
	{
		public Font font = new Font("Calibri", 40);
		public SolidBrush brush = new SolidBrush(Color.Aquamarine);
		public string text = "";

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.DrawString(text, font, brush, new Point(20, 20));
			base.OnPaint(e);
		}

		public void UpdateText(string text)
		{
			Invalidate();
			this.text = text;
		}
	}

	public class ImageContainer
	{
		public string filename;
		public Point location;
		public Size size;

		public ImageContainer(string filename, Point location, Size size)
		{
			this.filename = filename;
			this.location = location;
			this.size = size;
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
			UI.LoadUI(this);
			Game.Initialize(this);
		}
	}
}
