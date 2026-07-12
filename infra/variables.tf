variable "lambda_architecture" {
  type        = string
  description = "A arquitetura da máquina onde a Lambda vai rodar (x86_64 ou arm64)"
  default     = "x86_64"
}

variable "localstack_endpoint" {
  type        = string
  description = "URL do endpoint do LocalStack (ex: http://127.0.0.1:4566)"
  default     = "http://127.0.0.1:4566"
}
