# Hangfire.Storage.SQLite
[![NuGet](https://buildstats.info/nuget/Hangfire.Storage.SQLite)](https://www.nuget.org/packages/Hangfire.Storage.SQLite)
[![Actions Status Master](https://github.com/raisedapp/Hangfire.Storage.SQLite/workflows/CI-HS-SQLITE/badge.svg?branch=master)](https://github.com/raisedapp/Hangfire.Storage.SQLite/actions)
[![Actions Status Develop](https://github.com/raisedapp/Hangfire.Storage.SQLite/workflows/CI-HS-SQLITE/badge.svg?branch=develop)](https://github.com/raisedapp/Hangfire.Storage.SQLite/actions)
[![Official Site](https://img.shields.io/badge/site-hangfire.io-blue.svg)](http://hangfire.io)
[![License MIT](https://img.shields.io/badge/license-MIT-green.svg)](http://opensource.org/licenses/MIT)

## Overview

An Alternative SQLite Storage for Hangfire.

This project was created by abandonment **Hangfire.SQLite** storage (https://github.com/wanlitao/HangfireExtension), as an alternative to use SQLite with Hangfire.

Is production ready? **Yes**

![dashboard_servers](content/dashboard_servers.png)

![dashboard_recurring_jobs](content/dashboard_recurring_jobs.png)

![dashboard_heartbeat](content/dashboard_heartbeat.png)


## Installation

Install a package from Nuget.

```
Install-Package Hangfire.Storage.SQLite
```

## Usage

This is how you connect to an SQLite instance
```csharp
GlobalConfiguration.Configuration.UseSQLiteStorage();
```

### Example

```csharp
services.AddHangfire(configuration => configuration
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSQLiteStorage());
```

## Options

In the UseSQLiteStorage method you can use an instance of the Hangfire.Storage.SQLite.SQLiteStorageOptions class to specify some options of this plugin.

Below is a description of them:

`Option` | `Default Value`
--- | ---
**QueuePollInterval** |  **TimeSpan.FromSeconds(15)**
**InvisibilityTimeout** |  **TimeSpan.FromMinutes(30)**
**DistributedLockLifetime** | **TimeSpan.FromSeconds(30)**
**JobExpirationCheckInterval** | **TimeSpan.FromHours(1)**
**CountersAggregateInterval** | **TimeSpan.FromMinutes(5)**
**AutoVacuumSelected** | **AutoVacuum.NONE**, other options: **AutoVacuum.Full** or **AutoVacuum.Incremental**
**RecurringAutoCleanIsEnabled** | **false**, It needs to be enabled (**true**), so that it is executed at every ExpirationManager execution.

## Thanks

This project is mainly based on **Hangfire.LiteDB** storage by [@codeyu](https://github.com/codeyu) (https://github.com/codeyu/Hangfire.LiteDB)

## Donation
If this project help you reduce time to develop, you can give me a cup of coffee :) 

[![paypal](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=RMLQM296TCM38&item_name=For+the+development+of+Hangfire.Storage.SQLite&currency_code=USD&source=url)

## License
This project is under MIT license. You can obtain the license copy [here](https://github.com/raisedapp/Hangfire.Storage.SQLite/blob/develop/LICENSE).
