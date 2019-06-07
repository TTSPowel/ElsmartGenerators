using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage.Table;

namespace ElsmartEANGenerator.Models
{
    public class EanGeneratorConfig : TableEntity
    {
        public EanGeneratorConfig(string gridprefix, string lastserial)
        {
            this.PartitionKey = lastserial;
            this.RowKey = gridprefix;

        }
        public EanGeneratorConfig() { }
        public string Gridprefix  { get; set; }

        public string FixedNumbers { get; set; }
        public string StartSerial { get; set; }
      
        public string LastSerial { get; set; }
     
        public string MaxSerial { get; set; }

    }
}