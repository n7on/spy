@{

    # Script module or binary module file associated with this manifest.
    RootModule = 'DllSpy.PowerShell.dll'

    # Version number of this module.
    ModuleVersion = '0.2.7'

    # ID used to uniquely identify this module
    GUID = 'f7e8a9b0-1c2d-3e4f-5a6b-7c8d9e0f1a2b'

    # Author of this module
    Author = 'Anton Lindström'

    # Company or vendor of this module
    CompanyName = 'Anton Lindström'

    # Copyright statement for this module
    Copyright = '(c) Anton Lindström. All rights reserved.'

    # Description of the functionality provided by this module
    Description = 'Discovers and analyzes input surfaces (HTTP endpoints, SignalR hubs, WCF services, gRPC services, Razor Pages, Blazor components, Azure Functions, OData endpoints) in compiled .NET assemblies using reflection. Identifies security vulnerabilities, maps routes, and generates reports — all without running the application.'

    # Minimum version of the PowerShell engine required by this module
    PowerShellVersion = '5.1'

    # Minimum version of the .NET Framework required by this module
    DotNetFrameworkVersion = '4.6.1'

    # Minimum version of the common language runtime (CLR) required by this module
    CLRVersion = '4.0'

    # Assemblies that must be loaded prior to importing this module
    RequiredAssemblies = @('DllSpy.Core.dll')

    # Format files (.ps1xml) to be loaded when importing this module
    FormatsToProcess = @('DllSpy.Format.ps1xml')

    # Functions to export from this module
    FunctionsToExport = @()

    # Cmdlets to export from this module
    CmdletsToExport = @(
        'Search-DllSpy',
        'Test-DllSpy'
    )

    # Variables to export from this module
    VariablesToExport = @()

    # Aliases to export from this module
    AliasesToExport = @()

    # Private data to pass to the module specified in RootModule/ModuleToProcess
    PrivateData = @{
        PSData = @{
            # Tags applied to this module for discoverability in online galleries
            Tags = @('security', 'aspnet', 'webapi', 'signalr', 'wcf', 'grpc', 'razor', 'blazor', 'azure-functions', 'odata', 'reflection', 'endpoints', 'vulnerability', 'audit')

            # A URL to the license for this module.
            LicenseUri = 'https://github.com/n7on/dllspy/blob/main/LICENSE'

            # A URL to the main website for this project.
            ProjectUri = 'https://github.com/n7on/dllspy'

            # Release notes for this module
            ReleaseNotes = 'Added Azure Functions and OData endpoint discovery. PowerShell HttpMethod filter now matches all surface types with HTTP methods.'
        }
    }

}
