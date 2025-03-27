/*
 * GZ::CTF
 *
 * Copyright © 2022-present GZTimeWalker
 *
 * This source code is licensed under the AGPLv3 license found in the LICENSE file
 * in the root directory of this source tree.
 *
 * Identifiers related to "GZCTF" (including variations and derivations) are protected.
 * Examples include "GZCTF", "GZ::CTF", "GZCTF_FLAG", and similar constructs.
 *
 * Modifications to these identifiers are prohibited as per the LICENSE_ADDENDUM.txt
 */

global using GZCTF.Models.Data;
global using GZCTF.Utils;
global using static GZCTF.Server;
global using AppDbContext = GZCTF.Models.AppDbContext;
global using TaskStatus = GZCTF.Utils.TaskStatus;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using GZCTF.Extensions.Startup;
using GZCTF.Models;
using Serilog;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
Log.Logger = LogHelper.GetInitLogger();

Banner();

var builder = WebApplication.CreateBuilder(args);

await PathHelper.EnsureDirsAsync(builder.Environment);

builder.ConfigureWebHost();
builder.ConfigureDatabase();
builder.ConfigureStorage();
builder.ConfigureCacheAndSignalR();
builder.ConfigureIdentity();
builder.ConfigureTelemetry();

builder.AddServiceConfigurations();
builder.AddCustomServices();
builder.AddWebServices();
builder.AddDevelopmentServices();

var app = builder.Build();

Log.Logger = LogHelper.GetLogger(app.Configuration, app.Services);

await app.RunPrelaunchWorkAsync();

app.UseMiddlewares();

await app.RunServerAsync();

namespace GZCTF
{
    public class Program
    {
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DesignTimeAppDbContextFactory))]
        static Program()
        {
            using var stream = typeof(Program).Assembly
                .GetManifestResourceStream("GZCTF.Resources.favicon.webp")!;
            DefaultFavicon = new byte[stream.Length];

            stream.ReadExactly(DefaultFavicon);
            DefaultFaviconHash = Convert.ToHexStringLower(SHA256.HashData(DefaultFavicon));
        }

        internal static byte[] DefaultFavicon { get; }
        internal static string DefaultFaviconHash { get; }
    }
}
