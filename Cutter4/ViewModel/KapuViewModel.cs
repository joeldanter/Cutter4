using CommunityToolkit.Mvvm.ComponentModel;
using Cutter4.Model.Cutting;
using Org.BouncyCastle.Crypto.Agreement;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cutter4.ViewModel
{
    public partial class KapuViewModel : ObservableValidator
    {
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(1, Kapu.MaxLength)]
        int length;
        
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(1, Kapu.MaxHeight)]
        int height;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required]
        PanelColors? color;
        
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [CustomValidation(typeof(KapuViewModel), nameof(ValidateSerialNumber))]
        string serialNumber;

        public static ValidationResult ValidateSerialNumber(string serialNumber, ValidationContext validationContext)
        {
            if (KapuSerialNumber.IsValid(serialNumber, out string errorMessage))
                return ValidationResult.Success;

            return new(errorMessage);
        }

        public KapuViewModel()
        {
            Length = 0;
            Height = 0;
            Color = null;
            SerialNumber = string.Empty;

            ValidateAllProperties();
        }

        public KapuViewModel(Kapu kapu)
        {
            Length = kapu.Length;
            Height = kapu.Height;
            Color = kapu.Color;
            SerialNumber = kapu.SerialNumber.Value;
        
            ValidateAllProperties();
        }

        public Kapu ActualKapu()
        {
            if (HasErrors)
            {
                throw new InvalidOperationException("Can't create a new Kapu instance, because the given data has errors");
            }

            return new Kapu(Length, Height, (PanelColors)Color, new(SerialNumber));
        }
    }
}
