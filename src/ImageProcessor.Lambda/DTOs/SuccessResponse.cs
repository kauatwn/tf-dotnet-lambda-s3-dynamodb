namespace ImageProcessor.Lambda.DTOs;

public sealed record SuccessResponse<T>(string Message, T Data);