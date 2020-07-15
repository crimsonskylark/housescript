using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using MySqlConnector;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using HouseScript.Client;

namespace HouseScript.Server
{
    public class HouseScriptServer : BaseScript
    {

        MySqlConnection dbInterface = new MySqlConnection(GetConvar("mysql_connection_string", ""));
        /*
         * Let `Player` be 45, and `houseId` be 1:
         * hObjCache[45]
         *  \
         *   <1, { HouseObject[0], HouseObject[1], HouseObject[2] }> <-------
         *   <2, { HouseObject[0], HouseObject[1], HouseObject[2] }>
         *   <3, { HouseObject[0], HouseObject[1], HouseObject[2] }>
         */
        Dictionary<Player, Dictionary<int, List<HouseObject>>> hobjCache = new Dictionary<Player, Dictionary<int, List<HouseObject>>>();

        public HouseScriptServer()
        {}

        /*
         * <summary>
         * This function gets all the objects owned by a player after connection and caches them until the player leaves.
         * The purpose of this is to increase performance and loading times when entering/leaving the house.
         * </summary>
         */
        [EventHandler("playerJoining")]
        private void OnPlayerJoining([FromSource] Player player)
        {
            if (!hobjCache.ContainsKey(player))
            {
                hobjCache.Add(player, new Dictionary<int, List<HouseObject>>());
                GetPlayerObjects(player);
            }
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
        internal void GetPlayerObjects(Player player)
        {
            var playerRockstarIdentifier = player.Identifiers
                                .ToList()
                                .Where(e => e.StartsWith("license:"))
                                .First()
                                .Substring(8);
            Debug.WriteLine($"[HouseArch] Getting owner objects for {playerRockstarIdentifier}");
            if (playerRockstarIdentifier.Length > 1)
            {
                using (var command = dbInterface.CreateCommand())
                {
                    command.CommandText = @"SELECT * FROM house_objects WHERE owner=@owner LIMIT 300;";
                    command.Parameters.AddWithValue("@owner", playerRockstarIdentifier);

                    try
                    {
                        using (var r = command.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                List<HouseObject> temp = new List<HouseObject>();
                                HouseObject currObj = JsonConvert.DeserializeObject<HouseObject>(r.GetString(2));
                                temp.Add(currObj);
                                int key = r.GetInt32(3);
                                if (!hobjCache[player].ContainsKey(key))
                                {
                                    hobjCache[player].Add(key, temp);
                                } else
                                {
                                    hobjCache[player][key].Add(currObj);
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
                Debug.WriteLine($"[HouseArch] ...removed.");
            }
        }

        [EventHandler("onServerResourceStart")]
        private void InitDb(string resName)
        {
            if (GetCurrentResourceName() != resName) { return; }
            dbInterface.Open();
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
        private void OnGetAllUserObjects([FromSource] Player player, int houseId)
        {
            if (hobjCache.ContainsKey(player))
            {
                TriggerClientEvent(player, "HouseArchClient:OnReceiveHouseObjects", JsonConvert.SerializeObject(hobjCache[player][houseId]));
            }
        }
    }
}
