using NexusAs.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusAs.Domain.Entities
{
    public class User : BaseEntity
    {
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public ICollection<Sale> Sales { get; set; } = new List<Sale>();
        public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    }
}
