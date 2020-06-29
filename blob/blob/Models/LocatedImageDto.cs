using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace blob.Models
{
    public class LocatedImageDto
    {
        public LocationDto LocationDto { get; set; }
        public string Base64 { get; set; }
        public int UserId { get; set; }
    }
}
