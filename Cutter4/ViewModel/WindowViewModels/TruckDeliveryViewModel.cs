using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace Cutter4.ViewModel
{
    public partial class TruckDeliveryViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanImport))]
        int countToImport;

        public bool CanImport => 0 < CountToImport;

        public TruckDeliveryViewModel() { }
    }
}
