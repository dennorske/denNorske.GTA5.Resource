using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MySql.Data.MySqlClient;
using GTANetworkShared;
using GTANetworkServer;
using System.IO;
using System.Diagnostics;
using denNorske_gta5;
using structure;



namespace data
{
    public class userdatamanaging
    {
        #region "variables"
        data.Database db = new data.Database();
        #endregion

        #region "public functions"
        public bool userNameExist(string username)
        {
            bool nameFound = false;
            string myQue = "SELECT Userid FROM users where Name = @username";
            {
                try
                {
                    if (db.OpenConnection() == true)
                    {
                        using (MySqlCommand cmd = new MySqlCommand(myQue, db.connection))
                        {
                            cmd.Parameters.AddWithValue("@username", username);
                            using (MySqlDataReader rdr = cmd.ExecuteReader())
                            {
                                if (rdr.HasRows)
                                {
                                    nameFound = true;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    API.shared.consoleOutput("Error while checking if account exists = " + ex.Message);
                }
            }
            return nameFound;
        }



        public int CreateAccount(string username, string password)
        {
            string myQue = "INSERT INTO users (Name, Password) values (@name, @password);";

            try
            {
                if (db.OpenConnection() == true)
                {
                    MySqlCommand cmd = new MySqlCommand(myQue, db.connection);
                    cmd.Parameters.AddWithValue("@name", username);
                    cmd.Parameters.AddWithValue("@password", password);
                    cmd.ExecuteNonQuery();
                    db.CloseConnection();
                    return GetUserID(username);
                }

            }
            catch (Exception ex)
            {
                API.shared.consoleOutput("Error creating new user account: " + ex.Message);

            }

            return 0;
        }


        public string GetUserPass(string username)
        {
            string myQue = "select Password from users where Name = @username limit 0,1";
            string hash = "";
            try
            {
                if (db.OpenConnection() == true)
                {
                    using (MySqlCommand cmd = new MySqlCommand(myQue, db.connection))
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
                    db.CloseConnection();
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
                if (db.OpenConnection() == true)
                {
                    using (MySqlCommand cmd = new MySqlCommand(myQue, db.connection))
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
                                    db.CloseConnection();
                                }
                            }
                            else
                                return false;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                API.shared.consoleOutput("Failed to retrieve player saved data: " + ex.Message);
            }
            return true;
        }
        #endregion

        #region "private / local functions"
        private int GetUserID(string username)
        {
            try
            {

                string myQue = "Select Userid from users where Name = @username";
                if (db.OpenConnection() == true)
                {
                    MySqlCommand cmd = new MySqlCommand(myQue, db.connection);
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
                    db.CloseConnection();
                }
            }


            catch (Exception ex)
            {
                API.shared.consoleOutput("Error getting user ID: " + ex.Message);
            }
            return 0;
        }

        #endregion
    }
}
