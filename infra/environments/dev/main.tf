module "storage" {
  source = "../../modules/storage"
}

module "compute" {
  source = "../../modules/compute"

  lambda_architecture = var.lambda_architecture
  image_tag           = var.image_tag

  # Injecting outputs from the Storage module
  bucket_name         = module.storage.bucket_name
  bucket_arn          = module.storage.bucket_arn
  dynamodb_table_name = module.storage.dynamodb_table_name
  dynamodb_table_arn  = module.storage.dynamodb_table_arn
}

module "api" {
  source = "../../modules/api"

  # Injecting outputs from the Compute module
  lambda_invoke_arn    = module.compute.lambda_invoke_arn
  lambda_function_name = module.compute.lambda_function_name
}