using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace blob.Models
{
    public class ImageLocation
    { 
        public int Id { get; set; }
        
        public string Uri { get; set; }

        public long UserId { get; set; }

        public User User { get; set; }

        public float Latitude { get; set; }

        public float Longitude { get; set; }
    }
}
