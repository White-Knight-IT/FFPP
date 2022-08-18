# This is Proof of Concept (PoC) code not for production use! (Yet)

## FFPP Overview

This is a C# ASP.NET Core web API minimal port of the awesome [CIPP-API](https://github.com/KelvinTegelaar/CIPP-API) project which is currently developed in PowerShell, and deployed as an Azure Function app.

It also contains a UI to consume the API wrote in HTML/SCSS/JS (Bootstrap) and the UI aims to be light weight and easier to maintain than the existing React based CIPP UI.

The main goal of this project is to speed up CIPP by utilising a Web API minimal version of CIPP-API instead of Azure Functions (no more error 500), with a secondary goal of decoupling CIPP-API from the requirement to use Azure Functions/Azure Storage etc.

This port is designed/targeted for small Linux devices/servers, however being dotnet core you should be able to run it anywhere (in theory at least).

## Licensing

Licensed under the [AGPL-3.0 License](https://choosealicense.com/licenses/agpl-3.0/) you get no warranty and it's a case of use at your own risk. The license and contained copyright notice would need to be included with any copies of this repository/software as per A-GPL v3.0 licensing requirements. There is also a license (See SECURITY_LICENSE) describing that there is no liability on the owner/contributors of this repository for security vulnerabilities that may arise.

## Prerequisites for development environment (devenv)

- You have an [SSH key configured in your account on GitHub](https://docs.github.com/en/authentication/connecting-to-github-with-ssh/adding-a-new-ssh-key-to-your-github-account) and you can clone repositories using SSH.

- You have a SAM app (Azure AD Enterprise Application) setup for CIPP ([see here](https://cipp.app/docs/user/gettingstarted/permissions/)).

- You have all the required tokens and SAM app info just as if you were creating a CIPP instance eg: TenantId, ApplicationId, ApplicationSecret, RefreshToken and ExchangeRefreshToken.

- You have the [.NET 6 SDK installed](https://dotnet.microsoft.com/en-us/download/dotnet/6.0), and have the dotnet binary defined in your PATH environment variable so that in **cmd/terminal** you can type `dotnet --version` and it reports a version which is 6.0.302 or greater.

- You have an IDE for dotnet core development such as [Visual Studio](https://visualstudio.microsoft.com/) or [Visual Studio Code (Free)](https://visualstudio.microsoft.com/) + [C# Extension](https://code.visualstudio.com/docs/languages/dotnet), unless you are hardcore and enjoy coding from the command line (I have known people who do this for stuff that isn't Python 😂).

- You know how to run a .NET 6 project in "Debug" mode in your IDE (this will allow you to make use of the Swagger UI).

- You have a development environment setup to run the [CIPP](https://github.com/KelvinTegelaar/CIPP) react/swa front end. You can find the instructions to setup a devenv [here](https://cipp.app/docs/dev/settingup/).

- **[Optional]** If you wish to contribute code, it is a good idea to run a devenv of the official [CIPP-API](https://github.com/KelvinTegelaar/CIPP-API) and compare output between APIs to ensure both are matching. If you follow the complete instructions [here](https://cipp.app/docs/dev/settingup/) you will end up with a devenv for both CIPP and CIPP-API which is ideal. Also, it's a nice thing to do to port new functionality back to original CIPP-API if we can do it, to try and maintain feature parity across repositories.

## Setting up devenv

### Clone the repository

If you are on Windows make sure you have [git installed](https://git-scm.com/downloads) first.

On any platform, open **cmd/terminal** and navigate (`cd`) to where you want to download this project.

Run the command `git clone git@github.com:White-Knight-IT/FFPP.git` and it will download this project into a folder named FFPP.

### Secrets

This project utilises the official dotnet core method of creating secrets for our local devenv, that is using the [user-secrets](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-6.0&tabs=linux) tool. This puts our secrets in a location outside of the repository, so it is impossible for us to accidentally commit local secrets into the project.

Open **cmd/terminal** and navigate (`cd`) to the folder that contains the project file (FFPP.csproj). If you cloned the repository to your home directory this will be `cd ~/FFPP/FFPP` .

A project specific secrets container identified by a GUID will exist at the following locations after we add our first secret:

- Windows: `%APPDATA%\Microsoft\UserSecrets\`

- macOS/Linux: `~/.microsoft/usersecrets/`

To add secrets in the project secret container, when we are in the project folder with **cmd/terminal**, we run `dotnet user-secrets set "[secret_name]" "[value]"`. An example of saving the TenantId secret would be `dotnet user-secrets set "tenantId" "goatfloater.onmicrosoft.com"`. We can repeat this step for all the secrets listed below.

We must be sure to stash the following secrets in our secrets cache (**CASE SENSITIVE**):

- TenantId

- ApplicationId

- ApplicationSecret

- RefreshToken

- ExchangeRefreshToken

This has created a file named `secrets.json` in the project secrets container. Feel free to modify the JSON in the file manually if you wish should you need to update tokens, it's not encrypted or anything. Using the user-secrets tool just means you get perfect JSON and not something that might break your build.

### Setting up Entity Framework & Databases

This project is using [Microsoft's Entity Framework Core platform](https://docs.microsoft.com/en-au/ef/core/cli/dotnet#update-the-tools) to consume local SQLite databases for lightweight/portable data stores.

We must [install the dotnet ef tools](https://docs.microsoft.com/en-au/ef/core/cli/dotnet#install-the-tools) to manage the databases. Using **cmd/terminal** in the project folder, run the command `dotnet tool install --global dotnet-ef` and this will eventually tell us that we have successfully installed the tools.

### Setting ports

The ports that the project listens for HTTP/HTTPS requests can both be configured in the `/FFPP/FFPP/appsettings.json` file.

### Running the project

Now that we have stashed our secrets and created our databases, we are free to run the project in our IDE. Open `/FFPP/FFPP.sln` in Visual Studio for example, ensure `Debug` is selected top left and **NOT** `Release` (to get the Swagger UI you must run in DEBUG mode), then hit the play `▶️` button.

Given that this is a web API, it is not expected to have a user interface when it runs in production. For development however, we are utilising the tool [Swagger](https://swagger.io/) which provides both automated documentation of our API, and a user interface that lets us perform the RESTful API HTTP methods against our API routes from the browser.

### Swagger UI screenshot example

![Swagger UI Screenshot 1](/README-IMAGES/Swagger-UI-1.png)

![Swagger UI Screenshot 2](/README-IMAGES/Swagger-UI-2.png)

![Swagger UI Screenshot 3](/README-IMAGES/Swagger-UI-3.png)

**NOTE:** When running in debug mode in Visual Studio Code, the Swagger UI is not automatically shown in the browser when you launch the debugging session. When the browser opens up with an error page at https://localhost:port, add `/swagger/index.html` onto the URL in the address bar and hit enter, then you will see the Swagger UI. Swagger UI is never shown when running in release mode.
