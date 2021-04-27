using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Minecraft_Monitor.Helpers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Minecraft_Monitor.Models
{
    public class Player
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid UUID { get; set; }
        public string Name { get; set; }
        public bool IsOnline { get; set; }
        public DateTimeOffset LastOnlineDate { get; set; }
        public virtual Coordinates Coordinates { get; set; }

        /// <summary>
        /// The inventory is kept as a string in the entity.
        /// </summary>
        [JsonIgnore]
        public string InventoryJson { get; set; }

        /// <summary>
        /// Deserialize the inventory when needed.
        /// </summary>
        [NotMapped]
        public List<Item> Inventory => InventoryJson != null ? JsonSerializer.Deserialize<List<Item>>(InventoryJson, new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        }) : null;
    }
}