using Bongo.Models;
using System.Xml;

namespace Bongo.Data
{
    public class TimetableRepository : RepositoryBase<Timetable>, ITimetableRepository
    {
        public TimetableRepository(AppDbContext appDbContext) : base(appDbContext)
        { }

        public IEnumerable<Session> GetAllSessions()
        {
            return _appDbContext.Sessions.ToList();
        }
        public IEnumerable<Session> GetStudentSessions(Student student)
        {
            return _appDbContext.Sessions.Where(session => session.Username == student.StudentNumber).ToList();
        }

        public Timetable GetUserTimetable(string username)
        {
            return _appDbContext.Timetables.FirstOrDefault(t => t.Username == username);
        }
    }
}
