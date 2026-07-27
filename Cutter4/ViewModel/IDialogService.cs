using System;
using System.Collections.Generic;
using System.Text;

namespace Cutter4.ViewModel
{
    public interface IDialogService
    {
        void ShowError(string message);
        void ShowInfo(string message);
    }
}
