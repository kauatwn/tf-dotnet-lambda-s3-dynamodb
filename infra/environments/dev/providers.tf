terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~>6.54.0"
    }
    # The 'null' provider was removed as we are now using the native 'terraform_data'
  }
}

provider "aws" {
  access_key = "mock_access_key"
  secret_key = "mock_secret_key"
  region     = "us-east-1"

  # Global tags applied automatically to all supported resources
  default_tags {
    tags = {
      Environment = "Local"
      Project     = "ImageProcessor"
      ManagedBy   = "Terraform"
    }
  }

  # Only required for non-virtual hosted-style endpoint use case
  s3_use_path_style           = true
  skip_credentials_validation = true
  skip_metadata_api_check     = true
  skip_requesting_account_id  = true

  endpoints {
    s3             = var.localstack_endpoint
    dynamodb       = var.localstack_endpoint
    apigateway     = var.localstack_endpoint
    cloudwatchlogs = var.localstack_endpoint
    lambda         = var.localstack_endpoint
    iam            = var.localstack_endpoint
    sts            = var.localstack_endpoint
    ecr            = var.localstack_endpoint
  }
}