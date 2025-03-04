# ByondChangelogAzureFunction

## About

A dotnet 8 (LTS) Azure Function to handle BYOND version changelogs via Discord webhooks.

It works by checking the BYOND update file every hour, checking if an update has been pushed, then parsing the changelog into
a discord webhook before sending to a configured list of webhooks.

## Why an Azure Function?

Easier cron scheduling and its set and forget, I dont have to worry about uptime.

## Contributing

Despite being an Azure function, the project will still run locally. You will need a dotnet 8 capable IDE
(Visual Studio recommended), but other than that you just launch it and have it run, though I do advise the
following change to make testing a lot faster.

Before:
```cs
        [Function("CheckByondVersions")]
        public async Task Run([TimerTrigger("0 0 * * * *")] TimerInfo trigger)
```


After:
```cs
        [Function("CheckByondVersions")]
        public async Task Run([TimerTrigger("0 0 * * * *", RunOnStartup = true)] TimerInfo trigger)
```

This will make the function execute on startup, meaning you dont have to reconfigure the cron expression.
The configuration will automatically be created inside a `bv_data` directory.

The project code style is not C# norms, its Java styling. If this offends you please acquire a box of tissues.

## Deploying

This can be deployed on any Azure functions plan on both Windows and Linux, however you need to add an Azure files
share mapped to `bv_data` in order to preserve data and have a config.

## License

The project is licensed under MIT. You can find a copy in `LICENSE.MD`.

## Special thanks

A huge thanks to @alexkar598 for laying the ground work with the following node project:
[https://github.com/alexkar598/byondver](https://github.com/alexkar598/byondver)