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
		static int NUMBER_OF_LEVELS = 3;

		// state of game
		static bool started = false;
		public static bool stopped = false;
		static int currentLevel = 0;
		static int computer_wins = 0;
		static int human_wins = 0;

		// contains plan of current level
		public static Room[,,] levelMap;		

		// basic components of the game
		public static Player[] players = new Player[2];
		public static Room[] rooms = new Room[2];
		
		// special rooms
		static Room noRoom = new Room('X');
		static Room airport = new Room('A');

		// handles events when key is pressed
		public static void EventOnKeyPress(char key)
		{
			if (started)
			{
				// if player is dead, don't do anything
				if (!players[0].alive)
					return;


				// otherwise do something depending on the key that was pressed
				switch (key)
				{
					// movement
					case 'W': players[0].MovePlayer('U'); break;
					case 'S': players[0].MovePlayer('D'); break;
					case 'A': players[0].MovePlayer('L'); break;
					case 'D': players[0].MovePlayer('R'); break;

					// examining furniture and opening doors
					case 'X':
						// player is in a room examining furniture
						if (players[0].state == 0)
						{
							int closeFurniture = rooms[players[0].panelOnScreen].FurnitureNearby(players[0].playerPosition.floorCoordinates);
							// player is standing in front of a furniture
							if (closeFurniture != -1)
							{
								rooms[players[0].panelOnScreen].furnitures[closeFurniture].Lift(0);
								UI.Wait(500);
								rooms[players[0].panelOnScreen].furnitures[closeFurniture].Release(0);
							}
							else
							{
								int closeDoor = rooms[players[0].panelOnScreen].DoorNearby(players[0].playerPosition.floorCoordinates);
								// player is standing in front of a door
								if (closeDoor != -1)
								{
									rooms[players[0].panelOnScreen].doors[closeDoor].Switch(0);
								}
								// no furniture and no door => drop item in a random furniture in the room
								else
								{
									int furniture = rooms[players[0].panelOnScreen].GetRandomEmptyFurniture();
									players[0].DropItemToFurniture(furniture);
								}
							}
						}
						// player is holding a trap and wants to set it
						else if (players[0].state == 2)
						{
							players[0].state = 0;

							// player is holding a time bomb
							if (players[0].trap == 1)
							{
								Trap.SetTrap(rooms[0]);     // hard-coded 0 because player can only set trap when he is alone in his room
							}
							// player is holding a water bucket
							else if (players[0].trap == 2)
							{
								int closeDoor = rooms[players[0].panelOnScreen].DoorNearby(players[0].playerPosition.floorCoordinates);
								if (closeDoor != -1)
								{
									Trap.SetTrap(rooms[0].doors[closeDoor]);
								}
							}
							// player is holding a bomb
							else if (players[0].trap == 3)
							{
								int closeFurniture = rooms[players[0].panelOnScreen].FurnitureNearby(players[0].playerPosition.floorCoordinates);
								if (closeFurniture != -1)
								{
									Trap.SetTrap(rooms[0].furnitures[closeFurniture]);
								}
							}

							// note that if player is not next to a furniture or door and they press the release button, they will lose the trap
						}
						break;

					// entering trapulator
					case 'Z':
						if (players[0].state == 0)
						{
							players[0].state = 1;
							UI.HighlightPanel(0);
						}
						break;

					// choosing a trap
					case '1':
					case '2':
					case '3':
						if (players[0].state == 1)
						{
							players[0].trap = Convert.ToInt32(key) - 48;
							players[0].state = 2;
							UI.UnhighlightPanel(0);
						}
						break;

					// map is not implemented
					case '4':
						if (players[0].state == 1)
						{
							UI.ShowMessage("Your supervisor failed to give you the map.\nThat sucks.", 0);
							players[0].state = 0;
						}
						break;

					// hit other player
					case ' ': players[0].Hit(players[1]); break;

					// toggle help
					case 'H':
						UI.ToggleHelp();
						break;
				}
			}
			else if (key == 'H')
			{
				started = true;
				currentLevel++;
				InitializeNextLevel();
				UI.ToggleHelp();
			}
		}

		// loads next room given door in current room
		public static void LoadRoomByDoor(int door, int player)
		{
			Room currentRoom = rooms[players[player].panelOnScreen];
			Triplet leadsTo = currentRoom.doors[door].leadsTo;
			Debug.WriteLine(leadsTo.x.ToString(), leadsTo.y, leadsTo.z);
			Room nextRoom = levelMap[leadsTo.x, leadsTo.y, leadsTo.z];

			// update player's position
			players[player].playerPosition.floor = leadsTo.x;
			players[player].playerPosition.roomX = leadsTo.y;
			players[player].playerPosition.roomY = leadsTo.z;

			// both players are in the current room => both new rooms are drawn into their respective default place
			if (currentRoom.occupiedBy == 2)
			{
				// update player's panel to default
				players[player].panelOnScreen = player;
				int otherPlayer = player == 0 ? 1 : 0;
				players[otherPlayer].panelOnScreen = otherPlayer;

				// update room panels
				currentRoom.occupiedBy = -1;
				nextRoom.occupiedBy = -1;
				currentRoom.LoadRoom(UI.roomPanels[otherPlayer], otherPlayer);
				nextRoom.LoadRoom(UI.roomPanels[player], player);
				rooms[otherPlayer] = currentRoom;
				rooms[player] = nextRoom;

				// redraw players into their respective panels
				UI.RemovePlayerFromCurrentView(player);
				UI.RemovePlayerFromCurrentView(otherPlayer);
				players[player].DisplayPlayerInView();
				players[otherPlayer].DisplayPlayerInView();
			}
			// if player is entering a room that is already occupied, his panel gets white and the player is drawn into the other panel
			else if (nextRoom.occupiedBy != -1)
			{
				currentRoom.LeaveRoom(player);
				noRoom.LoadRoom(UI.roomPanels[players[player].panelOnScreen], player);
				UI.RemovePlayerFromCurrentView(player);
				players[player].SwitchPanel();
				rooms[players[player].panelOnScreen].AddPlayer(player);
			}
			else
			{
				currentRoom.LeaveRoom(player);
				nextRoom.LoadRoom(UI.roomPanels[player], player);
				rooms[player] = nextRoom;
			}

			// restore player health
			players[player].health = 10;
		}

		// try entering airport
		public static void LoadAirport(int player)
		{
			if (players[player].ItemInPosession() == 4 && Suitcase.numberOfItems == 4)
			{
				airport.LoadRoom(UI.roomPanels[players[player].panelOnScreen], player);
				currentLevel++;
				players[0].StopDoingActions();
				players[1].StopDoingActions();
				stopped = true;
				UI.Wait(3000);
				UI.ClearLevel();
				// if we reached the end of the game, show winner
				if (currentLevel > NUMBER_OF_LEVELS)
				{
					Stop(human_wins > computer_wins ? 1 : 0);
				}
				else
				{
					stopped = false;
					InitializeNextLevel();
				}
			}
			else
			{
				players[player].GoTo(new Coordinates(250, 150));
				UI.ShowMessage("You can't go there yet!", player);
				players[player].Die();
				UI.RemoveMessage(player);
			}
		}

		// stop game
		public static void Stop(int loser)
		{
			players[0].StopDoingActions();
			UI.countdown.Stop();
			UI.Wait(1500);
			UI.ToggleScreen(false);
			stopped = true;
			players[1].StopDoingActions();
			if (loser == 2)
			{
				UI.DisplayWinnerMessage(loser);
			}
			else
			{
				int winner = loser == 0 ? 1 : 0;
				UI.DisplayWinnerMessage(winner);
			}
		}

		// loads all components of current level and starts countdown
		public static void InitializeNextLevel()
		{
			Triplet humanFirstRoom;
			Triplet computerFirstRoom;
			UI.LoadLevel(currentLevel, out humanFirstRoom, out computerFirstRoom);
			rooms[0] = levelMap[humanFirstRoom.x, humanFirstRoom.y, humanFirstRoom.z];
			rooms[1] = levelMap[computerFirstRoom.x, computerFirstRoom.y, computerFirstRoom.z];
			players[0] = new Player(0, humanFirstRoom);
			players[1] = new Player(1, computerFirstRoom);
			rooms[0].LoadRoom(UI.roomPanels[0], 0);
			rooms[1].LoadRoom(UI.roomPanels[1], 1);
			UI.Countdown();
			ComputerAI.Start();
		}

		// starts game
		public static void InitializeGame(Form1 parentForm)
		{
			UI.LoadUI(parentForm);
		}

	}

	// player functionality
	public class Player
	{
		// basic characteristics of player
		int playerType;     // 0 for human, 1 for computer
		public int state;   // 0 - default, 1 - on trapulator, 2 - holding a trap
		public bool alive = true;

		// visible part of player
		public PictureBox playerImage;
		public Position playerPosition = new Position(1, 1, 1, new Coordinates(250, 140));
		Coordinates playerImageCoordinates = new Coordinates(0, 0);
		public Size imageSize = new Size(40, 40);
		public int panelOnScreen;

		// traps
		public int trap;    // 1 - time bomb, 2 - water bucket, 3 - bomb, -1 - none
		public int disarm;  // 2 - umbrella, 3 - shield, -1 - none

		// numeric data
		public int secondsLeft = 240;
		public int numberOfItems = 0;
		public int health = 10;

		// posessions
		bool[] items = new bool[5];     // 0-passport, 1-key, 2-money, 3-secret plans, 4-suitcase

		// for internal use
		string aliveImage;
		string deadImage;
		string umbrellaImage;
		string shieldImage;
		string fightingImage;
		string suitcaseImage;

		// !! TEMPORARY !! - will depend on type of player, reduce repeating code etc
		public Player(int type, Triplet initialRoom)
		{
			this.playerType = type;
			playerPosition.floor = initialRoom.x;
			playerPosition.roomX = initialRoom.y;
			playerPosition.roomY = initialRoom.z;
			state = 0;
			trap = -1;
			disarm = -1;
			panelOnScreen = type;           // starting panel is the same as player type
			if (type == 0)
			{
				aliveImage = "playerWhite.png";
				deadImage = "playerWhiteDead.png";
				umbrellaImage = "playerWhiteUmbrella.png";
				shieldImage = "playerWhiteShield.png";
				fightingImage = "playerWhiteFighting.png";
				suitcaseImage = "playerWhiteSuitcase.png";
			}
			else if (type == 1)
			{
				// TODO change these to computer pictures
				aliveImage = "playerBlack.png";
				deadImage = "playerBlackDead.png";
				umbrellaImage = "playerBlackUmbrella.png";
				shieldImage = "playerBlackShield.png";
				fightingImage = "playerBlackFighting.png";
				suitcaseImage = "playerBlackSuitcase.png";
			}
			UpdatePlayerImageCoordinates();
			playerImage = UI.CreatePictureBox(aliveImage, playerImageCoordinates, imageSize);
			UI.roomPanels[type].BackColor = Color.Transparent;     // ?? why doesn't this work when it is in UI.LoadUI only?
			DisplayPlayerInView();
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
				// if player crosses a door with trap set, they die
				if (Game.rooms[panelOnScreen].doors[doorCrossed].trap)
				{
					Game.rooms[panelOnScreen].doors[doorCrossed].trap = false;
					Trap.Activate(playerType, 2);
					return;
				}

				// update player's position
				UpdateImageInNewRoom(doorCrossed);

				// load new room
				Game.LoadRoomByDoor(doorCrossed, playerType);
			}
		}

		// places player to an explicitly defined position
		public void GoTo(Coordinates newPosition)
		{
			playerPosition.floorCoordinates = newPosition;
			UpdatePlayerImageCoordinates();
			UI.ChangePictureBoxLocation(playerImage, playerImageCoordinates);
		}

		// please dont
		public void Die()
		{
			bool previousState = alive;
			alive = false;
			disarm = -1;
			secondsLeft -= 15;
			DropItemToFurniture(Game.rooms[panelOnScreen].GetRandomEmptyFurniture());
			UI.ChangeImageInPictureBox(playerImage, deadImage);
			UI.FadeAway(playerImage);
			// after a while, player appears at the same place where he died
			UI.Wait(3000);
			UI.ChangeImageInPictureBox(playerImage, aliveImage);
			UI.ChangePictureBoxLocation(playerImage, playerImageCoordinates);
			UI.ChangePictureBoxVisibility(playerImage, true);
			alive = previousState;
			health = 10;
		}

		// stops player from doing anything
		public void StopDoingActions()
		{
			alive = false;
		}

		// TODO hit the other player
		public void Hit(Player opponent)
		{
			if (ArePlayersClose(this, opponent))
			{
				if (opponent.alive)
				{
					// remember which image player had previously
					string previousImage = items[4] ? suitcaseImage : aliveImage;
					UI.ChangeImageInPictureBox(playerImage, fightingImage);
					UI.Wait(100);
					UI.ChangeImageInPictureBox(playerImage, previousImage);
					opponent.health--;
					if (opponent.health == 0)
					{
						if (opponent.numberOfItems > 0)
						{
							PickUpItem(opponent.ItemInPosession());
							opponent.LoseAllItems();
						}
						opponent.Die();
					}
				}
			}
		}

		// pick up item in furniture; return what item is now in furniture (-1 for none)
		public int PickUpItem(int item)
		{
			// throw away disarm
			DropDisarm();
			
			// furniture contained suitcase -> player now has suitcase and everything in it
			if (item == 4)
			{
				if (ItemInPosession() != -1)
				{
					Suitcase.AddItem(ItemInPosession());
				}
				UI.ChangeImageInPictureBox(playerImage, suitcaseImage);
				items[4] = true;
				for (int i = 0; i < 4; ++i)
				{
					items[i] = Suitcase.contents[i];
					if (items[i])
					{
						Item.ShowOnTrapulator(i, playerType);
					}
				}
				// update number of items
				numberOfItems = Suitcase.numberOfItems;
				return -1;
			}
			// player has suitcase -> add the item to it
			else if (items[4])
			{
				Suitcase.AddItem(item);
				numberOfItems = Suitcase.numberOfItems;
				items[item] = true;
				Item.ShowOnTrapulator(item, playerType);
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
					Item.ShowOnTrapulator(item, playerType);
					Item.HideFromTrapulator(itemInPosession, playerType);
					return itemInPosession;
				}
				// player has no item -> simply pick up the one in the furniture
				else
				{
					items[item] = true;
					Item.ShowOnTrapulator(item, playerType);
					numberOfItems = 1;
					return itemInPosession;
				}
			}
		}

		// puts any item the player is holding inside a furniture
		public void DropItemToFurniture(int furniture)
		{
			int item = ItemInPosession();
			if (item == 4)
			{
				UI.ChangeImageInPictureBox(playerImage, aliveImage);
			}
			numberOfItems = 0;
			Game.rooms[panelOnScreen].furnitures[furniture].item = item;
			//Debug.WriteLine("Player dropped item " + item + " into furniture " + Convert.ToString(furniture));
			// set all items to false; if player only had one item, it will get dropped; if they had a suitcase, they will lose all items
			for (int i = 0; i < 4; ++i)
			{
				items[i] = false;
				Item.HideFromTrapulator(i, playerType);
			}
			items[4] = false;
			Debug.WriteLine("Item " + item + " has been dropped into " + furniture);
		}

		// drop all items
		public void LoseAllItems()
		{
			for (int i = 0; i < 5; ++i)
			{
				items[i] = false;
			}
		}

		// takes disarm
		public void PickUpDisarm(int furniture)
		{
			// throw away all items
			int randFurniture = Game.rooms[panelOnScreen].GetRandomEmptyFurniture();
			if (ItemInPosession() != -1)
			{
				DropItemToFurniture(randFurniture);
			}

			// take the disarm
			if (furniture == 6)
			{
				// umbrella in a coat rack
				disarm = 2;
				UI.ChangeImageInPictureBox(playerImage, umbrellaImage);
			}
			else if (furniture == 7)
			{
				// shield in a first aid kit
				disarm = 3;
				UI.ChangeImageInPictureBox(playerImage, shieldImage);
			}
		}

		// removes all disarms from player and sets his image to default
		public void DropDisarm()
		{
			disarm = -1;
			UI.ChangeImageInPictureBox(playerImage, aliveImage);
		}

		// changes where player should be drawn
		public void SwitchPanel()
		{
			panelOnScreen = panelOnScreen == 0 ? 1 : 0;
			DisplayPlayerInView();
		}

		// displays player image in given part of the screen
		public void DisplayPlayerInView()
		{
			UI.roomPanels[panelOnScreen].Controls.Add(playerImage);
		}

		// updates player's image after crossing door
		public void UpdateImageInNewRoom(int doorCrossed)
		{
			Coordinates newPosition = CalculatePositionAfterCrossingDoor(doorCrossed, playerPosition.floorCoordinates);
			playerPosition.floorCoordinates = newPosition;
			UpdatePlayerImageCoordinates();
			UI.ChangePictureBoxLocation(playerImage, playerImageCoordinates);
		}

		// removes player image from current panel
		void RemovePlayerFromView()
		{
			UI.roomPanels[panelOnScreen].Controls.Remove(playerImage);
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
				if (Game.rooms[panelOnScreen].doors[i] != null && Door.PositionInDoor(i, pos) && Game.rooms[panelOnScreen].doors[i].open)
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
			Coordinates newCoords = new Coordinates(curCoords.x, curCoords.y);      // set the position to the same as current, only update the wrong coordinate
			switch (door)
			{
				case 0: newCoords.x = 295 + curCoords.y; break; // puts player 1px away from the wall
				case 1: newCoords.y = 195; break;               // player appears at the bottom of the screen
				case 2: newCoords.x = 205 - curCoords.y; break; // 1px away from the wall
				case 3: newCoords.y = 101; break;               // directly in front of the top door
			}
			return newCoords;
		}

		// returns number of item that player already has
		public int ItemInPosession()
		{
			// first check if player has the suitcase
			if (items[4])
			{
				return 4;
			}
			// otherwise check other items
			for (int i = 0; i < 4; ++i)
			{
				if (items[i])
				{
					return i;
				}
			}
			// if player doesn' have any item, return -1
			return -1;
		}

		// returns true if players are in the same room and within 20pts of each other
		public static bool ArePlayersClose(Player player1, Player player2)
		{
			return (player1.playerPosition.floor == player2.playerPosition.floor) &&
				(player1.playerPosition.roomX == player2.playerPosition.roomX) &&
				(player1.playerPosition.roomY == player2.playerPosition.roomY) &&
				(Math.Abs(player1.playerPosition.floorCoordinates.x - player2.playerPosition.floorCoordinates.x) <= 20) &&
				(Math.Abs(player1.playerPosition.floorCoordinates.y - player2.playerPosition.floorCoordinates.y) <= 20);
		}
	}

	// keeps track of furniture, doors, traps and objects in the room
	public class Room
	{
		// basic features
		public char color;
		public int occupiedBy;		// indicates which player is currently in the room, -1 if none, 2 if both
		public Furniture[] furnitures = new Furniture[8];
		public Door[] doors = new Door[4];

		public List<int> furnituresPresent = new List<int> { };     // list of all pieces of furniture by number present in the room
		public List<int> doorsPresent = new List<int> { };

		// used for displaying on screen
		public List<ImageContainer> images = new List<ImageContainer> { };

		public Room(char color)
		{
			this.color = color;
			occupiedBy = -1;
			images.Add(new ImageContainer(RoomFilename(), new Point(0, 0), new Size(500, 200)));
		}

		// loads background, furniture and doors into image
		public void LoadRoom(TransparentPanel frame, int player)
		{
			AddPlayer(player);
			if (color == 'X')
			{
				occupiedBy = -1;
			}
			frame.images = images;
			frame.Invalidate();
		}

		// sets room to default state
		public void LeaveRoom(int player)
		{
			// sets player who is currently in the room
			if (occupiedBy == 2)
			{
				occupiedBy = (-1 + player) * -1;		// result will be opposite of player = the player who didn't leave stays
			}
			else
			{
				occupiedBy = -1;
			}
		}

		// changes the occupiedBy variable depending on which player(s) are currently in the room
		public void AddPlayer(int player)
		{
			if (occupiedBy != -1)
			{
				occupiedBy = 2;
			}
			else
			{
				occupiedBy = player;
			}
		}

		// returns the number of furniture next to which the player is standing, -1 if none
		public int FurnitureNearby(Coordinates playerPosition)
		{
			for (int i = 0; i < 8; ++i)
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
			// we don't want to add coat rack or first aid kit to the list
			if (type < 6)
			{
				furnituresPresent.Add(type);
			}

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
		public int GetRandomEmptyFurniture()
		{
			Random random = new Random();
			List<int> emptyFurnitures = new List<int> { };
			// don't drop into already occupied furniture
			foreach (int i in furnituresPresent)
			{
				if (furnitures[i].item == -1)
				{
					emptyFurnitures.Add(i);
				}
			}
			int index = random.Next(emptyFurnitures.Count);
			return furnituresPresent[index];
		}

		// starts countdown for time bomb and kills everyone who is in the room when it runs out
		public void ActivateTimeBomb()
		{
			UI.Wait(10000);
			if (occupiedBy != -1)
			{
				if (occupiedBy == 2)
				{
					Trap.Activate(0, 1);
					Trap.Activate(1, 1);
				}
				else
				{
					Trap.Activate(occupiedBy, 1);
				}
			}
		}

		// returns the filename of background image depending on the color of room
		string RoomFilename()
		{
			return "room" + color + ".png";
		}
	}

	// handles furniture behavior
	public class Furniture
	{
		// basic characteristics
		public int type;    // from left (0) to right (5): bookcase, table, small shelf, shelf, microwave, drawer; 6 - coat rack, 7 - first aid kit
		public int item;
		public bool trap = false;

		// visible parts of the furniture
		public ImageContainer image;
		public Size imageSize;
		public Coordinates imagePosition;

		// for displaying the picture
		string filename;

		public Furniture(int type, int item)
		{
			this.type = type;
			this.item = item;
			CalculateImageSize();
			CalculateImagePosition();
			SetFilename();
			image = new ImageContainer(filename, imagePosition.ToPoint(), imageSize);
		}

		// puts the furniture higher in the air
		public void Lift(int player)
		{
			if (player == 0) Debug.WriteLine("Item before lifting: " + item);
			image.location = new Point(imagePosition.x, imagePosition.y - 15);
			UI.UpdateObject(UI.roomPanels[Game.players[player].panelOnScreen], image, 15);

			// if furniture contained a trap, player dies and the trap deactivates
			if (trap)
			{
				Trap.Activate(player, 3);
				trap = false;
			}

			// if furniture is either coat rack or first aid kit, take the disarm
			if (type == 6 || type == 7)
			{
				Game.players[player].PickUpDisarm(type);
				return;
			}

			// if furniture contained an item, pick it up
			if (item != -1)
			{
				int newItem = Game.players[player].PickUpItem(item);
				item = newItem;
			}
			// otherwise put any object player is carrying inside that furniture
			else
			{
				// we can't put it into a coat rack or first aid kit
				if (type != 6 && type != 7)
				{
					Game.players[player].DropItemToFurniture(type);
				}
				else
				{
					Game.players[player].DropItemToFurniture(Game.rooms[Game.players[player].panelOnScreen].GetRandomEmptyFurniture());
				}
			}
			if (player == 0) Debug.WriteLine("Item after lifting: " + item);
		}

		// puts the furniture back in its original position
		public void Release(int player)
		{
			image.location = imagePosition.ToPoint();
			UI.UpdateObject(UI.roomPanels[Game.players[player].panelOnScreen], image, 15);
		}
		
		// returns whether position is close to a specific type of furniture
		public static bool PositionInRangeOfFurniture(int furnitureType, Coordinates position)
		{
			switch (furnitureType)
			{
				case 0: return position.x < Coordinates.CalculateFloorLimit(position.y, 40, 'l') && position.y < 150;	// bookcase
				case 1: return position.x > 130 && position.x < 250 && position.y > 100 && position.y < 120;			// desk
				case 2: return position.x > 150 && position.x < 200 && position.y > 100 && position.y < 120;			// small shelf
				case 3: return position.x > 250 && position.x < 320 && position.y > 100 && position.y < 120;			// shelf
				case 4: return position.x > 285 && position.x < 365 && position.y > 100 && position.y < 120;			// microwave
				case 5: return position.x > Coordinates.CalculateFloorLimit(position.y, 40, 'r') && position.y < 140;   // drawer
				case 6: return position.x > 150 && position.x < 200 && position.y > 100 && position.y < 120;			// coat rack
				case 7: return position.x > Coordinates.CalculateFloorLimit(position.y, 40, 'r') && position.y > 120 && position.y < 152;	// first aid kit
				default: return false;
			}
		}

		// return midpoint coordinates
		public static Coordinates GetCenterLocation(int furnitureType)
		{
			switch (furnitureType)
			{
				case 0: return new Coordinates(85, 115);
				case 1: return new Coordinates(190, 110);
				case 2: return new Coordinates(175, 110);
				case 3: return new Coordinates(285, 110);
				case 4: return new Coordinates(325, 110);
				case 5: return new Coordinates(410, 85);
				default: return new Coordinates(251, 151);
			}
		}

		// calculates position of the furniture on the screen
		void CalculateImagePosition()
		{
			switch (type)
			{
				case 0: imagePosition = new Coordinates(50, 30); break;		// bookcase
				case 1: imagePosition = new Coordinates(130, 50); break;	// desk
				case 2: imagePosition = new Coordinates(150, 60); break;	// small shelf
				case 3: imagePosition = new Coordinates(250, 60); break;	// shelf
				case 4: imagePosition = new Coordinates(285, 60); break;	// microwave
				case 5: imagePosition = new Coordinates(380, 30); break;    // drawer
				case 6: imagePosition = new Coordinates(150, 40); break;    // coat rack
				case 7: imagePosition = new Coordinates(420, 50); break;	// first aid kit
			}
		}

		// calculates size of image depending on type
		void CalculateImageSize()
		{
			switch (type)
			{
				case 0: imageSize = new Size(70, 120); break;
				case 1: imageSize = new Size(120, 60); break;
				case 2: imageSize = new Size(50, 50); break;
				case 3: imageSize = new Size(70, 50); break;
				case 4: imageSize = new Size(80, 50); break;
				case 5: imageSize = new Size(60, 110); break;
				case 6: imageSize = new Size(50, 70); break;
				case 7: imageSize = new Size(30, 48); break;
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
				case 2: filename = "smallshelf.png"; break;
				case 3: filename = "shelf.png"; break;
				case 4: filename = "microwave.png"; break;
				case 5: filename = "drawer.png"; break;
				case 6: filename = "coatrack.png"; break;
				case 7: filename = "firstaid.png"; break;
			}
		}
	}

	// handles door behavior
	public class Door
	{
		// basic characteristics
		public int location;	// possible values 0-3, in the middle of each wall
		public bool open;
		public Triplet leadsTo;
		public bool trap = false;

		// visible parts
		public ImageContainer image;
		public Size imageSize;
		public Coordinates imagePosition;

		string openFileName;
		string closedFileName;

		public Door(int location, Triplet leadsTo)
		{
			this.location = location;
			this.leadsTo = leadsTo;
			CalculateImageSize();
			CalculateImagePosition();
			SetFilename();
			image = new ImageContainer(closedFileName, imagePosition.ToPoint(), imageSize);
		}

		// closes the door if open and vice versa
		public int Switch(int player)
		{
			try
			{
				int oppositeDoor = GetCorrespondingDoor(location);
				Room adjacentRoom = Game.levelMap[leadsTo.x, leadsTo.y, leadsTo.z];
				if (open)
				{
					Close(player);
					adjacentRoom.doors[oppositeDoor].Close(player);
				}
				else
				{
					Open(player);
					adjacentRoom.doors[oppositeDoor].Open(player);
				}
				return 0;
			}
			catch
			{
				// this should only happen if the adjacent room is the airport
				Game.players[player].UpdateImageInNewRoom(location);
				Game.LoadAirport(player);
				return -1;
			}
		}

		// returns true if position is in front of a specific door
		public static bool PositionInRangeOfDoor(int location, Coordinates position)
		{
			switch (location)
			{
				case 0: return position.x <= Coordinates.CalculateFloorLimit(position.y, 20, 'l') && position.y >= 150 && position.y <= 180;	// left wall
				case 1: return position.x >= 220 && position.x <= 280 && position.y >= 100 && position.y <= 110;								// back wall
				case 2: return position.x >= Coordinates.CalculateFloorLimit(position.y, 20, 'r') && position.y >= 150 && position.y <= 180;	// right wall
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
		public static int GetCorrespondingDoor(int location)
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

		// returns the center of the bottom edge of the door
		public static Coordinates GetCenterLocation(int location)
		{
			switch (location)
			{
				case 0: return new Coordinates(35, 165);
				case 1: return new Coordinates(250, 100);
				case 2: return new Coordinates(465, 165);
				case 3: return new Coordinates(250, 195);
				default: return new Coordinates(251, 151);
			}
		}

		// switches the image to that of open door
		void Open(int player)
		{
			image.filename = openFileName;
			UI.UpdateObject(UI.roomPanels[Game.players[player].panelOnScreen], image, 0);
			open = true;
		}

		// switches the image to closed
		void Close(int player)
		{
			image.filename = closedFileName;
			UI.UpdateObject(UI.roomPanels[Game.players[player].panelOnScreen], image, 0);
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
		// TODO change this for computer as well
		//static TransparentPanel[] itemsUp = new TransparentPanel[4];

		static string placeholderImage = "placeholderItem.png";
		static Size imageSize = new Size(50, 50);

		// creates space for item images on screen
		public static void InitializeItems()
		{
			for (int i = 0; i < 2; ++i)
			{
				for (int j = 0; j < 4; ++j)
				{
					UI.sidePanels[i].images.Add(new ImageContainer(placeholderImage, CalculatePositionOnTrapulator(j).ToPoint(), imageSize));
				}
			}
		}

		// displays item's image on trapulator
		public static void ShowOnTrapulator(int item, int player)
		{
			UI.sidePanels[player].images[item].filename = GetFilename(item);
			UI.UpdateObject(UI.sidePanels[player], UI.sidePanels[player].images[item], 0);
		}

		// hides image from trapulator
		public static void HideFromTrapulator(int item, int player)
		{
			UI.sidePanels[player].images[item].filename = placeholderImage;
			UI.UpdateObject(UI.sidePanels[player], UI.sidePanels[player].images[item], 0);
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
			Coordinates coords = new Coordinates(0, 150);
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
		public static int numberOfItems;

		// initialize suitcase with only one item
		public Suitcase(int initialItem)
		{
			for (int i = 0; i < 4; ++i)
			{
				contents[i] = false;
			}
			contents[initialItem] = true;
			numberOfItems = 1;
		}

		// adds new item to suitcase
		public static void AddItem(int item)
		{
			contents[item] = true;
			numberOfItems++;
			Debug.WriteLine(item + " item added, suitcase now has items: " + numberOfItems);
		}
	}

	// setting and activating traps
	public class Trap
	{
		// sets trap depending on type

		public static void SetTrap(Room r)
		{
			r.ActivateTimeBomb();
		}

		public static void SetTrap(Furniture f)
		{
			f.trap = true;
		}

		public static void SetTrap(Door d)
		{
			d.trap = true;
		}
		
		// activate and kill player
		public static void Activate(int player, int trapType)
		{
			Debug.WriteLine("player has disarm " + Game.players[player].disarm);
			if (Game.players[player].disarm != trapType)
			{
				Game.players[player].Die();
			}
			else
			{
				Game.players[player].DropDisarm();
			}
		}
	}

	// keeps track of all aspects of position, i.e. floor, room and position in the room
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

	// handles actions of computer player
	public class ComputerAI
	{
		static Player computer;
		static Player opponent;

		static DoorsInRoom[,,] memoryDoors;
		static FurnitureAndDoorsInRoom[,,] memoryObjects;

		static List<Door> DoorToAirportStack = new List<Door> { };
		static List<Door> DoorToExploreStack = new List<Door> { };

		static bool hitLastTime = false;

		// initializes AI
		public static void Start()
		{
			computer = Game.players[1];
			opponent = Game.players[0];

			Timer timer = new Timer()
			{
				Interval = 150,
				Enabled = true
			};

			timer.Start();
			timer.Tick += (s, e) =>
			{
				CalculateNextMove();
			};
		}

		// data structure to keep track of visited objects in a room
		public static void CreateMemory(Triplet levelShape)
		{
			memoryDoors = new DoorsInRoom[levelShape.x, levelShape.y, levelShape.z];
			memoryObjects = new FurnitureAndDoorsInRoom[levelShape.x, levelShape.y, levelShape.z];
		}

		// adds all objects as unexplored
		public static void CopyObjectsIntoRoom(Triplet roomCoords)
		{
			// initialize memory cells
			memoryDoors[roomCoords.x, roomCoords.y, roomCoords.z] = new DoorsInRoom();
			memoryObjects[roomCoords.x, roomCoords.y, roomCoords.z] = new FurnitureAndDoorsInRoom();

			// add doors
			foreach (int element in Game.levelMap[roomCoords.x, roomCoords.y, roomCoords.z].doorsPresent)
			{
				memoryDoors[roomCoords.x, roomCoords.y, roomCoords.z].unvisitedDoors.Add(element);
				memoryObjects[roomCoords.x, roomCoords.y, roomCoords.z].unvisitedDoors.Add(element);
			}
			// add furniture
			foreach (int element in Game.levelMap[roomCoords.x, roomCoords.y, roomCoords.z].furnituresPresent)
			{
				memoryObjects[roomCoords.x, roomCoords.y, roomCoords.z].unexaminedFurniture.Add(element);
			}
		}

		// returns a random direction
		static char GetRandomDirection()
		{
			List<char> directions = new List<char> { 'R', 'L', 'U', 'D' };

			Random random = new Random();
			return directions[random.Next(directions.Count)];
		}

		// based on current state, decides what to do after timer ticks
		static void CalculateNextMove()
		{
			// only do things if alive
			if (computer.alive)
			{
				// if players are close to each other, fight because otherwise the other one will fight
				if (Player.ArePlayersClose(computer, opponent))
				{
					Fight();
				}
				// players are in the same room and the other player has a suitcase
				else if (Game.rooms[computer.panelOnScreen].occupiedBy == 2 && opponent.ItemInPosession() == 4)
				{
					GoToLocation(opponent.playerPosition.floorCoordinates);
				}
				// if player has collected all items, find your way to airport
				else if (computer.ItemInPosession() == 4 && Suitcase.numberOfItems == 4)
				{
					ExploreAllDoors(DoorToAirportStack);
				}
				// otherwise try to collect all items
				else
				{
					ExploreLevel();
				}
			}
		}

		// fight other player
		static void Fight()
		{
			// only hit every second tick to give human time
			if (!hitLastTime)
			{
				computer.Hit(opponent);
				hitLastTime = true;
			}
			else
			{
				hitLastTime = false;
			}
		}

		// perform a depth-first search to visit every door
		static void ExploreAllDoors(List<Door> doorStack)
		{
			// there are still some unvisited doors left
			if (memoryDoors[computer.playerPosition.floor, computer.playerPosition.roomX, computer.playerPosition.roomY].unvisitedDoors.Count != 0)
			{
				// if player is next to the first door in the unvisited list
				int firstDoor = memoryDoors[computer.playerPosition.floor, computer.playerPosition.roomX, computer.playerPosition.roomY].unvisitedDoors[0];
				if (Door.PositionInRangeOfDoor(firstDoor, computer.playerPosition.floorCoordinates))
				{
					// if door is currently open, cross it
					if (Game.rooms[computer.panelOnScreen].doors[firstDoor].open)
					{
						// save as visited
						memoryDoors[computer.playerPosition.floor, computer.playerPosition.roomX, computer.playerPosition.roomY].unvisitedDoors.Remove(firstDoor);
						memoryDoors[computer.playerPosition.floor, computer.playerPosition.roomX, computer.playerPosition.roomY].visitedDoors.Add(firstDoor);

						// save opposite door as visited
						int oppositeDoor = Door.GetCorrespondingDoor(firstDoor);
						Triplet leadsTo = Game.rooms[computer.panelOnScreen].doors[firstDoor].leadsTo;
						memoryDoors[leadsTo.x, leadsTo.y, leadsTo.z].unvisitedDoors.Remove(oppositeDoor);
						memoryDoors[leadsTo.x, leadsTo.y, leadsTo.z].visitedDoors.Add(oppositeDoor);

						// save opposite door to stack
						doorStack.Add(Game.levelMap[leadsTo.x, leadsTo.y, leadsTo.z].doors[oppositeDoor]);

						// load next room and update player's location
						Game.LoadRoomByDoor(firstDoor, 1);
						computer.UpdateImageInNewRoom(firstDoor);
					}
					// otherwise open them
					else
					{
						int res = Game.rooms[computer.panelOnScreen].doors[firstDoor].Switch(1);
						// if it's not possible to open door - most likely because there's an airport and player can't go there yet
						if (res == -1)
						{
							memoryDoors[computer.playerPosition.floor, computer.playerPosition.roomX, computer.playerPosition.roomY].unvisitedDoors.Remove(firstDoor);
							memoryDoors[computer.playerPosition.floor, computer.playerPosition.roomX, computer.playerPosition.roomY].visitedDoors.Add(firstDoor);
						}
					}
				}
				// otherwise go to the first door on list
				else
				{
					GoToLocation(Door.GetCenterLocation(firstDoor));
				}
			}
			// otherwise go back
			else
			{
				// try going back
				if (doorStack.Count > 0)
				{
					int lastDoorOnStack = doorStack[doorStack.Count - 1].location;
					// cross if you are close to it
					if (Door.PositionInRangeOfDoor(lastDoorOnStack, computer.playerPosition.floorCoordinates))
					{
						if (Game.rooms[computer.panelOnScreen].doors[lastDoorOnStack].open)
						{
							int oppositeDoor = Door.GetCorrespondingDoor(lastDoorOnStack);
							Triplet leadsTo = Game.rooms[computer.panelOnScreen].doors[lastDoorOnStack].leadsTo;
							doorStack.Remove(doorStack[doorStack.Count - 1]);

							Game.LoadRoomByDoor(lastDoorOnStack, 1);
							computer.UpdateImageInNewRoom(lastDoorOnStack);
						}
						else
						{
							Game.rooms[computer.panelOnScreen].doors[lastDoorOnStack].Switch(1);
						}
					}
					// or go to it
					else
					{
						GoToLocation(Door.GetCenterLocation(lastDoorOnStack));
					}
				}
				// or you've exhausted all possibilities and just wander around mindlessly
				else
				{
					computer.MovePlayer(GetRandomDirection());
				}
			}
		}

		// lift all furniture and progress through the level
		static void ExploreLevel()
		{
			// there are still furnitures player hasn't visited
			if (memoryObjects[computer.playerPosition.floor, computer.playerPosition.roomX, computer.playerPosition.roomY].unexaminedFurniture.Count != 0)
			{
				int firstFurniture = memoryObjects[computer.playerPosition.floor, computer.playerPosition.roomX, computer.playerPosition.roomY].unexaminedFurniture[0];
				// if player is close to the furniture, lift it
				if (Furniture.PositionInRangeOfFurniture(firstFurniture, computer.playerPosition.floorCoordinates))
				{
					int initialItems = computer.numberOfItems;
					Game.rooms[computer.panelOnScreen].furnitures[firstFurniture].Lift(1);
					int finalItems = computer.numberOfItems;

					// if player lost at least one item in this furniture, keep it as unvisited, otherwise mark as visited
					if (finalItems < initialItems)
					{
						UI.Wait(100);
					}
					else
					{
						memoryObjects[computer.playerPosition.floor, computer.playerPosition.roomX, computer.playerPosition.roomY].unexaminedFurniture.Remove(firstFurniture);
						memoryObjects[computer.playerPosition.floor, computer.playerPosition.roomX, computer.playerPosition.roomY].examinedFurniture.Add(firstFurniture);
						UI.Wait(500);
					}
					
					Game.rooms[computer.panelOnScreen].furnitures[firstFurniture].Release(1);
				}
				// otherwise go towards it
				else
				{
					GoToLocation(Furniture.GetCenterLocation(firstFurniture));
				}
			}
			// otherwise walk through a door
			else
			{
				ExploreAllDoors(DoorToExploreStack);
			}
		}

		// move player towards a given location
		static void GoToLocation(Coordinates coords)
		{
			Coordinates previousPosition = computer.playerPosition.floorCoordinates;

			Coordinates playerCoords = computer.playerPosition.floorCoordinates;

			// if the horizontal difference is larger than the vertical difference
			if (Math.Abs(playerCoords.x - coords.x) > Math.Abs(playerCoords.y - coords.y))
			{
				if (playerCoords.x < coords.x)
				{
					computer.MovePlayer('R');
				}
				else
				{
					computer.MovePlayer('L');
				}
			}
			else
			{
				if (playerCoords.y < coords.y)
				{
					computer.MovePlayer('D');
				}
				else
				{
					computer.MovePlayer('U');
				}
			}

			Coordinates currentPosition = computer.playerPosition.floorCoordinates;
			// if player didn't move (because of and unhandled case.. ugh), move randomly
			if (previousPosition == currentPosition)
			{
				computer.MovePlayer(GetRandomDirection());
			}
		}

		// doors in a room used when finding a way to the airport
		class DoorsInRoom
		{
			public List<int> visitedDoors = new List<int> { };
			public List<int> unvisitedDoors = new List<int> { };
		}

		// objects in a room used when exploring the floor
		class FurnitureAndDoorsInRoom
		{
			public List<int> examinedFurniture = new List<int> { };
			public List<int> unexaminedFurniture = new List<int> { };
			public List<int> visitedDoors = new List<int> { };
			public List<int> unvisitedDoors = new List<int> { };
		}
	}

	// UI functionality
	public class UI
	{
		// form where all the graphics is displayed
		public static Form1 parentForm;

		public static string baseImageAddress = "../../Assets/Images/";
		static string baseMapAddress = "../../Assets/LevelMaps/";

		// fundamental parts of the UI
		public static TransparentPanel[] roomPanels = new TransparentPanel[2];
		public static TransparentPanel[] sidePanels = new TransparentPanel[2];
		public static TextPanel[] countdowns = new TextPanel[2];
		public static TextPanel[] messages = new TextPanel[2];
		public static PictureBox help;

		// for use in methods
		static ImageContainer highlight = new ImageContainer("highlightedSidePanel.png", new Point(0, 0), new Size(200, 200));

		// some visuals
		static Font countdownFont = new Font("Calibri", 35);
		static SolidBrush countdownBrush = new SolidBrush(Color.Chartreuse);
		static Font messageFont = new Font("Calibri", 8);
		static SolidBrush messageBrush = new SolidBrush(Color.Black);

		// countdown
		public static Timer countdown;

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
		public static void Countdown(bool stop=false)
		{
			countdown = new Timer()
			{
				Interval = 1000,
				Enabled = true
			};
			countdown.Start();
			countdown.Tick += (s, e) =>
			{
				Game.players[0].secondsLeft--;
				Game.players[1].secondsLeft--;
				UpdateTimer();
				if (Game.players[0].secondsLeft <= 0)
				{
					countdown.Stop();
					// if both players timed out, no one wins
					if (Game.players[1].secondsLeft <= 0)
					{
						Game.Stop(2);
					}
					else
					{
						Game.Stop(0);
					}
					Game.players[0].Die();
				}
				if (Game.players[1].secondsLeft <= 0)
				{
					countdown.Stop();
					Game.Stop(1);
					Game.players[1].Die();
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

		// creates a new PictureBox in the specified position and returns the PictureBox instance
		public static PictureBox CreatePictureBox(string filename, Coordinates coords, Size size)
		{
			PictureBox pb = new PictureBox();
			pb.ImageLocation = baseImageAddress + filename;
			pb.Size = size;
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

		// changes the image in given panel
		public static void ChangeImageInPanel(TransparentPanel panel, string filename)
		{
			panel.ChangeImage(baseImageAddress + filename, panel.Size);
		}

		// changes the image displayed in a panel
		public static void ChangeImage(ImageContainer image, string filename)
		{
			image.filename = filename;
		}
		
		// changes the location of given panel
		public static void ChangePanelLocation(TransparentPanel panel, Coordinates coords)
		{
			panel.Location = new Point(coords.x, coords.y);
			RedrawRoom('u');
		}

		// updates image location
		public static void ChangeImageLocation(ImageContainer image, Point newPosition)
		{
			image.location = newPosition;
		}
		
		// redraws the area with furniture
		public static void UpdateObject(TransparentPanel panel, ImageContainer image, int margin)
		{
			Rectangle areaToUpdate = GetRectangleWithMargin(image.location, image.size, margin);
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

		// removes player from panel
		public static void RemovePlayerFromCurrentView(int player)
		{
			roomPanels[Game.players[player].panelOnScreen].Controls.Remove(Game.players[player].playerImage);
		}

		// incrementally puts image higher and higher until it eventually disappears
		public static void FadeAway(PictureBox pb)
		{
			for (int i = 0; i < 10; ++i)
			{
				pb.Location = new Point(pb.Location.X, pb.Location.Y - 10);
				Wait(200);
			}
			ChangePictureBoxVisibility(pb, false);
		}

		// loads map of the rooms from file, sets starting room for player and computer
		public static void LoadLevel(int level, out Triplet humanRoom, out Triplet computerRoom)
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
			ComputerAI.CreateMemory(new Triplet(floors, rows, cols));

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

						ComputerAI.CopyObjectsIntoRoom(new Triplet(f, r, c));
					}
			string[] humanRoomSplit = sr.ReadLine().Split(',');
			string[] computerRoomSplit = sr.ReadLine().Split(',');
			humanRoom = new Triplet(Convert.ToInt32(humanRoomSplit[0]), Convert.ToInt32(humanRoomSplit[1]), Convert.ToInt32(humanRoomSplit[2]));
			computerRoom = new Triplet(Convert.ToInt32(computerRoomSplit[0]), Convert.ToInt32(computerRoomSplit[1]), Convert.ToInt32(computerRoomSplit[2]));
		}

		// updates text on timer
		static void UpdateTimer()
		{
			int minutesHuman = Game.players[0].secondsLeft / 60;
			int secondsHuman = Game.players[0].secondsLeft % 60;
			string timeTextHuman = Convert.ToString(minutesHuman) + ":" + Convert.ToString(secondsHuman);
			countdowns[0].UpdateText(timeTextHuman, countdownFont, countdownBrush);

			int minutesComputer = Game.players[1].secondsLeft / 60;
			int secondsComputer = Game.players[1].secondsLeft % 60;
			string timeTextComputer = Convert.ToString(minutesComputer) + ":" + Convert.ToString(secondsComputer);
			countdowns[1].UpdateText(timeTextComputer, countdownFont, countdownBrush);
		}

		// loads main parts of the UI
		public static void LoadUI(Form1 form)
		{
			parentForm = form;
			help = CreatePictureBox("help.png", new Coordinates(0, 0), new Size(800, 500));
			parentForm.Controls.Add(help);

			for (int i = 0; i < 2; ++i)
			{
				roomPanels[i] = new TransparentPanel();
				roomPanels[i].Location = new Point(20, 220 * i + 20);
				roomPanels[i].Size = new Size(500, 200);
				parentForm.Controls.Add(roomPanels[i]);
				roomPanels[i].BackColor = Color.Transparent;

				sidePanels[i] = new TransparentPanel();
				sidePanels[i].Location = new Point(540, 220 * i + 20);
				sidePanels[i].Size = new Size(200, 200);
				parentForm.Controls.Add(sidePanels[i]);
			
				countdowns[i] = new TextPanel();
				countdowns[i].Location = new Point(0, 60);
				countdowns[i].Size = new Size(200, 50);
				sidePanels[i].Controls.Add(countdowns[i]);

				messages[i] = new TextPanel();
				messages[i].Location = new Point(0, 110);
				messages[i].Size = new Size(200, 40);
				sidePanels[i].Controls.Add(messages[i]);

			}

			// we must initialize items before traps because code in Item.ShowOnTrapulator() relies on the item index
			Item.InitializeItems();
			DisplayTraps();
			// at first, hide all images
			ToggleScreen(false);
		}

		// removes players
		public static void ClearLevel()
		{
			Game.players[0].playerImage.Dispose();
			Game.players[1].playerImage.Dispose();
		}

		// removes or adds all images to screen
		public static void ToggleScreen(bool visible)
		{
			for (int i = 0; i < 2; ++i)
			{
				roomPanels[i].Visible = visible;
				sidePanels[i].Visible = visible;
			}
		}

		// displays or hides help
		public static void ToggleHelp()
		{
			if (help.Visible)
			{
				help.Visible = false;
				ToggleScreen(true);
			}
			else
			{
				help.Visible = true;
				ToggleScreen(false);
			}
		}

		// displays message to player
		public static void ShowMessage(string text, int player)
		{
			messages[player].UpdateText(text, messageFont, messageBrush);
		}

		// displays message about who won over the whole form
		public static void DisplayWinnerMessage(int winner)
		{
			string filename = "winnerMessage" + winner.ToString() + ".png";
			PictureBox pb = CreatePictureBox(filename, new Coordinates(0, 0), new Size(800, 500));
			parentForm.Controls.Add(pb);
		}

		// removes message from player's inbox
		public static void RemoveMessage(int player)
		{
			ShowMessage("", player);
		}

		// this causes buffer - find a better way?
		public static void RedrawRoom(char frame)
		{
			TransparentPanel roomView;
			if (frame == 'u')
				roomView = roomPanels[0];
			else
				roomView = roomPanels[1];
			
			roomView.Invalidate(GetRectangleWithMargin(Game.players[0].playerImage.Location, Game.players[0].playerImage.Size, 5));
			Game.players[0].playerImage.Invalidate();
		}

		// highlights trapulator
		public static void HighlightPanel(int panel)
		{
			sidePanels[panel].images.Add(highlight);
			sidePanels[panel].Invalidate();
		}

		// removes highlight from panel
		public static void UnhighlightPanel(int panel)
		{
			sidePanels[panel].images.Remove(highlight);
			sidePanels[panel].Invalidate();
		}

		// returns Rectangle with a given margin around a given area
		static Rectangle GetRectangleWithMargin(Point startingPoint, Size startingSize, int margin)
		{
			Point newPoint = new Point(startingPoint.X - margin, startingPoint.Y - margin);
			Size newSize = new Size(startingSize.Width + 2 * margin, startingSize.Height + 2 * margin);
			return new Rectangle(newPoint, newSize);
		}
		
		// draws traps on both trapulators
		static void DisplayTraps()
		{
			string[] filenames = { "timebomb.png", "waterbucket.png", "bomb.png", "floorplan.png" };

			for (int i = 0; i < 2; ++i)
			{
				for (int j = 0; j < 4; ++j)
				{
					ImageContainer trap = new ImageContainer(filenames[j], new Point(j * 50, 0), new Size(50, 50));
					sidePanels[i].images.Add(trap);
				}
			}
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
		public Font font = new Font("Calibri", 35);
		public SolidBrush brush = new SolidBrush(Color.Chartreuse);
		public string text = "";

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.DrawString(text, font, brush, new Point(5, 5));
			base.OnPaint(e);
		}

		public void UpdateText(string text, Font font, SolidBrush brush)
		{
			Invalidate();
			this.font = font;
			this.brush = brush;
			this.text = text;
		}
	}

	// keeps track of file, location and size of image to be drawn
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
				case Keys.Z: Game.EventOnKeyPress('Z'); break;
				case Keys.D1: Game.EventOnKeyPress('1'); break;
				case Keys.D2: Game.EventOnKeyPress('2'); break;
				case Keys.D3: Game.EventOnKeyPress('3'); break;
				case Keys.D4: Game.EventOnKeyPress('4'); break;
				case Keys.Space: Game.EventOnKeyPress(' '); break;
				case Keys.H: Game.EventOnKeyPress('H'); break;
				case Keys.Enter:
					if (Game.stopped)
					{
						Application.Exit();
					}
					break;
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}

		public Form1()
		{
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			this.BackColor = Color.White;
			this.Text = "SpyVsSpy";
			this.Size = new Size(800, 500);
			Game.InitializeGame(this);
		}
	}
}
