# ------------------------------------------------------
# Amazon S3 - Armazenamento Físico das Imagens
# ------------------------------------------------------
resource "aws_s3_bucket" "image_bucket" {
  bucket = "image-processor-bucket-local"
}

# ------------------------------------------------------
# Amazon DynamoDB - Armazenamento de Metadados
# ------------------------------------------------------
resource "aws_dynamodb_table" "image_metadata_table" {
  name         = "ImageMetadata"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "ImageId"

  # Apenas a Partition Key (Hash Key) precisa ser definida na estrutura inicial
  attribute {
    name = "ImageId"
    type = "S" # String
  }

  tags = {
    Environment = "Local"
    Project     = "ImageProcessor"
  }
}