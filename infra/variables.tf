variable "lambda_architecture" {
  type        = string
  description = "The architecture of the machine where the Lambda will run (x86_64 or arm64)"
  default     = "x86_64"
}

variable "localstack_endpoint" {
  type        = string
  description = "LocalStack endpoint URL (e.g., http://127.0.0.1:4566)"
  default     = "http://127.0.0.1:4566"
}