output "api_url" {
  value       = module.api.api_url
  description = "Generated local URL to send images (POST)"
}

output "api_id" {
  value       = module.api.api_id
  description = "The ID of the REST API Gateway"
}