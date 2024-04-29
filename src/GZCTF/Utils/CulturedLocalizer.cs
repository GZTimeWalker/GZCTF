using System.Globalization;
using Microsoft.Extensions.Localization;

namespace GZCTF.Utils;

class CulturedLocalizer<T>(CultureInfo cultureInfo) : IStringLocalizer<T>
{
    public LocalizedString this[string name]
    {
        get
        {
            var str = Resources.Program.ResourceManager.GetString(name, cultureInfo);
            return new(name, str ?? name, str is null);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var str = Resources.Program.ResourceManager.GetString(name, cultureInfo);
            return new(name, string.Format(str ?? name, arguments), str is null);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
        throw new NotImplementedException();
}
