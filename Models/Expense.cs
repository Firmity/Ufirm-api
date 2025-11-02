using System;

namespace UrestComplaintWebApi.Models
{
    // -------------------------
    // ExpenseMaster Model
    // -------------------------
    public class ExpenseMaster
    {
        public int Id { get; set; }
        public string ExpenseType { get; set; }        // expense_type
        public string ExpenseSubtype { get; set; }     // expense_subtype
        public DateTime DateFrom { get; set; }         // date_from
        public DateTime DateTo { get; set; }           // date_to
        public decimal Amount { get; set; }            // amount
        public string Description { get; set; }        // description
        public byte[] BillImage { get; set; }          // bill_image (binary data)
        public int OfficeId { get; set; }              // office_id
        public bool IsActive { get; set; }             // is_active
        public int CreatedBy { get; set; }             // created_by
        public DateTime CreatedOn { get; set; }        // created_on
        public int? UpdatedBy { get; set; }            // updated_by
        public DateTime? UpdatedOn { get; set; }       // updated_on
    }

    // -------------------------
    // ExpenseType Model
    // -------------------------
    public class ExpenseType
    {
        public int ExpenseId { get; set; }             // expense_id
        public string ExpenseTypeName { get; set; }    // expense_type
        public string ExpenseSubtype { get; set; }     // expense_subtype
        public int CreatedBy { get; set; }             // created_by
        public int? UpdatedBy { get; set; }            // updated_by
        public DateTime CreatedAt { get; set; }        // created_at
        public DateTime? UpdatedAt { get; set; }       // updated_at
        public bool IsActive { get; set; }             // is_active
        public int OfficeId { get; set; }              // office_id
    }
}
