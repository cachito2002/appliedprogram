// Program.cs
// StoryQuest - Text Adventure Game
// A small C# console text-adventure demonstrating classes, inheritance, file I/O, and game logic.
// Build with: dotnet build / dotnet run
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StoryQuest
{
    // Abstract base for characters in the game (Player, Enemy, NPC)
    public abstract class Character
    {
        public string Name { get; set; }
        public int MaxHealth { get; set; }
        public int Health { get; set; }

        protected Character(string name, int maxHealth)
        {
            Name = name;
            MaxHealth = maxHealth;
            Health = maxHealth;
        }

        public bool IsAlive => Health > 0;

        public virtual void TakeDamage(int amount)
        {
            Health -= amount;
            if (Health < 0) Health = 0;
        }

        public virtual void Heal(int amount)
        {
            Health += amount;
            if (Health > MaxHealth) Health = MaxHealth;
        }
    }

    // Player class with inventory and methods
    public class Player : Character
    {
        public List<Item> Inventory { get; } = new List<Item>();
        public Room CurrentRoom { get; set; }

        public Player(string name, int maxHealth) : base(name, maxHealth) { }

        public void AddItem(Item item)
        {
            Inventory.Add(item);
            Console.WriteLine($"You picked up: {item.Name}");
        }

        public bool RemoveItem(string itemName)
        {
            var it = Inventory.Find(i => i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));
            if (it != null) { Inventory.Remove(it); return true; }
            return false;
        }

        public Item GetItem(string itemName)
        {
            return Inventory.Find(i => i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));
        }

        public void ShowInventory()
        {
            if (Inventory.Count == 0) Console.WriteLine("Inventory: (empty)");
            else
            {
                Console.WriteLine("Inventory:");
                foreach (var i in Inventory) Console.WriteLine($" - {i.Name}: {i.Description}");
            }
        }
    }

    // Enemy inherits from Character (demonstrates inheritance & override)
    public class Enemy : Character
    {
        public int AttackPower { get; set; }

        public Enemy(string name, int maxHealth, int attackPower) : base(name, maxHealth)
        {
            AttackPower = attackPower;
        }

        public virtual void Attack(Character target)
        {
            Console.WriteLine($"{Name} attacks {target.Name} for {AttackPower} damage!");
            target.TakeDamage(AttackPower);
        }
    }

    // Simple non-hostile NPC
    public class NPC
    {
        public string Name { get; set; }
        public string Dialogue { get; set; }

        public NPC(string name, string dialogue)
        {
            Name = name;
            Dialogue = dialogue;
        }

        public void Talk()
        {
            Console.WriteLine($"{Name} says: \"{Dialogue}\"");
        }
    }

    // Item class
    public class Item
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsUsable { get; set; } = false;

        public Item(string name, string desc, bool usable = false)
        {
            Name = name;
            Description = desc;
            IsUsable = usable;
        }

        public virtual void Use(Player player)
        {
            // Default has no effect
            Console.WriteLine($"You try to use {Name}, but nothing happens.");
        }
    }

    // A consumable HealthPotion item that overrides Use
    public class HealthPotion : Item
    {
        public int HealAmount { get; set; }
        public HealthPotion(string name, string desc, int healAmount) : base(name, desc, true)
        {
            HealAmount = healAmount;
        }

        public override void Use(Player player)
        {
            player.Heal(HealAmount);
            Console.WriteLine($"You used {Name} and recovered {HealAmount} health. (Now: {player.Health}/{player.MaxHealth})");
            player.RemoveItem(Name); // remove one potion
        }
    }

    // Room class with connections and contents
    public class Room
    {
        public string Id { get; set; } // unique id for save/load
        public string Name { get; set; }
        public string Description { get; set; }
        public Dictionary<string, string> Exits { get; } = new Dictionary<string, string>(); // direction -> roomId
        public List<Item> Items { get; } = new List<Item>();
        public NPC Resident { get; set; }
        public Enemy Hostile { get; set; }

        public Room(string id, string name, string desc)
        {
            Id = id;
            Name = name;
            Description = desc;
        }

        public void Show()
        {
            Console.WriteLine($"\n== {Name} ==");
            Console.WriteLine(Description);
            if (Items.Count > 0)
            {
                Console.WriteLine("You see:");
                foreach (var it in Items) Console.WriteLine($" - {it.Name}: {it.Description}");
            }
            if (Resident != null) Console.WriteLine($"Here: {Resident.Name}");
            if (Hostile != null && Hostile.IsAlive) Console.WriteLine($"Danger: {Hostile.Name} (Hostile)");
            if (Exits.Count > 0)
            {
                Console.WriteLine("Exits:");
                foreach (var kv in Exits) Console.WriteLine($" - {kv.Key}");
            }
        }
    }

    // Simple structure representing the saved game state
    public class SaveData
    {
        public string PlayerName { get; set; }
        public int PlayerHealth { get; set; }
        public string CurrentRoomId { get; set; }
        public List<string> Inventory { get; set; } = new List<string>();
        // For simplicity we don't serialize entire objects; rebuild on load
    }

    // Game engine
    public class Game
    {
        private Dictionary<string, Room> _rooms = new Dictionary<string, Room>();
        private Player _player;
        private bool _isRunning = false;
        private const string SAVE_FILE = "savegame.json";

        public Game() { SetupWorld(); }

        // Build rooms, items, NPCs, and place player
        private void SetupWorld()
        {
            // Create rooms
            var courtyard = new Room("courtyard", "Castle Courtyard",
                "A cold stone courtyard with creeping fog. Torches flicker in the wind.");
            var hall = new Room("hall", "Great Hall",
                "A grand hall with long tables. A dusty banner hangs on the wall.");
            var armory = new Room("armory", "Old Armory",
                "Racks of rusted weapons and a locked chest in the corner.");
            var tower = new Room("tower", "Wizard's Tower",
                "A spiral staircase winds upward. Magical sigils glow on the walls.");

            // Exits
            courtyard.Exits["north"] = "hall";
            hall.Exits["south"] = "courtyard";
            hall.Exits["east"] = "armory";
            hall.Exits["up"] = "tower";
            armory.Exits["west"] = "hall";
            tower.Exits["down"] = "hall";

            // Items
            courtyard.Items.Add(new Item("coin", "A tarnished gold coin."));
            armory.Items.Add(new Item("sword", "A short sword. It looks usable."));
            armory.Items.Add(new HealthPotion("small potion", "A small red bottle. Restores 20 HP.", 20));
            hall.Items.Add(new Item("map", "A map of the castle. Helpful to not get lost."));
            tower.Items.Add(new Item("spellbook", "A leather-bound book filled with arcane notes."));

            // NPC
            hall.Resident = new NPC("Old Butler", "Welcome traveler. Beware the tower at night.");

            // Enemy
            courtyard.Hostile = new Enemy("Goblin Scout", 15, 4);
            armory.Hostile = new Enemy("Armory Warden", 30, 7);

            // Register rooms
            _rooms[courtyard.Id] = courtyard;
            _rooms[hall.Id] = hall;
            _rooms[armory.Id] = armory;
            _rooms[tower.Id] = tower;

            // Create player
            _player = new Player("Adventurer", 50);
            _player.CurrentRoom = courtyard;
            // Give player a starter potion
            _player.Inventory.Add(new HealthPotion("starter potion", "A tiny potion to help you begin.", 10));
        }

        // Main loop
        public void Run()
        {
            _isRunning = true;
            Console.Clear();
            Console.WriteLine("Welcome to StoryQuest: A C# Text Adventure!");
            Console.WriteLine("Type 'help' for a list of commands.\n");

            while (_isRunning)
            {
                DescribeCurrentRoom();
                if (!_player.IsAlive)
                {
                    Console.WriteLine("You have perished. Game over.");
                    _isRunning = false;
                    break;
                }

                Console.Write("\n> ");
                var input = Console.ReadLine();
                if (input == null) continue;
                HandleInput(input.Trim());
            }

            Console.WriteLine("Thanks for playing StoryQuest.");
        }

        private void DescribeCurrentRoom()
        {
            _player.CurrentRoom.Show();
            Console.WriteLine($"Player: {_player.Name} HP: {_player.Health}/{_player.MaxHealth}");
        }

        // Basic command parser
        private void HandleInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return;
            var tokens = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var cmd = tokens[0].ToLower();
            var arg = tokens.Length > 1 ? tokens[1] : "";

            switch (cmd)
            {
                case "help": ShowHelp(); break;
                case "go": Move(arg); break;
                case "look": _player.CurrentRoom.Show(); break;
                case "take": TakeItem(arg); break;
                case "inventory": _player.ShowInventory(); break;
                case "use": UseItem(arg); break;
                case "talk": TalkTo(arg); break;
                case "attack": Attack(arg); break;
                case "save": SaveGame(); break;
                case "load": LoadGame(); break;
                case "quit":
                case "exit": _isRunning = false; break;
                default:
                    Console.WriteLine("Unknown command. Type 'help' for commands.");
                    break;
            }
        }

        private void ShowHelp()
        {
            Console.WriteLine("\nCommands:");
            Console.WriteLine(" - help : Show this help.");
            Console.WriteLine(" - look : Examine the current room.");
            Console.WriteLine(" - go <direction> : Move to another room (e.g., go north).");
            Console.WriteLine(" - take <item> : Pick up an item (e.g., take sword).");
            Console.WriteLine(" - inventory : Show your inventory.");
            Console.WriteLine(" - use <item> : Use an item from your inventory (e.g., use small potion).");
            Console.WriteLine(" - talk <name> : Talk to an NPC in the room.");
            Console.WriteLine(" - attack <target> : Attack a hostile in the room.");
            Console.WriteLine(" - save : Save your current game.");
            Console.WriteLine(" - load : Load the saved game (if available).");
            Console.WriteLine(" - quit/exit : Quit the game.");
        }

        private void Move(string direction)
        {
            if (string.IsNullOrWhiteSpace(direction))
            {
                Console.WriteLine("Go where? Specify a direction.");
                return;
            }

            var cur = _player.CurrentRoom;
            if (cur.Exits.TryGetValue(direction.ToLower(), out string destId))
            {
                _player.CurrentRoom = _rooms[destId];
                Console.WriteLine($"You move {direction} to {_player.CurrentRoom.Name}.");
                // Possibly trigger hostile encounter
                if (_player.CurrentRoom.Hostile != null && _player.CurrentRoom.Hostile.IsAlive)
                {
                    Console.WriteLine($"A hostile {_player.CurrentRoom.Hostile.Name} notices you!");
                    // Simple enemy attack on entry (demonstration)
                    _player.CurrentRoom.Hostile.Attack(_player);
                }
            }
            else Console.WriteLine("You can't go that way.");
        }

        private void TakeItem(string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName))
            {
                Console.WriteLine("Take what?");
                return;
            }

            var room = _player.CurrentRoom;
            var it = room.Items.Find(i => i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));
            if (it != null)
            {
                _player.AddItem(it);
                room.Items.Remove(it);
            }
            else Console.WriteLine($"No {itemName} here.");
        }

        private void UseItem(string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName))
            {
                Console.WriteLine("Use what?");
                return;
            }

            var item = _player.GetItem(itemName);
            if (item == null) { Console.WriteLine("You don't have that item."); return; }
            if (!item.IsUsable) { Console.WriteLine($"The {item.Name} can't be used."); return; }

            // Many items may have a custom Use; we call it
            item.Use(_player);
        }

        private void TalkTo(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { Console.WriteLine("Talk to whom?"); return; }
            var room = _player.CurrentRoom;
            if (room.Resident != null && room.Resident.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                room.Resident.Talk();
            }
            else Console.WriteLine($"No one named {name} here to talk to.");
        }

        private void Attack(string targetName)
        {
            if (string.IsNullOrWhiteSpace(targetName)) { Console.WriteLine("Attack what?"); return; }
            var enemy = _player.CurrentRoom.Hostile;
            if (enemy == null || !enemy.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase) || !enemy.IsAlive)
            {
                Console.WriteLine($"No hostile {targetName} to attack here.");
                return;
            }

            // Basic attack resolution (player uses sword if they have one)
            int playerAttack = _player.GetItem("sword") != null ? 10 : 3;
            Console.WriteLine($"You attack {enemy.Name} for {playerAttack} damage!");
            enemy.TakeDamage(playerAttack);

            if (!enemy.IsAlive)
            {
                Console.WriteLine($"You defeated {enemy.Name}!");
                // maybe drop a coin or item
                _player.CurrentRoom.Items.Add(new Item("trophy", $"A remnant of {enemy.Name}."));
            }
            else
            {
                // enemy retaliates
                enemy.Attack(_player);
                if (!_player.IsAlive) Console.WriteLine("You were slain by the enemy's attack.");
            }
        }

        // Save minimal game state to file (demonstrates file I/O)
        private void SaveGame()
        {
            var save = new SaveData
            {
                PlayerName = _player.Name,
                PlayerHealth = _player.Health,
                CurrentRoomId = _player.CurrentRoom.Id,
                Inventory = new List<string>()
            };
            foreach (var it in _player.Inventory) save.Inventory.Add(it.Name);

            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(save, options);
                File.WriteAllText(SAVE_FILE, json);
                Console.WriteLine($"Game saved to {SAVE_FILE}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save game: {ex.Message}");
            }
        }

        // Load game and reconstruct state. Note: we only saved primitive values, so rebuild objects.
        private void LoadGame()
        {
            if (!File.Exists(SAVE_FILE)) { Console.WriteLine("No saved game found."); return; }
            try
            {
                string json = File.ReadAllText(SAVE_FILE);
                var save = JsonSerializer.Deserialize<SaveData>(json);
                if (save == null) { Console.WriteLine("Save file corrupted."); return; }

                // Restore player health and position
                _player.Name = save.PlayerName;
                _player.Health = save.PlayerHealth;
                if (_rooms.TryGetValue(save.CurrentRoomId, out var room))
                {
                    _player.CurrentRoom = room;
                }

                // Rebuild inventory by matching names to items present in world or default items
                _player.Inventory.Clear();
                foreach (var name in save.Inventory)
                {
                    // Search in rooms for an item of that name (could be improved)
                    Item found = null;
                    foreach (var r in _rooms.Values)
                    {
                        found = r.Items.Find(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                        if (found != null) break;
                    }

                    // If not found in rooms, create by known name mapping
                    if (found == null)
                    {
                        if (name.ToLower().Contains("potion")) found = new HealthPotion(name, "Restores health.", 20);
                        else if (name.ToLower() == "sword") found = new Item("sword", "A short sword.");
                        else found = new Item(name, "Recovered item.");
                    }
                    _player.Inventory.Add(found);
                }

                Console.WriteLine("Game loaded.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load game: {ex.Message}");
            }
        }
    }

    // Entrypoint
    class Program
    {
        static void Main(string[] args)
        {
            var game = new Game();

            // Optionally allow quick load argument
            if (args.Length > 0 && args[0] == "--load")
            {
                Console.WriteLine("Attempting to auto-load saved game...");
                // Game exposes load via command; we simulate by saving and loading from within Game
                // For simplicity, start the loop (player can type 'load')
            }

            game.Run();
        }
    }
}
