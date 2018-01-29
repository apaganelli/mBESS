using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mBESS
{
    class ConfigurationViewModel : IPageViewModel
    {
        ApplicationViewModel _app;

        public string Name => throw new NotImplementedException();

        public ConfigurationViewModel(ApplicationViewModel app)
        {
            _app = app;
        }

    }
}
