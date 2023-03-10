# Introduction

When you create a new Azure IoT Edge module project via the Microsoft templates, you end up with a very basic project that still requires a lot of plumbing to be 'enterprise ready'.

The `fgiotedgemodule` template tries to offer an alternative where the IoT Edge module project that is generated by this template offers:

- Gracefull shutdown functionality for the IoT Edge module
- Logging via the ILogger infrastructure
- An easy way to load settings from the module twin
- The module supports a hosted environment by default which allows you to easily work with BackgroundProcesses and Dependency Injection.  (Overrideable via the `no-backgroundservices` parameter)

# Using the template

The template can be installed (or updated) from your local machine by executing  the `dotnet new install` command from the directory that contains the `.template.config` folder:

```cmd
dotnet new install ./
```

Afterwards, create a new Fg.IoTEdge.Module project via the CLI:

```cmd
dotnet new fgiotedgemodule -n <mymodulename>
```

If you execute the above command in the directory where the `.sln` file of the Azure IoT Edge solution exists, then:
- The new project will be automatically added to the solution
- The `deployment.template.json` file of the IoT Edge solution will be updated so that the new module is incorporated in the deployment manifest.

The above command provides some parameters:

|parameter|description
|-|-|
|repository|The address of the image container repository.  If not specified, the `module.json` file specifies the repository as a variable that can be set / replaced during deployment.
|no-backgroundservices|Specifies that a module must be generated which does not make use of hosted background-services.  (Default: false)