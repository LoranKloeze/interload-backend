using Hangfire;


namespace InterLoad.Jobs;

public static class SetupJobs
{
    public static void Setup(bool enableCrons)
    {
        JobStorage.Current.JobExpirationTimeout = TimeSpan.FromDays(7);

        const string cronEveryXSeconds = "*/30 * * * * *";
        RecurringJob.AddOrUpdate<SyncUltraOfficeJob>("syncUltraOffice",
            SyncUltraOfficeJob.HangfireQueue, x => x.StartAsync(null),
            enableCrons ? cronEveryXSeconds : Cron.Never(), new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Local
            });

        RecurringJob.AddOrUpdate<ResetAllDataFromUltraOfficeJob>("DELETEAllAndResetFromUltraOffice",
            ResetAllDataFromUltraOfficeJob.HangfireQueue, (x) => x.StartAsync(null),
            Cron.Never(), new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Local
            });
    }
}