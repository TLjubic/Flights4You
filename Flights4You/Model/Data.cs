using System;
using System.Collections.Generic;
using System.Text;

namespace Model
{
    public class Data
    {
        public string Type { get; set; }
        public string Id { get; set; }
        public List<OfferItem> OfferItems { get; set; }
    }
}
