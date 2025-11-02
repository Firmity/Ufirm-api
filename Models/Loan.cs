using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UrestComplaintWebApi.Models
{
    
        public class LoanMaster
        {
            public int LoanID { get; set; }
            public int EmployeeID { get; set; }
            public decimal LoanAdvanceAmount { get; set; }
            public string CurrentMonth { get; set; }
            public int TenureMonths { get; set; }
            public DateTime IssueDate { get; set; }
            public DateTime RepaymentStartDate { get; set; }
        }

        public class LoanEMI
        {
            public int RowID { get; set; }
            public int LoanID { get; set; }
            public int EmployeeID { get; set; }
            public decimal MonthlyInstallment { get; set; }
            public DateTime? RepaymentDoneDate { get; set; }
            public decimal BalanceAmount { get; set; }
        }

        public class LoanData
        {
            public LoanMaster LoanMaster { get; set; }
            public List<LoanEMI> LoanEMIs { get; set; } = new List<LoanEMI>();
        }

    
}