using System;
using System.Collections.Generic;
using System.Text;

namespace Model
{
    public class OfferItem
    {
        public List<Service> Services { get; set; }
        public Price Price { get; set; }
    }
}
