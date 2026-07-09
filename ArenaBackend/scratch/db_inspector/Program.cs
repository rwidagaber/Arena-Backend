using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ArenaDomain.Entities;
using ArenaDomain.Interfaces;
using ArenaInfrastructure.Data;
using ArenaApplication.IServices;
using ArenaInfrastructure.Repositories;
using ArenaApplication.Services;
using ArenaInfrastructure.AI;
using ArenaApplication.AI.Planning;
using ArenaInfrastructure.AI.Planning;
using ArenaInfrastructure.AI.Planning.Steps;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Localization;
using ArenaDomain.Shared;
using ArenaApplication.Dtos.QRCode;

class Program
{
    static async Task Main()
    {
        string connStr = "Server=db55749.public.databaseasp.net; Database=db55749; User Id=db55749; Password=4Wz?h%L2#N3q; Encrypt=False; MultipleActiveResultSets=True;";
        var services = new ServiceCollection();

        // Register AppDbContext
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connStr));

        // Register repositories
        services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
        services.AddScoped<IMemberProfileRepository, MemberProfileRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register services
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IWorkoutAIService, WorkoutAIService>();
        services.AddScoped<INutritionAIService, NutritionAIService>();
        services.AddScoped<IBookingAIService, BookingAIService>();
        services.AddScoped<IQRCodeService, StubQRCodeService>();
        services.AddScoped<IAttendanceService, StubAttendanceService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IBackgroundJobService, BackgroundJobService>();
        services.AddScoped<IBookingService>(sp => {
            var bRepo = sp.GetRequiredService<IGenericRepository<Booking, Guid>>();
            var sRepo = sp.GetRequiredService<IGenericRepository<UserSubscription, Guid>>();
            var wRepo = sp.GetRequiredService<IGenericRepository<WorkingHours, int>>();
            var uow = sp.GetRequiredService<IUnitOfWork>();
            var notif = sp.GetRequiredService<INotificationService>();
            var job = sp.GetRequiredService<IBackgroundJobService>();
            var loc = sp.GetRequiredService<IStringLocalizer<ArenaLocalization>>();
            Console.WriteLine($"MANUAL FACTORY: job is null? {job == null}");
            return new BookingService(bRepo, sRepo, wRepo, uow, notif, job, loc);
        });
        services.AddScoped<IRAGService, SimpleRAGService>();
        services.AddScoped<IMemberHealthRAGService, MemberHealthRAGService>();
        services.AddScoped<IHealthIntelligenceService, HealthIntelligenceService>();
        services.AddScoped<IFitnessPlanningPipeline, FitnessPlanningPipeline>();
        services.AddScoped<IAttendanceSuggestionService, AttendanceSuggestionService>();
        services.AddScoped<IAnalyticsCacheVersionService, StubAnalyticsCacheVersionService>();

        // Planning steps
        services.AddScoped<AnalyzeUserAndMessageStep>();
        services.AddScoped<GoalAndTimeAssessmentStep>();
        services.AddScoped<MedicalSafetyStep>();
        services.AddScoped<MissingInfoCheckStep>();
        services.AddScoped<PlanGeneratorStep>();
        services.AddScoped<PlanValidatorStep>();

        // Mocks for Hangfire & Gemini
        services.AddScoped<Hangfire.IBackgroundJobClient, MockBackgroundJobClient>();
        
        // Add localization & settings
        services.AddLogging();
        services.AddLocalization();
        services.AddScoped<IStringLocalizer<ArenaLocalization>, StubLocalizer>();

        // Gemini Settings
        var geminiSettings = new GeminiSettings
        {
            ApiKey = "AQ.Ab8RN6JiQiLfxgChc4O--IpA0b6lXGKf7UJwS0L83NttBUkI1Q",
            BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models",
            Model = "gemini-2.5-flash"
        };
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(geminiSettings));
        services.AddHttpClient<IGeminiCompletionService, GeminiService>();
        services.AddHttpClient<IEmbeddingService, GeminiEmbeddingService>();

        var provider = services.BuildServiceProvider();
        var ctors = typeof(BookingService).GetConstructors();
        foreach (var ctor in ctors)
        {
            Console.WriteLine($"Ctor: {ctor}");
            foreach (var param in ctor.GetParameters())
            {
                Console.WriteLine($"  Param: {param.Name}, Type: {param.ParameterType}, DefaultValue: {param.DefaultValue}, HasDefaultValue: {param.HasDefaultValue}");
            }
        }

        var client = provider.GetService<Hangfire.IBackgroundJobClient>();
        Console.WriteLine($"Resolved IBackgroundJobClient: {(client == null ? "NULL" : client.GetType().Name)}");

        var jobService = provider.GetService<IBackgroundJobService>();
        Console.WriteLine($"Resolved IBackgroundJobService: {(jobService == null ? "NULL" : jobService.GetType().Name)}");

        var bookingServiceObj = provider.GetService<IBookingService>();
        Console.WriteLine($"Resolved IBookingService: {(bookingServiceObj == null ? "NULL" : bookingServiceObj.GetType().Name)}");

        var chatService = provider.GetRequiredService<IChatService>();
        var profileId = Guid.Parse("28801749-e812-4056-182d-08ded925a767");
        var convId = Guid.Parse("abab890d-2708-410e-a616-e41ca3a479ff");

        Console.WriteLine("Simulating SendMessageAsync with \"23\"...");
        try
        {
            var response = await chatService.SendMessageAsync(profileId, convId, "23");
            Console.WriteLine($"Reply: {response.Reply}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("EXCEPTION CAUGHT:");
            Console.WriteLine(ex.ToString());
        }
    }
}

public class MockBackgroundJobClient : Hangfire.IBackgroundJobClient
{
    public bool ChangeState(string jobId, Hangfire.States.IState state, string expectedState) => true;
    public string Create(Hangfire.Common.Job job, Hangfire.States.IState state) => "mock_job_id";
}

public class StubQRCodeService : IQRCodeService
{
    public Task<QrDto> GenerateAsync(Guid bookingId) => Task.FromResult(new QrDto { Code = "mock_qr" });
    public Task<QrDto> GetByBookingIdAsync(Guid bookingId) => Task.FromResult(new QrDto { Code = "mock_qr" });
    public Task<QrDto> ScanAsync(string qrCode, Guid? memberId = null) => Task.FromResult(new QrDto { Code = "mock_qr" });
}

public class StubAttendanceService : IAttendanceService
{
    public Task<Result<AttendanceDto>> RecordAttendanceAsync(Guid memberId, string qrCode) => Task.FromResult(Result<AttendanceDto>.Success(new AttendanceDto()));
    public Task<Result<List<AttendanceDto>>> GetMemberAttendanceHistoryAsync(Guid memberId) => Task.FromResult(Result<List<AttendanceDto>>.Success(new List<AttendanceDto>()));
}

public class StubAnalyticsCacheVersionService : IAnalyticsCacheVersionService
{
    public Task<long> GetVersionAsync(string key) => Task.FromResult(1L);
    public Task IncrementVersionAsync(string key) => Task.CompletedTask;
}

public class StubLocalizer : IStringLocalizer<ArenaLocalization>
{
    public LocalizedString this[string name] => new LocalizedString(name, name);
    public LocalizedString this[string name, params object[] arguments] => new LocalizedString(name, name);
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => System.Linq.Enumerable.Empty<LocalizedString>();
}

// AttendanceDto stub
public class AttendanceDto
{
    public Guid Id { get; set; }
    public Guid MemberProfileId { get; set; }
    public DateTime ScannedAt { get; set; }
}
