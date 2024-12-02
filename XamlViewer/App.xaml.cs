using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Uno.Extensions.Configuration;
using Uno.Extensions.Localization;
using Uno.Resizetizer;
using Windows.Globalization;
using Windows.System.UserProfile;

namespace XamlViewer;
public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
    }

    protected Window? MainWindow { get; private set; }
    protected IHost? Host { get; private set; }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            // Add navigation support for toolkit controls such as TabBar and NavigationView
            .UseToolkitNavigation()
            .Configure(host => host
#if DEBUG
                // Switch to Development environment when running in DEBUG
                .UseEnvironment(Environments.Development)
#endif
                .UseLogging(configure: (context, logBuilder) =>
                {
                    // Configure log levels for different categories of logging
                    logBuilder
                        .SetMinimumLevel(
                            context.HostingEnvironment.IsDevelopment() ?
                                LogLevel.Information :
                                LogLevel.Warning)

                        // Default filters for core Uno Platform namespaces
                        .CoreLogLevel(LogLevel.Warning);

                }, enableUnoLogging: true)
                .UseConfiguration(configure: configBuilder =>
                    configBuilder
                        .EmbeddedSource<App>()
                        .Section<AppConfig>()
                        .Section<AppSettings>()
                , configureHostConfiguration: (builder) => builder.AddEmbeddedConfigurationFile<App>("appsettings.desktop.json"))
                .ConfigureAppConfiguration((ctx, b) =>
                {
                    Assembly executingAssembly = typeof(App).Assembly;

                    var files = executingAssembly
                        .GetManifestResourceNames()
                        .Where(name => name.Contains("Assets") && name.EndsWith(".xml"))
                        .Select(name => new EmbeddedAppConfigurationFile(name, executingAssembly))
                        .ToList();

                    var sources = new Microsoft.Extensions.Configuration.Memory.MemoryConfigurationSource()
                    {
                        InitialData = files
                         .Select(a => new KeyValuePair<string, string?>($"files.{a.FileName.Replace($"{executingAssembly.GetName().Name}.Assets.", "").Replace(".xml", "")}", a.GetContent().ReadToEnd()))
                             .ToList()
                    };

                    b.Add(sources);
                })
                // Enable localization (see appsettings.json for supported languages)
                .UseLocalization()
                // Register Json serializers (ISerializer and ISerializer)
                .UseSerialization((context, services) => services
                    .AddContentSerializer(context)
                    .AddJsonTypeInfo(WeatherForecastContext.Default.IImmutableListWeatherForecast))
                .UseHttp((context, services) => services
                    // Register HttpClient
#if DEBUG
                    // DelegatingHandler will be automatically injected into Refit Client
                    .AddTransient<DelegatingHandler, DebugHttpHandler>()
#endif
                    .AddSingleton<IWeatherCache, WeatherCache>()
                    .AddRefitClient<IApiClient>(context))
                .ConfigureServices((context, services) =>
                {
                    // TODO: Register your services
                    //services.AddSingleton<IMyService, MyService>();

                })
                .UseNavigation(RegisterRoutes)
            );
        MainWindow = builder.Window;

#if DEBUG
        MainWindow.UseStudio();
#endif
        MainWindow.SetWindowIcon();

        Host = await builder.NavigateAsync<Shell>();



        //多语言的初始化实现代码
        //Uno.Extensions.Localization.LocalizationService
        //Uno.Extensions.Localization.LocalizationSettings (LocalizationSettings/CurrentCulture)
        //Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride
        //System.Globalization.CultureInfo.DefaultThreadCurrentUICulture
        //System.Globalization.CultureInfo.DefaultThreadCurrentCulture
        //Thread.CurrentThread.CurrentUICulture
        //Thread.CurrentThread.CurrentCulture
        //LocalizationConfiguration/Cultures/First()


    }

    private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            new ViewMap(ViewModel: typeof(ShellViewModel)),
            new ViewMap<MainPage, MainViewModel>(),
            new ViewMap<SettingPage, SettingViewModel>(),
            new ViewMap<PackageDialog, PackageViewModel>(Data: new DataMap<PackageEntity>(), ResultData: typeof(PackageEntity))//,
            //new ViewMap<WorkPage, WorkViewModel>(Data: new DataMap<WorkEntity>())

        );

        routes.Register(
            new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
                Nested:
                [
                    new ("Main", View: views.FindByViewModel<MainViewModel>(), IsDefault:true),
                    new ("Settings",View:views.FindByViewModel<SettingViewModel>()),
                    new ("Package",View:views.FindByViewModel<PackageViewModel>())//,
                    //new ("Work",View:views.FindByViewModel<WorkViewModel>())
                ]
            )
        );
    }
}
