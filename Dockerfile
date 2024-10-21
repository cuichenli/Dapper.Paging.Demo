FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /app
COPY --from=build-env /app/out .
ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8}
ENV CORECLR_PROFILER_PATH=/app/datadog/linux-x64/Datadog.Trace.ClrProfiler.Native.so
ENV DD_DOTNET_TRACER_HOME=/app/datadog

# Run the createLogPath script on Linux to ensure the automatic instrumentation logs are generated without permission issues
RUN /app/datadog/createLogPath.sh

RUN apt update && apt install unzip -y && curl -sSfL https://github.com/signalfx/splunk-otel-dotnet/releases/latest/download/splunk-otel-dotnet-install.sh -O && sh ./splunk-otel-dotnet-install.sh

COPY ./entrypoint.sh /app/entrypoint.sh
RUN chmod +x /app/entrypoint.sh

ENTRYPOINT ["/app/entrypoint.sh"]