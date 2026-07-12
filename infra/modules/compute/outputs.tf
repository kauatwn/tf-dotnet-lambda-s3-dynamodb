output "lambda_invoke_arn" {
  value = aws_lambda_function.image_processor_lambda.invoke_arn
}

output "lambda_function_name" {
  value = aws_lambda_function.image_processor_lambda.function_name
}