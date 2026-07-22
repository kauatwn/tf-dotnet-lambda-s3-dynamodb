FROM public.ecr.aws/lambda/provided:al2023 AS base
WORKDIR /var/task

# Uses the official .NET 10 AOT image (Ubuntu 24.04 Noble).
# This image comes pre-installed with clang, zlib, and all Native AOT build tools.
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble-aot AS build
ARG TARGETARCH 
WORKDIR /src

# No apt-get required! The -aot tag natively supports compilation out of the box.

COPY ["src/ImageProcessor.Lambda.UploadImage/ImageProcessor.Lambda.UploadImage.csproj", "src/ImageProcessor.Lambda.UploadImage/"]
COPY ["src/ImageProcessor.Infrastructure/ImageProcessor.Infrastructure.csproj", "src/ImageProcessor.Infrastructure/"]
COPY ["src/ImageProcessor.Core/ImageProcessor.Core.csproj", "src/ImageProcessor.Core/"]
RUN dotnet restore "src/ImageProcessor.Lambda.UploadImage/ImageProcessor.Lambda.UploadImage.csproj"

COPY . .
WORKDIR "/src/src/ImageProcessor.Lambda.UploadImage"
ARG BUILD_CONFIGURATION=Release

# Resolves dynamic routing for the Native AOT compiler based on the GitHub Runner or Local Mac Architecture
RUN if [ "$TARGETARCH" = "amd64" ]; then \
        RID="linux-x64"; \
    else \
        RID="linux-arm64"; \
    fi; \
    dotnet publish "ImageProcessor.Lambda.UploadImage.csproj" \
    -c "$BUILD_CONFIGURATION" \
    -r $RID \
    --self-contained true \
    -o /app/publish

FROM base AS final
WORKDIR /var/task
COPY --from=build /app/publish .
CMD ["bootstrap"]