using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage.Table;

namespace ElsmartKIDGenerator.Models
{
    public class KidNumber  : TableEntity
    {
        public KidNumber(string gridprefix, string kid)
        {
            this.PartitionKey = gridprefix;
            this.RowKey = kid;

        }
        public KidNumber() { }
        public string GridPrefix { get; set; }

        public string KidId { get; set; }

        
    }
}