using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndependentProject
{
    static class SharedOptionsCollection
    {
        static SharedOptions sharedOptions = new SharedOptions();
        static public SharedOptions SharedOptions
        {
            get
            {
                return sharedOptions;
            }
            set
            {
                sharedOptions = value;
            }
        }
    }
}
