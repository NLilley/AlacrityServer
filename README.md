```
_____________                   __________         
___    |__  /_____ ________________(_)_  /_____  __
__  /| |_  /_  __ `/  ___/_  ___/_  /_  __/_  / / /
_  ___ |  / / /_/ // /__ _  /   _  / / /_ _  /_/ / 
/_/  |_/_/  \__,_/ \___/ /_/    /_/  \__/ _\__, /  
                                          /____/   

                    Alacrity Simulated Trading - Trade with Alacrity
```

Thank you for your interest in my Alacrity Simulated Trading application!

Alacrity is a minimalistic simulated trading application. You are reading through the README for the backend AlacrityServer component of this project. It is intended for this project to be used along-side it's companion React/TypeScript front-end.  Please see `alacrity-client` for more information, including build instructions for the web client.

NOTE: Alacrity is ONLY to be used for demonstrative purposes. Several shortcuts were taken in it's design and development which are most unsuitable for a production environment!

# Project Overview

AlacrityServer is a self contained AspNetCore C# application providing backend functionality for the alacrity web-client. 

#### This server supports:
- A Managed Order Book
- (Very simple) Market Participants which interact with the market using their own trading strategies.
- Basic account functionality (Login, Logout, Password Management, etc.)
- Real time streaming price data (for simulated instruments)
- Historic candle storage and recovery
- Market and Limit Orders
- Trading Notification System
- Profit Loss calculation, Portfolio Analysis
- ... and other essential backend behaviours

# Build

Alacrity is a simple net7 C# project. So long as you have the net7 SDK available on your machine and an internet connection, building the solution is as simple as either:
- Opening up the solution in Visual Studio and hitting run
- Navigating to the AlacrityServer root, and running\
 `dotnet run --project .\AlacrityServer\AlacrityServer.csproj`

If you have any problems building alacrity, please raise an issue, or reach out to me directly!

You will also want to build the web-client when using AlacrityServer. For detailed build instructions, please see the `alacrity-client` README.

Succinctly:
- Run the vite server locally with `yarn dev`, and use http://localhost:5173

# Technology

This project has relatively few dependencies, and is largely a hand-crafted C# application.  Data is stored in an SQL database, and CRUD operations are performed through the Dapper Micro ORM.  Structured logging support is provided by SeriLog.  Real Time Data Streaming is provided by SignalR.

Otherwise the project is standalone, and can be understood simply by reading the codebase.

# Testing

The project is tested by a small set of Unit and Integration tests. These tests would be insufficient for a production-ready system, but are more than adequate for the purposes of a demonstration, and serve as proof of fundamental functionality, as well as providing context regarding the intended behaviour of the underlying system.

The Unit Tests are found in the AlacrityTests.csproj project.
The Integration Tests can be found in the AlacrityIntegrationTests.csproj project.

# Structure

The underlying code is structured into a simple and intuitive model hierarchy. The solution itself contains 4 c# projects. These are:
- AlacrityCore
- AlacrityServer
- AlacrityTests
- AlacrityIntegrationTests

The AlacrityServer is the entry point for the application, and is responsible for bootstrapping the entire application, as well web app functionality through the use of Kestrel. Where possible, underlying logic is owned by the AlacrityCore project and is decoupled from the WebApp logic. Due to this separation of concerns, it would be relatively easy to split this functionality off into it's own micro-service oriented design if needed in support of scaling and improving reliability.

The Server project itself has something like the following file structure:
- Controllers: AspNet controllers serving most web requests
- Hubs: SignalR and related functionality
- Infrastructure: The boilerplate required to initialize and start up the full service

The AlacrityCore project is similarly simple:
- DataBase: SQL, SQLite data store and similar DB Logic
- Enums: All project enums
- Infrastructure: Other essential behaviours
- Models: Basic POCO models representing business logic entities
- Queries: Dapper logic for interacting with the database.
- Services: Main access point into underlying trading behaviours
- Utils: Other helper methods

# Conclusion

Thanks again for taking the time to review this project!
If you have any questions, don't hesitate to reach out, and happy trading with Alacrity!

\- N