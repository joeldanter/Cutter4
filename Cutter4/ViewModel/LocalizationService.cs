using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Cutter4.ViewModel
{
    public interface ILocalizationService
    {
        string GetString(string key, params object[] args);
    }

    public class LocalizationService : ILocalizationService
    {
        public string GetString(string key, params object[] args)
        {
            var template = Strings.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? $"!!{key}!!";
            return args.Length > 0 ? string.Format(template, args) : template;
        }
    }
}
