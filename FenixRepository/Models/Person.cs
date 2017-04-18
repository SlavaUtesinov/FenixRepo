using FenixRepo.Core;
using FenixRepo.Context.Migrations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FenixRepo.Context.Models
{
    [Fenix(nameof(Initial), nameof(PersonNewStuff))]    
    public class Person
    {
        public int Id { get; set; }
        [Index("IX_Names", 0)]
        [StringLength(128)]
        public string FirstName { get; set; }
        [Index("IX_Names", 1)]
        [StringLength(128)]
        public string LastName { get; set; }
        public int Age { get; set; }
        public int AddressId { get; set; }
        public virtual Address Address { get; set; }
        [Index]
        public DateTime BirthDay { get; set; }
    }
}
