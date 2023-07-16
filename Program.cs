using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Roulette.Messages.AppMessage;
using Roulette.Middlewares;
using Roulette.Models;
using TEA.Holder;

internal class Program {
    private static async Task Main(string[] args) {

        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        var initial =
            new AppModel(
                new SlotPageModel(
                    new LotteryNumber[] { },
                    new char[] { }));


        builder.RootComponents.Add<Roulette.Pages.Index>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        var holder = new TEASetupper<IAppMessage, AppModel>();
        var tea = new TEA.TEA<AppModel, IAppMessage>(initial, holder);
        var middleware = new AppMiddleware(tea, new Random());
        holder.Setup(middleware);

        builder.Services.AddSingleton<ITEASetupper<IAppMessage, AppModel>>(sp => holder);
        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

        var runTask = builder.Build().RunAsync();

        await runTask;
    }
}
