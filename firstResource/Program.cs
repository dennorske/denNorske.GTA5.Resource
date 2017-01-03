using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Data;
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
            API.shared.consoleOutput(conStr);
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
                        API.shared.consoleOutput("Cannot connect to server.  Contact administrator"+ ex.Message +"(ex.Number"+ex.Number+")");
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

        public bool userNameExist(string username)
        {
            bool nameFound = false;
            string myQue = "SELECT Userid FROM users where Name = @username";
            {
                try
                {
                    if(this.OpenConnection() == true)
                    {
                        using (MySqlCommand cmd = new MySqlCommand(myQue, connection))
                        {
                            cmd.Parameters.AddWithValue("@username", username);
                            using (MySqlDataReader rdr = cmd.ExecuteReader())
                            {
                                if(rdr.HasRows)
                                {
                                    nameFound = true;
                                }
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    API.shared.consoleOutput("Error while checking if account exists = " + ex.Message);
                }
            }
            return nameFound;
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
                
                string myQue = "Select Userid from users where Name = @username";
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
                    this.CloseConnection();
                }
            }
            
            
            catch (Exception ex)
            {
                API.shared.consoleOutput("Error getting user ID: " + ex.Message);
            }
            return 0;
        }

        public string GetUserPass(string username)
        {
            string myQue = "select Password from users where Name = @username limit 0,1";
            string hash = "";
            try
            {
                if (this.OpenConnection() == true)
                {
                    using (MySqlCommand cmd = new MySqlCommand(myQue, connection))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        using (MySqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.HasRows)
                            {
                                while (rdr.Read())
                                {
                                    hash = (string)rdr["Password"];
                                }
                            }
                        }
                    }
                    this.CloseConnection();
                }
            }
            catch (Exception ex)
            {
                API.shared.consoleOutput("Error while checking a users password: " + ex.Message);
            }
            return hash;

        }
        public bool LoadUserStats(Player player)
        {
            string myQue = "SELECT * from users where Name = @username limit 0,1";
            try
            {
                if (this.OpenConnection() == true)
                {
                    using (MySqlCommand cmd = new MySqlCommand(myQue, connection))
                    {
                        cmd.Parameters.AddWithValue("@username", player.playerName);
                        using (MySqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.HasRows)
                            {
                                while (rdr.Read())
                                {
                                    player.userid = (int)rdr["Userid"];
                                    player.skin = (int)rdr["Skin"];
                                    player.logged_in = true; //log player in
                                    player.kills = (int)rdr["Kills"];
                                    player.deaths = (int)rdr["Deaths"];
                                    player.level = (int)rdr["Level"]; //admin level
                                    this.CloseConnection();
                                }
                            }
                            else
                                return false;
                        }
                    }
                }
                
            }
            catch(Exception ex)
            {
                API.shared.consoleOutput("Failed to retrieve player saved data: " + ex.Message);
            }
            return true;
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
            if (vHash == 0)
            {
                API.sendChatMessageToPlayer(player, "~r~Error; ~w~This car model doesn't exist.");
                return;
            }
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

        [Command("register", "~r~Usage: ~w~/register [password > 5 chars]", GreedyArg = true, SensitiveInfo = true )]
        public void register(Client player, string password)
        {
            if (password.Length <= 5)
            {
                API.sendChatMessageToPlayer(player, "~r~Error: ~w~Password is not long enough, needs to be ~r~5~w~ or more characters");
            }
            else //password is long enough
            {
                //hash it
                string katt = BCrypt.Net.BCrypt.HashPassword(password);
                //save it

                foreach(Player pl in Players)
                {
                    if(pl.playerHandle == player.handle)
                    {
                        if (!db.userNameExist(player.name))
                        {
                            int uID = db.CreateAccount(player.name, katt);
                            if (uID != 0)
                            {
                                pl.userid = uID; //save the userid to the object
                                API.sendChatMessageToPlayer(player, "~g~Success! ~w~Your account has been saved. You have been logged in, remember the password for the next time!");
                                pl.logged_in = true;
                                break;
                            }
                            else //userid was equal to 0, so something went wrong
                            {
                                API.sendChatMessageToPlayer(player, "~r~Error: ~w~Something when wrong when creating your account. Please try again or contact an owner..");
                                break;
                            }
                        }
                        else //the user already exists
                        {
                            API.sendChatMessageToPlayer(player, "~r~Error:~w~ This name seems to be registered already. Please ~b~/login [pass]");
                        } 
                    }
                }//end loop  
            }
        }

        [Command("login", "~r~Usage: ~w~/login [password] - Logs you in to your account.", GreedyArg = true, SensitiveInfo = true)]
        public void login(Client player, string password)
        {
            if(db.userNameExist(player.name))
            {
                if (db.GetUserPass(player.name) == BCrypt.Net.BCrypt.HashPassword(password))
                {
                    foreach(Player pl in Players)
                    {
                        if(pl.playerHandle == player.handle)
                        {
                            pl.logged_in = true;
                        }
                    }
                }
            }
            else
            {
                API.sendChatMessageToPlayer(player, "~r~Error: ~w~This name (" + player.name+") is not registered before. Use ~r~/register");
            }
        }


        [Command("godmode")]
        public void godmode(Client player)
        {
            foreach(Player pl in Players)
            {
                if(pl.carHandle == player.handle)
                {
                    if (pl.logged_in == false)
                    {
                        API.sendChatMessageToPlayer(player, "~r~This command is only available for registered users. /register or /login please.");
                        return;
                    }
                    if (player.invincible)
                    {
                        player.invincible = false;
                        API.sendChatMessageToPlayer(player, "GodMode ~r~Disabled!");
                    }
                    else
                    {
                        player.invincible = true;
                        API.sendChatMessageToPlayer(player, "GodMode ~g~Enabled!");
                    }
                }
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
        public int kills;
        public int deaths;
        public int level;
        public int score;
        public int cookies;
        public int hashouse;
        public int skin;
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