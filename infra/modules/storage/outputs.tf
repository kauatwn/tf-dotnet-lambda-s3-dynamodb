output "bucket_name" {
  value = aws_s3_bucket.image_bucket.bucket
}

output "bucket_arn" {
  value = aws_s3_bucket.image_bucket.arn
}

output "dynamodb_table_name" {
  value = aws_dynamodb_table.image_metadata_table.name
}

output "dynamodb_table_arn" {
  value = aws_dynamodb_table.image_metadata_table.arn
}