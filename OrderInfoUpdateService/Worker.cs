using CinemaService.Models;
using Microsoft.EntityFrameworkCore;

namespace OrderInfoUpdateService;

/// <summary>
/// Represents worker type running in the background.
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly OrderUpdater _orderUpdater;
    private readonly CinemaContext _context;
    private readonly int _defaultDelay;

    /// <summary>
    /// Creates a new instance of <see cref="Worker"/>.
    /// </summary>
    /// <param name="logger">Just a logger.</param>
    /// <param name="config">Configuration to setup inner Entity framework context.</param>
    /// <exception cref="ArgumentNullException">If <see cref="_logger"/> or <see cref="config"/> is null</exception>
    public Worker(ILogger<Worker> logger, IConfiguration config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var options = new DbContextOptionsBuilder<CinemaContext>() .UseNpgsql(config.GetConnectionString("DefaultConnection")).Options;
        _context = new CinemaContext(options);
        _orderUpdater = new OrderUpdater(_context, int.Parse(config["OrderPaymentTimeout"]), int.Parse(config["RefundTimeout"]));
        _defaultDelay = int.Parse(config["DefaultDelay"]) * 1000;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                _orderUpdater.UpdateOrders();
                int delay = (int)(_context.Session.OrderBy(o => o.Date).First(o => o.Date.ToLocalTime() > DateTime.Now).Date.ToLocalTime() - DateTime.Now).TotalMilliseconds;
                if (delay <= 0 || delay > _defaultDelay) delay = _defaultDelay;
                _logger.LogInformation("Next update in {time} minutes", TimeSpan.FromMilliseconds(delay).TotalMinutes);
                await Task.Delay(delay, stoppingToken);
            }
            catch (InvalidOperationException e)
            {
                _logger.LogError(e.ToString());
            }
        }
    }
}