output "api_url" {
  value       = "${aws_api_gateway_stage.api_stage.invoke_url}/${aws_api_gateway_resource.images_resource.path_part}"
  description = "Generated local URL to send images (POST)"
}