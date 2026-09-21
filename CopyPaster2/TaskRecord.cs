using System;
using System.Collections.Generic;
using System.Text;

namespace CopyPaster2
{
    public record Credentials(string User, string Password);
    public enum TaskStatus
    {
        OK,
        Error,
        NotTried
    }
    public record TaskRecord(long Id, string TaskName, string Contest, string Username, string Submission, string Statements, TaskStatus Status);
    public interface IDatabase
    {
        List<TaskRecord> GetAllTasks();
        List<TaskRecord> Search(string Request);
        TaskRecord GetTask(long Id);


    }
}
