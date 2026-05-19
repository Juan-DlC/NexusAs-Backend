using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusAs.Domain.Entities
{
    public class Customer : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Document { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Notes { get; set; }
        public ICollection<Sale> Sales { get; set; } = new List<Sale>();
        public ICollection<Credit> Credits { get; set; } = new List<Credit>();
    }
}
