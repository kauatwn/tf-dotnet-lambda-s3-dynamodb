# CloudWatch Log Group for Lambda
resource "aws_cloudwatch_log_group" "lambda_log_group" {
  name              = "/aws/lambda/ImageProcessorLambdaUploadImage"
  retention_in_days = 7
}

# IAM Role - Trust Policy for Lambda
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

# IAM Policy - Least Privilege Permissions
resource "aws_iam_role_policy" "lambda_permissions" {
  name = "image-processor-lambda-permissions"
  role = aws_iam_role.lambda_execution_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "logs:CreateLogStream",
          "logs:PutLogEvents"
        ]
        Resource = "${aws_cloudwatch_log_group.lambda_log_group.arn}:*"
      },
      {
        Effect = "Allow"
        Action = [
          "s3:PutObject"
        ]
        Resource = "${var.bucket_arn}/*"
      },
      {
        Effect = "Allow"
        Action = [
          "dynamodb:PutItem"
        ]
        Resource = var.dynamodb_table_arn
      }
    ]
  })
}

# Amazon ECR - Private Container Registry
resource "aws_ecr_repository" "lambda_repo" {
  name                 = "image-processor-lambda"
  image_tag_mutability = "MUTABLE"

  # Disabled to speed up local deployments via LocalStack
  image_scanning_configuration {
    scan_on_push = false
  }
}

# Amazon ECR - Lifecycle Policy (Keep only the 5 most recent untagged images)
resource "aws_ecr_lifecycle_policy" "lambda_repo_policy" {
  repository = aws_ecr_repository.lambda_repo.name

  policy = jsonencode({
    rules = [{
      rulePriority = 1
      description  = "Expire untagged images older than 5 counts"
      selection = {
        tagStatus   = "untagged"
        countType   = "imageCountMoreThan"
        countNumber = 5
      }
      action = {
        type = "expire"
      }
    }]
  })
}

# AWS Lambda Function (Container Image)
resource "aws_lambda_function" "image_processor_lambda_upload_image" {
  function_name = "ImageProcessorLambdaUploadImage"
  role          = aws_iam_role.lambda_execution_role.arn
  architectures = [var.lambda_architecture]

  # Container specific configurations
  package_type = "Image"
  image_uri    = "${aws_ecr_repository.lambda_repo.repository_url}:latest"

  # Performance configurations explicitly defined
  memory_size = 512
  timeout     = 15

  environment {
    variables = {
      BUCKET_NAME = var.bucket_name
      TABLE_NAME  = var.dynamodb_table_name
    }
  }

  depends_on = [
    aws_cloudwatch_log_group.lambda_log_group
  ]
}