using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTANetwork;
using System.Diagnostics;
using MySql.Data.MySqlClient;
using GTANetworkShared;
using GTANetworkServer;


namespace denNorske.gta5.gamemode
{
    public class Database
    {
        public MySqlConnection connection;
        private string server;
        private string database;
        private string uid;
        private string password;

        //Constructor
        public Database()
        {
            connection = new MySqlConnection(GetMysqlConnectionString());
        }

        public string GetMysqlConnectionString()
        {
            string conStr = "";
            try
            {
                using (StreamReader rdr = new StreamReader("connectionstring.txt"))
                {
                    conStr = rdr.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                API.shared.consoleOutput("Failed to load MySql connectionstring file: " + ex.Message);
            }
            return conStr;
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
                    default:
                        API.shared.consoleOutput("Other error: " + ex.Number);
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
            try
            {
                DateTime Time = DateTime.Now;
                int year = Time.Year;
                int month = Time.Month;
                int day = Time.Day;
                int hour = Time.Hour;
                int minute = Time.Minute;
                int second = Time.Second;

                //Save file to C:\ with the current date as a filename
                string path;
                path = "C:\\MySqlBackup" + year + "-" + month + "-" + day +
            "-" + hour + "-" + minute + "-" + second + "-" + ".sql";
                StreamWriter file = new StreamWriter(path);


                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "mysqldump";
                psi.RedirectStandardInput = false;
                psi.RedirectStandardOutput = true;
                psi.Arguments = string.Format(@"-u{0} -p{1} -h{2} {3}",
                    uid, password, server, database);
                psi.UseShellExecute = false;

                Process process = Process.Start(psi);

                string output;
                output = process.StandardOutput.ReadToEnd();
                file.WriteLine(output);
                process.WaitForExit();
                file.Close();
                process.Close();
            }
            catch (IOException ex)
            {
                API.shared.consoleOutput("Error , unable to backup!: " +ex);
            }
        }

        public int  CreateAccount(string username, string password)
        {
            string myQue = "INSERT INTO users (Name, Password) values (@name, @password);";

            try
            {
                if (this.OpenConnection() == true)
                {
                    MySqlCommand cmd = new MySqlCommand(myQue, connection);
                    cmd.Parameters.AddWithValue("@name", username);
                    cmd.Parameters.AddWithValue("@password", password);
                    cmd.ExecuteNonQuery();
                    this.CloseConnection();
                    return GetUserID(username);
                }

            }
            catch (Exception ex)
            {
                API.shared.consoleOutput("Error creating new user account: " + ex.Message);

            }
            
            return 0;
        }
       
        public int GetUserID(string username)
        {
            try
            {
                
                string myQue = "Select Userid from users where username = @username";
                if (this.OpenConnection() == true)
                {
                    MySqlCommand cmd = new MySqlCommand(myQue, connection);
                    cmd.Parameters.AddWithValue("@username", username);
                    using (MySqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            while (rdr.Read())
                            {
                                return (int)rdr["Userid"];
                            }
                        }
                    }
                }
            }
            
            catch (Exception ex)
            {
                API.shared.consoleOutput("Error getting user ID: " + ex.Message);
            }
            return 0;
        }
            
    }

    public class Freeroam : Script
    {

        #region vars
        public static List<CarInfo> Cars = new List<CarInfo>(); //to save car objects
        public static List<Player> Players = new List<Player>(); // holds all the player objects
        Database db = new Database();
        #endregion

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
            if (password.Length <= 5)
            {
                API.sendChatMessageToPlayer(player, "~r~Error: ~w~Password is not long enough, needs to be ~r~5~w~ or more characters");
                API.sendChatMessageToPlayer(player, "~r~Syntax: ~w~/register [password]");
            }
            else //password is long enough
            {
                //hash it
                string katt = BCrypt.Net.BCrypt.HashPassword(password);
                //save it

                foreach(Player playa in Players)
                {
                    if(playa.playerHandle == player.handle)
                    {
                        int uID = db.CreateAccount(player.name, password);
                        if (uID != 0)
                        {
                            playa.userid = uID; //save the userid to the object
                            API.sendChatMessageToPlayer(player, "~g~Success! ~w~Your account has been saved. You have been logged in, remember the password for the next time!");
                            playa.logged_in = true;
                            break;
                        }
                        else
                        {
                            API.sendChatMessageToPlayer(player, "~r~Error: ~w~Something when wrong when creating your account. Please try again or contact an owner..");
                            break;
                        }
                    }
                }
                
               

                    
                    //show dialog or any window
            }
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
            API.sendChatMessageToPlayer(sender, "~o~Welcome! ");
            Players.Add(new Player(sender, sender.handle));
            API.setPlayerSkin(sender, API.pedNameToModel("Indian01AMY"));
            API.sendChatMessageToAll(sender.name + " has connected to the server!");
            API.sendChatMessageToPlayer(sender, "~g~Log in or register with ~w~/login~g~ or ~w~/register");

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
        public int userid;
        public bool logged_in = false;
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