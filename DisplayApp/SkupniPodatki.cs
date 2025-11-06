using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DisplayApp
{
    public static class SkupniPodatki
    {
        public static List<XElement> Izdelek { get; set; }
        public  static List<XElement> IzdelekNovo { get; set; }

        public static List<string> PasiceIzFolder { get; set; }
    }
}
