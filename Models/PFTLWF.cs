namespace UrestComplaintWebApi.Models
{
    public class PftMaster
    {
        public int PftId { get; set; }
        public int StateId { get; set; }
        public decimal AmountFrom { get; set; }
        public decimal AmountTo { get; set; }
        public decimal PftAmount { get; set; }
    }

    public class LwfMaster
    {
        public int LwfId { get; set; }
        public int StateId { get; set; }
        public decimal LwfAmount { get; set; }
        public decimal EmployeeAmount { get; set; }
        public decimal EmployerAmount { get; set; }
    }
}
