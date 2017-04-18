using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FenixRepo.Context.Models
{
    public class Address
    {
        public int Id { get; set; }
        [StringLength(64)]
        [Index]
        public string PostalCode { get; set; }
        [StringLength(64)]
        public string Street { get; set; }
        public int CityId { get; set; }
        public virtual City City { get; set; }
    }
}
