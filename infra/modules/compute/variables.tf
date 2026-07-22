variable "lambda_architecture" {
  type = string
}

variable "bucket_name" {
  type = string
}

variable "bucket_arn" {
  type = string
}

variable "dynamodb_table_name" {
  type = string
}

variable "dynamodb_table_arn" {
  type = string
}

variable "image_tag" {
  type        = string
  description = "The Docker image tag to deploy (e.g., latest or Git SHA)"
}