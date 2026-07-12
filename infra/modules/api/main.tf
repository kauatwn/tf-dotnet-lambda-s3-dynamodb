# API Gateway (REST API)
resource "aws_api_gateway_rest_api" "image_api" {
  name        = "ImageProcessorAPI"
  description = "Serverless REST API for image processing"

  endpoint_configuration {
    types = ["REGIONAL"]
  }
}

# API Resource / Route (e.g., /images)
resource "aws_api_gateway_resource" "images_resource" {
  rest_api_id = aws_api_gateway_rest_api.image_api.id
  parent_id   = aws_api_gateway_rest_api.image_api.root_resource_id
  path_part   = "images"
}

# HTTP Method (POST /images)
resource "aws_api_gateway_method" "post_image_method" {
  rest_api_id   = aws_api_gateway_rest_api.image_api.id
  resource_id   = aws_api_gateway_resource.images_resource.id
  http_method   = "POST"
  authorization = "NONE" # Public for local lab
}

# API Gateway to Lambda Integration
# AWS_PROXY (Lambda Proxy Integration) sends the raw HTTP payload to Lambda
resource "aws_api_gateway_integration" "api_lambda_integration" {
  rest_api_id             = aws_api_gateway_rest_api.image_api.id
  resource_id             = aws_api_gateway_resource.images_resource.id
  http_method             = aws_api_gateway_method.post_image_method.http_method
  integration_http_method = "POST"
  type                    = "AWS_PROXY"
  uri                     = var.lambda_invoke_arn
}

# Invocation Permission (Security/IAM)
# Grants API Gateway permission to invoke the Lambda function
resource "aws_lambda_permission" "api_gateway_lambda_permission" {
  statement_id  = "AllowAPIGatewayInvoke"
  action        = "lambda:InvokeFunction"
  function_name = var.lambda_function_name
  principal     = "apigateway.amazonaws.com"

  # Restricts permission only to this specific API
  source_arn = "${aws_api_gateway_rest_api.image_api.execution_arn}/*/*"
}

# API Deployment and Stage
resource "aws_api_gateway_deployment" "api_deployment" {
  rest_api_id = aws_api_gateway_rest_api.image_api.id

  # Triggers a new deployment on configuration changes
  triggers = {
    redeployment = sha1(jsonencode([
      aws_api_gateway_resource.images_resource.id,
      aws_api_gateway_method.post_image_method.id,
      aws_api_gateway_integration.api_lambda_integration.id,
    ]))
  }

  # Ensures permissions are applied before deployment (Fixes race condition)
  depends_on = [
    aws_api_gateway_integration.api_lambda_integration,
    aws_lambda_permission.api_gateway_lambda_permission
  ]

  lifecycle {
    create_before_destroy = true
  }
}

resource "aws_api_gateway_stage" "api_stage" {
  deployment_id = aws_api_gateway_deployment.api_deployment.id
  rest_api_id   = aws_api_gateway_rest_api.image_api.id
  stage_name    = "local"
}