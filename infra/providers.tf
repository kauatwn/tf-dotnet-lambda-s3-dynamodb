terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~>6.54.0"
    }
    null = {
      source  = "hashicorp/null"
      version = "~>3.2.0"
    }
  }
}

provider "aws" {
  access_key = "mock_access_key"
  secret_key = "mock_secret_key"
  region     = "us-east-1"

  # only required for non virtual hosted-style endpoint use case.
  # https://registry.terraform.io/providers/hashicorp/aws/latest/docs#s3_use_path_style
  s3_use_path_style = true
  skip_credentials_validation = true
  skip_metadata_api_check     = true
  skip_requesting_account_id  = true

  endpoints {
    s3         = var.localstack_endpoint
    dynamodb   = var.localstack_endpoint
    apigateway = var.localstack_endpoint
    lambda     = var.localstack_endpoint
    iam        = var.localstack_endpoint
    sts        = var.localstack_endpoint
  }
}
