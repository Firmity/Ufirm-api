using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UrestComplaintWebApi.Models
{
    public class Country
    {
        public int CountryId { get; set; }
        public string Name { get; set; }
    }

    public class State
    {
        public int CountryId { get; set; }
        public int StateId { get; set; }
        public string StateName { get; set; }

    }
}

