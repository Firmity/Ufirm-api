using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UrestComplaintWebApi.Models
{
    public class TaskSummary
    {
        public int? Propid { get; set; }
        public int? Daily { get; set; }
        public int? Monthly { get; set; }
        public int? Weekly { get; set; }                    

    }
}