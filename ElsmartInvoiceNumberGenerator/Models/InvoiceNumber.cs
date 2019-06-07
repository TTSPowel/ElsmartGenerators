using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage.Table;

namespace ElsmartInvoiceNumberGenerator.Models
{
    public class InvoiceNumber : TableEntity
    {
        public InvoiceNumber(string gridprefix, string invoiceno)
        {
            this.PartitionKey = gridprefix;
            this.RowKey = invoiceno;

        }

        public InvoiceNumber(){}

        public string GridPrefix { get; set; }

        public string InvoiceNo { get; set; }

    }
}