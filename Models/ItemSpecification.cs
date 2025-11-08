using System;
using System.Collections.Generic;

namespace UrestComplaintWebApi.Models
{
    public class ItemSpecification
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public string Item_Name { get; set; }
        public string Gender { get; set; }
        public int Quantity { get; set; }
        public int PropertyId { get; set; }
        public bool IsRequisition { get; set; }
        public bool IsHandover { get; set; }
        public DateTime Created_On { get; set; }
        public DateTime Updated_On { get; set; }
        public bool Is_Active { get; set; }
        public List<ItemSpecificationDetail> Details { get; set; }
    }

    public class ItemSpecificationDetail
    {
        public int Id { get; set; }
        public int Item_Specification_Id { get; set; }
        public string Specification_Name { get; set; }
        public string Specification_Value { get; set; }
        public bool Is_Active { get; set; }
    }

}
