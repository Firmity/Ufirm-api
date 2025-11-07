using System;
using System.Collections.Generic;

namespace UrestComplaintWebApi.Models
{
    public class SalaryAllowanceDto
    {
        public int ID { get; set; }
        public int SalaryGroup_ID { get; set; }
        public string SalaryGroup { get; set; }
        public decimal FixedSalary { get; set; }

        public decimal BaseSalary { get; set; }
        public int Property_ID { get; set; }
        public DateTime CreatedOn { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public bool IsActive { get; set; }

        public int TaxAmount { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? StartDate { get; set; }
        // Instead of single string
        public List<AllowanceDeductionDto> AllowancesDeductions { get; set; } = new List<AllowanceDeductionDto>();

    }
    public class LoanAdvanceDto
    {
        public int LoanID { get; set; }
        public int EmployeeID { get; set; }        // FacilityMemberId
        public decimal LoanAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        public int TenureMonths { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime RepaymentStartDate { get; set; }
        public DateTime RepaymentEndDate { get; set; }
        public decimal MonthlyInstallment { get; set; }
    }

    public class AllowanceDeductionDto
    {
        public int AD_Id { get; set; }
        public string Type { get; set; }   // Allowance / Deduction
        public string Name { get; set; }
        public decimal CalculatedAmount { get; set; }

        public FormulaDto Formula { get; set; }
    }


    public class AllowanceDeduction
    {
        public int ID { get; set; }

        public string Type { get; set; }   // Allowance / Deduction

        public string Name { get; set; }

        public int Property_ID { get; set; }

        public DateTime CreatedOn { get; set; }

        public int CreatedBy { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public int? UpdatedBy { get; set; }

        public bool IsActive { get; set; }

        public int? FormulaId { get; set; }  // Foreign key to Formula table
        public decimal? CalculatedAmount { get; set; }
    }

    public class FacilityDto
    {
        public int SG_Link_ID { get; set; }
        public List<SalaryAllowanceDtos> SalaryGroups { get; set; } = new List<SalaryAllowanceDtos>();
    }

    public class SalaryAllowanceDtos
    {
        public int SalaryGroup_ID { get; set; }
        public string SalaryGroup { get; set; }
        public decimal FixedSalary { get; set; }
        public decimal BaseSalary { get; set; }
        public List<AllowanceDeductionDtos> AllowancesDeductions { get; set; } = new List<AllowanceDeductionDtos>();
    }

    public class AllowanceDeductionDtos
    {
        public int AD_Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }

        public decimal CalculatedAmount { get; set; }

        public FormulaDto Formula { get; set; }
    }

    public class FacilityMemberCreateDto
    {
        public string FacilityMemberIds { get; set; }
        public int SalaryGroup_ID { get; set; }
        public int Taxamount { get; set; }
        public DateTime? EndDate { get; set; }

    }

    public class FormulaDto
    {
        public int Id { get; set; }

        // Matches the DB column 'NAME'
        public string Name { get; set; }

        // Matches the DB column 'Formula'
        public string Formula { get; set; }

        // Nullable because 'FixedValue' can be NULL in DB
        public int? FixedValue { get; set; }

        // Optional: include these if you want to handle audit fields
        public DateTime? CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public bool? IsActive { get; set; }
    }

}
