using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using MySqlConnector;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using HouseScript.Client;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace HouseScript.Server
{
    public class HouseScriptServer : BaseScript
    {

        private MySqlConnection dbInterface = new MySqlConnection(GetConvar("mysql_connection_string", ""));
        /*
         * Let `Player` be 45, and `houseId` be 1:
         * hObjCache[45]
         *  \
         *   <1, { HouseObject[0], HouseObject[1], HouseObject[2]... HouseObject[249] }> <-------
         *   <2, { HouseObject[0], HouseObject[1], HouseObject[2]... HouseObject[249] }>
         *   <3, { HouseObject[0], HouseObject[1], HouseObject[2]... HouseObject[249] }>
         */
        private Dictionary<Player, Dictionary<int, List<HouseObject>>> hobjCache = new Dictionary<Player, Dictionary<int, List<HouseObject>>>();

        string command = GetConvar("houseArchCmd", "house");

        public HouseScriptServer()
        {
            RegisterCommand(command, new Action<int, List<object>, string>(OpenInterface), false);
        }

        /*
         * <summary>
         * This function gets all the objects owned by a player after connection and caches them until the player leaves.
         * The purpose of this is to increase performance and loading times when entering/leaving the house.
         * </summary>
         */
        [EventHandler("playerJoining")]
        private async void OnPlayerJoining([FromSource] Player player)
        {
            if (!hobjCache.ContainsKey(player))
            {
                hobjCache.Add(player, new Dictionary<int, List<HouseObject>>());
                await GetPlayerObjects(player);
            }
            TriggerClientEvent(player, "chat:addSuggestion", $"/{command}", "Open the HouseArch interface");
        }

        /*
         * <summary>
         * Internal API to query the database for all of an user's objects and save them to the cache.
         * This is an expensive operation since it needs to deserialize the objects one by one
         * but it only does this once, when the player joins the server. All further requests will be
         * to the cache itself.
         * </summary>
         * <param name="player">The current <c>Player</c> object</param>
         */
        internal async Task GetPlayerObjects(Player player)
        {
            var playerRockstarIdentifier = GetPlayerRockstarId(player);
            Debug.WriteLine($"[HouseArch] Getting owner objects for {playerRockstarIdentifier}");
            if (playerRockstarIdentifier.Length > 1)
            {
                using (var command = dbInterface.CreateCommand())
                {
                    command.CommandText = @"SELECT * FROM house_objects WHERE owner=@owner;"; // Object limit is per house, not per player.
                    command.Parameters.AddWithValue("@owner", playerRockstarIdentifier);

                    try
                    {
                        using (var r = await command.ExecuteReaderAsync())
                        {
                            while (await r.ReadAsync())
                            {
                                List<HouseObject> temp = new List<HouseObject>();
                                HouseObject currObj = JsonConvert.DeserializeObject<HouseObject>(r.GetString(2));
                                temp.Add(currObj);
                                int houseid = r.GetInt32(3);
                                if (!hobjCache[player].ContainsKey(houseid))
                                {
                                    hobjCache[player].Add(houseid, temp);
                                } else
                                {
                                    hobjCache[player][houseid].Add(currObj);
                                }
                                Debug.WriteLine($"[HouseArch] Caching object: {r.GetString(2)} at house {r.GetInt32(3)}");
                            }
                        }

                    } catch (MySqlConnector.MySqlException)
                    {
                        Debug.WriteLine($"[HouseArch] User {playerRockstarIdentifier} does not have any objects.");
                    }
                }
            }
        }

        private string GetPlayerRockstarId(Player p)
        {
            return p.Identifiers
                    .ToList()
                    .Where(e => e.StartsWith("license:"))
                    .First()
                    .Substring(8);
        }

        /*
         * <summary>
         * Delete the player from the cache after he has left and the GC will do the rest
         * </summary>
         */
        [EventHandler("playerDropped")]
        private void OnPlayerDropped([FromSource] Player player, string reason)
        {
            Debug.WriteLine($"[HouseArch] Removing `{player.Name}` from the cache.");
            if (hobjCache.ContainsKey(player))
            {
                hobjCache.Remove(player);
                Debug.WriteLine($"[HouseArch] ... removed.");
            }
        }

        [EventHandler("onServerResourceStart")]
        private void InitDb(string resName)
        {
            if (GetCurrentResourceName() != resName) { return; }
            dbInterface.OpenAsync();
            Debug.WriteLine("[HouseArch] Opening database connection...");
        }

        [EventHandler("onServerResourceStart")]
        private void LicenseCheck(string resName)
        {
            if (GetCurrentResourceName() != resName) { return; }
            string licenseKey = GetConvar("vfms_LicenseKey", "");
            if (licenseKey.Length < 1)
            {
                Debug.WriteLine("\n^1[!!]^9 Your VFMS license key is^9 invalid.");
            } else
            {
                Debug.WriteLine("\n^1[!!] ^0Your VFMS license key is has been validated.");
            }
        }

        [EventHandler("HouseArch:GetAllUserObjects")]
        private async void OnGetAllUserObjects([FromSource] Player player, int houseId)
        {
            if (hobjCache.ContainsKey(player))
            {
                TriggerClientEvent(player, "HouseArchClient:OnReceiveHouseObjects", JsonConvert.SerializeObject(hobjCache[player][houseId]));
            } else
            {
                hobjCache.Add(player, new Dictionary<int, List<HouseObject>>());
                await GetPlayerObjects(player);
            }
        }

        private void OpenInterface(int source, List<object> args, string raw)
        {
            TriggerClientEvent(Players[source], "HouseArchClient:OpenInterface");
        }

        private void SaveObject(Player player, string obj)
        {
            using (var command = dbInterface.CreateCommand())
            {
                command.CommandText = @"INSERT INTO house_objects(owner, object, houseid) VALUES(@owner, @obj, @hid);";
                command.Parameters.AddWithValue("@owner", GetPlayerRockstarId(player));
                command.Parameters.AddWithValue("@obj", obj);
                command.Parameters.AddWithValue("@hid", new Random().Next(1, 5));
                var retval = command.ExecuteNonQuery();
                if (retval >= 1)
                {
                    Debug.WriteLine($"Saved object\n{obj}");
                } else
                {
                    Debug.WriteLine("No objects were saved.");
                }
            }
        }

        [EventHandler("HouseArch:PlayerAcquireObject")]
        private void OnPlayerAcquireObject([FromSource] Player player, string obj)
        {
            SaveObject(player, obj);
        }
    }
}
