using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.UI.WebControls;
using ElsmartKIDGenerator.Models;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace ElsmartKIDGenerator.Controllers
{
    [Authorize]
    public class KidController : ApiController
    {
        readonly CloudStorageAccount _storageAccount =
               CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

        readonly CloudTableClient _tableClient;

        CloudTable _tblGeneratedKid;

        public KidController()
        {
            _tableClient = _storageAccount.CreateCloudTableClient();

            var tableGenerated = _tableClient.GetTableReference("kidnumbergenerated");
            tableGenerated.CreateIfNotExists();

            _tblGeneratedKid = _tableClient.GetTableReference("kidnumbergenerated");
        }

        public string GetValue(string id)
        {
            return "your value: " + id;
        }

        public string GetKidNumber(string prefix, int offernumber, int numberofoffer, int customerid)
        {
            //gridprefix(3)_tilbudsnummer(8)_serial(1)_kundeid(5)

            string sCustomerId = customerid.ToString();
            if (customerid.ToString().Length > 5)
                sCustomerId = customerid.ToString().Remove(0, customerid.ToString().Length - 5);
            else
                sCustomerId = customerid.ToString("D5");

            string kidbasic = prefix + offernumber.ToString("D6") + DateTime.Now.ToString("yy") + numberofoffer.ToString("D2") + sCustomerId;
            
            var kidnumber = kidbasic + "001";

            int controNumber = GetMod10Digit(kidnumber);

            kidnumber += controNumber;

            //Check if exists
            while (KidExists(kidnumber, prefix))
            {
                var internalcounter = Convert.ToInt32(kidnumber.Substring(18, 3));
                internalcounter = internalcounter + 1;
                kidnumber = kidbasic + internalcounter.ToString("D3");
                kidnumber += GetMod10Digit(kidnumber);
            }
            
            AddGenerated(kidnumber, prefix);
            
            return kidnumber;
        }

        public string GetKidNumber(string prefix, int offernumber, int numberofoffer, int customerid, int modulus)
        {
            //gridprefix(3)_tilbudsnummer(8)_serial(1)_kundeid(5)
            string sCustomerId = customerid.ToString();
            if (customerid.ToString().Length > 5)
                sCustomerId = customerid.ToString().Remove(0, customerid.ToString().Length - 5);
            else
                sCustomerId = customerid.ToString("D5");

            string kidbasic = prefix + offernumber.ToString("D6") + DateTime.Now.ToString("yy") + numberofoffer.ToString("D2") + sCustomerId;

            var kidnumber = kidbasic + "001";

            string controNumber = modulus.Equals(11) ? GetControlNumberMod11(kidnumber).ToString() : GetMod10Digit(kidnumber).ToString();

            if (controNumber.Equals("10"))
                controNumber = "-";
            else if (controNumber.Equals("11"))
                controNumber = "0";

            kidnumber += controNumber;

            //Check if exists
            while (KidExists(kidnumber, prefix))
            {
                var internalcounter = Convert.ToInt32(kidnumber.Substring(18, 3));
                internalcounter = internalcounter + 1;
                kidnumber = kidbasic + internalcounter.ToString("D3");
                controNumber = modulus.Equals(11) ? GetControlNumberMod11(kidnumber).ToString() : GetMod10Digit(kidnumber).ToString();
                if (controNumber.Equals("10"))
                    controNumber = "-";
                else if (controNumber.Equals("11"))
                    controNumber = "0";
                kidnumber += controNumber;
            }

            AddGenerated(kidnumber, prefix);

            return kidnumber;
        }

        

        private void AddGenerated(string kid, string gridprefix)
        {

            var newKid = new KidNumber(gridprefix,kid)
            {
                KidId = kid,
                GridPrefix = gridprefix,
                Timestamp = DateTimeOffset.Now
            };

            //var retrieveOp = TableOperation.Retrieve(gridprefix, kid);

            var insert = TableOperation.Insert(newKid);
            _tblGeneratedKid.Execute(insert);
        }

        private int GetControlNumber(string serial)
        {
            bool isOne = false;
            int controlNumber = -1;
            foreach (char number in serial.Reverse())
            {
                var intNumber = int.Parse(number.ToString());
                var sum = isOne ? intNumber : 3 * intNumber;

                isOne = !isOne;
                controlNumber += sum;
            }
            return (10 - (controlNumber % 10)) % 10 == 0 ? 0 : 10 - (controlNumber % 10);
        }
        private int GetMod10Digit(string data)
        {
            int sum = 0;
            bool odd = true;
            for (int i = data.Length - 1; i >= 0; i--)
            {
                if (odd == true)
                {
                    int tSum = Convert.ToInt32(data[i].ToString()) * 2;
                    if (tSum >= 10)
                    {
                        string tData = tSum.ToString();
                        tSum = Convert.ToInt32(tData[0].ToString()) + Convert.ToInt32(tData[1].ToString());
                    }
                    sum += tSum;
                }
                else
                    sum += Convert.ToInt32(data[i].ToString());
                odd = !odd;
            }

            int result = (((sum / 10) + 1) * 10) - sum;
            return result % 10;
        }

        private int GetControlNumberMod11(string serial)
        {
            int weighted = 1;
            int sum = 0;
            foreach (char number in serial.Reverse())
            {
                var intNumber = int.Parse(number.ToString());
                weighted = getWeightNumber(weighted);
                sum += intNumber * weighted;
            }

            sum = sum % 11;

            return ((11 - sum) == 0) ? 0 : 11 - sum;
        }
        private int getWeightNumber(int i)
        {
            return i == 7 ? 2 : i + 1;
        }

        private bool KidExists(string kid, string prefix)
        {
            var retrieveOp = TableOperation.Retrieve(prefix, kid);
            var result = _tblGeneratedKid.Execute(retrieveOp);
            if (result.HttpStatusCode.Equals(404))
                return false;
            else
                return true;
            
        }
        
    }
}
