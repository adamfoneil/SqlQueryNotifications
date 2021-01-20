[![Nuget](https://img.shields.io/nuget/v/AO.SqlQueryAlerts)](https://www.nuget.org/packages/AO.SqlQueryAlerts/)

In one of the products I work on, we have an Azure queue processor that runs on demand. Sometimes there are failures, which are easy to query in the database that the queue relates to. However, there's no notification that tells us when a failure happened. We could of course add notification capability to our queue processor, but let's say we don't really want to make changes to that, or we'd like some way to bundle several query-based notifications together that may or may not be queue-related. Or, we don't want an email about every single failure, but rather just one email per day. What I've sketched out here is the backend of such a notification service. The basics are in place, but you need some kind of cron job service to initiate it.

1. Install NuGet package AO.SqlQueryAlerts

2. Create one or more queries that can trigger notifications along with relevant email subject and contact info by implementing [IQuerySource](https://github.com/adamfoneil/SqlQueryNotifications/blob/master/SqlQueryNotifications/Interfaces/IQuerySource.cs). You have a lot of options as to how this can be done. The repo comes with a simple built-in [QuerySource](https://github.com/adamfoneil/SqlQueryNotifications/blob/master/SqlQueryNotifications/QuerySource.cs) type for creating this inline in your application. You can see this in use in the [integration test](https://github.com/adamfoneil/SqlQueryNotifications/blob/master/Testing/QueryNotifications.cs#L57). More typically, I think you'd want to create your queries in an external location like `App_Data` or blob storage as json-based content. At the moment I don't have an implementation to share that demonstrates this, however.

See the [Query](https://github.com/adamfoneil/SqlQueryNotifications/blob/master/SqlQueryNotifications/Models/Query.cs) class. Create `Query` objects to define your email contact info and the [rule](https://github.com/adamfoneil/SqlQueryNotifications/blob/master/SqlQueryNotifications/Models/Query.cs#L8) causes emails to send or not.

3. Create or inject an `SmtpClient`. This will be passed as an argument to the service. Here's [how I do it](https://github.com/adamfoneil/SqlQueryNotifications/blob/master/Testing/QueryNotifications.cs#L51..L55) in my test.

4. Optional, create or inject an `ILogger`, which is used to log any failures by the service itself. My test doesn't use this.

5. Create a public endpoint (for example MVC action) in your web app that a cron job service can reach. This action should call the [ExecuteAsync](https://github.com/adamfoneil/SqlQueryNotifications/blob/master/SqlQueryNotifications/SqlQueryNotificationService.cs#L39) like I do in my [test](https://github.com/adamfoneil/SqlQueryNotifications/blob/master/Testing/QueryNotifications.cs#L73).

Configure your cron job service to execute your notifications on a schedule that works for you. You should receive emails when queries execute with rules you set.
