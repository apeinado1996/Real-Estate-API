using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PropertiesInformation.Core.Entities
{
    public sealed class PropertyTrace
    {
        public int Id { get; set; }
        public DateTime DateSale { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal Tax { get; set; }
        public int IdProperty { get; set; }
    }
}
