
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Web;

namespace Simulysis.Helpers.SignalSearch
{
    public class Signal
    {
        public String Name { get; set; }
        public String From { get; set; }
        public String To { get; set; }

        public Signal(String Name,String From,String To)
        {
            this.Name = Name;
            this.From = From;
            this.To = To;
        }

        public override bool Equals(object obj)
        {
            return obj is Signal result &&
                   Name == result.Name &&
                   From == result.From &&
                   To == result.To;
        }

        public override int GetHashCode()
        {
            int hashCode = -1878242681;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(From);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(To);
            return hashCode;
        }


        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }

}