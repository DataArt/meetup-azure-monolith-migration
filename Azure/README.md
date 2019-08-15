# dotnet-atlas

Azure solution
  This solution illustrates hosting simple MS Azure Cloud Services Worker Role using Atlas's infrastructure for MS Azure.
  You should just inherit your Worker Role from DataArt.Atlas.Hosting.Azure.EntryPoint class to glue all things together.

  Configuration settings handling
  You declare configuration settings in AzureService/ServiceDefinition.csdef file and set them for particular environment in
  ServiceConfiguration.<Environment>.cscfg file
  Each setting should be named <SectionName>.<SettingName>, for example Logging.SinkUrl will set value of setting "SinkUrl" in configuration section "Logging".

  Additionally there is a predefined setting "Hosting.Url" which designates address for input endpoint of the service.

- after.<SolutionName>.sln.targets
  This file will be used when doing Continuous Integration and run MSBuild from command line (not Visual Studio)
  to make sure Package action works correctly
