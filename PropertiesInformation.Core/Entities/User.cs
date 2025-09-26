using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PropertiesInformation.Core.Entities
{
    public sealed class User
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string NormalizedUserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string NormalizedEmail { get; set; } = string.Empty;
        [JsonIgnore]
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Rol { get; set; } = "User";

        public void Normalize()
        {
            NormalizedUserName = (UserName ?? string.Empty).ToUpperInvariant();
            NormalizedEmail = (Email ?? string.Empty).ToUpperInvariant();
        }
    }
}
