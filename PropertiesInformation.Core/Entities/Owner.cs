using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PropertiesInformation.Core.Entities
{
    public sealed class Owner
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public DateTime? Birthday { get; set; }
        public byte[]? Photo { get; set; }
    }
}
