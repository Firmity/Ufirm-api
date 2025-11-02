using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UrestComplaintWebApi.Models
{
    public class PropertyDetails
    {
        public int PropertyDetailsId { get; set; }
        public string PropertyName { get; set; }
        public string PropertyStatus { get; set; }
        public string TowerName { get; set; }
        public string FlatDetailNumber { get; set; }
        public int PropertyId { get; set; }
        public int PropertyTowerId { get; set; }
        public int Floor { get; set; }
        public string Flat { get; set; }
        public string ContactNumber { get; set; }
        public int UserId { get; set; }
        public int PropertyDetailTypeId { get; set; }
        public string TotalArea { get; set; }
        public string BuiltupArea { get; set; }
        public string CarpetArea { get; set; }
        public string SuperBuilUpArea { get; set; }
        public string MeasurementUnitsId { get; set; }
        public string UniteConfiguration { get; set; }
        public string CmdType { get; set; }  // C=Create, U=Update, D=Delete, R=Read
    }

}