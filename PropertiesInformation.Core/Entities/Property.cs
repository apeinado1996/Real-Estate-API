using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PropertiesInformation.Core.Entities
{
    public sealed class Property
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public decimal Price { get; set; }
        public string CodeInternal { get; set; } = string.Empty;
        public short? Year { get; set; }
        public int IdOwner { get; set; }
    }
}
