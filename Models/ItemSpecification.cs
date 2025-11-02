using System;

namespace UrestComplaintWebApi.Models
{
    public class ItemSpecification
    {
        public int Id { get; set; }
        public string Item_Name { get; set; }
        public string Gender { get; set; }
        public int Quantity { get; set; }
        public int Specification_Id { get; set; }
        public int Specification_Value { get; set; }
        public int PropertyId { get; set; }
        public DateTime Created_On { get; set; }
        public DateTime Updated_On { get; set; }
        public int Employee_Id { get; set; }
    }
}
