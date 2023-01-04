using System.Configuration;
using CinemaService.Models;
using Microsoft.EntityFrameworkCore;
using OrderInfoUpdateService;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();