using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcellentEmailExperience.Model
{

    public class AccountSettings
    {
        int signatureIndex = -1;
        
        public void changeSignature(int index)
        {
            signatureIndex = index;
        }
        // seje settings pending


    }
}
