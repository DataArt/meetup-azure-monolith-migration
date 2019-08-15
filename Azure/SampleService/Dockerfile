#Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
#For more information, please see http://aka.ms/containercompat 

FROM microsoft/dotnet-framework:4.7.2-runtime-windowsservercore-ltsc2016
WORKDIR /app
COPY ./bin/SampleService ./
ENTRYPOINT ["SampleService.exe"]
