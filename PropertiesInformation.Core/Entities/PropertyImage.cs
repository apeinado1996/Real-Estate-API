using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PropertiesInformation.Core.Entities
{
    public sealed class PropertyImage
    {
        public int Id { get; set; }
        public int IdProperty { get; set; }
        public string? FileName { get; set; }
        public string? ContentType { get; set; }
        public bool Enabled { get; set; }
        public byte[]? File { get; set; }
    }
}
