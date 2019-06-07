using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage.Table;

namespace ElsmartEANGenerator.Models
{
    public class EanGenerated : TableEntity
    {
        public EanGenerated(string gridprefix, string eannumber)
        {
            this.PartitionKey = eannumber;
            this.RowKey = gridprefix;

        }
        public EanGenerated() { }
        public string Gridprefix { get; set; }
       
        public string EanNumber { get; set; }
        
        public DateTime Generated { get; set; }
    }
}