# ------------------------------------------------------
# Lógica Dinâmica de SO/Arquitetura
# ------------------------------------------------------
locals {
  # Mapeia a arquitetura da AWS para o Runtime Identifier (RID) do .NET
  dotnet_rid = var.lambda_architecture == "arm64" ? "linux-arm64" : "linux-x64"

  # Caminho centralizado para o ZIP da Lambda
  lambda_zip_path = "${path.module}/../src/ImageProcessor.Lambda/bin/Release/net10.0/${local.dotnet_rid}/publish/ImageProcessor.Lambda.zip"

  # Hash determinístico baseado nos arquivos fonte (não no ZIP, que muda entre plan e apply)
  source_hash = base64sha256(join("", [
    for f in fileset("${path.module}/../src/ImageProcessor.Lambda", "**/*.{cs,csproj}") :
    filesha1("${path.module}/../src/ImageProcessor.Lambda/${f}")
  ]))
}

# ------------------------------------------------------
# 1. IAM Role - Política de Confiança (Trust Policy)
# ------------------------------------------------------
# Diz à AWS (ou LocalStack) que o serviço do Lambda tem permissão
# para assumir (AssumeRole) esta identidade.
resource "aws_iam_role" "lambda_execution_role" {
  name = "image-processor-lambda-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action    = "sts:AssumeRole"
        Effect    = "Allow"
        Principal = {
          Service = "lambda.amazonaws.com"
        }
      }
    ]
  })
}

# ------------------------------------------------------
# 2. IAM Policy - Menor Privilégio (Least Privilege)
# ------------------------------------------------------
# Define exatamente o que a nossa Lambda pode fazer:
# - Gravar logs no CloudWatch (essencial para monitoramento)
# - Colocar objetos no nosso Bucket S3 específico
# - Inserir itens na nossa tabela DynamoDB específica
resource "aws_iam_role_policy" "lambda_permissions" {
  name = "image-processor-lambda-permissions"
  role = aws_iam_role.lambda_execution_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "logs:CreateLogGroup",
          "logs:CreateLogStream",
          "logs:PutLogEvents"
        ]
        Resource = "arn:aws:logs:*:*:*"
      },
      {
        Effect = "Allow"
        Action = [
          "s3:PutObject"
        ]
        Resource = "${aws_s3_bucket.image_bucket.arn}/*"
      },
      {
        Effect = "Allow"
        Action = [
          "dynamodb:PutItem"
        ]
        Resource = aws_dynamodb_table.image_metadata_table.arn
      }
    ]
  })
}

# ------------------------------------------------------
# 3. Build & Package - Compilação Automática da Lambda
# ------------------------------------------------------
# Compila o projeto .NET com Native AOT e cria o ZIP automaticamente.
# O dotnet-lambda-tools cuida de usar Docker quando necessário para
# cross-compilation (macOS -> linux-arm64).
resource "null_resource" "lambda_build" {
  triggers = {
    # Recompila sempre que qualquer arquivo .cs ou o .csproj mudar
    source_hash = sha1(join("", [
      for f in fileset("${path.module}/../src/ImageProcessor.Lambda", "**/*.cs") :
      filesha1("${path.module}/../src/ImageProcessor.Lambda/${f}")
    ]))
    csproj_hash = filesha1("${path.module}/../src/ImageProcessor.Lambda/ImageProcessor.Lambda.csproj")
  }

  provisioner "local-exec" {
    command     = "dotnet lambda package --configuration Release --framework net10.0 --function-architecture ${var.lambda_architecture} --output-package bin/Release/net10.0/${local.dotnet_rid}/publish/ImageProcessor.Lambda.zip"
    working_dir = "${path.module}/../src/ImageProcessor.Lambda"
  }
}

# ------------------------------------------------------
# 4. AWS Lambda Function
# ------------------------------------------------------
resource "aws_lambda_function" "image_processor_lambda" {
  function_name = "ImageProcessorLambda"
  role          = aws_iam_role.lambda_execution_role.arn
  handler       = "bootstrap"
  runtime       = "provided.al2023"
  architectures = [var.lambda_architecture]

  # Aponta diretamente para o zip gerado pelo null_resource de build
  filename         = local.lambda_zip_path
  source_code_hash = local.source_hash

  # Variáveis de ambiente que sua API C# vai ler
  environment {
    variables = {
      BUCKET_NAME = aws_s3_bucket.image_bucket.bucket
      TABLE_NAME  = aws_dynamodb_table.image_metadata_table.name
    }
  }

  depends_on = [null_resource.lambda_build]
}