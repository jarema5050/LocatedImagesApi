using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace blob.Models
{
    public class LoginData
    {
        public long Id { get; set; }
        public string Email { get; set; }

        public string Status { get; set; }

        public string Message { get; set; }
    }
}
