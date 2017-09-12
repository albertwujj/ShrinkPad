using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndependentProject
{
    class SharedOptions
    {
        public SharedOptions()
        {
            this.densityOption = false;
            this.heightOption = false;
        }
        Boolean densityOption;
        public Boolean DensityOption
        {
            get
            {
                return densityOption;
            }
            set
            {
                densityOption = value;
            }
        }
    

        Boolean heightOption;
        public Boolean HeightOption
        {
            get
            {
                return heightOption;
            }
            set
            {
                heightOption = value;
            }
        }
     
       
    }
}
