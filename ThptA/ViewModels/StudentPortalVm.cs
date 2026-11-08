
using ThptA.Models;

namespace ThptA.ViewModels
{
    public class StudentPortalVm
    {
        public Student Student { get; set; } = null!;

        public List<TimetableEntry> Timetable { get; set; }
            = new();

        public List<StudentFee> Fees { get; set; }
            = new();
    }
}
