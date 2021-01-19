using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json.Serialization;

namespace SqlQueryNotifications.Models
{
    public enum TriggerSendRule
    {
        IfEmpty,
        IfAny,
        IfCustom
    }

    public class Query
    {
        public IEnumerable<string> Recipients { get; set; }
        public string Subject { get; set; }
        public string Sql { get; set; }
        public TriggerSendRule SendRule { get; set; }

        [JsonIgnore]
        public Func<DataTable, bool> CustomSendRule { get; set; }
    }
}
