using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace mBESS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ApplicationView : Window
    {

        private ConfigurationViewModel _configurationViewModel = null;
        private ParticipantsViewModel _participantsViewModel = null;
        private DoubleViewModel _doubleViewModel = null;

        public ApplicationView()
        {
            InitializeComponent();
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (e.Source is TabControl)
            {
                ApplicationViewModel app = (ApplicationViewModel)DataContext;

                if (TabItemConfiguration.IsSelected)
                {

                    if(_configurationViewModel == null)
                    {
                        _configurationViewModel = new ConfigurationViewModel(app);
                    }

                    app.CurrentPageViewModel = _configurationViewModel;


                } else if(TabItemParticipants.IsSelected)
                {
                    if(_participantsViewModel == null)
                    {
                        _participantsViewModel = new ParticipantsViewModel(app);
                    }
                    app.CurrentPageViewModel = _participantsViewModel;

                } else if(TabItemDoubleStance.IsSelected)
                {
                    if(_doubleViewModel == null)
                    {
                        _doubleViewModel = new DoubleViewModel(app);
                    }
                    app.CurrentPageViewModel = _doubleViewModel;

                } else if(TabItemSingleStance.IsSelected)
                {

                } else if(TabItemTandemStance.IsSelected)
                {

                }

            }

        }

            private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}
