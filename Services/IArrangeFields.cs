using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgramToConvertXmlToJson.Services
{
    public interface IArrangeFields
    {
        public void SortApplicationEdit();  
        public void SortApplicationView();
        public void SortPropertyEdit();

        public void AdjoiningPropertyEdit();
        public void AdjoiningPropertyNew();

        public void ApplicationContactNew(string UiSec);

        public void ForEdit(string XUiSec, string JUisec, string Xcon);

        public void ForEditTabs(string XUiSec, string JUisec, string Xcon);




    }
}
