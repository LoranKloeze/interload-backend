using System.CommandLine;
using InterLoad.Data;
using Microsoft.EntityFrameworkCore;

namespace InterLoad;

public class CommandRunner(IServiceScope scope, string[] args)
{
    public async Task RunAsync()
    {
        var rootCommand = new RootCommand("InterLoad admin tool");

        var dbmigrateCmd = new Command("dbmigrate", "Migrate the database");
        dbmigrateCmd.SetAction(_ => MigrateDatabase());
        rootCommand.Subcommands.Add(dbmigrateCmd);
        
        var parseResult = rootCommand.Parse(args);
        await parseResult.InvokeAsync();
    }
    private async Task MigrateDatabase()
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
    }
}