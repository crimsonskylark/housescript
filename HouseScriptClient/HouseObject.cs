using System;
using System.Collections.Generic;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace HouseScript.Client
{
    /* <summary>
    /* This class represents an object that has been put in the house by the propOwner.
    /* Its purpose is to store the state in which the objects were created in a format
    /* suited for (de)serialization in order to allow for easy manipulation.
    /* </summary>
    /* <param name="propHash">The hash of the prop. Necessary in order to re-create the object on demand.</param>
    /* <param name="propPos">The position of the prop in the world represented by a <c>Vector3</b>.</param>
    /* <param name="propRotation">The rotation of the prop represented by a <c>Vector3</b>.</param>
    /* <param name="propTimeCreated">UNIX timestamp of when the object was first materialized.</param>
    /* <param name="propOwner">Rockstar license identifier of the player that owns this object.</param>
    /* <param name="propPrice">The propPrice that was paid for this object in the shop.</param>
    /* <param name="propSellPrice">The propPrice that this object is going to be sold for. Defaults to 75% of the original <c>propPrice</c>.
    /* <param name="houseId">A single user can have multiple houses, so we take that into consideration.</param>
    /* <param name="propType">An easy, human-readable name to be displayed in the interface.</param>
    */

    public class HouseObject
    {
        public int propHash { get; set; }
        public Vector3 propPos { get; set; }
        public Vector3 propRotation { get; set; }
        public ulong propTimeCreated { get; private set; }
        public string propOwner { get; private set; }
        public int propPrice { get; private set; }
        public int propSellPrice { get; private set; }
        public int houseId { get; private set; }
        public string propType { get; private set; }
        public HouseObject(int prop_hash, Vector3 prop_pos, Vector3 prop_rotation, ulong time_created, string owner, int price)
        {
            propHash = prop_hash;
            propPos = prop_pos;
            propRotation = prop_rotation;
            propTimeCreated = (time_created == 0) ? GetUNIXTimeStamp() : time_created;
            propOwner = owner;
            propPrice = price;
            try
            {
                propSellPrice = (int)(price * float.Parse(GetConvar("sell_price_percent", "75")) / 100);
            }
            catch (FormatException)
            {
                Debug.WriteLine(GetConvar("sell_price_percent", "75").ToString());
                Debug.WriteLine("You configured an invalid value in the `sell_price_percent` convar.\n" +
                                "Make sure the value is between 1 and 100, inclusive.");
            }

        }
        // <summary>
        // Helper function to get the UNIX timestamp used in <c>propTimeCreated</c>.
        // </summary>
        private static ulong GetUNIXTimeStamp()
        {
            return (ulong)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        }

        public override string ToString()
        {
            const string obj = @"
            Owner: {0}
            Hash: {1}
            Position: Vector3({2}, {3}, {4})
            Rotation: Vector3({5}, {6}, {7})
            Time created: {8}
            Price: {9}
            Sell price: {10}
            ";
            return string.Format(obj,
                                propOwner,
                                propHash,
                                propPos.X, propPos.Y, propPos.Z,
                                propRotation.X, propRotation.Y, propRotation.Z,
                                propTimeCreated,
                                propPrice,
                                propSellPrice);
        }

    }
}
