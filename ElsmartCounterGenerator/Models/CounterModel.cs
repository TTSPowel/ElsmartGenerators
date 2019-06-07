using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Microsoft.WindowsAzure.Storage.Table;

namespace ElsmartCounterGenerator.Models
{
    public class CounterModel : TableEntity
    {
        public CounterModel(string gridprefix, string countername, int counter)
        {
            this.PartitionKey = gridprefix;
            this.RowKey = countername + ":" + counter;
        }
        public CounterModel() { }

        public string GridPrefix { get; set; }

        public int Counter { get; set; }

        public string CompleteCounter { get; set; }

        public string CounterName { get; set; }

        public string Status { get; set; }
    }
}