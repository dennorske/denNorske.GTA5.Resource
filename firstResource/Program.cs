using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTANetwork;
using MySql.Data.MySqlClient;
using GTANetworkShared;
using GTANetworkServer;


namespace denNorske.gta5.gamemode
{
    class DBConnect
    {
        private MySqlConnection connection;
        private string server;
        private string database;
        private string uid;
        private string password;

        //Constructor
        public DBConnect()
        {
            Initialize();
        }

        //Initialize values
        private void Initialize()
        {
            server = "localhost";
            database = "dennorske.gta5";
            uid = "dennorske.gta5";
            password = "simenl13";
            string connectionString;
            connectionString = "SERVER=" + server + ";" + "DATABASE=" +
            database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";

            connection = new MySqlConnection(connectionString);
        }

        //open connection to database
        private bool OpenConnection()
        {
            try
            {
                connection.Open();
                return true;
            }
            catch (MySqlException ex)
            {
                //When handling errors, you can your application's response based 
                //on the error number.
                //The two most common error numbers when connecting are as follows:
                //0: Cannot connect to server.
                //1045: Invalid user name and/or password.
                switch (ex.Number)
                {
                    case 0:
                        API.shared.consoleOutput("Cannot connect to server.  Contact administrator");
                        break;

                    case 1045:
                        API.shared.consoleOutput("Invalid username/password, please try again");
                        break;
                }
                return false;
            }
        }

        //Close connection
        private bool CloseConnection()
        {
            try
            {
                connection.Close();
                return true;
            }
            catch (MySqlException ex)
            {
                API.shared.consoleOutput(ex.Message);
                return false;
            }
        }
        
        //Backup
        public void Backup()
        {
        }

        //Restore
        public void Restore()
        {
        }
    }

    public class Freeroam : Script
    {
        

        public static List<CarInfo> Cars = new List<CarInfo>(); //to save car objects
        public static List<Player> Players = new List<Player>(); // holds all the player objects
       

        #region Commands
        [Command(GreedyArg = true)]
        public void v(Client player, string carname)
        {
            VehicleHash vHash = API.vehicleNameToModel(carname);
            var pos = API.getEntityPosition(player);
            // var spawnPos = pos; // Car Spawnpoint
            var heading = API.getEntityRotation(player);

            var car = API.createVehicle(vHash, pos, new Vector3(0, 0, heading.Z), 0, 0);
            API.setEntityData(car, "RESPAWNABLE", false);
            API.setEntityData(car, "SPAWN_POS", pos);
            API.setEntityData(car, "SPAWN_ROT", heading);
            API.sendChatMessageToPlayer(player, "Server", "~w~A ~r~" + carname + "~w~ has been spawned for you. Enjoy!");
            API.setPlayerIntoVehicle(player, car, 0);
            CarInfo car2 = new CarInfo(car, player); //make a new object out of it
            Cars.Add(car2); //add to object list 
            foreach(Player pl in Players)
            {
                if(pl.player == player)
                {
                    pl.carHandle = car;
                }
            }




        }

        [Command(GreedyArg = true, SensitiveInfo = true)]
        public void register(Client player, string password)
        {
            
        }
        #endregion

        #region other publics

        public Freeroam()
        {
            API.onResourceStart += resourcesStart;
            API.onPlayerConnected += onPlayerConnected;
            API.onVehicleDeath += onVehicleDeath;
            //API.onVehicleExplode += onVehicleExplode;

        }

        public void onPlayerConnected(Client sender)
        {

            Players.Add(new Player(sender, sender.handle));
            API.setPlayerSkin(sender, API.pedNameToModel("Indian01AMY"));
            API.sendChatMessageToAll(sender.name + " has connected to the server!");

        }
        private void resourcesStart()
        {
            API.consoleOutput("Resorce has been loaded, and script from denNorske.gta5.freeroam has been loaded.");
        }


        public void onVehicleDeath(NetHandle handle)
        {
            if (API.getEntityData(handle, "RESPAWNABLE") == true)
            {
                API.delay(7000, true, () =>
                {
                    var color1 = API.getVehiclePrimaryColor(handle);
                    var color2 = API.getVehicleSecondaryColor(handle);
                    var model = API.getEntityModel(handle);

                    var spawnPos = API.getEntityData(handle, "SPAWN_POS");
                    var spawnH = API.getEntityData(handle, "SPAWN_ROT");

                    API.deleteEntity(handle);

                    API.createVehicle((VehicleHash)model, spawnPos, new Vector3(0, 0, spawnH), color1, color2);

                    API.setEntityData(handle, "SPAWN_POS", spawnPos);
                    API.setEntityData(handle, "SPAWN_ROT", spawnH);
                    API.setEntityData(handle, "RESPAWNABLE", true);
                });
            }
            else
            {

                foreach (CarInfo car in Cars)
                {
                    if (car.handle == handle)
                    {
                        Cars.Remove(car);
                        API.deleteEntity(car.handle);//delete the chassis
                    }
                }
            }   
                


                // ...
            

        }
      /*  public void onVehicleExplode(NetHandle vehicle)
        {
            string name;
            if (Player.VehDic.TryGetValue(vehicle, out name) == true)
            { 
                foreach(CarInfo i in Cars)
                {
                    if (i.handle == vehicle)
                    {
                        API.sendChatMessageToPlayer(i.player, "Mechanic", "~r~Your " + API.getEntityModel(i.handle) + " just exploded.");
                        Player.VehDic.Remove(i.handle); //remove the vehicle entry from the list
                        //Cars.Remove(i); //already called when dead above
                    }
                }
                
            }

        }
        
        {

        }*/
        #endregion
    }

    public class Player
    {
        public NetHandle playerHandle;
        public Client player;
        public NetHandle carHandle;
        public string playerName;
        public static Dictionary<NetHandle, string> VehDic = new Dictionary<NetHandle, string>();

        public Player(Client player, NetHandle playerHandle)
        {
            this.player = player;
            this.playerHandle = playerHandle;
            this.playerName = API.shared.getPlayerName(player);
        }
    }

    public class CarInfo
    {
        public NetHandle handle;
        public Client player;

        public CarInfo(NetHandle handle, Client player)
        {
            this.player = player;
            this.handle = handle;

        }
    }
}