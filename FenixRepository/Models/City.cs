using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FenixRepo.Context.Models
{
    public class City
    {
        public int Id { get; set; }
        [StringLength(64)]
        public string Name { get; set; }        
    }
}
