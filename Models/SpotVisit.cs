using System;

namespace UrestComplaintWebApi.Models
{
    public class TaskQuestionnaireDetail
    {
        public string TaskName { get; set; }
        public string QuestionName { get; set; }
        public string Remarks { get; set; }
        public string Action { get; set; }
    }

    public class TaskDailySummary
    {
        public string PropertyName { get; set; }
        public int FacilityMemberId { get; set; }
        public string FacilityMemberName { get; set; }
        public int TaskId { get; set; }
        public DateTime TaskDate { get; set; }
        public int TaskCount { get; set; }
    }
}
