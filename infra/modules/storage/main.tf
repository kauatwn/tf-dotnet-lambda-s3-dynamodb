# Amazon S3 - Physical Image Storage
resource "aws_s3_bucket" "image_bucket" {
  bucket = "image-processor-bucket-local"
}

# S3 Public Access Block (Security Compliance)
resource "aws_s3_bucket_public_access_block" "image_bucket_access" {
  bucket = aws_s3_bucket.image_bucket.id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

# Amazon DynamoDB - Metadata Storage
resource "aws_dynamodb_table" "image_metadata_table" {
  name         = "ImageMetadata"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "ImageId"

  # Only the Partition Key (Hash Key) needs to be defined
  attribute {
    name = "ImageId"
    type = "S"
  }

  # Point-In-Time Recovery (Data Protection)
  point_in_time_recovery {
    enabled = true
  }
}