# Dynamic Paths Logic
locals {
  # Base directory variables using best practices (path.root) for deterministic paths
  project_root = "${path.root}/../../.."
  src_dir      = "${local.project_root}/src/ImageProcessor.Lambda.UploadImage"
}

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
# Defines exact permissions for the Lambda function
resource "aws_iam_role_policy" "lambda_permissions" {
  name = "image-processor-lambda-permissions"
  role = aws_iam_role.lambda_execution_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        # Permissions to write to the explicit log group
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

# Build & Package - Native terraform_data
# Automates the Docker build and push to the local ECR registry
resource "terraform_data" "lambda_docker_push" {
  triggers_replace = {
    # Rebuilds whenever any .cs, .csproj or Dockerfile changes
    source_hash = sha1(join("", [
      for f in fileset(local.src_dir, "**/*.cs") :
      filesha1("${local.src_dir}/${f}")
    ]))
    csproj_hash     = filesha1("${local.src_dir}/ImageProcessor.Lambda.UploadImage.csproj")
    dockerfile_hash = filesha1("${local.src_dir}/Dockerfile")
  }

  provisioner "local-exec" {
    # Executes the Docker build and push commands locally.
    # Uses dynamic platform mapping (AWS x86_64 -> Docker amd64) to guarantee compatibility.
    command = <<EOT
      docker build --platform linux/${var.lambda_architecture == "arm64" ? "arm64" : "amd64"} -t ${aws_ecr_repository.lambda_repo.repository_url}:latest -f ${local.src_dir}/Dockerfile ${local.project_root}
      docker push ${aws_ecr_repository.lambda_repo.repository_url}:latest
    EOT
  }

  # Guarantees the ECR repository exists before attempting to build/push
  depends_on = [
    aws_ecr_repository.lambda_repo
  ]
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

  # Forces Terraform to wait for the Docker image to be pushed before creating the Lambda
  depends_on = [
    aws_cloudwatch_log_group.lambda_log_group,
    terraform_data.lambda_docker_push
  ]
}