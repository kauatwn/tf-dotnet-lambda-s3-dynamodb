output "lambda_invoke_arn" {
  value = aws_lambda_function.image_processor_lambda_upload_image.invoke_arn
}

output "lambda_function_name" {
  value = aws_lambda_function.image_processor_lambda_upload_image.function_name
}