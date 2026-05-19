using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusAs.Application.DTOs.Products
{
    public class CreateProductDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Brand { get; set; }
        public decimal Cost { get; set; }
        public decimal SalePrice { get; set; }
        public int Stock { get; set; }
        public int MinStock { get; set; }
        public string? ImagePath { get; set; }
        public int CategoryId { get; set; }
    }
}
