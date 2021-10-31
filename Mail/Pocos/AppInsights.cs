using System.Collections.Generic;

namespace AppInsightExceptionsMailer.Mail.Pocos
{
    public class Column
    {
        public string name { get; set; }
        public string type { get; set; }
    }

    public class Table
    {
        public string name { get; set; }
        public List<Column> columns { get; set; }
        public List<List<object>> rows { get; set; }
    }

    public class Root
    {
        public List<Table> tables { get; set; }
    }
    public class AppInsightLog
    {
        public string CloudRoleName { get; set; }

        public string ProblemId { get; set; }

        public string OuterMessage { get; set; }

        public int Count { get; set; }
    }
}
