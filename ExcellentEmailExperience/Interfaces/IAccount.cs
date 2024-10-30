using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcellentEmailExperience.Interfaces
{
    public interface IAccount
    {
        void Login();
        void Logout();
        IMailHandler GetMailHandler();
    }
}
