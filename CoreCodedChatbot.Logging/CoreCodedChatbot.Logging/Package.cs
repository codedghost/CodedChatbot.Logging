using System;
using System.Collections.Generic;
using System.Text;
using CoreCodedChatbot.Secrets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Common;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using NLog.Web;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace CoreCodedChatbot.Logging
{
    public static class Package
    {
        public static IServiceCollection AddChatbotNLog(this IServiceCollection services)
        {
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(LogLevel.Information);

                builder.AddNLog((serviceProvider) =>
                {
                    var secretService = serviceProvider.GetService<ISecretService>();

                    var config = new LoggingConfiguration();
                    var dbTarget = new DatabaseTarget("dbLog")
                    {
                        ConnectionString = new SimpleLayout(secretService.GetSecret<string>("DbConnectionString"), ConfigurationItemFactory.Default),
                        CommandText =
                            "INSERT INTO LogEntry(Level, LoggedAt, Message, Logger, Callsite, Exception, StackTrace, ProcessName, AppDomain) " +
                            "VALUES(@Level, @LoggedAt, @Message, @Logger, @Callsite, @Exception, @StackTrace, @ProcessName, @AppDomain);",
                        Parameters =
                    {
                        new DatabaseParameterInfo
                        {
                            Name = "@Level",
                            Layout = new SimpleLayout("${level}")
                        },
                        new DatabaseParameterInfo
                        {
                            Name = "@LoggedAt",
                            Layout = new SimpleLayout("${longdate}")
                        },
                        new DatabaseParameterInfo
                        {
                            Name = "@Message",
                            Layout = new SimpleLayout("${message}")
                        },
                        new DatabaseParameterInfo
                        {
                            Name = "@Logger",
                            Layout = new SimpleLayout("${logger}")
                        },
                        new DatabaseParameterInfo
                        {
                            Name = "@Callsite",
                            Layout = new SimpleLayout("${callsite}")
                        },
                        new DatabaseParameterInfo
                        {
                            Name = "@Exception",
                            Layout = new SimpleLayout("${exception:format=toString,Data}")
                        },
                        new DatabaseParameterInfo
                        {
                            Name = "@StackTrace",
                            Layout = new SimpleLayout("${stacktrace}")
                        },
                        new DatabaseParameterInfo
                        {
                            Name = "@ProcessName",
                            Layout = new SimpleLayout("${processname}")
                        },
                        new DatabaseParameterInfo
                        {
                            Name = "@AppDomain",
                            Layout = new SimpleLayout("${appdomain}")
                        }
                    },
                        DBProvider = "Microsoft.Data.SqlClient.SqlConnection, Microsoft.Data.SqlClient"
                    };

                    InternalLogger.LogToConsoleError = true;

                    config.AddTarget(dbTarget);
                    config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, "dbLog", "*");

                    return new LogFactory(config);
                });
            });

            return services;
        }

        public static void ConfigureNLogForConsoleApp<T>(IServiceProvider serviceProvider)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler((sender, eventArgs) => UnhandledExceptionHandler<T>(sender, eventArgs, serviceProvider));
        }

        private static void UnhandledExceptionHandler<T>(object sender, UnhandledExceptionEventArgs eventArgs,
            IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetService<ILogger<T>>();

            logger.LogError((Exception)eventArgs.ExceptionObject, $"Unhandled error encountered - Terminating: {eventArgs.IsTerminating}");
        }
    }
}
