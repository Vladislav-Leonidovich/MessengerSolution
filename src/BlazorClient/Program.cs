using BlazorClient;
using BlazorClient.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Додаємо кореневий компонент у файл index.html
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Налаштовуємо HttpClient з базовою адресою (зазвичай це адреса Gateway)
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7100/") });

// Реєструємо сервіси для роботи з API
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IChatService, ChatService>();

await builder.Build().RunAsync();
