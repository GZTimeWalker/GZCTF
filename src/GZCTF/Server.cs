using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Serilog;

namespace GZCTF;

public class Server
{
    internal const int MetricPort = 3000;
    internal const int ServerPort = 8080;

    internal static readonly string[] SupportedCultures =
    [
        "en-US",
        "zh-CN",
        "zh-TW",
        "ja-JP",
        "id-ID",
        "ko-KR",
        "ru-RU",
        "de-DE",
        "fr-FR",
        "es-ES",
        "vi-VN"
    ];

    static readonly string LanguageWarning =
        $"Warning: Current language {CultureInfo.CurrentCulture.DisplayName} is machine translated and may not be accurate.\n";

    internal static IStringLocalizer<Program> StaticLocalizer { get; } =
        new CulturedLocalizer<Program>(CultureInfo.CurrentCulture);

    internal static void Banner()
    {
        // GZCTF Banner ASCII Art
        // Core: AGPLv3; Restricted components: LicenseRef-GZCTF-Restricted
        // Refer to NOTICE / LICENSE_ADDENDUM.txt for licensing & trademark policy.
        const string banner =
            """
                  ___           ___           ___                       ___
                 /  /\         /  /\         /  /\          ___        /  /\
                /  /:/_       /  /::|       /  /:/         /  /\      /  /:/_
               /  /:/ /\     /  /:/:|      /  /:/         /  /:/     /  /:/ /\
              /  /:/_/::\   /  /:/|:|__   /  /:/  ___    /  /:/     /  /:/ /:/
             /__/:/__\/\:\ /__/:/ |:| /\ /__/:/  /  /\  /  /::\    /__/:/ /:/
             \  \:\ /~~/:/ \__\/  |:|/:/ \  \:\ /  /:/ /__/:/\:\   \  \:\/:/
              \  \:\  /:/      |  |:/:/   \  \:\  /:/  \__\/  \:\   \  \::/
               \  \:\/:/       |  |::/     \  \:\/:/        \  \:\   \  \:\
                \  \::/        |  |:/       \  \::/          \__\/    \  \:\
                 \__\/         |__|/         \__\/                     \__\/
            """ + "\n";
        Console.WriteLine(banner);

        var versionStr = "";
        var version = typeof(Program).Assembly.GetName().Version;
        if (version is not null)
            versionStr = $"Version: {version.Major}.{version.Minor}.{version.Build}";

        var currentYear = DateTime.Now.Year;

        // ReSharper disable once LocalizableElement
        Console.WriteLine($"GZCTF © 2022-{currentYear} GZTimeWalker {versionStr,33}\n");

        // Show warning if a language is machine translated
        string[] machineTranslated = ["de-DE", "fr-FR", "es-ES"];
        if (machineTranslated.Contains(CultureInfo.CurrentCulture.Name))
            Console.WriteLine(LanguageWarning);
    }

    internal static void ExitWithFatalMessage(string msg)
    {
        Log.Logger.Fatal("{msg}", msg);
        Thread.Sleep(30000);
        Environment.Exit(1);
    }

    internal static IActionResult InvalidModelStateHandler(ActionContext context)
    {
        var localizer =
            context.HttpContext.RequestServices.GetRequiredService<IStringLocalizer<Program>>();
        if (context.ModelState.ErrorCount <= 0)
            return RequestResponse.Result(
                localizer[nameof(Resources.Program.Model_ValidationFailed)]);

        var error = context.ModelState.Values.Where(v => v.Errors.Count > 0)
            .Select(v => v.Errors.FirstOrDefault()?.ErrorMessage).FirstOrDefault();

        return RequestResponse.Result(error is [_, ..]
            ? error
            : localizer[nameof(Resources.Program.Model_ValidationFailed)]);
    }
}
