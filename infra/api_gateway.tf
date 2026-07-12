# ------------------------------------------------------
# 1. Criação do API Gateway (REST API)
# ------------------------------------------------------
resource "aws_api_gateway_rest_api" "image_api" {
  name        = "ImageProcessorAPI"
  description = "API REST Serverless para processamento de imagens"

  endpoint_configuration {
    types = ["REGIONAL"]
  }
}

# ------------------------------------------------------
# 2. Rota/Recurso da API (Ex: /images)
# ------------------------------------------------------
resource "aws_api_gateway_resource" "images_resource" {
  rest_api_id = aws_api_gateway_rest_api.image_api.id
  parent_id   = aws_api_gateway_rest_api.image_api.root_resource_id
  path_part   = "images" # Caminho da URL
}

# ------------------------------------------------------
# 3. Método HTTP (POST /images)
# ------------------------------------------------------
resource "aws_api_gateway_method" "post_image_method" {
  rest_api_id   = aws_api_gateway_rest_api.image_api.id
  resource_id   = aws_api_gateway_resource.images_resource.id
  http_method   = "POST"
  authorization = "NONE" # Pública para o laboratório local
}

# ------------------------------------------------------
# 4. Integração do API Gateway com a Lambda
# ------------------------------------------------------
# O tipo AWS_PROXY (Lambda Proxy Integration) faz com que o API Gateway
# envie o payload HTTP bruto para a Lambda e adote a resposta da Lambda como resposta HTTP.
resource "aws_api_gateway_integration" "api_lambda_integration" {
  rest_api_id             = aws_api_gateway_rest_api.image_api.id
  resource_id             = aws_api_gateway_resource.images_resource.id
  http_method             = aws_api_gateway_method.post_image_method.http_method
  integration_http_method = "POST" # O API Gateway sempre chama a Lambda via POST internamente
  type                    = "AWS_PROXY"
  uri                     = aws_lambda_function.image_processor_lambda.invoke_arn
}

# ------------------------------------------------------
# 5. Permissão de Invocação (Segurança/IAM)
# ------------------------------------------------------
# Essencial para exames: Recursos não se comunicam sem permissão explícita.
# Aqui autorizamos o API Gateway a acionar a Lambda.
resource "aws_lambda_permission" "api_gateway_lambda_permission" {
  statement_id  = "AllowAPIGatewayInvoke"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.image_processor_lambda.function_name
  principal     = "apigateway.amazonaws.com"

  # O Source ARN restringe a permissão apenas para esta API específica
  source_arn = "${aws_api_gateway_rest_api.image_api.execution_arn}/*/*"
}

# ------------------------------------------------------
# 6. Deployment e Stage da API
# ------------------------------------------------------
resource "aws_api_gateway_deployment" "api_deployment" {
  rest_api_id = aws_api_gateway_rest_api.image_api.id

  # Garante que qualquer mudança de método ou integração force um novo deploy da API
  triggers = {
    redeployment = sha1(jsonencode([
      aws_api_gateway_resource.images_resource.id,
      aws_api_gateway_method.post_image_method.id,
      aws_api_gateway_integration.api_lambda_integration.id,
    ]))
  }

  lifecycle {
    create_before_destroy = true
  }
}

resource "aws_api_gateway_stage" "api_stage" {
  deployment_id = aws_api_gateway_deployment.api_deployment.id
  rest_api_id   = aws_api_gateway_rest_api.image_api.id
  stage_name    = "local"
}

# ------------------------------------------------------
# Output - URL de Entrada da API
# ------------------------------------------------------
output "api_url" {
  value       = "${aws_api_gateway_stage.api_stage.invoke_url}/${aws_api_gateway_resource.images_resource.path_part}"
  description = "URL local gerada para enviar as imagens (POST)"
}
