variable "lambda_architecture" {
  type        = string
  description = "A arquitetura da máquina onde a Lambda vai rodar (x86_64 ou arm64)"
  default     = "x86_64"
}
