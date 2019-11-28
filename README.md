# Hangfire.Storage.SQLite
[![NuGet](https://buildstats.info/nuget/Hangfire.Storage.SQLite)](https://www.nuget.org/packages/Hangfire.Storage.SQLite)
[![Actions Status](https://github.com/raisedapp/Hangfire.Storage.SQLite/workflows/CI-HS-SQLITE/badge.svg)](https://github.com/raisedapp/Hangfire.Storage.SQLite/actions)
[![Official Site](https://img.shields.io/badge/site-hangfire.io-blue.svg)](http://hangfire.io)
[![License MIT](https://img.shields.io/badge/license-MIT-green.svg)](http://opensource.org/licenses/MIT)

## Overview

An Alternative SQLite Storage for Hangfire.

This project was created by abandonment **Hangfire.SQLite** storage (https://github.com/wanlitao/HangfireExtension), as an alternative to use SQLite with Hangfire.

## Build Status
`Platform` | `Master` | `Develop`
--- | --- | ---
**Windows** | [![Build Status](https://circleci.com/gh/raisedapp/Hangfire.Storage.SQLite/tree/master.svg?style=svg)](https://circleci.com/gh/raisedapp/Hangfire.Storage.SQLite/tree/master) | [![Build Status](https://circleci.com/gh/raisedapp/Hangfire.Storage.SQLite/tree/develop.svg?style=svg)](https://circleci.com/gh/raisedapp/Hangfire.Storage.SQLite/tree/develop)
**Linux / Mac OS** | [![Build Status](https://travis-ci.org/raisedapp/Hangfire.Storage.SQLite.svg?branch=master)](https://travis-ci.org/raisedapp/Hangfire.Storage.SQLite/) | [![Build Status](https://travis-ci.org/raisedapp/Hangfire.Storage.SQLite.svg?branch=develop)](https://travis-ci.org/raisedapp/Hangfire.Storage.SQLite/)

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

## Thanks

This project is mainly based on **Hangfire.LiteDB** storage by [@codeyu](https://github.com/codeyu) (https://github.com/codeyu/Hangfire.LiteDB)

## Donation
If this project help you reduce time to develop, you can give me a cup of coffee :) 

[![paypal](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=RMLQM296TCM38&item_name=For+the+development+of+Hangfire.Storage.SQLite&currency_code=USD&source=url)

## License
This project is under MIT license. You can obtain the license copy [here](https://github.com/raisedapp/Hangfire.Storage.SQLite/blob/develop/LICENSE).
