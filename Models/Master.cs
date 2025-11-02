using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UrestComplaintWebApi.Models
{
    public class FrequencyModel
{    public int Id { get; set; }
    public string Name { get; set; }
    public int Fvalue { get; set; }
    public string Funit { get; set; }
    public string Occurence { get; set; }
}
}