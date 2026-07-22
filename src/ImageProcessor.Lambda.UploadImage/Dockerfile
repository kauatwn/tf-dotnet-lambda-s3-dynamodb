FROM public.ecr.aws/lambda/provided:al2023 AS base
WORKDIR /var/task

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH 
WORKDIR /src
RUN apt-get update && apt-get install -y clang zlib1g-dev
COPY ["src/ImageProcessor.Lambda.UploadImage/ImageProcessor.Lambda.UploadImage.csproj", "src/ImageProcessor.Lambda.UploadImage/"]
COPY ["src/ImageProcessor.Infrastructure/ImageProcessor.Infrastructure.csproj", "src/ImageProcessor.Infrastructure/"]
COPY ["src/ImageProcessor.Core/ImageProcessor.Core.csproj", "src/ImageProcessor.Core/"]
RUN dotnet restore "src/ImageProcessor.Lambda.UploadImage/ImageProcessor.Lambda.UploadImage.csproj"
COPY . .
WORKDIR "/src/src/ImageProcessor.Lambda.UploadImage"
ARG BUILD_CONFIGURATION=Release
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