using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;

namespace CarScriptClient
{
    // <summary>
    // This class represents an object that has been put in the house by the owner.
    // Its purpose is to store the state in which the objects were created in a format
    // suited for (de)serialization in order to allow for easy manipulation.
    // </summary>
    // <param name="prop_hash">The hash of the prop. Necessary in order to re-create the object on demand.</param>
    // <param name="prop_pos">The position of the prop in the world represented by a <c>Vector3</b>.</param>
    // <param name="prop_rotation">The rotation of the prop represented by a <c>Vector3</b>.</param>
    // <param name="time_created">UNIX timestamp of when the object was first materialized.</param>
    // <param name="owner">Rockstar license identifier of the player that owns this object.</param>
    // <param name="price">The price that was paid for this object in the shop.</param>
    // <param name="sell_price">The price that this object is going to be sold for. Defaults to 75% of the original <c>price</c>.
    public class HouseObject
    {
        public int      prop_hash { get; set; }
        public Vector3  prop_pos { get; set; }
        public Vector3  prop_rotation { get; set; }
        public ulong    time_created { get; private set; }
        public string   owner { get; private set; }
        public int      price { get; private set; }
        public int      sell_price { get; private set; }
        public HouseObject(int prop_hash, Vector3 prop_pos, Vector3 prop_rotation, ulong time_created, string owner, int price)
        {
            this.prop_hash = prop_hash;
            this.prop_pos = prop_pos;
            this.prop_rotation = prop_rotation;
            this.time_created = (time_created == 0) ? time_created : GetUNIXTimeStamp();
            this.owner = owner;
            this.price = price;
            this.sell_price = (int)(price * .75f);
            
        }
        // <summary>
        // Helper function to get the UNIX timestamp used in <c>time_created</c>.
        // </summary>
        private static ulong GetUNIXTimeStamp()
        {
            return (ulong)(TimeZoneInfo.ConvertTimeToUtc(DateTime.UtcNow) -
                   new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

    }
}
