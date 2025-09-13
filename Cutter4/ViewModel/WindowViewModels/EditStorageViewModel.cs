using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cutter4.Model.Cutting;
using Cutter4.Model.DataBase;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Cutter4.ViewModel
{
    public partial class EditStorageViewModel : ObservableObject
    {
        public MainViewModel MainViewModel { get; set; }
        public ObservableCollection<KeyValuePair<Panel, int>> DeltaCounts { get; } = [];
        
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddNewEditItemCommand))]
        PanelColors? newColor = null;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddNewEditItemCommand))]
        string newWidth = string.Empty;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddNewEditItemCommand))]
        string newLength = string.Empty;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddNewEditItemCommand))]
        string newDeltaCount = string.Empty;
        bool CanAddNewEditItem
        {
            get
            {
                return NewColor != null
                    && NewWidth != string.Empty
                    && int.TryParse(NewLength, out int newLength)
                    && 0 < newLength
                    && newLength <= Kapu.MaxLength
                    && int.TryParse(NewDeltaCount, out int newDeltaCount)
                    && 0 != newDeltaCount;
            }
        }

        [ObservableProperty]
        string message = string.Empty;

        public bool CanApplyEdit { get { return DeltaCounts.Count > 0; } }

        public EditStorageViewModel()
        {
            DeltaCounts.CollectionChanged += (sender, e) => { OnPropertyChanged(nameof(CanApplyEdit)); };
        }

        [RelayCommand(CanExecute = nameof(CanAddNewEditItem))]
        void AddNewEditItem()
        {
            PanelType newType = new (NewWidth, (PanelColors)NewColor);
            int newLength = int.Parse(NewLength);
            int newDeltaCount = int.Parse(NewDeltaCount);
            
            Panel newPanel = new(newType, newLength);
            int modifiedPanelIndex = DeltaCounts.ToList().FindIndex(entry => entry.Key == newPanel);
            if (modifiedPanelIndex == -1)
            {
                DeltaCounts.Add(new(newPanel, newDeltaCount));
            }
            else
            {
                DeltaCounts[modifiedPanelIndex] = new(DeltaCounts[modifiedPanelIndex].Key, DeltaCounts[modifiedPanelIndex].Value + newDeltaCount);
                
                if (DeltaCounts[modifiedPanelIndex].Value == 0)
                {
                    DeltaCounts.RemoveAt(modifiedPanelIndex);
                }
            }

            NewColor = null;
            NewWidth = string.Empty;
            NewLength = string.Empty;
            NewDeltaCount = string.Empty;
        }

        public void Apply()
        {
            Dictionary<Panel, int> deltaCounts = DeltaCounts.ToDictionary();
            MainViewModel.Storage.EditStoragePanels(Message, deltaCounts);
        }

        public void TruckDelivery(int panelCounts)
        {
            DeltaCounts.Clear();
            foreach (PanelType type in PanelType.GetAllTypes())
            {
                Panel panel = new(type, Kapu.MaxLength);
                DeltaCounts.Add(new(panel, panelCounts));
            }
        }
    }
}
